using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Refunds.Contracts;

namespace MotelLease.Application.Refunds.Validators;

public sealed class CreateRefundRequestValidator : AbstractValidator<CreateRefundRequest>
{
    public CreateRefundRequestValidator()
    {
        RuleFor(r => r.DepositId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.Reason)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class RejectRefundRequestValidator : AbstractValidator<RejectRefundRequest>
{
    public RejectRefundRequestValidator()
    {
        RuleFor(r => r.Reason)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}
