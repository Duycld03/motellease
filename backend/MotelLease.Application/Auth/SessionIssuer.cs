using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;

namespace MotelLease.Application.Auth;

/// <summary>
/// Creates a session: one RefreshTokens row plus the access token bound to it. Shared by
/// every entry point that hands out tokens (register, login, Google login, refresh) so the
/// rules live in one place and every session is revocable (docs/features.md §3.6).
/// </summary>
public sealed class SessionIssuer(
    IAppDbContext database,
    ITokenService tokens,
    IRequestContext requestContext,
    TimeProvider clock)
{
    /// <summary>
    /// Adds the refresh token to the change tracker but does not save — the caller owns the
    /// transaction boundary, which matters during rotation where the replaced token is
    /// revoked in the same unit of work.
    /// </summary>
    public IssuedSession Issue(User user)
    {
        var now = clock.GetUtcNow();
        var pair = tokens.CreateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = pair.Hash,
            ExpiresAt = now.Add(tokens.RefreshTokenLifetime),
            UserAgent = Truncate(requestContext.UserAgent, 512),
            IpAddress = Truncate(requestContext.IpAddress, 45)
        };

        database.RefreshTokens.Add(refreshToken);

        var accessToken = tokens.CreateAccessToken(user, refreshToken.Id);

        var response = new AuthTokensResponse(
            accessToken.Value,
            "Bearer",
            accessToken.ExpiresInSeconds,
            pair.RawValue,
            refreshToken.ExpiresAt,
            Describe(user));

        return new IssuedSession(response, refreshToken);
    }

    public static AuthenticatedUser Describe(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FullName,
        user.Role,
        user.AvatarUrl,
        user.PreferredLanguage,
        user.EmailConfirmed);

    /// <summary>
    /// User agents can be far longer than the column. Storing a prefix beats failing the
    /// login with a database error.
    /// </summary>
    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>
/// <paramref name="RefreshToken"/> is the not-yet-saved entity, exposed so a rotating
/// caller can point the old row at it through ReplacedByTokenId.
/// </summary>
public sealed record IssuedSession(AuthTokensResponse Tokens, RefreshToken RefreshToken);
