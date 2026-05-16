namespace Booking.Application.Features.SeatCategories.Commands.UpdateBasePriceCommand;

using Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Command to update the base price of a seat category.
/// </summary>
public sealed record UpdateBasePriceCommand(
    Guid CategoryId,
    decimal NewPrice,
    string Currency) : IRequest<Response<SeatCategoryResponse>>;
