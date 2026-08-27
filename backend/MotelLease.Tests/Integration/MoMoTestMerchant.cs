using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MotelLease.Tests.Integration;

/// <summary>
/// A MoMo merchant account the test host is configured with, plus callbacks signed the way MoMo signs
/// them: HMAC-SHA256 over a fixed list of named fields in the documented order, with the access key
/// folded in although it is never sent as a field.
///
/// Written out here rather than reused from <c>MoMoGateway</c>, for the same reason the VNPay helper
/// is: a verifier checked against its own signer agrees with itself no matter what either of them
/// does. The two helpers stay separate because the two protocols are — nothing about this digest
/// carries over to VNPay's.
/// </summary>
internal static class MoMoTestMerchant
{
    internal const string PartnerCode = "MOMOTEST01";
    internal const string AccessKey = "momo-test-access-key";
    internal const string SecretKey = "momo-test-secret-key-0123456789";

    /// <summary>A callback body reporting a completed payment. Result code 0, and only 0, is one.</summary>
    internal static Dictionary<string, object> Callback(
        string orderId,
        decimal amount,
        string transId,
        int resultCode = 0)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["partnerCode"] = PartnerCode,
            ["orderId"] = orderId,
            ["requestId"] = $"{orderId}-request",
            ["amount"] = ((long)decimal.Round(amount, 0)).ToString(CultureInfo.InvariantCulture),
            ["orderInfo"] = "Tien coc phong 101",
            ["orderType"] = "momo_wallet",
            ["transId"] = transId,
            ["resultCode"] = resultCode.ToString(CultureInfo.InvariantCulture),
            ["message"] = resultCode == 0 ? "Successful." : "Failed.",
            ["payType"] = "qr",
            ["responseTime"] = "1774608000000",
            ["extraData"] = string.Empty
        };

        var body = fields.ToDictionary(f => f.Key, f => (object)f.Value, StringComparer.Ordinal);

        body["signature"] = Sign(fields);

        return body;
    }

    /// <summary>The same callback with the digest broken, standing in for anything unsigned.</summary>
    internal static Dictionary<string, object> TamperedCallback(
        string orderId,
        decimal amount,
        string transId)
    {
        var body = Callback(orderId, amount, transId);

        body["signature"] = new string('0', 64);

        return body;
    }

    private static string Sign(Dictionary<string, string> f)
    {
        var raw =
            $"accessKey={AccessKey}" +
            $"&amount={f["amount"]}" +
            $"&extraData={f["extraData"]}" +
            $"&message={f["message"]}" +
            $"&orderId={f["orderId"]}" +
            $"&orderInfo={f["orderInfo"]}" +
            $"&orderType={f["orderType"]}" +
            $"&partnerCode={f["partnerCode"]}" +
            $"&payType={f["payType"]}" +
            $"&requestId={f["requestId"]}" +
            $"&responseTime={f["responseTime"]}" +
            $"&resultCode={f["resultCode"]}" +
            $"&transId={f["transId"]}";

        return Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SecretKey), Encoding.UTF8.GetBytes(raw)));
    }
}
