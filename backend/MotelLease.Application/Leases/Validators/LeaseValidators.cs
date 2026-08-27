using FluentValidation;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Leases.Contracts;

namespace MotelLease.Application.Leases.Validators;

public sealed class AddLeaseTenantValidator : AbstractValidator<AddLeaseTenantRequest>
{
    public AddLeaseTenantValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong)
            .Matches(CommonRules.FullNamePattern).WithMessage(MessageKeys.Validation.FullNameFormat);

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Matches(CommonRules.PhonePattern).WithMessage(MessageKeys.Validation.PhoneFormat);
        });

        When(x => !string.IsNullOrWhiteSpace(x.IdCardNumber), () =>
        {
            RuleFor(x => x.IdCardNumber!)
                .MaximumLength(20).WithMessage(MessageKeys.Validation.TooLong);
        });
    }
}

public sealed class TerminateLeaseValidator : AbstractValidator<TerminateLeaseRequest>
{
    public TerminateLeaseValidator()
    {
        RuleFor(x => x.FinalElectricityReading)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(x => x.FinalWaterReading)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(x => x.DepositDeducted)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        When(x => !string.IsNullOrWhiteSpace(x.EndReason), () =>
        {
            RuleFor(x => x.EndReason!)
                .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);
        });
    }
}