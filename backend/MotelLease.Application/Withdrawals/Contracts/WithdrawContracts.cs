using MotelLease.Domain.Enums;

namespace MotelLease.Application.Withdrawals.Contracts;

public sealed record CreateWithdrawRequest(
    decimal Amount,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankAccountHolder = null);

public sealed record RejectWithdrawRequest(
    string? Reason = null);

public sealed record WithdrawRequestResponse(
    Guid Id,
    Guid OwnerUserId,
    string OwnerFullName,
    decimal Amount,
    string BankName,
    string BankAccountNumber,
    string BankAccountHolder,
    RequestStatus Status,
    Guid? ProcessedByUserId,
    string? ProcessedByFullName,
    DateTimeOffset? ProcessedAt,
    string? RejectReason,
    DateTimeOffset CreatedAt);
