using FluentValidation;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Reviews.Contracts;

namespace MotelLease.Application.Reviews.Validators;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.BoardingHouseId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class UpdateReviewValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5).WithMessage(MessageKeys.Validation.OutOfRange);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class ReplyReviewValidator : AbstractValidator<ReplyReviewRequest>
{
    public ReplyReviewValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(2000).WithMessage(MessageKeys.Validation.TooLong);
    }
}
