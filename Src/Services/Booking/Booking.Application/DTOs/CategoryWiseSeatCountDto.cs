namespace Booking.Application.DTOs;

/// <summary>
/// Data transfer object representing seat count by category.
/// </summary>
public sealed record CategoryWiseSeatCountDto(
    string Category,
    string SeatType,
    int Total,
    int Available,
    int Held,
    int Reserved,
    int Sold);

/// <summary>
/// Data transfer object for category-wise seat summary including pricing.
/// </summary>
public sealed record CategoryWiseSeatSummaryDto(
    string Category,
    string SeatType,
    decimal BasePrice,
    string Currency,
    int TotalSeats,
    int AvailableSeats,
    int HeldSeats,
    int ReservedSeats,
    int SoldSeats);

/// <summary>
/// Request DTO for specifying seat count by category when creating an event.
/// </summary>
public sealed record CreateCategoryWiseSeatDto(
    string Category,
    string SeatType,
    int Count,
    string Row);
