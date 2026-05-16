namespace Booking.Application.Features.SeatCategories.Queries.GetAllSeatCategories;

using Booking.Application.Common.Interfaces;
using Booking.Application.Features.SeatCategories.Common;

public sealed class GetAllSeatCategoriesQueryHandler : IRequestHandler<GetAllSeatCategoriesQuery, Response<List<SeatCategoryResponse>>>
{
    private readonly ISeatCategoryRepository _repository;

    public GetAllSeatCategoriesQueryHandler(ISeatCategoryRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Response<List<SeatCategoryResponse>>> Handle(
        GetAllSeatCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _repository.GetAllAsync(cancellationToken);

            var responses = categories
                .Select(category =>
                {
                    var effectivePrice = category.GetEffectivePrice();
                    return new SeatCategoryResponse(
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
                        category.CreatedAt);
                })
                .ToList();

            return Response<List<SeatCategoryResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            return Response<List<SeatCategoryResponse>>.Failure(
                new Error("SeatCategory.QueryFailed", ex.Message));
        }
    }
}
