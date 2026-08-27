using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Withdrawals.Contracts;

namespace MotelLease.Application.Withdrawals.Validators;

public sealed class CreateWithdrawRequestValidator : AbstractValidator<CreateWithdrawRequest>
{
    public CreateWithdrawRequestValidator()
    {
        RuleFor(r => r.Amount)
            .GreaterThan(0).WithMessage(MessageKeys.Validation.Positive);

        RuleFor(r => r.BankName)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.BankAccountNumber)
            .MaximumLength(64).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.BankAccountHolder)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class RejectWithdrawRequestValidator : AbstractValidator<RejectWithdrawRequest>
{
    public RejectWithdrawRequestValidator()
    {
        RuleFor(r => r.Reason)
            .MaximumLength(1000).WithMessage(MessageKeys.Validation.TooLong);
    }
}
