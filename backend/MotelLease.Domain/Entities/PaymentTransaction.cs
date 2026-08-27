using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

/// <summary>
/// The only table allowed to move money state, and only from the IPN endpoint. Exactly one
/// of DepositId/PaymentBillId/RefundRequestId is set (CHECK constraint).
/// </summary>
public class PaymentTransaction : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public PaymentPurpose Purpose { get; set; }

    public Guid? DepositId { get; set; }
    public Deposit? Deposit { get; set; }

    public Guid? PaymentBillId { get; set; }
    public PaymentBill? PaymentBill { get; set; }

    public Guid? RefundRequestId { get; set; }
    public RefundRequest? RefundRequest { get; set; }

    public PaymentProvider Provider { get; set; }

    /// <summary>Order id we generate and send to the gateway.</summary>
    public string ProviderOrderId { get; set; } = null!;

    /// <summary>
    /// Gateway-side transaction id. Unique so a replayed IPN callback cannot be recorded
    /// twice (docs/domain-rules.md §9.7).
    /// </summary>
    public string? ProviderTxnId { get; set; }

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

    /// <summary>Raw callback body, kept verbatim for dispute resolution.</summary>
    public string? RawCallbackPayload { get; set; }

    /// <summary>A bill only becomes Paid when this is true (§9.8).</summary>
    public bool SignatureVerified { get; set; }

    public DateTimeOffset InitiatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class RefundRequest : Entity
{
    public Guid DepositId { get; set; }
    public Deposit Deposit { get; set; } = null!;

    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? Reason { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }
}

public class WithdrawRequest : Entity
{
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    /// <summary>May never exceed OwnerProfile.AvailableBalance (§9.11).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Bank details copied from the profile at request time — changing the profile later
    /// must not alter where an already-approved payout went.
    /// </summary>
    public string BankName { get; set; } = null!;
    public string BankAccountNumber { get; set; } = null!;
    public string BankAccountHolder { get; set; } = null!;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public Guid? ProcessedByUserId { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }
}
