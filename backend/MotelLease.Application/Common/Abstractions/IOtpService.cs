namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// What an OTP is being issued for. Part of the storage key, so a registration code can
/// never be replayed against a password reset.
/// </summary>
public enum OtpPurpose
{
    Registration,
    PasswordReset,
    EmailChange
}

/// <summary>
/// One-time codes with resend throttling and an attempt limit — the old project had a
/// bare OTP send with neither (docs/features.md §3.7).
/// Codes live in a distributed cache rather than a table: they expire in minutes, so
/// losing them on restart is harmless and the ERD needs no extra entity.
/// </summary>
public interface IOtpService
{
    Task<OtpIssueResult> IssueAsync(
        OtpPurpose purpose,
        string subject,
        CancellationToken cancellationToken = default);

    Task<OtpVerifyResult> VerifyAsync(
        OtpPurpose purpose,
        string subject,
        string code,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// <paramref name="Code"/> is set only when <paramref name="Issued"/> is true. When a
/// resend comes too early, <paramref name="RetryAfter"/> says how long to wait.
/// </summary>
public sealed record OtpIssueResult(
    bool Issued,
    string? Code,
    TimeSpan? RetryAfter,
    TimeSpan Lifetime);

public enum OtpVerifyResult
{
    Valid,

    /// <summary>No code on file, or it expired.</summary>
    NotFound,

    Mismatch,

    /// <summary>Attempt limit reached; the code was discarded and a new one is needed.</summary>
    TooManyAttempts
}

/// <summary>
/// Remembers that an address proved ownership, so registration can be a separate request
/// from OTP verification without trusting the client's word for it.
/// </summary>
public interface IVerifiedEmailStore
{
    Task MarkVerifiedAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsVerifiedAsync(string email, CancellationToken cancellationToken = default);

    Task ClearAsync(string email, CancellationToken cancellationToken = default);
}
