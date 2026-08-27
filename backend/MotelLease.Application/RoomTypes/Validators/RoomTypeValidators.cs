using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.RoomTypes.Contracts;

namespace MotelLease.Application.RoomTypes.Validators;

public sealed class SaveRoomTypeRequestValidator : AbstractValidator<SaveRoomTypeRequest>
{
    /// <summary>
    /// A cap high enough for a dorm room and low enough to stay a typo check; the house type
    /// decides whether more than one is allowed at all (docs/domain-rules.md §1).
    /// </summary>
    private const int MaxOccupantsCeiling = 20;

    public SaveRoomTypeRequestValidator()
    {
        RuleFor(r => r.TypeName)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(128).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Price)
            .GreaterThanOrEqualTo(0m).WithMessage(MessageKeys.Validation.NotNegative);

        // decimal(6,2) in the schema, so four digits before the point.
        RuleFor(r => r.RoomSizeM2)
            .GreaterThan(0m).WithMessage(MessageKeys.Validation.Positive)
            .LessThan(10_000m).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(r => r.MaxOccupants)
            .InclusiveBetween(1, MaxOccupantsCeiling)
            .WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(r => r.Description)
            .MaximumLength(1024).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.FacilityIds)
            .NotNull().WithMessage(MessageKeys.Validation.Required);

        RuleForEach(r => r.FacilityIds)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);
    }
}
