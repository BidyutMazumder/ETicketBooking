namespace Booking.Application.Features.SeatCategories.Commands.ApplyDiscountCommand;

using Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Command to apply a discount percentage to a seat category.
/// </summary>
public sealed record ApplyDiscountCommand(
    Guid CategoryId,
    decimal DiscountPercentage) : IRequest<Response<SeatCategoryResponse>>;
