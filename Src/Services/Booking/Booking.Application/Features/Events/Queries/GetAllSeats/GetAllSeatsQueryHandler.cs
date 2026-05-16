namespace Booking.Application.Features.Events.Queries.GetAllSeats;

/// <summary>
/// Handler for retrieving all seats of an event with their current status (Available, Held, Reserved, Sold).
/// This query shows the complete seat inventory status at a point in time.
/// </summary>
public sealed class GetAllSeatsQueryHandler : IRequestHandler<GetAllSeatsQuery, Response<List<SeatDto>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISeatMapper _mapper;

    public GetAllSeatsQueryHandler(IEventRepository eventRepository, ISeatMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<List<SeatDto>>> Handle(
        GetAllSeatsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
                return Response<List<SeatDto>>.Failure(
                    new Error("Event.NotFound", "Event not found"));

            // Get all seats regardless of status
            var allSeats = @event.Seats.ToList();

            // Sort by row and number for better readability
            var sortedSeats = allSeats
                .OrderBy(s => s.Row)
                .ThenBy(s => s.Number)
                .ToList();

            return Response<List<SeatDto>>.Success(_mapper.MapToDtoList(sortedSeats));
        }
        catch (Exception ex)
        {
            return Response<List<SeatDto>>.Failure(
                new Error("Seats.RetrievalFailed", ex.Message));
        }
    }
}
