using MotelLease.Domain.Entities;

namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// Issues access tokens and the raw/hashed pair for refresh tokens. Only the hash is ever
/// persisted, so a leaked RefreshTokens table cannot be replayed (docs/erd.md §1).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// <paramref name="sessionId"/> is the RefreshTokens row this access token belongs to.
    /// It rides along as the <c>sid</c> claim so /me/sessions can mark the current device
    /// and a password change can spare the session doing the changing.
    /// </summary>
    AccessToken CreateAccessToken(User user, Guid sessionId);

    /// <summary>
    /// Returns the value handed to the client and the hash to store. The raw value exists
    /// only in this response and is never recoverable from the database.
    /// </summary>
    RefreshTokenPair CreateRefreshToken();

    string HashRefreshToken(string rawRefreshToken);

    TimeSpan RefreshTokenLifetime { get; }
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt, int ExpiresInSeconds);

public sealed record RefreshTokenPair(string RawValue, string Hash);
