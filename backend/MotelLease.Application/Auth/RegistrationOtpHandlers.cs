using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Auth;

/// <summary>
/// POST /auth/register/send-otp — proves the address exists before an account is created.
/// </summary>
public sealed class SendRegistrationOtpHandler(
    IAppDbContext database,
    OtpDispatcher dispatcher)
{
    public async Task<OtpSentResponse> HandleAsync(
        SendRegistrationOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize.Email(request.Email);

        // An address already in use is a conflict worth reporting: the visitor is trying to
        // register, not to discover whether someone else has an account.
        var taken = await database.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (taken)
        {
            throw new ConflictException(MessageKeys.Auth.EmailTaken);
        }

        var expiresIn = await dispatcher.SendAsync(
            OtpPurpose.Registration, email, cancellationToken);

        return new OtpSentResponse(expiresIn);
    }
}

/// <summary>
/// POST /auth/register/verify-otp — on success the address is remembered as verified for a
/// short window, so POST /auth/register can trust it without carrying the code along.
/// </summary>
public sealed class VerifyRegistrationOtpHandler(
    IOtpService otpService,
    IVerifiedEmailStore verifiedEmails,
    VerifiedEmailWindow window)
{
    public async Task<OtpVerifiedResponse> HandleAsync(
        VerifyRegistrationOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize.Email(request.Email);

        var result = await otpService.VerifyAsync(
            OtpPurpose.Registration, email, request.Code, cancellationToken);

        OtpDispatcher.EnsureValid(result);

        await verifiedEmails.MarkVerifiedAsync(email, cancellationToken);

        return new OtpVerifiedResponse(true, (int)window.Lifetime.TotalSeconds);
    }
}

/// <summary>
/// How long a verified address stays usable for registration. Long enough to fill a form,
/// short enough that a stale verification is not worth stealing.
/// </summary>
public sealed record VerifiedEmailWindow(TimeSpan Lifetime)
{
    public static readonly VerifiedEmailWindow Default = new(TimeSpan.FromMinutes(30));
}
