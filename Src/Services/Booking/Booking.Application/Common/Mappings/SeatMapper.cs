namespace Booking.Application.Common.Mappings;

public sealed class SeatMapper : ISeatMapper
{
    public SeatDto MapToDto(Seat seat)
    {
        return new SeatDto(
            seat.Id,
            seat.Row,
            seat.Number,
            seat.Type.Value,
            seat.Price,
            seat.Status.Value,
            seat.HeldUntilUtc);
    }

    public List<SeatDto> MapToDtoList(List<Seat> seats)
    {
        return seats.Select(MapToDto).ToList();
    }
}
