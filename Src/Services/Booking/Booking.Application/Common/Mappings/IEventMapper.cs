namespace Booking.Application.Common.Mappings;

public interface IEventMapper
{
    EventDto MapToDto(Event @event);
    List<EventDto> MapToDtoList(List<Event> events);
}
