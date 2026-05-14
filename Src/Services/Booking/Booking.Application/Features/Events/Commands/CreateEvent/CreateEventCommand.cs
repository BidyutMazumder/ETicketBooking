namespace Booking.Application.Features.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string Title,
    string Description,
    DateTime StartDateTime,
    string VenueName
) : IRequest<Response<EventDto>>;
