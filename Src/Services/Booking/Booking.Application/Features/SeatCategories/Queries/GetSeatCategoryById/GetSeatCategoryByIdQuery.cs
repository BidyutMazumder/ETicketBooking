namespace Booking.Application.Features.SeatCategories.Queries.GetSeatCategoryById;

using Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Query to retrieve a specific seat category by its ID.
/// </summary>
public sealed record GetSeatCategoryByIdQuery(Guid CategoryId) : IRequest<Response<SeatCategoryResponse>>;
