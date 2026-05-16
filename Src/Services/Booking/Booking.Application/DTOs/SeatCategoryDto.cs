namespace Booking.Application.DTOs;

/// <summary>
/// Data transfer object for SeatCategory.
/// Contains all information about a seat category for API responses.
/// </summary>
public sealed record SeatCategoryDto(
    Guid Id,
    string Name,
    string SeatType,
    decimal BasePriceAmount,
    string BasePriceCurrency,
    decimal DiscountPercentage,
    decimal EffectivePriceAmount,
    string EffectivePriceCurrency,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
