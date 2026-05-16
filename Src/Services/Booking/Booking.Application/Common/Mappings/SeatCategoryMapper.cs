namespace Booking.Application.Common.Mappings;

public sealed class SeatCategoryMapper : ISeatCategoryMapper
{
    public SeatCategoryDto MapToDto(SeatCategory category)
    {
        var effectivePrice = category.GetEffectivePrice();

        return new SeatCategoryDto(
            category.Id,
            category.Name.Value,
            category.SeatType.Value,
            category.BasePrice.Amount,
            category.BasePrice.Currency,
            category.DiscountPercentage,
            effectivePrice.Amount,
            effectivePrice.Currency,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
    }

    public List<SeatCategoryDto> MapToDtoList(List<SeatCategory> categories)
    {
        return categories.Select(MapToDto).ToList();
    }
}
