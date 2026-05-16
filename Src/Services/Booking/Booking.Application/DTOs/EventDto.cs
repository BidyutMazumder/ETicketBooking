namespace Booking.Application.DTOs;

public sealed record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDateTime,
    string VenueName,
    bool IsPublished,
    DateTime CreatedAt);

/// <summary>
/// DTO for event with associated seats information.
/// Used when creating an event with category-wise seat allocation.
/// </summary>
public sealed record EventWithSeatsDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDateTime,
    string VenueName,
    bool IsPublished,
    DateTime CreatedAt,
    int TotalSeats,
    List<SeatDto> Seats);
