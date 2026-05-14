namespace Booking.Application.DTOs;

public sealed record ReservationDto(
    Guid Id,
    Guid UserId,
    Guid EventId,
    Guid SeatId,
    string Status,
    DateTime HoldExpiresAtUtc,
    DateTime CreatedAt);
