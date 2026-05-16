using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.SeatCategories.Commands.UpdateBasePriceCommand;

using Booking.Application.Common.Interfaces;
using Booking.Application.Features.SeatCategories.Common;

public sealed class UpdateBasePriceCommandHandler : IRequestHandler<UpdateBasePriceCommand, Response<SeatCategoryResponse>>
{
    private readonly ISeatCategoryRepository _repository;

    public UpdateBasePriceCommandHandler(ISeatCategoryRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Response<SeatCategoryResponse>> Handle(
        UpdateBasePriceCommand request,
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

            var newPrice = Money.Create(request.NewPrice, request.Currency);

            if (category.BasePrice.Currency != newPrice.Currency)
            {
                return Response<SeatCategoryResponse>.Failure(
                    new Error(
                        "SeatCategory.CurrencyMismatch",
                        $"Cannot update price: category uses {category.BasePrice.Currency} but {newPrice.Currency} was provided"));
            }

            var result = category.UpdateBasePrice(newPrice);
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
                new Error("SeatCategory.UpdateFailed", ex.Message));
        }
    }
}
