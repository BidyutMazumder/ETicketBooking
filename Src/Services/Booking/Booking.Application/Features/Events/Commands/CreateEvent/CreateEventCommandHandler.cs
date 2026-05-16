using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Handler for creating events with optional category-wise seat allocation.
/// If category-wise seats are provided, seats are automatically created with the specified counts.
/// </summary>
public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Response<EventWithSeatsDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventMapper _mapper;
    private readonly ISeatMapper _seatMapper;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IEventMapper mapper,
        ISeatMapper seatMapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _seatMapper = seatMapper;
    }

    public async ValueTask<Response<EventWithSeatsDto>> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventResult = Event.Create(
                request.Title,
                request.Description,
                request.StartDateTime,
                request.VenueName);

            if (eventResult.IsFailure)
                return Response<EventWithSeatsDto>.Failure(eventResult.Error);

            var @event = eventResult.Value;

            // Create category-wise seats if provided
            if (request.CategoryWiseSeats != null && request.CategoryWiseSeats.Count > 0)
            {
                var seatCreationResult = CreateCategoryWiseSeats(@event, request.CategoryWiseSeats);
                if (seatCreationResult.IsFailure)
                    return Response<EventWithSeatsDto>.Failure(seatCreationResult.Error);
            }

            await _eventRepository.AddAsync(@event, cancellationToken);

            var seatsDto = _seatMapper.MapToDtoList(@event.Seats.ToList());
            return Response<EventWithSeatsDto>.Success(new EventWithSeatsDto(
                @event.Id,
                @event.Title,
                @event.Description,
                @event.StartDateTime,
                @event.VenueName,
                @event.IsPublished,
                @event.CreatedAt,
                @event.TotalSeats,
                seatsDto));
        }
        catch (DomainException ex)
        {
            return Response<EventWithSeatsDto>.Failure(new Error("Event.CreationFailed", ex.Message));
        }
    }

    /// <summary>
    /// Creates seats in the event based on category-wise seat specifications.
    /// </summary>
    private Result CreateCategoryWiseSeats(Event @event, List<CreateCategoryWiseSeatDto> categoryWiseSeats)
    {
        var seatNumber = 1;

        foreach (var seatSpec in categoryWiseSeats)
        {
            for (int i = 0; i < seatSpec.Count; i++)
            {
                var seatType = SeatType.Create(seatSpec.SeatType);
                var price = Money.Create(seatSpec.Price, "USD");

                var seatResult = Seat.Create(
                    seatSpec.Row,
                    seatNumber,
                    seatType,
                    price);

                if (seatResult.IsFailure)
                    return Result.Failure(seatResult.Error);

                var addResult = @event.AddSeat(seatResult.Value);
                if (addResult.IsFailure)
                    return addResult;

                seatNumber++;
            }
        }

        return Result.Success();
    }
}

