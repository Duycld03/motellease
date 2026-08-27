using FluentValidation;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.BoardingHouses.Validators;

public sealed class SaveBoardingHouseRequestValidator
    : AbstractValidator<SaveBoardingHouseRequest>
{
    public SaveBoardingHouseRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(200).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Description)
            .MaximumLength(4000).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Type).IsInEnum();

        RuleFor(r => r.AddressLine)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(256).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Ward)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.District)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong);

        RuleFor(r => r.Province)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(100).WithMessage(MessageKeys.Validation.TooLong);

        // The same bounds the CHECK constraints enforce. Rejected here so the caller gets a
        // field-level message instead of a constraint violation.
        RuleFor(r => r.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage(MessageKeys.Validation.LatitudeRange);

        RuleFor(r => r.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage(MessageKeys.Validation.LongitudeRange);
    }
}

public sealed class UpdateUtilityPricesRequestValidator
    : AbstractValidator<UpdateUtilityPricesRequest>
{
    public UpdateUtilityPricesRequestValidator()
    {
        RuleFor(r => r.ElectricityUnitPrice)
            .GreaterThanOrEqualTo(0m).WithMessage(MessageKeys.Validation.NotNegative);

        RuleFor(r => r.WaterUnitPrice)
            .GreaterThanOrEqualTo(0m).WithMessage(MessageKeys.Validation.NotNegative);
    }
}
