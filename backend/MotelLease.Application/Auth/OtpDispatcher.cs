using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Auth;

/// <summary>
/// Issues a code and emails it. Shared by registration, password reset and email change so
/// throttling and the "never reveal whether the address exists" rule cannot drift apart.
/// </summary>
public sealed class OtpDispatcher(
    IOtpService otpService,
    IEmailSender emailSender,
    ILocalizer localizer,
    IRequestContext requestContext)
{
    public async Task<int> SendAsync(
        OtpPurpose purpose,
        string email,
        CancellationToken cancellationToken)
    {
        var issue = await otpService.IssueAsync(purpose, email, cancellationToken);

        if (!issue.Issued)
        {
            var retryAfter = issue.RetryAfter ?? TimeSpan.FromMinutes(1);

            throw new TooManyRequestsException(
                retryAfter,
                MessageKeys.Otp.ResendTooSoon,
                (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        var language = requestContext.Language;
        var (subjectKey, bodyKey) = TemplateFor(purpose);
        var minutes = (int)Math.Round(issue.Lifetime.TotalMinutes);

        await emailSender.SendAsync(
            new EmailMessage(
                email,
                localizer.Get(subjectKey, language),
                localizer.Get(bodyKey, language, issue.Code!, minutes)),
            cancellationToken);

        return (int)issue.Lifetime.TotalSeconds;
    }

    /// <summary>
    /// Turns a verification outcome into the matching HTTP failure. Kept here so all three
    /// verify endpoints answer identically.
    /// </summary>
    public static void EnsureValid(OtpVerifyResult result)
    {
        switch (result)
        {
            case OtpVerifyResult.Valid:
                return;
            case OtpVerifyResult.TooManyAttempts:
                throw new BusinessRuleException(MessageKeys.Otp.TooManyAttempts);
            case OtpVerifyResult.NotFound:
                throw new BusinessRuleException(MessageKeys.Otp.NotFound);
            default:
                throw new BusinessRuleException(MessageKeys.Otp.Mismatch);
        }
    }

    private static (string SubjectKey, string BodyKey) TemplateFor(OtpPurpose purpose) => purpose switch
    {
        OtpPurpose.Registration =>
            (MessageKeys.Email.RegistrationOtpSubject, MessageKeys.Email.RegistrationOtpBody),
        OtpPurpose.PasswordReset =>
            (MessageKeys.Email.PasswordResetOtpSubject, MessageKeys.Email.PasswordResetOtpBody),
        OtpPurpose.EmailChange =>
            (MessageKeys.Email.EmailChangeOtpSubject, MessageKeys.Email.EmailChangeOtpBody),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
    };
}
