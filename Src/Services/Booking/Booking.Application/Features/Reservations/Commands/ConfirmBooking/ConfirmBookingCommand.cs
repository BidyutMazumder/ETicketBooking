namespace Booking.Application.Features.Reservations.Commands.ConfirmBooking;

public sealed record ConfirmBookingCommand(
    Guid ReservationId
) : IRequest<Response<ReservationDto>>;
