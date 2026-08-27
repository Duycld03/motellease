using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Expenses.Contracts;

namespace MotelLease.Application.Expenses.Validators;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(r => r.Month)
            .InclusiveBetween(1, 12).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(r => r.Year)
            .InclusiveBetween(2000, 2100).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(r => r.ElectricityOld)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityQty)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityAmount)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterOld)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterQty)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterAmount)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleForEach(r => r.OtherExpenses).ChildRules(item =>
        {
            item.RuleFor(i => i.FeeName)
                .NotEmpty().WithMessage(MessageKeys.Validation.Required)
                .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

            item.RuleFor(i => i.FeeAmount)
                .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);
        });
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(r => r.ElectricityOld)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityQty)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.ElectricityAmount)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterOld)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterQty)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterAmount)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleForEach(r => r.OtherExpenses).ChildRules(item =>
        {
            item.RuleFor(i => i.FeeName)
                .NotEmpty().WithMessage(MessageKeys.Validation.Required)
                .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

            item.RuleFor(i => i.FeeAmount)
                .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);
        });
    }
}
