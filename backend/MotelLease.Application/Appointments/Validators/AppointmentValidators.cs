using FluentValidation;
using MotelLease.Application.Appointments.Contracts;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Appointments.Validators;

public sealed class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(r => r.RoomId)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        // Whether the date is still ahead is checked in the handler, which has the clock.
        RuleFor(r => r.AppointmentDate)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required);

        RuleFor(r => r.Note)
            .MaximumLength(1024).WithMessage(MessageKeys.Validation.TooLong);
    }
}

public sealed class RejectAppointmentRequestValidator : AbstractValidator<RejectAppointmentRequest>
{
    public RejectAppointmentRequestValidator() =>
        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage(MessageKeys.Validation.Required)
            .MaximumLength(512).WithMessage(MessageKeys.Validation.TooLong);
}

public sealed class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator() =>
        RuleFor(r => r.Reason)
            .MaximumLength(512).WithMessage(MessageKeys.Validation.TooLong);
}
