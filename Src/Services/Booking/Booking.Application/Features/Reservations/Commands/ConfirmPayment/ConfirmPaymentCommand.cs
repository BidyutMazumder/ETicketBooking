namespace Booking.Application.Features.Reservations.Commands.ConfirmPayment;

public sealed record ConfirmPaymentCommand(
    Guid ReservationId,
    string StripePaymentIntentId) : IRequest<Response<ReservationDto>>;
