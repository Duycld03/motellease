using MotelLease.Domain.Enums;

namespace MotelLease.Application.Refunds.Contracts;

public sealed record CreateRefundRequest(
    Guid DepositId,
    string? Reason = null);

public sealed record RejectRefundRequest(
    string? Reason = null);

public sealed record RefundRequestResponse(
    Guid Id,
    Guid DepositId,
    Guid? LeaseId,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid UserId,
    string UserFullName,
    decimal Amount,
    RequestStatus Status,
    string? Reason,
    Guid? ProcessedByUserId,
    string? ProcessedByFullName,
    DateTimeOffset? ProcessedAt,
    string? RejectReason,
    DateTimeOffset CreatedAt);
