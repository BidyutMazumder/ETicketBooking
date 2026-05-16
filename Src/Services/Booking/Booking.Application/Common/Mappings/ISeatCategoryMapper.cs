namespace Booking.Application.Common.Mappings;

public interface ISeatCategoryMapper
{
    SeatCategoryDto MapToDto(SeatCategory category);
    List<SeatCategoryDto> MapToDtoList(List<SeatCategory> categories);
}
