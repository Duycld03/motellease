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
    DateTimeOffset CreatedAt,
    DateTimeOffset? EndedAt = null,
    string? EndReason = null,
    decimal? FinalElectricityReading = null,
    decimal? FinalWaterReading = null,
    decimal DepositDeducted = 0,
    decimal DepositRefunded = 0);

public sealed record LeaseTenantResponse(
    Guid Id,
    Guid? UserId,
    string FullName,
    string? PhoneNumber,
    string? IdCardNumber,
    bool IsPrimary,
    DateTimeOffset MovedInAt,
    DateTimeOffset? MovedOutAt);

public sealed record AddLeaseTenantRequest(
    string FullName,
    string? PhoneNumber = null,
    string? IdCardNumber = null,
    Guid? UserId = null);

public sealed record TerminateLeaseRequest(
    decimal FinalElectricityReading,
    decimal FinalWaterReading,
    decimal DepositDeducted = 0,
    string? EndReason = null);

public sealed record LeaseTerminationPreviewResponse(
    Guid LeaseId,
    Guid RoomId,
    decimal DepositHeld,
    decimal ElectricityOld,
    decimal FinalElectricityReading,
    decimal ElectricityQty,
    decimal ElectricityUnitPrice,
    decimal ElectricityAmount,
    decimal WaterOld,
    decimal FinalWaterReading,
    decimal WaterQty,
    decimal WaterUnitPrice,
    decimal WaterAmount,
    decimal DepositDeducted,
    decimal DepositRefunded);

public sealed record CreateExtensionRequest(
    Guid LeaseId,
    DateOnly RequestedEndDate,
    string? TenantNote);

public sealed record RejectExtensionRequest(
    string? OwnerNote);

public sealed record ExtensionRequestResponse(
    Guid Id,
    Guid LeaseId,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid RequestedByUserId,
    string RequesterFullName,
    DateOnly CurrentEndDate,
    DateOnly RequestedEndDate,
    RequestStatus Status,
    string? TenantNote,
    string? OwnerNote,
    Guid? HandledByUserId,
    DateTimeOffset CreatedAt);
