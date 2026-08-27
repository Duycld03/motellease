using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Leases.Contracts;

namespace MotelLease.Application.Extensions.Validators;

public sealed class CreateExtensionRequestValidator : AbstractValidator<CreateExtensionRequest>
{
    public CreateExtensionRequestValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(x => x.RequestedEndDate)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        When(x => !string.IsNullOrWhiteSpace(x.TenantNote), () =>
        {
            RuleFor(x => x.TenantNote!)
                .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);
        });
    }
}

public sealed class RejectExtensionRequestValidator : AbstractValidator<RejectExtensionRequest>
{
    public RejectExtensionRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.OwnerNote), () =>
        {
            RuleFor(x => x.OwnerNote!)
                .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);
        });
    }
}