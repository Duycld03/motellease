using System.ComponentModel.DataAnnotations;

namespace MotelLease.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    /// <summary>User-secrets locally, GitHub Actions secrets in CI — never committed.</summary>
    public string? Password { get; set; }

    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@motellease.local";

    public string FromName { get; set; } = "MotelLease";

    /// <summary>
    /// STARTTLS on the submission port. Turned off only for a local mail catcher, which is
    /// also the only case where credentials are absent.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
