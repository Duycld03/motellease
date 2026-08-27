using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Auth;

/// <summary>
/// POST /auth/refresh. Rotates: the presented token is revoked and pointed at its
/// replacement, so presenting it a second time proves the value leaked and the whole chain
/// is dropped (docs/features.md §3.6).
/// </summary>
public sealed class RefreshTokenHandler(
    IAppDbContext database,
    ITokenService tokens,
    SessionIssuer sessionIssuer,
    TimeProvider clock)
{
    public async Task<AuthTokensResponse> HandleAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var now = clock.GetUtcNow();

        await using var transaction = await database.BeginTransactionAsync(cancellationToken);

        var stored = await database.RefreshTokens
            .Include(t => t.User)
            // Without this the soft-delete filter would leave User null on a deleted account
            // and the checks below would never run. The IsDeleted case is handled explicitly.
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new AuthenticationException(MessageKeys.Auth.RefreshTokenInvalid);

        if (stored.RevokedAt is not null)
        {
            // A token that was rotated has a successor. Seeing it again means two holders
            // exist, so every live token for this user is dropped — whoever copied the value
            // is signed out along with the legitimate device.
            if (stored.ReplacedByTokenId is not null)
            {
                await RevokeAllForUserAsync(stored.UserId, now, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                throw new AuthenticationException(MessageKeys.Auth.RefreshTokenReused);
            }

            // Revoked without a successor: logout, "sign this device out", or a password
            // change did it. That is not evidence of a leak, and treating it as one would let
            // a stale client sign every other device out by retrying once.
            throw new AuthenticationException(MessageKeys.Auth.RefreshTokenInvalid);
        }

        if (stored.ExpiresAt <= now)
        {
            throw new AuthenticationException(MessageKeys.Auth.RefreshTokenInvalid);
        }

        // A user locked after the token was issued must not be able to extend the session.
        if (stored.User.IsLocked || stored.User.IsDeleted)
        {
            throw new ForbiddenException(MessageKeys.Auth.AccountLocked);
        }

        var session = sessionIssuer.Issue(stored.User);

        stored.RevokedAt = now;
        stored.ReplacedByTokenId = session.RefreshToken.Id;

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return session.Tokens;
    }

    private async Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var live = await database.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in live)
        {
            token.RevokedAt = now;
        }
    }
}

/// <summary>
/// POST /auth/logout. Revokes the presented refresh token only — other devices stay signed
/// in, which is what a user expects from "log out" on one phone.
/// </summary>
public sealed class LogoutHandler(
    IAppDbContext database,
    ITokenService tokens,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task HandleAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var hash = tokens.HashRefreshToken(request.RefreshToken);

        var stored = await database.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash && t.UserId == userId && t.RevokedAt == null,
                cancellationToken);

        // Logging out something already invalid is not an error: the caller's goal is met.
        if (stored is null)
        {
            return;
        }

        stored.RevokedAt = clock.GetUtcNow();

        await database.SaveChangesAsync(cancellationToken);
    }
}
