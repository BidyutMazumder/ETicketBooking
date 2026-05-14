namespace Booking.Application.Features.Reservations.Commands.HoldSeat;

public sealed record HoldSeatCommand(
    Guid EventId,
    Guid SeatId,
    Guid UserId
) : IRequest<Response<ReservationDto>>;
