using FluentValidation;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Staff.Contracts;

namespace MotelLease.Application.Staff.Validators;

public sealed class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffRequestValidator()
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
    }
}

public sealed class UpdateStaffRequestValidator : AbstractValidator<UpdateStaffRequest>
{
    public UpdateStaffRequestValidator()
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

public sealed class AssignStaffRequestValidator : AbstractValidator<AssignStaffRequest>
{
    public AssignStaffRequestValidator()
    {
        RuleFor(r => r.StaffUserId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);
    }
}
