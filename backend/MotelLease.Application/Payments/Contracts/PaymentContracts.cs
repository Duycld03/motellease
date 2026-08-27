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
/// What an IPN endpoint answers. VNPay reads <c>RspCode</c> and retries unless it is one of the
/// codes that mean "we are done with this one", so the value is part of the protocol rather than a
/// convenience: answering the wrong code makes the gateway either give up on a payment or keep
/// resending one that was already recorded.
/// </summary>
public sealed record GatewayAcknowledgement(string RspCode, string Message)
{
    public static readonly GatewayAcknowledgement Confirmed = new("00", "Confirm Success");
    public static readonly GatewayAcknowledgement OrderNotFound = new("01", "Order not found");
    public static readonly GatewayAcknowledgement AlreadyConfirmed = new("02", "Order already confirmed");
    public static readonly GatewayAcknowledgement InvalidAmount = new("04", "Invalid amount");
    public static readonly GatewayAcknowledgement InvalidSignature = new("97", "Invalid signature");
    public static readonly GatewayAcknowledgement Failed = new("99", "Unknown error");
}
