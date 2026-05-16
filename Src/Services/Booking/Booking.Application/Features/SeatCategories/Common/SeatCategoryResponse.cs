namespace Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Response DTO for SeatCategory operations.
/// </summary>
public sealed record SeatCategoryResponse(
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
    DateTime CreatedAt);
