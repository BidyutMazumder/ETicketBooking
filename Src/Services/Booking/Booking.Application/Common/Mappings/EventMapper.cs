namespace Booking.Application.Common.Mappings;

public sealed class EventMapper : IEventMapper
{
    public EventDto MapToDto(Event @event)
    {
        return new EventDto(
            @event.Id,
            @event.Title,
            @event.Description,
            @event.StartDateTime,
            @event.VenueName,
            @event.IsPublished,
            @event.CreatedAt);
    }

    public List<EventDto> MapToDtoList(List<Event> events)
    {
        return events.Select(MapToDto).ToList();
    }
}
