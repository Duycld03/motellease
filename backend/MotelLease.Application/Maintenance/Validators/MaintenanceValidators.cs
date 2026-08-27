using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Maintenance.Contracts;

namespace MotelLease.Application.Maintenance.Validators;

public sealed class CreateMaintenanceRequestValidator : AbstractValidator<CreateMaintenanceRequest>
{
    public CreateMaintenanceRequestValidator()
    {
        RuleFor(r => r.LeaseId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.Category).IsInEnum();

        RuleFor(r => r.Description)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class AcceptMaintenanceRequestValidator : AbstractValidator<AcceptMaintenanceRequest>
{
    public AcceptMaintenanceRequestValidator()
    {
        RuleFor(r => r.TaskTitle)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class RejectMaintenanceRequestValidator : AbstractValidator<RejectMaintenanceRequest>
{
    public RejectMaintenanceRequestValidator()
    {
        RuleFor(r => r.Reason)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}
