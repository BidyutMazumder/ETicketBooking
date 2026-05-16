using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.SeatCategories.Commands.ApplyDiscountCommand;

using Booking.Application.Common.Interfaces;
using Booking.Application.Features.SeatCategories.Common;

public sealed class ApplyDiscountCommandHandler : IRequestHandler<ApplyDiscountCommand, Response<SeatCategoryResponse>>
{
    private readonly ISeatCategoryRepository _repository;

    public ApplyDiscountCommandHandler(ISeatCategoryRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Response<SeatCategoryResponse>> Handle(
        ApplyDiscountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await _repository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Response<SeatCategoryResponse>.Failure(
                    new Error("SeatCategory.NotFound", $"Seat category with id {request.CategoryId} not found"));
            }

            var result = category.ApplyDiscount(request.DiscountPercentage);
            if (result.IsFailure)
                return Response<SeatCategoryResponse>.Failure(result.Error);

            await _repository.UpdateAsync(category, cancellationToken);

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
        catch (DomainException ex)
        {
            return Response<SeatCategoryResponse>.Failure(
                new Error("SeatCategory.DiscountFailed", ex.Message));
        }
    }
}
