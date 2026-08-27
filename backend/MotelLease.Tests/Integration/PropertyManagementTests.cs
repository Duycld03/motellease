using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Auth.Contracts;
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
/// Drives the boarding house / room type / room group of docs/api-design.md against a real PostGIS
/// database. The assertions are the rules that cannot be read off the schema: who reaches which
/// property, which status changes a person may make, and what a soft delete frees up.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PropertyManagementTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string Password = "Passw0rd123";

    private readonly PostgresFixture _postgres;
    private readonly RecordingImageStorage _images = new();
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public PropertyManagementTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString, imageStorage: _images);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task An_owner_lists_only_their_own_properties()
    {
        var first = await RegisterAsync(UserRole.Owner);
        var second = await RegisterAsync(UserRole.Owner);

        var mine = await CreateHouseAsync(first);
        await CreateHouseAsync(second);

        var listed = await ListHousesAsync(first);

        Assert.Equal(1, listed.Total);
        Assert.Equal(mine, listed.Items[0].Id);
        Assert.Equal(ListingStatus.Draft, listed.Items[0].ListingStatus);
    }

    [Fact]
    public async Task Another_owners_property_is_out_of_reach()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var stranger = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);

        var response = await SendAsync(
            HttpMethod.Get, $"/api/v1/my/boarding-houses/{houseId}", stranger);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("error.boarding_house.not_yours", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task A_tenant_cannot_reach_the_management_endpoints()
    {
        var tenant = await RegisterAsync(UserRole.Tenant);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/my/boarding-houses", tenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_reach_a_property_only_while_the_assignment_is_live()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var staff = await AssignStaffAsync(houseId, await UserIdAsync(owner));

        var listed = await ListHousesAsync(staff.AccessToken);

        Assert.Equal(1, listed.Total);
        Assert.Equal(houseId, listed.Items[0].Id);

        await UnassignStaffAsync(staff.UserId);

        var afterRevoke = await SendAsync(
            HttpMethod.Get, $"/api/v1/my/boarding-houses/{houseId}", staff.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, afterRevoke.StatusCode);
        Assert.Empty((await ListHousesAsync(staff.AccessToken)).Items);
    }

    [Fact]
    public async Task Staff_may_edit_a_property_but_not_delete_it()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var staff = await AssignStaffAsync(houseId, await UserIdAsync(owner));

        var edit = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/boarding-houses/{houseId}",
            staff.AccessToken,
            NewHouse(name: "Renamed by staff"));

        var delete = await SendAsync(
            HttpMethod.Delete, $"/api/v1/my/boarding-houses/{houseId}", staff.AccessToken);

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal("Renamed by staff", (await ReadAsync<BoardingHouseDetailResponse>(edit)).Name);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task A_listing_needs_a_room_before_it_can_be_submitted_for_review()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);

        var empty = await SendAsync(
            HttpMethod.Put, $"/api/v1/my/boarding-houses/{houseId}/submit-review", owner);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        Assert.Equal("error.boarding_house.nothing_to_publish", await ReadCodeAsync(empty));

        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var submitted = await SendAsync(
            HttpMethod.Put, $"/api/v1/my/boarding-houses/{houseId}/submit-review", owner);

        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        Assert.Equal(
            ListingStatus.PendingReview,
            (await ReadAsync<BoardingHouseDetailResponse>(submitted)).ListingStatus);

        // Already queued: submitting again would put an admin decision back in the owner's hands.
        var again = await SendAsync(
            HttpMethod.Put, $"/api/v1/my/boarding-houses/{houseId}/submit-review", owner);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal("error.boarding_house.already_under_review", await ReadCodeAsync(again));
    }

    [Fact]
    public async Task A_shared_room_type_is_refused_outside_a_dorm_style_house()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var traditional = await CreateHouseAsync(owner);

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{traditional}/room-types",
            owner,
            NewRoomType(maxOccupants: 4));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.room_type.single_occupant_house", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task A_dorm_house_cannot_become_single_occupant_while_a_room_type_shares()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner, BoardingHouseType.DormStyle);

        await CreateRoomTypeAsync(owner, houseId, maxOccupants: 4);

        var response = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/boarding-houses/{houseId}",
            owner,
            NewHouse(type: BoardingHouseType.Traditional));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "error.boarding_house.type_conflicts_with_occupancy", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task A_room_type_with_rooms_cannot_be_deleted()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var blocked = await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{houseId}/room-types/{roomTypeId}",
            owner);

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("error.room_type.in_use", await ReadCodeAsync(blocked));

        await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{houseId}/rooms/{roomId}",
            owner);

        var allowed = await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{houseId}/room-types/{roomTypeId}",
            owner);

        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    [Fact]
    public async Task A_room_number_is_unique_per_property_and_a_soft_delete_frees_it()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var duplicate = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{houseId}/rooms",
            owner,
            NewRoom(roomTypeId, "101"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("error.room.number_taken", await ReadCodeAsync(duplicate));

        await SendAsync(
            HttpMethod.Delete, $"/api/v1/my/boarding-houses/{houseId}/rooms/{roomId}", owner);

        // The unique index is partial on IsDeleted = false, so the number is available again.
        var reused = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{houseId}/rooms",
            owner,
            NewRoom(roomTypeId, "101"));

        Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
        Assert.Single(await ListRoomsAsync(owner, houseId));
    }

    [Fact]
    public async Task A_room_type_from_another_property_is_refused()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var first = await CreateHouseAsync(owner);
        var second = await CreateHouseAsync(owner);
        var foreignType = await CreateRoomTypeAsync(owner, second);

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{first}/rooms",
            owner,
            NewRoom(foreignType, "101"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.room.room_type_from_another_house", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Only_maintenance_and_available_can_be_set_by_hand()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var maintenance = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/status",
            owner,
            new UpdateRoomStatusRequest(RoomStatus.Maintenance));

        Assert.Equal(HttpStatusCode.OK, maintenance.StatusCode);
        Assert.Equal(
            RoomStatus.Maintenance, (await ReadAsync<RoomResponse>(maintenance)).Status);

        // Occupied is derived from the lease rows (docs/domain-rules.md §9.3).
        var occupied = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/status",
            owner,
            new UpdateRoomStatusRequest(RoomStatus.Occupied));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, occupied.StatusCode);
        Assert.Equal("error.room.status_not_manually_settable", await ReadCodeAsync(occupied));
    }

    [Fact]
    public async Task A_room_held_by_an_occupant_neither_changes_status_nor_disappears()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        await SeedActiveLeaseAsync(roomId, await UserIdAsync(owner));

        var status = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/status",
            owner,
            new UpdateRoomStatusRequest(RoomStatus.Maintenance));

        var delete = await SendAsync(
            HttpMethod.Delete, $"/api/v1/my/boarding-houses/{houseId}/rooms/{roomId}", owner);

        Assert.Equal(HttpStatusCode.Conflict, status.StatusCode);
        Assert.Equal("error.room.status_locked_by_occupancy", await ReadCodeAsync(status));
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal("error.room.in_use", await ReadCodeAsync(delete));
    }

    [Fact]
    public async Task A_meter_reading_never_goes_backwards()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var recorded = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/meter-readings",
            owner,
            new UpdateMeterReadingsRequest(1_250.5m, 42m));

        Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);

        var room = await ReadAsync<RoomResponse>(recorded);

        Assert.Equal(1_250.5m, room.CurrentElectricityReading);
        Assert.Equal(42m, room.CurrentWaterReading);

        var backwards = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/meter-readings",
            owner,
            new UpdateMeterReadingsRequest(1_000m, 42m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, backwards.StatusCode);
        Assert.Equal("error.room.reading_went_backwards", await ReadCodeAsync(backwards));

        // An unchanged meter is not a decrease: a room nobody used still gets a reading.
        var unchanged = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/rooms/{roomId}/meter-readings",
            owner,
            new UpdateMeterReadingsRequest(1_250.5m, 42m));

        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_property_hides_its_rooms_and_room_types_too()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        var deleted = await SendAsync(
            HttpMethod.Delete, $"/api/v1/my/boarding-houses/{houseId}", owner);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty((await ListHousesAsync(owner)).Items);

        // The query filter is per entity, so the children have to be marked as well; otherwise
        // they would still turn up in every room query.
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        Assert.Empty(await database.Rooms.Where(r => r.BoardingHouseId == houseId).ToListAsync());
        Assert.Empty(
            await database.RoomTypes.Where(t => t.BoardingHouseId == houseId).ToListAsync());
    }

    [Fact]
    public async Task A_property_with_an_active_lease_cannot_be_deleted()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var roomTypeId = await CreateRoomTypeAsync(owner, houseId);
        var roomId = await CreateRoomAsync(owner, houseId, roomTypeId, "101");

        await SeedActiveLeaseAsync(roomId, await UserIdAsync(owner));

        var response = await SendAsync(
            HttpMethod.Delete, $"/api/v1/my/boarding-houses/{houseId}", owner);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("error.boarding_house.in_use", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task A_listing_always_has_exactly_one_primary_image()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);

        var first = await UploadImageAsync(owner, houseId);
        var second = await UploadImageAsync(owner, houseId);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);

        var promoted = await SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/boarding-houses/{houseId}/images/{second.Id}/primary",
            owner);

        Assert.Equal(HttpStatusCode.NoContent, promoted.StatusCode);
        Assert.Equal(second.Id, (await PrimaryImageAsync(owner, houseId)).Id);

        // Removing the cover leaves the other picture as the cover, not the listing without one.
        var deleted = await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{houseId}/images/{second.Id}",
            owner);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(first.Id, (await PrimaryImageAsync(owner, houseId)).Id);
    }

    [Fact]
    public async Task An_image_the_storage_holds_is_removed_with_its_row()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);
        var image = await UploadImageAsync(owner, houseId);

        await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{houseId}/images/{image.Id}",
            owner);

        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var publicId = image.Url.Replace("https://images.test/", string.Empty)
            .Replace(".jpg", string.Empty);

        Assert.True(_images.WasDeleted(publicId));
        Assert.Empty(await database.Images.Where(i => i.OwnerId == houseId).ToListAsync());
    }

    [Fact]
    public async Task A_file_that_is_not_an_accepted_image_is_refused_before_any_upload()
    {
        var owner = await RegisterAsync(UserRole.Owner);
        var houseId = await CreateHouseAsync(owner);

        var response = await PostImageAsync(owner, houseId, "application/pdf", "invoice.pdf");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.image.type_not_supported", await ReadCodeAsync(response));
    }

    private static SaveBoardingHouseRequest NewHouse(
        string name = "Nha tro Ben Thanh",
        BoardingHouseType type = BoardingHouseType.Traditional) =>
        new(
            Name: name,
            Description: "Close to the market.",
            Type: type,
            AddressLine: "12 Le Loi",
            Ward: "Ben Thanh",
            District: "District 1",
            Province: "Ho Chi Minh",
            // Longitude first when PostGIS builds the point; the pair is stored as written here.
            Latitude: 10.772m,
            Longitude: 106.698m);

    private static SaveRoomTypeRequest NewRoomType(
        decimal price = 3_000_000m,
        int maxOccupants = 1) =>
        new(
            TypeName: "Standard",
            Price: price,
            RoomSizeM2: 20m,
            MaxOccupants: maxOccupants,
            Description: null,
            FacilityIds: []);

    private static SaveRoomRequest NewRoom(Guid roomTypeId, string roomNumber) =>
        new(roomTypeId, roomNumber, Description: null);

    private async Task<Guid> CreateHouseAsync(
        string accessToken,
        BoardingHouseType type = BoardingHouseType.Traditional)
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/v1/my/boarding-houses", accessToken, NewHouse(type: type));

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<BoardingHouseDetailResponse>(response)).Id;
    }

    private async Task<Guid> CreateRoomTypeAsync(
        string accessToken,
        Guid boardingHouseId,
        decimal price = 3_000_000m,
        int maxOccupants = 1)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/room-types",
            accessToken,
            NewRoomType(price, maxOccupants));

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<RoomTypeResponse>(response)).Id;
    }

    private async Task<Guid> CreateRoomAsync(
        string accessToken,
        Guid boardingHouseId,
        Guid roomTypeId,
        string roomNumber)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/rooms",
            accessToken,
            NewRoom(roomTypeId, roomNumber));

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<RoomResponse>(response)).Id;
    }

    private async Task<PagedResponse<BoardingHouseSummaryResponse>> ListHousesAsync(
        string accessToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/my/boarding-houses", accessToken);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<PagedResponse<BoardingHouseSummaryResponse>>(response);
    }

    private async Task<IReadOnlyList<RoomResponse>> ListRoomsAsync(
        string accessToken,
        Guid boardingHouseId)
    {
        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/rooms",
            accessToken);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<List<RoomResponse>>(response);
    }

    private async Task<ImageResponse> PrimaryImageAsync(string accessToken, Guid boardingHouseId)
    {
        var response = await SendAsync(
            HttpMethod.Get, $"/api/v1/my/boarding-houses/{boardingHouseId}", accessToken);

        response.EnsureSuccessStatusCode();

        var detail = await ReadAsync<BoardingHouseDetailResponse>(response);

        return Assert.Single(detail.Images, i => i.IsPrimary);
    }

    private async Task<ImageResponse> UploadImageAsync(string accessToken, Guid boardingHouseId)
    {
        var response = await PostImageAsync(accessToken, boardingHouseId);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<ImageResponse>(response);
    }

    private async Task<HttpResponseMessage> PostImageAsync(
        string accessToken,
        Guid boardingHouseId,
        string contentType = "image/jpeg",
        string fileName = "photo.jpg")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0x01, 0x02, 0x03, 0x04]);

        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/images")
        {
            Content = form,
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        return await _client.SendAsync(request);
    }

    private async Task<string> RegisterAsync(UserRole role)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp", new SendRegistrationOtpRequest(email));

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register/verify-otp",
            new VerifyRegistrationOtpRequest(email, _app.Emails.LastCodeFor(email)));

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                Username: $"u{Guid.NewGuid():N}"[..16],
                Email: email,
                Password: Password,
                FullName: "Nguyen Van A",
                PhoneNumber: "0912345678",
                Gender: Gender.Male,
                Role: role,
                PreferredLanguage: "vi"),
            Json);

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<AuthTokensResponse>(response)).AccessToken;
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, Password));

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<AuthTokensResponse>(response)).AccessToken;
    }

    private async Task<Guid> UserIdAsync(string accessToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/me", accessToken);

        response.EnsureSuccessStatusCode();

        return (await ReadAsync<ProfileResponse>(response)).Id;
    }

    /// <summary>
    /// Staff accounts are created by an owner through an endpoint that belongs to a later feature
    /// group, so the rows are seeded here and the account then signs in through the real login.
    /// </summary>
    private async Task<StaffAccount> AssignStaffAsync(Guid boardingHouseId, Guid ownerUserId)
    {
        var email = $"staff-{Guid.NewGuid():N}@example.com";
        Guid staffUserId;

        using (var scope = _app.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var staff = new User
            {
                Username = email,
                Email = email,
                PasswordHash = passwordHasher.Hash(Password),
                FullName = "Tran Thi B",
                Role = UserRole.Staff,
                EmailConfirmed = true
            };

            database.Users.Add(staff);

            database.StaffProfiles.Add(new StaffProfile
            {
                UserId = staff.Id,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedByUserId = ownerUserId
            });

            database.StaffAssignments.Add(new StaffAssignment
            {
                BoardingHouseId = boardingHouseId,
                StaffUserId = staff.Id,
                AssignedByUserId = ownerUserId,
                AssignedAt = DateTimeOffset.UtcNow
            });

            await database.SaveChangesAsync();

            staffUserId = staff.Id;
        }

        return new StaffAccount(staffUserId, await LoginAsync(email));
    }

    private async Task UnassignStaffAsync(Guid staffUserId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        foreach (var assignment in await database.StaffAssignments
                     .Where(a => a.StaffUserId == staffUserId && a.UnassignedAt == null)
                     .ToListAsync())
        {
            assignment.UnassignedAt = DateTimeOffset.UtcNow;
        }

        await database.SaveChangesAsync();
    }

    private sealed record StaffAccount(Guid UserId, string AccessToken);

    /// <summary>
    /// The lease flow is a later feature group, so an occupied room is produced by writing the
    /// rows it will write: an Active lease with one live tenant, and the room marked Occupied.
    /// </summary>
    private async Task SeedActiveLeaseAsync(Guid roomId, Guid createdByUserId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = $"tenant-{Guid.NewGuid():N}@example.com";

        var tenant = new User
        {
            Username = email,
            Email = email,
            PasswordHash = passwordHasher.Hash(Password),
            FullName = "Le Van C",
            Role = UserRole.Tenant,
            EmailConfirmed = true
        };

        database.Users.Add(tenant);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var lease = new Lease
        {
            RoomId = roomId,
            PrimaryTenantUserId = tenant.Id,
            StartDate = today,
            EndDate = today.AddMonths(6),
            TermMonths = 6,
            MonthlyRent = 3_000_000m,
            DepositHeld = 3_000_000m,
            Status = LeaseStatus.Active,
            CreatedByUserId = createdByUserId
        };

        lease.Tenants.Add(new LeaseTenant
        {
            UserId = tenant.Id,
            FullName = tenant.FullName,
            IsPrimary = true,
            MovedInAt = DateTimeOffset.UtcNow
        });

        database.Leases.Add(lease);

        var room = await database.Rooms.FirstAsync(r => r.Id == roomId);

        room.Status = RoomStatus.Occupied;

        await database.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return _client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json)
        ?? throw new InvalidOperationException($"Empty {typeof(T).Name} body.");

    /// <summary>The message key behind a problem+json response.</summary>
    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
