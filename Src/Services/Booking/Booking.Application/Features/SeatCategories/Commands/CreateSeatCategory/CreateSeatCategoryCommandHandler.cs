using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.SeatCategories.Commands.CreateSeatCategory;

using Booking.Domain.Events;
using Booking.Application.Common.Interfaces;
using Booking.Application.Features.SeatCategories.Common;

public sealed class CreateSeatCategoryCommandHandler : IRequestHandler<CreateSeatCategoryCommand, Response<SeatCategoryResponse>>
{
    private readonly ISeatCategoryRepository _repository;

    public CreateSeatCategoryCommandHandler(ISeatCategoryRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Response<SeatCategoryResponse>> Handle(
        CreateSeatCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if category with same name already exists
            var existing = await _repository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
            {
                return Response<SeatCategoryResponse>.Failure(
                    new Error(
                        "SeatCategory.AlreadyExists",
                        $"A seat category with name '{request.Name}' already exists"));
            }

            // Create Money value object
            var price = Money.Create(request.Price, request.Currency);

            // Create the aggregate using factory method
            var categoryResult = SeatCategory.Create(
                request.Name,
                request.SeatType,
                price,
                request.Description);

            if (categoryResult.IsFailure)
                return Response<SeatCategoryResponse>.Failure(categoryResult.Error);

            var category = categoryResult.Value;

            // Save to repository
            await _repository.AddAsync(category, cancellationToken);

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
                new Error("SeatCategory.CreationFailed", ex.Message));
        }
    }
}
