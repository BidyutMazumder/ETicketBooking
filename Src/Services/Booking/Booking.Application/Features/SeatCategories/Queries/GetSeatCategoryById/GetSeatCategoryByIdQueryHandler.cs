namespace Booking.Application.Features.SeatCategories.Queries.GetSeatCategoryById;

using Booking.Application.Common.Interfaces;
using Booking.Application.Features.SeatCategories.Common;

public sealed class GetSeatCategoryByIdQueryHandler : IRequestHandler<GetSeatCategoryByIdQuery, Response<SeatCategoryResponse>>
{
    private readonly ISeatCategoryRepository _repository;

    public GetSeatCategoryByIdQueryHandler(ISeatCategoryRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Response<SeatCategoryResponse>> Handle(
        GetSeatCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.CategoryId == Guid.Empty)
            {
                return Response<SeatCategoryResponse>.Failure(
                    new Error("SeatCategory.InvalidId", "Category ID cannot be empty"));
            }

            var category = await _repository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Response<SeatCategoryResponse>.Failure(
                    new Error("SeatCategory.NotFound", $"Seat category with id {request.CategoryId} not found"));
            }

            var effectivePrice = category.GetEffectivePrice();

            var response = new SeatCategoryResponse(
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

            return Response<SeatCategoryResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Response<SeatCategoryResponse>.Failure(
                new Error("SeatCategory.QueryFailed", ex.Message));
        }
    }
}
