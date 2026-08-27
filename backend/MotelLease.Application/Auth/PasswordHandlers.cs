using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;

namespace MotelLease.Application.Auth;

/// <summary>
/// POST /auth/password/forgot. Answers the same way whether or not the address is on file —
/// the response must not become an account-existence oracle.
/// </summary>
public sealed class ForgotPasswordHandler(
    IAppDbContext database,
    OtpDispatcher dispatcher,
    IOtpService otpService)
{
    public async Task<OtpSentResponse> HandleAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize.Email(request.Email);

        var exists = await database.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
        {
            var expiresIn = await dispatcher.SendAsync(
                OtpPurpose.PasswordReset, email, cancellationToken);

            return new OtpSentResponse(expiresIn);
        }

        // Unknown address: no mail is sent, but the shape and timing of the answer match the
        // success path, including the throttle — otherwise the throttle itself leaks whether
        // the account exists.
        var throttle = await otpService.IssueAsync(
            OtpPurpose.PasswordReset, email, cancellationToken);

        if (!throttle.Issued)
        {
            var retryAfter = throttle.RetryAfter ?? TimeSpan.FromMinutes(1);

            throw new TooManyRequestsException(
                retryAfter,
                MessageKeys.Otp.ResendTooSoon,
                (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        return new OtpSentResponse((int)throttle.Lifetime.TotalSeconds);
    }
}

/// <summary>
/// POST /auth/password/reset. Also the way a Google-only account gains a password, since the
/// address is proven by the code.
/// </summary>
public sealed class ResetPasswordHandler(
    IAppDbContext database,
    IOtpService otpService,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    public async Task HandleAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize.Email(request.Email);

        var result = await otpService.VerifyAsync(
            OtpPurpose.PasswordReset, email, request.Code, cancellationToken);

        OtpDispatcher.EnsureValid(result);

        var user = await database.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // Whoever else was signed in may be the reason for the reset, so no session survives.
        await RevokeSessions.AllAsync(database, user.Id, clock.GetUtcNow(), null, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PUT /auth/password. The current session is kept so changing a password does not sign the
/// user out of the device they are using.
/// </summary>
public sealed class ChangePasswordHandler(
    IAppDbContext database,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task HandleAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await database.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        // A Google-only account has no current password to check against; it has to go
        // through the reset flow, which proves the address instead.
        if (UnusablePassword.IsUnusable(user.PasswordHash))
        {
            throw new BusinessRuleException(MessageKeys.Auth.PasswordNotSet);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthenticationException(MessageKeys.Auth.CurrentPasswordWrong);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new BusinessRuleException(MessageKeys.Auth.NewPasswordSameAsCurrent);
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await RevokeSessions.AllAsync(
            database, user.Id, clock.GetUtcNow(), currentUser.SessionId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
    }
}

internal static class RevokeSessions
{
    /// <summary>
    /// Revokes every live refresh token for a user, optionally sparing one session. Marks the
    /// entities only — the caller saves, so this composes with the rest of the use case.
    /// </summary>
    internal static async Task AllAsync(
        IAppDbContext database,
        Guid userId,
        DateTimeOffset now,
        Guid? exceptSessionId,
        CancellationToken cancellationToken)
    {
        var live = await database.RefreshTokens
            .Where(t => t.UserId == userId
                        && t.RevokedAt == null
                        && (exceptSessionId == null || t.Id != exceptSessionId))
            .ToListAsync(cancellationToken);

        foreach (var token in live)
        {
            token.RevokedAt = now;
        }
    }
}
