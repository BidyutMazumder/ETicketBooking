namespace Booking.Application.Features.SeatCategories.Queries.GetAllSeatCategories;

using Booking.Application.Features.SeatCategories.Common;

/// <summary>
/// Query to retrieve all seat categories.
/// </summary>
public sealed record GetAllSeatCategoriesQuery : IRequest<Response<List<SeatCategoryResponse>>>;
