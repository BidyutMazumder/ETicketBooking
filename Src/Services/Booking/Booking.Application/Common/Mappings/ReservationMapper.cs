namespace Booking.Application.Common.Mappings;

public sealed class ReservationMapper : IReservationMapper
{
    public ReservationDto MapToDto(Reservation reservation)
    {
        return new ReservationDto(
            reservation.Id,
            reservation.UserId,
            reservation.EventId,
            reservation.SeatId,
            reservation.Status.Value,
            reservation.HoldExpiresAtUtc,
            reservation.CreatedAt);
    }

    public List<ReservationDto> MapToDtoList(List<Reservation> reservations)
    {
        return reservations.Select(MapToDto).ToList();
    }
}
