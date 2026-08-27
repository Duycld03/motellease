using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Tests.Integration;

/// <summary>
/// A property to act on, built through the real endpoints so the flow tests keep exercising the
/// shipped code. Every group past the listing work needs one, so it lives here rather than being
/// copied per test class.
/// </summary>
internal static class ListingSetup
{
    internal const decimal MonthlyRent = 3_000_000m;

    /// <summary>An owner, one published room, ready to be booked or deposited on.</summary>
    internal static async Task<Listing> PublishedListingAsync(
        this MotelLeaseAppFactory app,
        HttpClient client,
        string roomNumber = "101")
    {
        var owner = await client.RegisterAsync(app.Emails, UserRole.Owner);
        var houseId = await client.CreateHouseAsync(owner);
        var roomTypeId = await client.CreateRoomTypeAsync(owner, houseId);
        var roomId = await client.CreateRoomAsync(owner, houseId, roomTypeId, roomNumber);

        await app.PublishAsync(houseId);

        return new Listing(owner, houseId, roomTypeId, roomId);
    }

    internal static async Task<Guid> UserIdAsync(this HttpClient client, string accessToken)
    {
        var response = await client.SendAsync(HttpMethod.Get, "/api/v1/me", accessToken);

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<ProfileResponse>()).Id;
    }

    internal static async Task<Guid> CreateHouseAsync(
        this HttpClient client,
        string ownerToken,
        BoardingHouseType type = BoardingHouseType.Traditional)
    {
        var response = await client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                Name: "Nha tro Ben Thanh",
                Description: null,
                Type: type,
                AddressLine: "12 Le Loi",
                Ward: "Ben Thanh",
                District: "District 1",
                Province: "Ho Chi Minh",
                Latitude: 10.772m,
                Longitude: 106.698m));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<BoardingHouseDetailResponse>()).Id;
    }

    internal static async Task<Guid> CreateRoomTypeAsync(
        this HttpClient client,
        string ownerToken,
        Guid boardingHouseId,
        decimal price = MonthlyRent)
    {
        var response = await client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/room-types",
            ownerToken,
            new SaveRoomTypeRequest(
                TypeName: "Standard",
                Price: price,
                RoomSizeM2: 20m,
                MaxOccupants: 1,
                Description: null,
                FacilityIds: []));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<RoomTypeResponse>()).Id;
    }

    internal static async Task<Guid> CreateRoomAsync(
        this HttpClient client,
        string ownerToken,
        Guid boardingHouseId,
        Guid roomTypeId,
        string roomNumber)
    {
        var response = await client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{boardingHouseId}/rooms",
            ownerToken,
            new SaveRoomRequest(roomTypeId, roomNumber, Description: null));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<RoomResponse>()).Id;
    }
}

internal sealed record Listing(
    string OwnerToken,
    Guid HouseId,
    Guid RoomTypeId,
    Guid RoomId);
