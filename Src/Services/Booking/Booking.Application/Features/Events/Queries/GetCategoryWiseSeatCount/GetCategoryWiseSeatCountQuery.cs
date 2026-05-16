namespace Booking.Application.Features.Events.Queries.GetCategoryWiseSeatCount;

public sealed record GetCategoryWiseSeatCountQuery(
    Guid EventId
) : IRequest<Response<List<CategoryWiseSeatSummaryDto>>>;
