using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.Events.Commands.CreateEvent;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Response<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventMapper _mapper;

    public CreateEventCommandHandler(IEventRepository eventRepository, IEventMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<EventDto>> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = Event.Create(
                request.Title,
                request.Description,
                request.StartDateTime,
                request.VenueName);

            await _eventRepository.AddAsync(@event, cancellationToken);

            return Response<EventDto>.Success(_mapper.MapToDto(@event));
        }
        catch (DomainException ex)
        {
            return Response<EventDto>.Failure(new Error("Event.CreationFailed", ex.Message));
        }
    }
}
