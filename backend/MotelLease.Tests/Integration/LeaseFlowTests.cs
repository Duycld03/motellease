using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the step where a paid deposit becomes a contract. What is worth asserting is that the rent
/// on the lease is the figure frozen at deposit time rather than today's asking price (§3), that a
/// room cannot carry two live contracts (§9.1), and that the room's status follows the lease (§9.3).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class LeaseFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public LeaseFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task A_paid_deposit_becomes_a_lease_and_the_room_is_occupied()
    {
        var held = await _app.PaidDepositAsync(_client);

        var response = await ConfirmAsync(held);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lease = await response.ReadAsync<LeaseResponse>();

        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Equal(held.Deposit.Id, lease.DepositId);
        Assert.Equal(held.TenantUserId, lease.PrimaryTenantUserId);
        Assert.Equal(held.Deposit.RequestedTermMonths, lease.TermMonths);
        Assert.Equal(held.Deposit.RequestedStartDate, lease.StartDate);
        Assert.Equal(
            held.Deposit.RequestedStartDate.AddMonths(held.Deposit.RequestedTermMonths),
            lease.EndDate);

        // The frozen figure travels onto the contract rather than being read again (§3).
        Assert.Equal(ListingSetup.MonthlyRent, lease.MonthlyRent);
        Assert.Equal(ListingSetup.MonthlyRent, lease.DepositHeld);

        // The tenant is on the contract, as the primary, which is what occupancy counts (§9.2).
        var tenant = Assert.Single(lease.Tenants);

        Assert.True(tenant.IsPrimary);
        Assert.Equal(held.TenantUserId, tenant.UserId);
        Assert.Null(tenant.MovedOutAt);

        // The deposit has been consumed, and the room is lived in rather than held (§9.3).
        Assert.Equal(DepositStatus.Completed, await DepositStatusAsync(held.Deposit.Id));
        Assert.Equal(RoomStatus.Occupied, await RoomStatusAsync(held.Listing.RoomId));
    }

    [Fact]
    public async Task An_unpaid_deposit_becomes_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);

        var response = await ConfirmAsync(held);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.deposit.not_paid", await response.ReadCodeAsync());
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(held.Listing.RoomId));
    }

    [Fact]
    public async Task A_room_carries_one_live_contract_only()
    {
        var held = await _app.PaidDepositAsync(_client);

        (await ConfirmAsync(held)).EnsureSuccessStatusCode();

        var again = await ConfirmAsync(held);

        // The second attempt is refused on the deposit before it ever reaches the lease index: the
        // deposit was consumed by the first one.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal("error.deposit.not_paid", await again.ReadCodeAsync());

        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        Assert.Equal(
            1,
            await database.Leases.CountAsync(
                l => l.RoomId == held.Listing.RoomId && l.Status == LeaseStatus.Active));
    }

    [Fact]
    public async Task Only_the_property_side_signs_the_contract()
    {
        var held = await _app.PaidDepositAsync(_client);
        var outsider = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        var byTenant = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.TenantToken);

        var byOutsider = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            outsider);

        Assert.Equal(HttpStatusCode.Forbidden, byTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byOutsider.StatusCode);
        Assert.Equal(DepositStatus.Paid, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task Assigned_staff_sign_the_contract_too()
    {
        var held = await _app.PaidDepositAsync(_client);
        var staff = await _app.AssignStaffAsync(_client, held.Listing.HouseId, held.OwnerUserId);

        var response = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            staff.AccessToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(RoomStatus.Occupied, await RoomStatusAsync(held.Listing.RoomId));
    }

    [Fact]
    public async Task An_occupied_room_cannot_be_deleted_or_taken_out_of_service()
    {
        var held = await _app.PaidDepositAsync(_client);

        (await ConfirmAsync(held)).EnsureSuccessStatusCode();

        var deleted = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{held.Listing.HouseId}/rooms/{held.Listing.RoomId}",
            held.Listing.OwnerToken);

        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        Assert.Equal("error.room.in_use", await deleted.ReadCodeAsync());
    }

    [Fact]
    public async Task Tenant_can_fetch_current_lease_and_owner_can_list_leases()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await ConfirmAsync(held);
        var createdLease = await confirmResp.ReadAsync<LeaseResponse>();

        // Tenant gets /me/current-lease
        var currentResp = await _client.SendAsync(HttpMethod.Get, "/api/v1/me/current-lease", held.TenantToken);
        Assert.Equal(HttpStatusCode.OK, currentResp.StatusCode);
        var currentLease = await currentResp.ReadAsync<LeaseResponse>();
        Assert.Equal(createdLease.Id, currentLease.Id);

        // Owner lists leases
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/leases?boardingHouseId={held.Listing.HouseId}",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var paged = await listResp.ReadAsync<MotelLease.Application.Common.Contracts.PagedResponse<LeaseResponse>>();
        Assert.Contains(paged.Items, l => l.Id == createdLease.Id);

        // Room lease history
        var historyResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/rooms/{held.Listing.RoomId}/lease-history",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, historyResp.StatusCode);
        var history = await historyResp.ReadAsync<IReadOnlyList<LeaseResponse>>();
        Assert.Contains(history, l => l.Id == createdLease.Id);
    }

    [Fact]
    public async Task Co_tenants_can_be_added_and_removed_with_occupancy_enforced()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await ConfirmAsync(held);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        // By default house is Traditional / Single so MaxOccupants = 1.
        // Adding a 2nd tenant should fail with RoomFullyOccupied
        var addResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/leases/{lease.Id}/tenants",
            held.Listing.OwnerToken,
            new AddLeaseTenantRequest("Co-tenant One", "0912345678", "123456789012"));

        Assert.Equal(HttpStatusCode.Conflict, addResp.StatusCode);
        Assert.Equal("error.lease.room_fully_occupied", await addResp.ReadCodeAsync());

        // Update house to DormStyle with RoomType MaxOccupants = 3
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var house = await db.BoardingHouses.FirstAsync(b => b.Id == held.Listing.HouseId);
            house.Type = BoardingHouseType.DormStyle;
            var roomType = await db.RoomTypes.FirstAsync(rt => rt.Id == held.Listing.RoomTypeId);
            roomType.MaxOccupants = 3;
            await db.SaveChangesAsync();
        }

        // Now add co-tenant should succeed
        var addResp2 = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/leases/{lease.Id}/tenants",
            held.Listing.OwnerToken,
            new AddLeaseTenantRequest("Co-tenant One", "0912345678", "123456789012"));

        Assert.Equal(HttpStatusCode.OK, addResp2.StatusCode);
        var updatedLease = await addResp2.ReadAsync<LeaseResponse>();
        Assert.Equal(2, updatedLease.Tenants.Count);
        var coTenant = updatedLease.Tenants.First(t => !t.IsPrimary);
        Assert.Equal("Co-tenant One", coTenant.FullName);
        Assert.Null(coTenant.MovedOutAt);

        // Remove primary tenant fails
        var primaryTenant = updatedLease.Tenants.First(t => t.IsPrimary);
        var removePrimaryResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/leases/{lease.Id}/tenants/{primaryTenant.Id}",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, removePrimaryResp.StatusCode);
        Assert.Equal("error.lease.cannot_remove_primary_tenant", await removePrimaryResp.ReadCodeAsync());

        // Remove co-tenant succeeds and sets MovedOutAt
        var removeResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/leases/{lease.Id}/tenants/{coTenant.Id}",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, removeResp.StatusCode);
        var leaseAfterRemove = await removeResp.ReadAsync<LeaseResponse>();
        var removedCoTenant = leaseAfterRemove.Tenants.First(t => t.Id == coTenant.Id);
        Assert.NotNull(removedCoTenant.MovedOutAt);
    }

    [Fact]
    public async Task Lease_termination_preview_and_execution_updates_meters_and_refunds()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await ConfirmAsync(held);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        // Preview termination
        var previewResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/leases/{lease.Id}/termination-preview?finalElectricityReading=50&finalWaterReading=10&depositDeducted=200000",
            held.Listing.OwnerToken);

        Assert.Equal(HttpStatusCode.OK, previewResp.StatusCode);
        var preview = await previewResp.ReadAsync<LeaseTerminationPreviewResponse>();
        Assert.Equal(50, preview.FinalElectricityReading);
        Assert.Equal(10, preview.FinalWaterReading);
        Assert.Equal(200000, preview.DepositDeducted);
        Assert.True(preview.DepositRefunded > 0);

        // Terminate
        var terminateResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/leases/{lease.Id}/terminate",
            held.Listing.OwnerToken,
            new TerminateLeaseRequest(50, 10, 200000, "Early termination by agreement"));

        Assert.Equal(HttpStatusCode.OK, terminateResp.StatusCode);
        var terminatedLease = await terminateResp.ReadAsync<LeaseResponse>();
        Assert.Equal(LeaseStatus.Terminated, terminatedLease.Status);
        Assert.Equal(50, terminatedLease.FinalElectricityReading);
        Assert.Equal(10, terminatedLease.FinalWaterReading);
        Assert.Equal(200000, terminatedLease.DepositDeducted);
        Assert.Equal(preview.DepositRefunded, terminatedLease.DepositRefunded);

        // Room is back to Available (§9.3)
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(held.Listing.RoomId));

        // Room meter readings advanced
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var room = await db.Rooms.FirstAsync(r => r.Id == held.Listing.RoomId);
        Assert.Equal(50, room.CurrentElectricityReading);
        Assert.Equal(10, room.CurrentWaterReading);
    }

    [Fact]
    public async Task Sweep_lease_expiry_updates_status_and_frees_ended_room()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await ConfirmAsync(held);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var l = await db.Leases.FirstAsync(x => x.Id == lease.Id);
            // Put end date in 15 days
            l.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
            await db.SaveChangesAsync();

            var sweeper = scope.ServiceProvider.GetRequiredService<MotelLease.Application.Leases.SweepLeaseExpiryHandler>();
            var count = await sweeper.HandleAsync();
            Assert.True(count >= 1);

            var updated = await db.Leases.FirstAsync(x => x.Id == lease.Id);
            Assert.Equal(LeaseStatus.Expiring, updated.Status);

            // Put end date and start date in the past
            updated.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
            updated.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            await db.SaveChangesAsync();

            await sweeper.HandleAsync();
            var ended = await db.Leases.Include(x => x.Room).FirstAsync(x => x.Id == lease.Id);
            Assert.Equal(LeaseStatus.Ended, ended.Status);
            Assert.Equal(RoomStatus.Available, ended.Room.Status);
        }
    }

    private Task<HttpResponseMessage> ConfirmAsync(HeldRoom held) =>
        _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken);

    private async Task<DepositStatus> DepositStatusAsync(Guid depositId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Deposits
            .Where(d => d.Id == depositId)
            .Select(d => d.Status)
            .FirstAsync();
    }

    private async Task<RoomStatus> RoomStatusAsync(Guid roomId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Rooms.Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync();
    }
}

