namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// Validates a Google ID token against Google's published keys. Returning null means the
/// token is invalid, expired, or issued for another client — never a partial result.
/// </summary>
public interface IGoogleTokenVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// <paramref name="Subject"/> is Google's stable user id, stored as User.SocialId. Email
/// can change on the Google side, the subject cannot, so it is the join key.
/// </summary>
public sealed record GoogleIdentity(
    string Subject,
    string Email,
    bool EmailVerified,
    string? FullName,
    string? PictureUrl);
