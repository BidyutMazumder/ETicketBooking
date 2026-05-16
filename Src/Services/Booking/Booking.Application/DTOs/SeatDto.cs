namespace Booking.Application.DTOs;

public sealed record SeatDto(
    Guid Id,
    string Row,
    int Number,
    string Type,
    decimal PriceAmount,
    string PriceCurrency,
    string Status,
    DateTime? HeldUntilUtc);
