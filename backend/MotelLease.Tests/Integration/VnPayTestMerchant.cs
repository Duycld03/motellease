using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MotelLease.Tests.Integration;

/// <summary>
/// A merchant account the test host is configured with, plus callbacks signed the way VNPay signs
/// them. The canonical string and the HMAC are written out here rather than reused from
/// <c>VnPayGateway</c> on purpose: a verifier checked against its own signer agrees with itself no
/// matter what either of them does, so the signature has to be produced independently for the check
/// to mean anything.
/// </summary>
internal static class VnPayTestMerchant
{
    internal const string TmnCode = "TESTTMN1";
    internal const string HashSecret = "integration-test-vnpay-hash-secret-0123456789";

    internal const string SuccessCode = "00";

    /// <summary>A callback that reports a completed payment, signed and ready to send.</summary>
    internal static string IpnQuery(
        string orderId,
        decimal amount,
        string transactionNo,
        string responseCode = SuccessCode,
        string transactionStatus = SuccessCode)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = ((long)decimal.Round(amount, 0) * 100)
                .ToString(CultureInfo.InvariantCulture),
            ["vnp_BankCode"] = "NCB",
            ["vnp_CardType"] = "ATM",
            ["vnp_OrderInfo"] = "Tien coc phong 101",
            ["vnp_PayDate"] = "20260827120000",
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TmnCode"] = TmnCode,
            ["vnp_TransactionNo"] = transactionNo,
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TxnRef"] = orderId
        };

        var canonical = Canonical(fields);

        return $"?{canonical}&vnp_SecureHash={Sign(canonical)}";
    }

    /// <summary>The same callback with the digest broken, standing in for anything unsigned.</summary>
    internal static string TamperedIpnQuery(string orderId, decimal amount, string transactionNo)
    {
        var query = IpnQuery(orderId, amount, transactionNo);
        var index = query.IndexOf("&vnp_SecureHash=", StringComparison.Ordinal);

        return $"{query[..index]}&vnp_SecureHash={new string('0', 128)}";
    }

    private static string Canonical(SortedDictionary<string, string> fields) =>
        string.Join(
            '&',
            fields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));

    private static string Sign(string canonical) =>
        Convert.ToHexStringLower(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes(HashSecret),
            Encoding.UTF8.GetBytes(canonical)));
}
