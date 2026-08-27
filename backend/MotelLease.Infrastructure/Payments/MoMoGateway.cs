using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Domain.Enums;

namespace MotelLease.Infrastructure.Payments;

/// <summary>
/// MoMo, API v2. Deliberately not modelled on <see cref="VnPayGateway"/>: the two protocols agree on
/// almost nothing beyond both being HMAC-signed, and pretending otherwise would mean one class of
/// special cases instead of two straightforward ones.
///
/// Three differences drive the shape of this class. A payment is not a URL we can assemble — MoMo is
/// asked for one over its own API, which is why creating one is asynchronous. The signed string is a
/// fixed list of named fields in a documented order rather than whatever happened to be in the
/// request, so an unexpected extra field cannot change the digest. And the amount is plain VND, not
/// the smallest unit.
/// </summary>
public sealed class MoMoGateway(
    IHttpClientFactory httpClients,
    IOptions<MoMoOptions> options,
    IOptions<AppUrlOptions> urls,
    TimeProvider time) : IPaymentGateway
{
    /// <summary>
    /// The named client this gateway sends through. Created per call from the factory rather than held
    /// for the lifetime of the process: this class is a singleton, and a pinned HttpClient would keep
    /// using a connection whose DNS entry has since moved.
    /// </summary>
    public const string HttpClientName = "MoMo";

    private readonly MoMoOptions _options = options.Value;

    private readonly string _returnUrl =
        $"{urls.Value.ApiBaseUrl.TrimEnd('/')}/api/v1/payments/momo/return";

    private readonly string _ipnUrl =
        $"{urls.Value.ApiBaseUrl.TrimEnd('/')}/api/v1/payments/momo/ipn";

    public PaymentProvider Provider => PaymentProvider.MoMo;

    public async Task<string> CreatePaymentUrlAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var partnerCode = Require(_options.PartnerCode, nameof(MoMoOptions.PartnerCode));
        var accessKey = Require(_options.AccessKey, nameof(MoMoOptions.AccessKey));
        var amount = ((long)decimal.Round(request.Amount, 0)).ToString(CultureInfo.InvariantCulture);

        // MoMo distinguishes the order from the attempt on it: orderId is ours to keep unique, and
        // requestId identifies this one call about it.
        var requestId = $"{request.OrderId}-{Guid.CreateVersion7():N}"[..48];

        var signature = Sign(
            $"accessKey={accessKey}" +
            $"&amount={amount}" +
            "&extraData=" +
            $"&ipnUrl={_ipnUrl}" +
            $"&orderId={request.OrderId}" +
            $"&orderInfo={request.Description}" +
            $"&partnerCode={partnerCode}" +
            $"&redirectUrl={_returnUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={_options.RequestType}");

        var body = new Dictionary<string, object>
        {
            ["partnerCode"] = partnerCode,
            ["requestId"] = requestId,
            ["amount"] = amount,
            ["orderId"] = request.OrderId,
            ["orderInfo"] = request.Description,
            ["redirectUrl"] = _returnUrl,
            ["ipnUrl"] = _ipnUrl,
            ["requestType"] = _options.RequestType,
            ["extraData"] = string.Empty,
            ["lang"] = "vi",
            // Whole minutes, and at least one: MoMo rejects a window that has already closed, and the
            // caller's deadline can be seconds away by the time it reaches here.
            ["orderExpireTime"] = Math.Max(
                1, (int)Math.Ceiling((request.ExpiresAt - time.GetUtcNow()).TotalMinutes)),
            ["signature"] = signature
        };

        var response = await httpClients
            .CreateClient(HttpClientName)
            .PostAsJsonAsync(_options.CreateUrl, body, cancellationToken);

        // A gateway that will not open a payment is not an unexpected failure — it is an answer, and
        // the tenant can be told to try again or pick the other provider.
        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessRuleException(MessageKeys.Payment.GatewayRejected);
        }

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return created.TryGetProperty("resultCode", out var resultCode)
               && resultCode.GetInt32() == 0
               && created.TryGetProperty("payUrl", out var payUrl)
               && payUrl.GetString() is { Length: > 0 } url
            ? url
            : throw new BusinessRuleException(MessageKeys.Payment.GatewayRejected);
    }

    public GatewayCallback ReadCallback(IReadOnlyDictionary<string, string> fields)
    {
        var accessKey = Require(_options.AccessKey, nameof(MoMoOptions.AccessKey));

        // The exact field list MoMo signs a callback with, in the order it documents. Fixed rather
        // than derived from what arrived: a caller who adds a field must not be able to change the
        // digest, and one who omits a signed field must fail rather than sign a shorter string.
        var expected = Sign(
            $"accessKey={accessKey}" +
            $"&amount={Field(fields, "amount")}" +
            $"&extraData={Field(fields, "extraData")}" +
            $"&message={Field(fields, "message")}" +
            $"&orderId={Field(fields, "orderId")}" +
            $"&orderInfo={Field(fields, "orderInfo")}" +
            $"&orderType={Field(fields, "orderType")}" +
            $"&partnerCode={Field(fields, "partnerCode")}" +
            $"&payType={Field(fields, "payType")}" +
            $"&requestId={Field(fields, "requestId")}" +
            $"&responseTime={Field(fields, "responseTime")}" +
            $"&resultCode={Field(fields, "resultCode")}" +
            $"&transId={Field(fields, "transId")}");

        var provided = fields.GetValueOrDefault("signature");

        return new GatewayCallback(
            OrderId: fields.GetValueOrDefault("orderId"),
            ProviderTxnId: fields.GetValueOrDefault("transId"),
            Amount: ReadAmount(fields.GetValueOrDefault("amount")),
            SignatureVerified: provided is not null
                && CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(provided.ToLowerInvariant()),
                    Encoding.ASCII.GetBytes(expected)),
            // Zero, and only zero, is a completed payment; every other code is a reason it is not.
            Succeeded: fields.GetValueOrDefault("resultCode") == "0",
            RawPayload: JsonSerializer.Serialize(fields),
            ResponseCode: fields.GetValueOrDefault("resultCode"));
    }

    private static string Field(IReadOnlyDictionary<string, string> fields, string name) =>
        fields.GetValueOrDefault(name, string.Empty);

    /// <summary>Plain VND, unlike VNPay: nothing to scale back.</summary>
    private static decimal? ReadAmount(string? raw) =>
        long.TryParse(raw, CultureInfo.InvariantCulture, out var amount) ? amount : null;

    private string Sign(string raw) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(Require(_options.SecretKey, nameof(MoMoOptions.SecretKey))),
            Encoding.UTF8.GetBytes(raw)));

    private static string Require(string? value, string name) =>
        value ?? throw new InvalidOperationException($"MoMo:{name} is not configured.");
}
