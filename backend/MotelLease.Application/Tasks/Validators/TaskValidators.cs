using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Tasks.Contracts;

namespace MotelLease.Application.Tasks.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(r => r.BoardingHouseId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.AssignedToUserId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.Title)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Details)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Priority).IsInEnum();
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(r => r.AssignedToUserId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.Title)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Details)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Priority).IsInEnum();
    }
}

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(r => r.Status).IsInEnum();
    }
}
