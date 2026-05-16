namespace Booking.Application.Features.Events.Queries.GetCategoryWiseSeatCount;

/// <summary>
/// Handler for retrieving category-wise seat counts including status breakdown.
/// Provides a summary view of seat inventory by category (VIP, Regular, Economy) with counts for each status.
/// </summary>
public sealed class GetCategoryWiseSeatCountQueryHandler : IRequestHandler<GetCategoryWiseSeatCountQuery, Response<List<CategoryWiseSeatSummaryDto>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISeatCategoryRepository _seatCategoryRepository;

    public GetCategoryWiseSeatCountQueryHandler(
        IEventRepository eventRepository,
        ISeatCategoryRepository seatCategoryRepository)
    {
        _eventRepository = eventRepository;
        _seatCategoryRepository = seatCategoryRepository;
    }

    public async ValueTask<Response<List<CategoryWiseSeatSummaryDto>>> Handle(
        GetCategoryWiseSeatCountQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
                return Response<List<CategoryWiseSeatSummaryDto>>.Failure(
                    new Error("Event.NotFound", "Event not found"));

            // Group seats by type
            var seatsByType = @event.Seats
                .GroupBy(s => s.Type.Value)
                .ToList();

            var result = new List<CategoryWiseSeatSummaryDto>();

            foreach (var typeGroup in seatsByType)
            {
                var seatType = typeGroup.Key;
                var seatsOfType = typeGroup.ToList();

                // Get category information
                var categories = await _seatCategoryRepository.GetByTypeAsync(seatType, cancellationToken);
                var category = categories?.FirstOrDefault();
                var categoryName = category?.Name.Value ?? seatType;
                var basePrice = category?.BasePrice.Amount ?? seatsOfType.FirstOrDefault()?.Price.Amount ?? 0m;
                var currency = category?.BasePrice.Currency ?? seatsOfType.FirstOrDefault()?.Price.Currency ?? "USD";

                // Count seats by status
                var totalSeats = seatsOfType.Count;
                var availableSeats = seatsOfType.Count(s => s.Status == SeatStatus.Available);
                var heldSeats = seatsOfType.Count(s => s.Status == SeatStatus.Held && !s.IsHoldExpired());
                var reservedSeats = seatsOfType.Count(s => s.Status == SeatStatus.Reserved);
                var soldSeats = seatsOfType.Count(s => s.Status == SeatStatus.Sold);

                result.Add(new CategoryWiseSeatSummaryDto(
                    categoryName,
                    seatType,
                    basePrice,
                    currency,
                    totalSeats,
                    availableSeats,
                    heldSeats,
                    reservedSeats,
                    soldSeats));
            }

            return Response<List<CategoryWiseSeatSummaryDto>>.Success(result.OrderBy(x => x.SeatType).ToList());
        }
        catch (Exception ex)
        {
            return Response<List<CategoryWiseSeatSummaryDto>>.Failure(
                new Error("SeatCount.RetrievalFailed", ex.Message));
        }
    }
}
