using MotelLease.Domain.Enums;

namespace MotelLease.Application.Admin.Contracts;

// Accounts
public sealed record AdminCreateAccountRequest(
    string Email,
    string Username,
    string Password,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    UserRole Role);

public sealed record AdminUpdateAccountRequest(
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    UserRole Role);

public sealed record AdminLockAccountRequest(
    string? Reason = null);

public sealed record AdminAccountSummaryResponse(
    Guid Id,
    string Email,
    string Username,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    string? AvatarUrl,
    UserRole Role,
    bool EmailConfirmed,
    bool IsLocked,
    string? LockedReason,
    bool IsDeleted,
    DateTimeOffset CreatedAt);

public sealed record AdminAccountDetailResponse(
    Guid Id,
    string Email,
    string Username,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    string? AvatarUrl,
    UserRole Role,
    bool EmailConfirmed,
    bool IsLocked,
    string? LockedReason,
    bool IsDeleted,
    int BoardingHousesCount,
    int ActiveLeasesCount,
    decimal? AvailableBalance,
    DateTimeOffset CreatedAt);

// Boarding Houses
public sealed record AdminRejectListingRequest(
    string? Reason = null);

public sealed record AdminBoardingHouseResponse(
    Guid Id,
    string Name,
    string AddressLine,
    string Province,
    string District,
    string Ward,
    Guid OwnerUserId,
    string OwnerFullName,
    string OwnerEmail,
    ListingStatus ListingStatus,
    string? RejectionReason,
    bool IsDeleted,
    int RoomsCount,
    decimal Rating,
    int ReviewCount,
    DateTimeOffset CreatedAt);

// Facilities
public sealed record CreateFacilityRequest(
    string Name,
    string? CodeName = null,
    string? IconKey = null,
    string? Description = null);

public sealed record UpdateFacilityRequest(
    string Name,
    string? CodeName = null,
    string? IconKey = null,
    string? Description = null);

public sealed record FacilityDetailResponse(
    Guid Id,
    string Name,
    string CodeName,
    string? IconKey,
    string? Description,
    int InUseByRoomTypesCount,
    DateTimeOffset CreatedAt);

// Reviews
public sealed record AdminReviewResponse(
    Guid Id,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid UserId,
    string UserFullName,
    short? Rating,
    string Content,
    bool IsDeleted,
    DateTimeOffset CreatedAt);

// Audit Logs
public sealed record AuditLogResponse(
    Guid Id,
    Guid ActorUserId,
    string? ActorFullName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? IpAddress,
    DateTimeOffset CreatedAt);

// Platform Stats
public sealed record AdminPlatformStatsResponse(
    int TotalUsers,
    Dictionary<string, int> UsersByRole,
    int TotalBoardingHouses,
    Dictionary<string, int> HousesByStatus,
    int TotalRooms,
    Dictionary<string, int> RoomsByStatus,
    int ActiveLeases,
    int TotalTransactions,
    decimal TotalTransactionVolume,
    int PendingReports,
    int PendingWithdrawals,
    int PendingListingReviews);
