namespace Booking.Application.Features.Events.Queries.GetAllSeats;

public sealed record GetAllSeatsQuery(
    Guid EventId
) : IRequest<Response<List<SeatDto>>>;
