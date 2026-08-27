using MotelLease.Domain.Enums;

namespace MotelLease.Application.Deposits.Contracts;

/// <summary>
/// One request to hold a room. <c>Amount</c> is the figure frozen when the request was made, not
/// today's asking price (docs/domain-rules.md §2), and <c>ExpiresAt</c> is filled once the request
/// has been accepted and the tenant is on the clock to pay.
/// </summary>
public sealed record DepositResponse(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid TenantUserId,
    string TenantFullName,
    string? TenantPhoneNumber,
    decimal Amount,
    DepositStatus Status,
    DateOnly RequestedStartDate,
    int RequestedTermMonths,
    DateTimeOffset? ExpiresAt,
    string? ReasonForCancel,
    Guid? HandledByUserId,
    DateTimeOffset CreatedAt);

public sealed record RequestDepositRequest(
    Guid RoomId,
    DateOnly RequestedStartDate,
    int RequestedTermMonths);

public sealed record RejectDepositRequest(string Reason);

public sealed record CancelDepositRequest(string? Reason);

/// <summary>
/// What the contract would say if it were signed now. Every figure comes from the deposit row, so
/// the preview cannot drift from the amount already agreed: a historical document never reads a
/// current price (docs/domain-rules.md §3).
/// </summary>
public sealed record DepositContractPreviewResponse(
    Guid DepositId,
    string BoardingHouseName,
    string AddressLine,
    string Ward,
    string District,
    string Province,
    string RoomNumber,
    string TenantFullName,
    string? TenantPhoneNumber,
    decimal MonthlyRent,
    decimal DepositHeld,
    int TermMonths,
    DateOnly StartDate,
    DateOnly EndDate);
