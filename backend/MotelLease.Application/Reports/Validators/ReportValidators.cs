using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Reports.Contracts;

namespace MotelLease.Application.Reports.Validators;

public sealed class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();

        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(500).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(x => x.Details)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class ResolveReportValidator : AbstractValidator<ResolveReportRequest>
{
    public ResolveReportValidator()
    {
        RuleFor(x => x.Resolution)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class DismissReportValidator : AbstractValidator<DismissReportRequest>
{
    public DismissReportValidator()
    {
        RuleFor(x => x.Resolution)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}
