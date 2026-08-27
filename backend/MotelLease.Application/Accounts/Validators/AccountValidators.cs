using FluentValidation;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Accounts.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.FullNamePattern).WithMessage(MessageKeys.Validation.FullNameFormat);

        RuleFor(r => r.PhoneNumber)
            .Matches(CommonRules.PhonePattern).WithMessage(MessageKeys.Validation.PhoneFormat)
            .When(r => !string.IsNullOrWhiteSpace(r.PhoneNumber));

        RuleFor(r => r.Gender).IsInEnum();
    }
}

public sealed class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    public UpdateLanguageRequestValidator() =>
        RuleFor(r => r.Language).NotEmpty().WithMessage(MessageKeys.Validation.Required);
}

public sealed class SendEmailChangeOtpRequestValidator
    : AbstractValidator<SendEmailChangeOtpRequest>
{
    public SendEmailChangeOtpRequestValidator()
    {
        RuleFor(r => r.NewEmail)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);
    }
}

public sealed class VerifyEmailChangeOtpRequestValidator
    : AbstractValidator<VerifyEmailChangeOtpRequest>
{
    public VerifyEmailChangeOtpRequestValidator()
    {
        RuleFor(r => r.NewEmail)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);

        RuleFor(r => r.Code)
            .Matches(CommonRules.OtpPattern).WithMessage(MessageKeys.Validation.OtpFormat);
    }
}
