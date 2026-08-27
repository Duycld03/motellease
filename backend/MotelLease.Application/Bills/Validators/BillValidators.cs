using FluentValidation;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Bills.Validators;

public sealed class CreateRoomAdditionalFeeValidator : AbstractValidator<CreateRoomAdditionalFeeRequest>
{
    public CreateRoomAdditionalFeeValidator()
    {
        RuleFor(x => x.FeeName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(x => x.FeeAmount)
            .GreaterThan(0).WithMessage(MessageKeys.Validation.Positive);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage(MessageKeys.Validation.OutOfRange);
    }
}

public sealed class UpdateRoomAdditionalFeeValidator : AbstractValidator<UpdateRoomAdditionalFeeRequest>
{
    public UpdateRoomAdditionalFeeValidator()
    {
        RuleFor(x => x.FeeName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(x => x.FeeAmount)
            .GreaterThan(0).WithMessage(MessageKeys.Validation.Positive);
    }
}

public sealed class PreviewBillValidator : AbstractValidator<PreviewBillRequest>
{
    public PreviewBillValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.ElectricityNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(x => x.WaterNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);
    }
}

public sealed class CreateBillValidator : AbstractValidator<CreateBillRequest>
{
    public CreateBillValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.ElectricityNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(x => x.WaterNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);
    }
}

public sealed class UpdateDraftBillValidator : AbstractValidator<UpdateDraftBillRequest>
{
    public UpdateDraftBillValidator()
    {
        RuleFor(x => x.ElectricityNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(x => x.WaterNew)
            .GreaterThanOrEqualTo(0).WithMessage(MessageKeys.Validation.NotNegative);
    }
}

public sealed class IssueDraftBillValidator : AbstractValidator<IssueDraftBillRequest>
{
    public IssueDraftBillValidator()
    {
        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);
    }
}
