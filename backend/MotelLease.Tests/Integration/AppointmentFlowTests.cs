using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Appointments;
using MotelLease.Application.Appointments.Contracts;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the viewing appointment group of docs/api-design.md. The rules worth asserting are who
/// may book, answer or cancel a visit, and what the sweep does with one whose time has passed.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AppointmentFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public AppointmentFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task A_tenant_books_a_visit_and_the_owner_accepts_it()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var booked = await BookAsync(tenant, listing.RoomId);

        Assert.Equal(RequestStatus.Pending, booked.Status);
        Assert.Equal("101", booked.RoomNumber);
        Assert.Null(booked.HandledByUserId);

        var approved = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/appointments/{booked.Id}/approve", listing.OwnerToken);

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var answer = await approved.ReadAsync<AppointmentResponse>();

        Assert.Equal(RequestStatus.Accepted, answer.Status);
        Assert.Equal(await UserIdAsync(listing.OwnerToken), answer.HandledByUserId);

        // The tenant is told, in the same save as the status change, and the row carries keys
        // rather than a sentence (docs/domain-rules.md §7).
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var notification = await database.Notifications.SingleAsync(
            n => n.UserId == booked.TenantUserId);

        Assert.Equal(NotificationType.AppointmentHandled, notification.Type);
        Assert.Equal("notification.AppointmentHandled.title", notification.TitleKey);
        Assert.False(notification.IsRead);

        var payload = JsonDocument.Parse(notification.PayloadJson).RootElement;

        Assert.Equal("Accepted", payload.GetProperty("status").GetString());
        Assert.Equal("101", payload.GetProperty("roomNumber").GetString());
    }

    [Fact]
    public async Task A_draft_listing_takes_no_bookings()
    {
        var owner = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostBookingAsync(tenant, roomId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.appointment.listing_not_published", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task A_room_out_of_service_takes_no_bookings()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{listing.RoomId}/status",
            listing.OwnerToken,
            new UpdateRoomStatusRequest(RoomStatus.Maintenance));

        var response = await PostBookingAsync(tenant, listing.RoomId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.appointment.room_not_available", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task A_visit_cannot_be_booked_for_a_time_that_has_passed()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostBookingAsync(
            tenant, listing.RoomId, DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.appointment.date_in_past", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task One_live_request_per_person_per_room()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        await BookAsync(tenant, listing.RoomId);

        var second = await PostBookingAsync(tenant, listing.RoomId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("error.appointment.already_requested", await second.ReadCodeAsync());

        // Somebody else asking about the same room is a different matter.
        var other = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        Assert.Equal(
            HttpStatusCode.Created, (await PostBookingAsync(other, listing.RoomId)).StatusCode);
    }

    [Fact]
    public async Task A_request_is_answered_once_and_the_reason_is_kept()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var booked = await BookAsync(tenant, listing.RoomId);

        var rejected = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/appointments/{booked.Id}/reject",
            listing.OwnerToken,
            new RejectAppointmentRequest("The room is being repainted that day."));

        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);

        var answer = await rejected.ReadAsync<AppointmentResponse>();

        Assert.Equal(RequestStatus.Rejected, answer.Status);
        Assert.Equal("The room is being repainted that day.", answer.ReasonForCancel);

        var again = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/appointments/{booked.Id}/approve", listing.OwnerToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal("error.appointment.not_pending", await again.ReadCodeAsync());
    }

    [Fact]
    public async Task A_tenant_cancels_their_own_request_and_nobody_elses()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var stranger = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var booked = await BookAsync(tenant, listing.RoomId);

        var byStranger = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/appointments/{booked.Id}/cancel",
            stranger,
            new CancelAppointmentRequest("Changed my mind."));

        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);
        Assert.Equal("error.appointment.not_yours", await byStranger.ReadCodeAsync());

        var byTenant = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/appointments/{booked.Id}/cancel",
            tenant,
            new CancelAppointmentRequest("Found another place."));

        Assert.Equal(HttpStatusCode.OK, byTenant.StatusCode);
        Assert.Equal(
            RequestStatus.Cancelled,
            (await byTenant.ReadAsync<AppointmentResponse>()).Status);
    }

    [Fact]
    public async Task Assigned_staff_answer_a_request_and_an_unrelated_owner_cannot_see_it()
    {
        var listing = await PublishedListingAsync();
        var staff = await _app.AssignStaffAsync(
            _client, listing.HouseId, await UserIdAsync(listing.OwnerToken));
        var outsider = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var booked = await BookAsync(tenant, listing.RoomId);

        var byOutsider = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/appointments/{booked.Id}", outsider);

        var byStaff = await _client.SendAsync(
            HttpMethod.Put, $"/api/v1/appointments/{booked.Id}/approve", staff.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, byOutsider.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byStaff.StatusCode);
        Assert.Equal(
            staff.UserId, (await byStaff.ReadAsync<AppointmentResponse>()).HandledByUserId);
    }

    [Fact]
    public async Task Each_side_lists_only_the_requests_that_concern_them()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var otherTenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var otherOwner = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        var mine = await BookAsync(tenant, listing.RoomId);
        await BookAsync(otherTenant, listing.RoomId);

        var tenantView = await ListAsync(tenant);
        var ownerView = await ListAsync(listing.OwnerToken);
        var strangerView = await ListAsync(otherOwner);

        Assert.Equal(mine.Id, Assert.Single(tenantView.Items).Id);
        Assert.Equal(2, ownerView.Total);
        Assert.Empty(strangerView.Items);
    }

    [Fact]
    public async Task The_sweep_expires_an_unanswered_visit_and_completes_an_accepted_one()
    {
        var listing = await PublishedListingAsync();
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var unanswered = await SeedPastAppointmentAsync(
            listing.RoomId, await UserIdAsync(tenant), RequestStatus.Pending);

        var accepted = await SeedPastAppointmentAsync(
            listing.RoomId, await UserIdAsync(tenant), RequestStatus.Accepted);

        var upcoming = await BookAsync(tenant, listing.RoomId);

        using var scope = _app.Services.CreateScope();
        var swept = await scope.ServiceProvider
            .GetRequiredService<ExpirePastAppointmentsHandler>()
            .HandleAsync();

        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        Assert.Equal(2, swept);
        Assert.Equal(
            RequestStatus.Expired,
            (await database.Appointments.FirstAsync(a => a.Id == unanswered)).Status);
        Assert.Equal(
            RequestStatus.Completed,
            (await database.Appointments.FirstAsync(a => a.Id == accepted)).Status);

        // A visit still ahead is left alone.
        Assert.Equal(
            RequestStatus.Pending,
            (await database.Appointments.FirstAsync(a => a.Id == upcoming.Id)).Status);
    }

    private sealed record Listing(string OwnerToken, Guid HouseId, Guid RoomId);

    private async Task<Listing> PublishedListingAsync()
    {
        var owner = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        await _app.PublishAsync(houseId);

        return new Listing(owner, houseId, roomId);
    }

    private async Task<AppointmentResponse> BookAsync(string tenantToken, Guid roomId)
    {
        var response = await PostBookingAsync(tenantToken, roomId);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<AppointmentResponse>();
    }

    private Task<HttpResponseMessage> PostBookingAsync(
        string tenantToken,
        Guid roomId,
        DateTimeOffset? at = null) =>
        _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/appointments",
            tenantToken,
            new BookAppointmentRequest(
                roomId,
                at ?? DateTimeOffset.UtcNow.AddDays(2),
                Note: "Afternoon suits me best."));

    private async Task<PagedResponse<AppointmentResponse>> ListAsync(string accessToken)
    {
        var response = await _client.SendAsync(
            HttpMethod.Get, "/api/v1/appointments", accessToken);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PagedResponse<AppointmentResponse>>();
    }

    /// <summary>
    /// A visit whose time has already passed. The endpoint refuses one on purpose, so the row for
    /// the sweep to find is written directly.
    /// </summary>
    private async Task<Guid> SeedPastAppointmentAsync(
        Guid roomId,
        Guid tenantUserId,
        RequestStatus status)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var appointment = new Appointment
        {
            RoomId = roomId,
            UserId = tenantUserId,
            AppointmentDate = DateTimeOffset.UtcNow.AddDays(-1),
            Status = status
        };

        database.Appointments.Add(appointment);

        await database.SaveChangesAsync();

        return appointment.Id;
    }

    private async Task<Guid> UserIdAsync(string accessToken)
    {
        var response = await _client.SendAsync(HttpMethod.Get, "/api/v1/me", accessToken);

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<ProfileResponse>()).Id;
    }

    private async Task<Guid> CreateHouseAsync(string ownerToken)
    {
        var response = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                Name: "Nha tro Ben Thanh",
                Description: null,
                Type: BoardingHouseType.Traditional,
                AddressLine: "12 Le Loi",
                Ward: "Ben Thanh",
                District: "District 1",
                Province: "Ho Chi Minh",
                Latitude: 10.772m,
                Longitude: 106.698m));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<BoardingHouseDetailResponse>()).Id;
    }

    private async Task<Guid> CreateRoomTypeAsync(string ownerToken, Guid boardingHouseId)
    {
        var response = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/room-types",
            ownerToken,
            new SaveRoomTypeRequest(
                TypeName: "Standard",
                Price: 3_000_000m,
                RoomSizeM2: 20m,
                MaxOccupants: 1,
                Description: null,
                FacilityIds: []));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<RoomTypeResponse>()).Id;
    }

    private async Task<Guid> CreateRoomAsync(
        string ownerToken,
        Guid boardingHouseId,
        Guid roomTypeId,
        string roomNumber)
    {
        var response = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/rooms",
            ownerToken,
            new SaveRoomRequest(roomTypeId, roomNumber, Description: null));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<RoomResponse>()).Id;
    }
}
