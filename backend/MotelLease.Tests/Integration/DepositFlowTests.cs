using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Deposits;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the deposit group of docs/api-design.md, minus the payment endpoints. What is worth
/// asserting here is the amount being frozen at request time, who may answer a request, and that the
/// room's status keeps following the deposit rows rather than drifting from them (§9.3).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DepositFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public DepositFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task A_tenant_asks_to_hold_a_room_and_the_owner_accepts()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var requested = await RequestAsync(tenant, listing.RoomId);

        Assert.Equal(DepositStatus.Pending, requested.Status);
        Assert.Equal(ListingSetup.MonthlyRent, requested.Amount);
        Assert.Null(requested.ExpiresAt);

        // A pending request promises nothing, so the room is still on the market.
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(listing.RoomId));

        var approved = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var accepted = await approved.ReadAsync<DepositResponse>();

        Assert.Equal(DepositStatus.Accepted, accepted.Status);
        Assert.Equal(await _client.UserIdAsync(listing.OwnerToken), accepted.HandledByUserId);
        Assert.NotNull(accepted.ExpiresAt);
        Assert.True(accepted.ExpiresAt > DateTimeOffset.UtcNow);

        // And now it is held (docs/domain-rules.md §9.3).
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(listing.RoomId));
    }

    [Fact]
    public async Task The_amount_is_frozen_when_the_request_is_made()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var requested = await RequestAsync(tenant, listing.RoomId);

        // The owner raises the asking price after the fact. The agreed figure must not follow it
        // (docs/domain-rules.md §2).
        var repriced = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/room-types/{listing.RoomTypeId}",
            listing.OwnerToken,
            new SaveRoomTypeRequest(
                TypeName: "Standard",
                Price: ListingSetup.MonthlyRent * 2,
                RoomSizeM2: 20m,
                MaxOccupants: 1,
                Description: null,
                FacilityIds: []));

        repriced.EnsureSuccessStatusCode();

        var reread = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/deposits/{requested.Id}", tenant);

        Assert.Equal(
            ListingSetup.MonthlyRent, (await reread.ReadAsync<DepositResponse>()).Amount);
    }

    [Fact]
    public async Task A_draft_listing_holds_no_rooms()
    {
        var owner = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var houseId = await _client.CreateHouseAsync(owner);
        var roomTypeId = await _client.CreateRoomTypeAsync(owner, houseId);
        var roomId = await _client.CreateRoomAsync(owner, houseId, roomTypeId, "101");
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostRequestAsync(tenant, roomId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.deposit.listing_not_published", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task A_move_in_date_that_has_passed_is_refused()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostRequestAsync(
            tenant, listing.RoomId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.deposit.start_date_in_past", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task One_live_request_per_person_per_room()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        await RequestAsync(tenant, listing.RoomId);

        var second = await PostRequestAsync(tenant, listing.RoomId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("error.deposit.already_requested", await second.ReadCodeAsync());

        // Somebody else asking about the same room is a different matter: the owner picks.
        var other = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        Assert.Equal(
            HttpStatusCode.Created, (await PostRequestAsync(other, listing.RoomId)).StatusCode);
    }

    [Fact]
    public async Task A_room_can_only_be_promised_once()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var first = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var second = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var accepted = await RequestAsync(first, listing.RoomId);
        var alsoAsked = await RequestAsync(second, listing.RoomId);

        var approved = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{accepted.Id}/approve", listing.OwnerToken);

        approved.EnsureSuccessStatusCode();

        var secondApproval = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{alsoAsked.Id}/approve", listing.OwnerToken);

        Assert.Equal(HttpStatusCode.Conflict, secondApproval.StatusCode);
        Assert.Equal("error.deposit.room_not_available", await secondApproval.ReadCodeAsync());
    }

    [Fact]
    public async Task A_request_is_answered_once_and_the_reason_is_kept()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var requested = await RequestAsync(tenant, listing.RoomId);

        var rejected = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/deposits/{requested.Id}/reject",
            listing.OwnerToken,
            new RejectDepositRequest("The room is promised to a returning tenant."));

        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);

        var answer = await rejected.ReadAsync<DepositResponse>();

        Assert.Equal(DepositStatus.Rejected, answer.Status);
        Assert.Equal("The room is promised to a returning tenant.", answer.ReasonForCancel);

        // A rejection frees nothing, because a pending request was holding nothing.
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(listing.RoomId));

        var again = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal("error.deposit.not_pending", await again.ReadCodeAsync());
    }

    [Fact]
    public async Task A_tenant_withdrawing_an_accepted_request_frees_the_room()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var stranger = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var requested = await RequestAsync(tenant, listing.RoomId);

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        var byStranger = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/deposits/{requested.Id}/cancel",
            stranger,
            new CancelDepositRequest("Not mine."));

        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);
        Assert.Equal("error.deposit.not_yours", await byStranger.ReadCodeAsync());
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(listing.RoomId));

        var byTenant = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/deposits/{requested.Id}/cancel",
            tenant,
            new CancelDepositRequest("Found another place."));

        Assert.Equal(HttpStatusCode.OK, byTenant.StatusCode);

        var cancelled = await byTenant.ReadAsync<DepositResponse>();

        Assert.Equal(DepositStatus.Rejected, cancelled.Status);
        Assert.Equal("Found another place.", cancelled.ReasonForCancel);
        Assert.Null(cancelled.ExpiresAt);
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(listing.RoomId));
    }

    [Fact]
    public async Task Answering_tells_the_tenant_and_requesting_tells_the_property()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerUserId = await _client.UserIdAsync(listing.OwnerToken);
        var staff = await _app.AssignStaffAsync(_client, listing.HouseId, ownerUserId);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var requested = await RequestAsync(tenant, listing.RoomId);

        // The owner and the assigned staff member can both answer, so both are told (§7).
        Assert.Equal(
            NotificationType.DepositRequested, await NotificationTypeAsync(ownerUserId));
        Assert.Equal(
            NotificationType.DepositRequested, await NotificationTypeAsync(staff.UserId));

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var told = await database.Notifications.SingleAsync(n => n.UserId == requested.TenantUserId);

        Assert.Equal(NotificationType.DepositAccepted, told.Type);
        Assert.Equal("notification.DepositAccepted.title", told.TitleKey);

        var payload = JsonDocument.Parse(told.PayloadJson).RootElement;

        Assert.Equal("101", payload.GetProperty("roomNumber").GetString());
        Assert.Equal(ListingSetup.MonthlyRent, payload.GetProperty("amount").GetDecimal());

        // And told over the realtime channel, once the change is committed.
        Assert.Equal(told.Id, Assert.Single(_app.Realtime.PushedTo(requested.TenantUserId)).Id);
    }

    [Fact]
    public async Task Assigned_staff_answer_a_request_and_an_unrelated_owner_cannot_see_it()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var staff = await _app.AssignStaffAsync(
            _client, listing.HouseId, await _client.UserIdAsync(listing.OwnerToken));
        var outsider = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var requested = await RequestAsync(tenant, listing.RoomId);

        var byOutsider = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/deposits/{requested.Id}", outsider);

        var byStaff = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", staff.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, byOutsider.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byStaff.StatusCode);
        Assert.Equal(staff.UserId, (await byStaff.ReadAsync<DepositResponse>()).HandledByUserId);
    }

    [Fact]
    public async Task Each_side_lists_only_the_requests_that_concern_them()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var otherTenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var otherOwner = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        var mine = await RequestAsync(tenant, listing.RoomId);
        await RequestAsync(otherTenant, listing.RoomId);

        Assert.Equal(mine.Id, Assert.Single((await ListAsync(tenant)).Items).Id);
        Assert.Equal(2, (await ListAsync(listing.OwnerToken)).Total);
        Assert.Empty((await ListAsync(otherOwner)).Items);
    }

    [Fact]
    public async Task The_contract_preview_shows_the_agreed_terms_and_only_after_acceptance()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var requested = await RequestAsync(tenant, listing.RoomId, startDate, termMonths: 6);

        var tooEarly = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/deposits/{requested.Id}/contract-preview", tenant);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooEarly.StatusCode);
        Assert.Equal("error.deposit.not_accepted", await tooEarly.ReadCodeAsync());

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        var response = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/deposits/{requested.Id}/contract-preview", tenant);

        response.EnsureSuccessStatusCode();

        var preview = await response.ReadAsync<DepositContractPreviewResponse>();

        Assert.Equal("101", preview.RoomNumber);
        Assert.Equal(ListingSetup.MonthlyRent, preview.MonthlyRent);
        Assert.Equal(6, preview.TermMonths);
        Assert.Equal(startDate, preview.StartDate);
        Assert.Equal(startDate.AddMonths(6), preview.EndDate);
    }

    [Fact]
    public async Task The_sweep_releases_a_request_whose_payment_deadline_has_passed()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var requested = await RequestAsync(tenant, listing.RoomId);

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        await MoveDeadlineIntoThePastAsync(requested.Id);

        using var scope = _app.Services.CreateScope();
        var swept = await scope.ServiceProvider
            .GetRequiredService<ExpireOverdueDepositsHandler>()
            .HandleAsync();

        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        Assert.Equal(1, swept);
        Assert.Equal(
            DepositStatus.Expired,
            (await database.Deposits.FirstAsync(d => d.Id == requested.Id)).Status);

        // The whole point of the deadline: the room goes back on the market (§2).
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(listing.RoomId));
        Assert.Equal(NotificationType.DepositExpired, await NotificationTypeAsync(requested.TenantUserId));
    }

    [Fact]
    public async Task The_sweep_leaves_a_request_that_is_still_within_its_deadline()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var requested = await RequestAsync(tenant, listing.RoomId);

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        using var scope = _app.Services.CreateScope();
        var swept = await scope.ServiceProvider
            .GetRequiredService<ExpireOverdueDepositsHandler>()
            .HandleAsync();

        Assert.Equal(0, swept);
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(listing.RoomId));
    }

    [Fact]
    public async Task A_held_room_cannot_be_taken_out_of_service_by_hand()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var requested = await RequestAsync(tenant, listing.RoomId);

        await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        var response = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{listing.RoomId}/status",
            listing.OwnerToken,
            new UpdateRoomStatusRequest(RoomStatus.Maintenance));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("error.room.status_locked_by_occupancy", await response.ReadCodeAsync());
    }

    private async Task<DepositResponse> RequestAsync(
        string tenantToken,
        Guid roomId,
        DateOnly? startDate = null,
        int termMonths = 12)
    {
        var response = await PostRequestAsync(tenantToken, roomId, startDate, termMonths);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<DepositResponse>();
    }

    private Task<HttpResponseMessage> PostRequestAsync(
        string tenantToken,
        Guid roomId,
        DateOnly? startDate = null,
        int termMonths = 12) =>
        _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/deposits",
            tenantToken,
            new RequestDepositRequest(
                roomId,
                startDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
                termMonths));

    private async Task<PagedResponse<DepositResponse>> ListAsync(string accessToken)
    {
        var response = await _client.SendAsync(HttpMethod.Get, "/api/v1/deposits", accessToken);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PagedResponse<DepositResponse>>();
    }

    private async Task<RoomStatus> RoomStatusAsync(Guid roomId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Rooms.Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync();
    }

    private async Task<NotificationType> NotificationTypeAsync(Guid userId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => n.Type)
            .FirstAsync();
    }

    /// <summary>
    /// The deadline is set from the real clock when the request is accepted, and the endpoint offers
    /// no way to bring it forward, so the row the sweep should find is written directly.
    /// </summary>
    private async Task MoveDeadlineIntoThePastAsync(Guid depositId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var deposit = await database.Deposits.FirstAsync(d => d.Id == depositId);

        deposit.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await database.SaveChangesAsync();
    }
}
