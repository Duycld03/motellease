using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Deposits.Contracts;

namespace MotelLease.Application.Deposits.Validators;

public sealed class RequestDepositRequestValidator : AbstractValidator<RequestDepositRequest>
{
    /// <summary>
    /// Two years. Terms are normalised to whole months (docs/domain-rules.md §2), and a bound keeps
    /// a typo from producing a contract nobody meant to sign.
    /// </summary>
    private const int MaxTermMonths = 24;

    public RequestDepositRequestValidator()
    {
        RuleFor(r => r.RoomId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        // Whether the start date is still ahead is checked in the handler, which has the clock.
        RuleFor(r => r.RequestedStartDate)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.RequestedTermMonths)
            .InclusiveBetween(1, MaxTermMonths).WithMessage(MessageKeys.Validation.OutOfRange);
    }
}

public sealed class RejectDepositRequestValidator : AbstractValidator<RejectDepositRequest>
{
    public RejectDepositRequestValidator() =>
        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(512).WithMessage(MessageKeys.Validation.TooLong);
}

public sealed class CancelDepositRequestValidator : AbstractValidator<CancelDepositRequest>
{
    public CancelDepositRequestValidator() =>
        RuleFor(r => r.Reason)
            .MaximumLength(512).WithMessage(MessageKeys.Validation.TooLong);
}
