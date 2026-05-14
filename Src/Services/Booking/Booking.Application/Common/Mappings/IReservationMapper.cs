namespace Booking.Application.Common.Mappings;

public interface IReservationMapper
{
    ReservationDto MapToDto(Reservation reservation);
    List<ReservationDto> MapToDtoList(List<Reservation> reservations);
}
