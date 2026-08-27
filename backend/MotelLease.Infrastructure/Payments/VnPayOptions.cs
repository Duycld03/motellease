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
