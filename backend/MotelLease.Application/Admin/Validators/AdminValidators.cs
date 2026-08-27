using FluentValidation;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Admin.Validators;

public sealed class AdminCreateAccountRequestValidator : AbstractValidator<AdminCreateAccountRequest>
{
    public AdminCreateAccountRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.EmailPattern).WithMessage(MessageKeys.Validation.EmailFormat);

        RuleFor(r => r.Username)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .Matches(CommonRules.UsernamePattern).WithMessage(MessageKeys.Validation.UsernameFormat);

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

public sealed class AdminUpdateAccountRequestValidator : AbstractValidator<AdminUpdateAccountRequest>
{
    public AdminUpdateAccountRequestValidator()
    {
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

public sealed class CreateFacilityRequestValidator : AbstractValidator<CreateFacilityRequest>
{
    public CreateFacilityRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Description)
            .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class UpdateFacilityRequestValidator : AbstractValidator<UpdateFacilityRequest>
{
    public UpdateFacilityRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Description)
            .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);
    }
}
