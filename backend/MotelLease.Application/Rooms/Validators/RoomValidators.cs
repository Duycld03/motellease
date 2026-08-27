using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Rooms.Contracts;

namespace MotelLease.Application.Rooms.Validators;

public sealed class SaveRoomRequestValidator : AbstractValidator<SaveRoomRequest>
{
    public SaveRoomRequestValidator()
    {
        RuleFor(r => r.RoomTypeId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.RoomNumber)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(32).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Description)
            .MaximumLength(1024).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class UpdateRoomStatusRequestValidator : AbstractValidator<UpdateRoomStatusRequest>
{
    public UpdateRoomStatusRequestValidator() => RuleFor(r => r.Status).IsInEnum();
}

public sealed class UpdateMeterReadingsRequestValidator
    : AbstractValidator<UpdateMeterReadingsRequest>
{
    /// <summary>decimal(12,2) in the schema, so ten digits before the point.</summary>
    private const decimal ReadingCeiling = 10_000_000_000m;

    public UpdateMeterReadingsRequestValidator()
    {
        RuleFor(r => r.ElectricityReading)
            .GreaterThanOrEqualTo(0m).WithMessage(MessageKeys.Validation.NotNegative)
            .LessThan(ReadingCeiling).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(r => r.WaterReading)
            .GreaterThanOrEqualTo(0m).WithMessage(MessageKeys.Validation.NotNegative)
            .LessThan(ReadingCeiling).WithMessage(MessageKeys.Validation.OutOfRange);
    }
}
