namespace Booking.Application.Common.Mappings;

public interface ISeatMapper
{
    SeatDto MapToDto(Seat seat);
    List<SeatDto> MapToDtoList(List<Seat> seats);
}
