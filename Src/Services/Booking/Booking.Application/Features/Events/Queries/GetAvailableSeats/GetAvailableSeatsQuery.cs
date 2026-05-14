namespace Booking.Application.Features.Events.Queries.GetAvailableSeats;

public sealed record GetAvailableSeatsQuery(
    Guid EventId
) : IRequest<Response<List<SeatDto>>>;
