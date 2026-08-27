using FluentValidation;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Catalogue.Validators;

public sealed class BoardingHouseNearbyValidator : AbstractValidator<BoardingHouseNearbyRequest>
{
    public BoardingHouseNearbyValidator()
    {
        RuleFor(x => x.Lat)
            .InclusiveBetween(-90, 90).WithMessage(MessageKeys.Validation.LatitudeRange);

        RuleFor(x => x.Lon)
            .InclusiveBetween(-180, 180).WithMessage(MessageKeys.Validation.LongitudeRange);

        RuleFor(x => x.RadiusKm)
            .GreaterThan(0).WithMessage(MessageKeys.Validation.Positive)
            .LessThanOrEqualTo(50).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage(MessageKeys.Validation.OutOfRange);
    }
}

public sealed class BoardingHouseMapValidator : AbstractValidator<BoardingHouseMapRequest>
{
    public BoardingHouseMapValidator()
    {
        RuleFor(x => x.SwLat)
            .InclusiveBetween(-90, 90).WithMessage(MessageKeys.Validation.LatitudeRange);

        RuleFor(x => x.SwLon)
            .InclusiveBetween(-180, 180).WithMessage(MessageKeys.Validation.LongitudeRange);

        RuleFor(x => x.NeLat)
            .InclusiveBetween(-90, 90).WithMessage(MessageKeys.Validation.LatitudeRange);

        RuleFor(x => x.NeLon)
            .InclusiveBetween(-180, 180).WithMessage(MessageKeys.Validation.LongitudeRange);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 300).WithMessage(MessageKeys.Validation.OutOfRange);
    }
}
