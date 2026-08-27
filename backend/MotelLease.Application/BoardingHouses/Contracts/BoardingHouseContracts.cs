using MotelLease.Domain.Enums;

namespace MotelLease.Application.BoardingHouses.Contracts;

/// <summary>
/// One row of the owner's or staff member's property list. The room counts and the price range
/// are computed per request rather than stored: a cached count that drifts gives no sign that
/// it has (docs/features.md §0.2).
/// </summary>
public sealed record BoardingHouseSummaryResponse(
    Guid Id,
    string Name,
    BoardingHouseType Type,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    ListingStatus ListingStatus,
    decimal Rating,
    int ReviewCount,
    int RoomCount,
    int AvailableRoomCount,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? PrimaryImageUrl,
    DateTimeOffset CreatedAt);

public sealed record BoardingHouseDetailResponse(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    BoardingHouseType Type,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    decimal Latitude,
    decimal Longitude,
    decimal ElectricityUnitPrice,
    decimal WaterUnitPrice,
    ListingStatus ListingStatus,
    string? RejectionReason,
    decimal Rating,
    int ReviewCount,
    RoomCountsResponse RoomCounts,
    IReadOnlyList<ImageResponse> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Rooms by status, so the owner sees occupancy without loading every room.</summary>
public sealed record RoomCountsResponse(
    int Total,
    int Available,
    int Reserved,
    int Occupied,
    int Maintenance);

public sealed record ImageResponse(Guid Id, string Url, bool IsPrimary, int SortOrder);

/// <summary>
/// Utility prices are absent on purpose: they have their own endpoint because they are the
/// owner's decision, while everything here is also editable by assigned staff.
/// </summary>
public sealed record SaveBoardingHouseRequest(
    string Name,
    string? Description,
    BoardingHouseType Type,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    decimal Latitude,
    decimal Longitude);

public sealed record UpdateUtilityPricesRequest(
    decimal ElectricityUnitPrice,
    decimal WaterUnitPrice);

public sealed record AddImageRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
