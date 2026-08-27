using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.SavedListings.Contracts;

namespace MotelLease.Application.SavedListings.Validators;

public sealed class SaveListingValidator : AbstractValidator<SaveListingRequest>
{
    public SaveListingValidator()
    {
        RuleFor(x => x.BoardingHouseId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);
    }
}
