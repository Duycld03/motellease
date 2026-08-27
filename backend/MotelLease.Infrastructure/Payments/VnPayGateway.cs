using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Enums;

namespace MotelLease.Infrastructure.Payments;

/// <summary>
/// VNPay, version 2.1.0. The whole protocol is a query string signed with HMAC-SHA512 over its own
/// parameters sorted by name, so both directions come down to building that string the same way —
/// <see cref="Sign"/> is used to produce a request and to check a callback, because a verifier that
/// reconstructs the string differently from the signer is a verifier that passes anything.
/// </summary>
public sealed class VnPayGateway(
    IOptions<VnPayOptions> options,
    IOptions<AppUrlOptions> urls,
    TimeProvider time) : IPaymentGateway
{
    /// <summary>
    /// VNPay states its timestamps in GMT+7. A fixed offset rather than a named zone: Vietnam has no
    /// daylight saving, and a missing tzdata entry on a slim container image must not break payments.
    /// </summary>
    private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);

    private const string TimestampFormat = "yyyyMMddHHmmss";
    private const string SecureHashField = "vnp_SecureHash";
    private const string SecureHashTypeField = "vnp_SecureHashType";

    /// <summary>Both must read 00 before a payment counts as completed.</summary>
    private const string SuccessCode = "00";

    private readonly VnPayOptions _options = options.Value;

    /// <summary>
    /// Where VNPay sends the payer's browser afterwards. Our own endpoint rather than the frontend
    /// directly: it verifies the signature before redirecting, and it writes nothing, because the
    /// user controls the URL they land on (CLAUDE.md, Hard prohibitions).
    /// </summary>
    private readonly string _returnUrl =
        $"{urls.Value.ApiBaseUrl.TrimEnd('/')}/api/v1/payments/vnpay/return";

    public PaymentProvider Provider => PaymentProvider.VNPay;

    public Task<string> CreatePaymentUrlAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().ToOffset(Vietnam);

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode
                ?? throw new InvalidOperationException("VnPay:TmnCode is not configured."),
            // VNPay takes the amount in the smallest unit, so VND is multiplied by 100. Rounded to
            // a whole unit first: a fractional dong cannot be sent and must not be silently dropped.
            ["vnp_Amount"] = ((long)decimal.Round(request.Amount, 0) * 100)
                .ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.OrderId,
            ["vnp_OrderInfo"] = request.Description,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _returnUrl,
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = now.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            ["vnp_ExpireDate"] = request.ExpiresAt
                .ToOffset(Vietnam)
                .ToString(TimestampFormat, CultureInfo.InvariantCulture)
        };

        var query = Canonical(fields);

        // Nothing to await: VNPay's whole request is a signed query string built here.
        return Task.FromResult($"{_options.PaymentUrl}?{query}&{SecureHashField}={Sign(query)}");
    }

    public GatewayCallback ReadCallback(IReadOnlyDictionary<string, string> fields)
    {
        var signed = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in fields)
        {
            // The hash itself and the hash type are not part of what was signed. Anything not
            // prefixed vnp_ was not sent by VNPay and must not be allowed to alter the digest.
            if (key is SecureHashField or SecureHashTypeField
                || !key.StartsWith("vnp_", StringComparison.Ordinal))
            {
                continue;
            }

            signed[key] = value;
        }

        var provided = fields.GetValueOrDefault(SecureHashField);
        var expected = Sign(Canonical(signed));

        return new GatewayCallback(
            OrderId: fields.GetValueOrDefault("vnp_TxnRef"),
            ProviderTxnId: fields.GetValueOrDefault("vnp_TransactionNo"),
            Amount: ReadAmount(fields.GetValueOrDefault("vnp_Amount")),
            SignatureVerified: provided is not null
                && CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(provided.ToLowerInvariant()),
                    Encoding.ASCII.GetBytes(expected)),
            Succeeded: fields.GetValueOrDefault("vnp_ResponseCode") == SuccessCode
                       && fields.GetValueOrDefault("vnp_TransactionStatus") == SuccessCode,
            RawPayload: JsonSerializer.Serialize(fields),
            ResponseCode: fields.GetValueOrDefault("vnp_ResponseCode"));
    }

    /// <summary>
    /// The exact string VNPay signs: parameters in ascending name order, values URL-encoded, joined
    /// with &amp;. Empty values are dropped, which is what the gateway does on its side.
    /// </summary>
    private static string Canonical(SortedDictionary<string, string> fields) =>
        string.Join(
            '&',
            fields
                .Where(f => !string.IsNullOrEmpty(f.Value))
                .Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));

    private string Sign(string canonical)
    {
        var secret = _options.HashSecret
            ?? throw new InvalidOperationException("VnPay:HashSecret is not configured.");

        return Convert.ToHexStringLower(
            HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>Back from the smallest unit into VND, so the caller can compare it to the row.</summary>
    private static decimal? ReadAmount(string? raw) =>
        long.TryParse(raw, CultureInfo.InvariantCulture, out var amount) ? amount / 100m : null;
}
