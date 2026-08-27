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
