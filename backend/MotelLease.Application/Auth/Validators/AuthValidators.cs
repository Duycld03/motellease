using FluentValidation;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Auth.Validators;

/// <summary>
/// Messages are resource keys, never sentences — the Api layer renders them in the request
/// language. The keys describe fixed rules, so they need no placeholders.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.UsernamePattern).WithMessage(MessageKeys.Validation.UsernameFormat);

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.PasswordPattern).WithMessage(MessageKeys.Validation.PasswordTooWeak);

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.FullNamePattern).WithMessage(MessageKeys.Validation.FullNameFormat);

        RuleFor(r => r.PhoneNumber)
            .Matches(CommonRules.PhonePattern).WithMessage(MessageKeys.Validation.PhoneFormat)
            .When(r => !string.IsNullOrWhiteSpace(r.PhoneNumber));

        RuleFor(r => r.Gender).IsInEnum();
        RuleFor(r => r.Role).IsInEnum();
    }
}

public sealed class SendRegistrationOtpRequestValidator
    : AbstractValidator<SendRegistrationOtpRequest>
{
    public SendRegistrationOtpRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);
    }
}

public sealed class VerifyRegistrationOtpRequestValidator
    : AbstractValidator<VerifyRegistrationOtpRequest>
{
    public VerifyRegistrationOtpRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);

        RuleFor(r => r.Code)
            .Matches(CommonRules.OtpPattern).WithMessage(MessageKeys.Validation.OtpFormat);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Login).NotEmpty().WithMessage(MessageKeys.Validation.Required);
        RuleFor(r => r.Password).NotEmpty().WithMessage(MessageKeys.Validation.Required);
    }
}

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(r => r.IdToken).NotEmpty().WithMessage(MessageKeys.Validation.Required);
        RuleFor(r => r.Role).IsInEnum().When(r => r.Role is not null);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator() =>
        RuleFor(r => r.RefreshToken).NotEmpty().WithMessage(MessageKeys.Validation.Required);
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator() =>
        RuleFor(r => r.RefreshToken).NotEmpty().WithMessage(MessageKeys.Validation.Required);
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);

        RuleFor(r => r.Code)
            .Matches(CommonRules.OtpPattern).WithMessage(MessageKeys.Validation.OtpFormat);

        RuleFor(r => r.NewPassword)
            .Matches(CommonRules.PasswordPattern).WithMessage(MessageKeys.Validation.PasswordTooWeak);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.CurrentPassword).NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.NewPassword)
            .Matches(CommonRules.PasswordPattern).WithMessage(MessageKeys.Validation.PasswordTooWeak);
    }
}
