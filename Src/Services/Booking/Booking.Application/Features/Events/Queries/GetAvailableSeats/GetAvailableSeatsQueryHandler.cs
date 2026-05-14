namespace Booking.Application.Features.Events.Queries.GetAvailableSeats;

public sealed class GetAvailableSeatsQueryHandler : IRequestHandler<GetAvailableSeatsQuery, Response<List<SeatDto>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISeatMapper _mapper;

    public GetAvailableSeatsQueryHandler(IEventRepository eventRepository, ISeatMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<List<SeatDto>>> Handle(
        GetAvailableSeatsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
                return Response<List<SeatDto>>.Failure(
                    new Error("Event.NotFound", "Event not found"));

            // Get available seats (not Reserved, not Sold, and not under active hold)
            var availableSeats = @event.Seats
                .Where(s => s.Status == SeatStatus.Available || 
                           (s.Status == SeatStatus.Held && s.IsHoldExpired()))
                .ToList();

            return Response<List<SeatDto>>.Success(_mapper.MapToDtoList(availableSeats));
        }
        catch (Exception ex)
        {
            return Response<List<SeatDto>>.Failure(
                new Error("Seats.RetrievalFailed", ex.Message));
        }
    }
}
