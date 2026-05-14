namespace Booking.Application.DTOs;

public sealed record SeatDto(
    Guid Id,
    string Row,
    int Number,
    string Type,
    decimal Price,
    string Status,
    DateTime? HeldUntilUtc);
