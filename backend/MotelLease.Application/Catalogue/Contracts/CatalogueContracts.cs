using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Catalogue.Contracts;

public sealed record BoardingHouseSearchFilter(
    string? Q = null,
    string? Province = null,
    string? District = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    IReadOnlyList<Guid>? Facilities = null,
    BoardingHouseType? Type = null,
    decimal? MinRating = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PublicBoardingHouseCardResponse(
    Guid Id,
    string Name,
    BoardingHouseType Type,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    decimal Latitude,
    decimal Longitude,
    decimal Rating,
    int ReviewCount,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? PrimaryImageUrl,
    int TotalRoomsCount,
    int AvailableRoomsCount,
    IReadOnlyList<FacilityResponse> Facilities,
    DateTimeOffset CreatedAt);

public sealed record BoardingHouseNearbyRequest(
    double Lat,
    double Lon,
    double RadiusKm = 5,
    int Limit = 20);

public sealed record BoardingHouseNearbyResponse(
    Guid Id,
    string Name,
    BoardingHouseType Type,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    decimal Latitude,
    decimal Longitude,
    decimal Rating,
    int ReviewCount,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? PrimaryImageUrl,
    int TotalRoomsCount,
    int AvailableRoomsCount,
    IReadOnlyList<FacilityResponse> Facilities,
    double DistanceMeters,
    DateTimeOffset CreatedAt);

public sealed record BoardingHouseMapRequest(
    double SwLat,
    double SwLon,
    double NeLat,
    double NeLon,
    int Limit = 100);

public sealed record BoardingHouseMapMarkerResponse(
    Guid Id,
    string Name,
    decimal Latitude,
    decimal Longitude,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? PrimaryImageUrl,
    string AddressLine,
    decimal Rating,
    int ReviewCount);

public sealed record PublicBoardingHouseDetailResponse(
    Guid Id,
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
    decimal Rating,
    int ReviewCount,
    int TotalRoomsCount,
    int AvailableRoomsCount,
    PublicOwnerInfoResponse Owner,
    IReadOnlyList<ImageResponse> Images,
    IReadOnlyList<PublicRoomTypeResponse> RoomTypes,
    DateTimeOffset CreatedAt);

public sealed record PublicOwnerInfoResponse(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl);

public sealed record PublicRoomTypeResponse(
    Guid Id,
    string TypeName,
    decimal Price,
    decimal RoomSizeM2,
    int MaxOccupants,
    string? Description,
    int TotalRoomsCount,
    int AvailableRoomsCount,
    IReadOnlyList<FacilityResponse> Facilities,
    IReadOnlyList<ImageResponse> Images);

public sealed record PublicVacantRoomResponse(
    Guid Id,
    string RoomNumber,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal Price,
    decimal RoomSizeM2,
    int MaxOccupants,
    string? Description);

public sealed record PublicReviewResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    short Rating,
    string Content,
    bool IsVerified,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PublicReviewReplyResponse> Replies);

public sealed record PublicReviewReplyResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record ProvinceResponse(
    string Code,
    string Name);

public sealed record DistrictResponse(
    string Code,
    string Name,
    string ProvinceCode);
