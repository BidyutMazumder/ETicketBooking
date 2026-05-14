namespace Booking.Application.Features.Reservations.Commands.ConfirmPayment;

using FluentValidation;

public sealed class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("Reservation ID is required");

        RuleFor(x => x.StripePaymentIntentId)
            .NotEmpty().WithMessage("Stripe Payment Intent ID is required")
            .MinimumLength(10).WithMessage("Invalid Stripe Payment Intent ID format");
    }
}
