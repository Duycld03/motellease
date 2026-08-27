using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Auth;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Accounts;

/// <summary>
/// POST /me/email/send-otp. The code goes to the new address, which is the only way to prove
/// the caller can receive mail there.
/// </summary>
public sealed class SendEmailChangeOtpHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    OtpDispatcher dispatcher)
{
    public async Task<OtpSentResponse> HandleAsync(
        SendEmailChangeOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var newEmail = Normalize.Email(request.NewEmail);

        var user = await database.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        if (user.Email == newEmail)
        {
            throw new BusinessRuleException(MessageKeys.Account.EmailUnchanged);
        }

        if (await database.Users.AnyAsync(u => u.Email == newEmail, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.EmailTaken);
        }

        // Keyed by the target address, so a code sent to one address cannot confirm another.
        var expiresIn = await dispatcher.SendAsync(
            OtpPurpose.EmailChange, newEmail, cancellationToken);

        return new OtpSentResponse(expiresIn);
    }
}

/// <summary>POST /me/email/verify-otp — commits the new address.</summary>
public sealed class VerifyEmailChangeOtpHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    IOtpService otpService)
{
    public async Task HandleAsync(
        VerifyEmailChangeOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var newEmail = Normalize.Email(request.NewEmail);

        var result = await otpService.VerifyAsync(
            OtpPurpose.EmailChange, newEmail, request.Code, cancellationToken);

        OtpDispatcher.EnsureValid(result);

        var user = await database.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        // Re-checked at commit time: another account may have taken the address while this
        // one was reading its mail.
        if (await database.Users.AnyAsync(
                u => u.Email == newEmail && u.Id != userId, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.EmailTaken);
        }

        user.Email = newEmail;
        user.EmailConfirmed = true;

        await database.SaveChangesAsync(cancellationToken);
    }
}
