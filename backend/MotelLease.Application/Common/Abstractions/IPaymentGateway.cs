using MotelLease.Domain.Enums;

namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// One payment gateway, as the use-case handlers see it. Two things are asked of it: build the URL
/// the tenant is sent to, and read a callback the gateway sent back. Both are provider-specific in
/// their wire format and identical in meaning, which is why they sit behind this interface — the
/// handlers must not know whether the signature was HMAC-SHA512 over a sorted query string or
/// SHA256 over a fixed field order.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    string BuildPaymentUrl(GatewayPaymentRequest request);

    /// <summary>
    /// Reads a callback. Never throws on a bad signature: an unverified callback is a fact the
    /// handler has to record and answer, not an exception to swallow.
    /// </summary>
    GatewayCallback ReadCallback(IReadOnlyDictionary<string, string> fields);
}

/// <param name="OrderId">Our own id for the attempt, echoed back by the gateway.</param>
/// <param name="Amount">In VND. The provider decides how to encode it on the wire.</param>
/// <param name="ExpiresAt">
/// When the gateway should stop accepting the payment. Set so a tenant cannot pay into an attempt
/// the application has already given up on.
/// </param>
/// <param name="IpAddress">The payer's address, which the gateways require in the request.</param>
public sealed record GatewayPaymentRequest(
    string OrderId,
    decimal Amount,
    string Description,
    DateTimeOffset ExpiresAt,
    string IpAddress);

/// <param name="SignatureVerified">
/// Whether the payload was signed with our secret. Money state may only move when this is true
/// (docs/domain-rules.md §9.8).
/// </param>
/// <param name="Succeeded">Whether the gateway is reporting a completed payment.</param>
/// <param name="ProviderTxnId">
/// The gateway's own id for the transaction. Recorded once, which is what stops a replayed callback
/// from being counted twice (§9.7).
/// </param>
/// <param name="RawPayload">Kept verbatim on the row for dispute resolution.</param>
public sealed record GatewayCallback(
    string? OrderId,
    string? ProviderTxnId,
    decimal? Amount,
    bool SignatureVerified,
    bool Succeeded,
    string RawPayload,
    string? ResponseCode);
