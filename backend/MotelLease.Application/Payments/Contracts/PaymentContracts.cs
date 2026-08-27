using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments.Contracts;

public sealed record StartPaymentRequest(PaymentProvider Provider);

/// <summary>
/// Where to send the tenant, and until when. The transaction id is returned so a client can poll or
/// display the attempt without having to parse the gateway URL.
/// </summary>
public sealed record PaymentCheckoutResponse(
    Guid TransactionId,
    string ProviderOrderId,
    PaymentProvider Provider,
    decimal Amount,
    DateTimeOffset ExpiresAt,
    string PaymentUrl);

public sealed record PaymentTransactionResponse(
    Guid Id,
    Guid UserId,
    PaymentPurpose Purpose,
    PaymentProvider Provider,
    string ProviderOrderId,
    string? ProviderTxnId,
    decimal Amount,
    PaymentStatus Status,
    bool SignatureVerified,
    Guid? DepositId,
    Guid? PaymentBillId,
    Guid? RefundRequestId,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// What a callback was found to be, independently of how any one gateway spells it. The IPN handler
/// answers in this vocabulary and each provider's endpoint translates it into that provider's own
/// wire format — VNPay reads a code in a JSON body, MoMo reads the HTTP status.
/// </summary>
public enum PaymentConfirmation
{
    /// <summary>Understood and applied, whether the payment itself succeeded or failed.</summary>
    Confirmed,

    /// <summary>Names an attempt this system never opened.</summary>
    OrderNotFound,

    /// <summary>A retry of a callback already settled. Nothing was changed.</summary>
    AlreadyConfirmed,

    /// <summary>Signed, but for a different amount than the one agreed.</summary>
    InvalidAmount,

    /// <summary>Not signed with our secret, so it proves nothing about who sent it.</summary>
    InvalidSignature
}

/// <summary>
/// VNPay's acknowledgement body. It reads <c>RspCode</c> and retries unless it is one of the codes
/// that mean "we are done with this one", so the value is part of the protocol rather than a
/// convenience: answering the wrong code makes the gateway either give up on a payment or keep
/// resending one that was already recorded.
/// </summary>
public sealed record VnPayAcknowledgement(string RspCode, string Message)
{
    public static VnPayAcknowledgement For(PaymentConfirmation confirmation) => confirmation switch
    {
        PaymentConfirmation.Confirmed => new("00", "Confirm Success"),
        PaymentConfirmation.OrderNotFound => new("01", "Order not found"),
        PaymentConfirmation.AlreadyConfirmed => new("02", "Order already confirmed"),
        PaymentConfirmation.InvalidAmount => new("04", "Invalid amount"),
        PaymentConfirmation.InvalidSignature => new("97", "Invalid signature"),
        _ => new("99", "Unknown error")
    };
}
