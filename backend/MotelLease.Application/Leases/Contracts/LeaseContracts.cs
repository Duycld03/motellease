using MotelLease.Domain.Enums;

namespace MotelLease.Application.Leases.Contracts;

/// <summary>
/// A rental contract. <c>MonthlyRent</c> and <c>DepositHeld</c> are the figures frozen at signing;
/// bills read them rather than the room type's current price (docs/domain-rules.md §3).
/// </summary>
public sealed record LeaseResponse(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid? DepositId,
    Guid PrimaryTenantUserId,
    string PrimaryTenantFullName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TermMonths,
    decimal MonthlyRent,
    decimal DepositHeld,
    LeaseStatus Status,
    IReadOnlyList<LeaseTenantResponse> Tenants,
    DateTimeOffset CreatedAt);

public sealed record LeaseTenantResponse(
    Guid Id,
    Guid? UserId,
    string FullName,
    string? PhoneNumber,
    bool IsPrimary,
    DateTimeOffset MovedInAt,
    DateTimeOffset? MovedOutAt);
