using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Accounts;

/// <summary>
/// GET /me/sessions — the "signed-in devices" list. Each live refresh token is one device,
/// which is what makes the list possible at all (docs/features.md §3.6).
/// </summary>
public sealed class GetSessionsHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<SessionResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = clock.GetUtcNow();
        var currentSessionId = currentUser.SessionId;

        var sessions = await database.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionResponse(
                t.Id,
                t.UserAgent,
                t.IpAddress,
                t.CreatedAt,
                t.ExpiresAt,
                t.Id == currentSessionId))
            .ToListAsync(cancellationToken);

        return sessions;
    }
}

/// <summary>
/// DELETE /me/sessions/{id} — signs one device out. Scoped to the caller's own tokens, so an
/// id guessed from another account cannot be revoked.
/// </summary>
public sealed class RevokeSessionHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task HandleAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var session = await database.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.Id == sessionId && t.UserId == userId && t.RevokedAt == null,
                cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.SessionNotFound);

        session.RevokedAt = clock.GetUtcNow();

        await database.SaveChangesAsync(cancellationToken);
    }
}
