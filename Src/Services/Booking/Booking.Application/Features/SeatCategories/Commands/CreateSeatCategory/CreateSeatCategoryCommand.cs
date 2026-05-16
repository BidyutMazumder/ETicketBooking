namespace Booking.Application.Features.SeatCategories.Commands.CreateSeatCategory;

using Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Command to create a new seat category.
/// </summary>
public sealed record CreateSeatCategoryCommand(
    string Name,
    string SeatType,
    decimal Price,
    string Currency,
    string Description) : IRequest<Response<SeatCategoryResponse>>;
