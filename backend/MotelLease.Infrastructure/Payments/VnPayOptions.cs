using System.ComponentModel.DataAnnotations;

namespace MotelLease.Infrastructure.Payments;

public sealed class VnPayOptions
{
    public const string SectionName = "VnPay";

    /// <summary>The merchant website code VNPay issues. Sent in the clear as vnp_TmnCode.</summary>
    public string? TmnCode { get; set; }

    /// <summary>
    /// The shared secret behind every signature. Never committed: user-secrets locally and
    /// GitHub Actions secrets in CI (CLAUDE.md).
    /// </summary>
    public string? HashSecret { get; set; }

    [Required]
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret);
}

/// <summary>
/// Where this deployment lives. The gateway needs an absolute URL to send the payer back to, and
/// that endpoint needs a page to redirect them on to.
/// </summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    [Required]
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    [Required]
    public string WebBaseUrl { get; set; } = "http://localhost:3000";
}

public sealed class MoMoOptions
{
    public const string SectionName = "MoMo";

    /// <summary>Merchant identifier, sent in the clear.</summary>
    public string? PartnerCode { get; set; }

    /// <summary>
    /// Part of every signed string but never sent as a field of its own — MoMo folds it into the
    /// digest so a payload cannot be reconstructed by someone who only saw one go past.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>The signing secret. Never committed (CLAUDE.md).</summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Unlike VNPay, a MoMo payment is not a URL we can assemble: this endpoint is asked for one.
    /// </summary>
    [Required]
    public string CreateUrl { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    /// <summary>
    /// Which MoMo product the payment goes through. <c>captureWallet</c> is the hosted page that
    /// covers both the app and the QR code, which is what a web checkout wants.
    /// </summary>
    [Required]
    public string RequestType { get; set; } = "captureWallet";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PartnerCode)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey);
}
