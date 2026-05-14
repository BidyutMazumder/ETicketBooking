namespace Booking.Application.DTOs;

public sealed record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDateTime,
    string VenueName,
    bool IsPublished,
    DateTime CreatedAt);
