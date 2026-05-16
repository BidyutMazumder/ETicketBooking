namespace Booking.Domain.Events;

using Booking.Domain.Events.ValueObjects;

/// <summary>
/// SeatCategory aggregate root representing a category of seats with specific pricing and properties.
/// Manages pricing rules for different seat types and allows bulk seat configuration.
/// This is an independent aggregate that can be referenced by multiple Events.
/// </summary>
public sealed class SeatCategory : AuditableEntity
{
    private SeatCategory(
        Guid id,
        SeatCategoryName name,
        SeatType seatType,
        Money basePrice,
        string description) : base(id)
    {
        Name = name;
        SeatType = seatType;
        BasePrice = basePrice;
        Description = description;
        IsActive = true;
    }

    // Required for EF Core
    private SeatCategory() { }

    /// <summary>
    /// Gets the category name (e.g., "VIP Premium", "Standard", "Budget").
    /// </summary>
    public SeatCategoryName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the seat type associated with this category.
    /// </summary>
    public SeatType SeatType { get; private set; } = null!;

    /// <summary>
    /// Gets the base price for seats in this category.
    /// </summary>
    public Money BasePrice { get; private set; } = null!;

    /// <summary>
    /// Gets the description of this category (e.g., what makes it special).
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether this category is available for use.
    /// Inactive categories cannot be used for new seat creation.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the discount percentage applied to the base price (0-100).
    /// Prevents primitive obsession by storing as value.
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    /// <summary>
    /// Creates a new SeatCategory aggregate using the factory method with validation.
    /// </summary>
    public static Response<SeatCategory> Create(
        string name,
        string seatType,
        Money basePrice,
        string description)
    {
        if (!SeatType.IsValid(seatType))
        {
            return Response<SeatCategory>.Failure(
                new Error("SeatCategory.InvalidSeatType", $"Invalid seat type: {seatType}"));
        }

        var nameResult = SeatCategoryName.TryCreate(name);
        if (nameResult.IsFailure)
            return Response<SeatCategory>.Failure(nameResult.Error);

        if (basePrice is null)
        {
            return Response<SeatCategory>.Failure(
                new Error("SeatCategory.NullPrice", "Base price cannot be null"));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Response<SeatCategory>.Failure(
                new Error("SeatCategory.InvalidDescription", "Description cannot be empty"));
        }

        var category = new SeatCategory(
            Guid.NewGuid(),
            nameResult.Data,
            SeatType.Create(seatType),
            basePrice,
            description.Trim());

        return Response<SeatCategory>.Success(category);
    }

    /// <summary>
    /// Updates the base price of this category.
    /// </summary>
    public Result UpdateBasePrice(Money newPrice)
    {
        if (newPrice is null)
            return Result.Failure(new Error("SeatCategory.NullPrice", "Price cannot be null"));

        if (!IsActive)
            return Result.Failure(
                new Error("SeatCategory.InactiveCategory", "Cannot update an inactive category"));

        BasePrice = newPrice;
        return Result.Success();
    }

    /// <summary>
    /// Applies a discount percentage to this category.
    /// The discount is stored but prices are calculated on-the-fly to prevent data inconsistency.
    /// </summary>
    public Result ApplyDiscount(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            return Result.Failure(
                new Error(
                    "SeatCategory.InvalidDiscount",
                    "Discount percentage must be between 0 and 100"));

        if (!IsActive)
            return Result.Failure(
                new Error("SeatCategory.InactiveCategory", "Cannot apply discount to an inactive category"));

        DiscountPercentage = discountPercentage;
        return Result.Success();
    }

    /// <summary>
    /// Calculates the effective price for a seat in this category after applying discounts.
    /// </summary>
    public Money GetEffectivePrice()
    {
        if (DiscountPercentage == 0)
            return BasePrice;

        var discountAmount = BasePrice * (DiscountPercentage / 100m);
        return BasePrice - discountAmount;
    }

    /// <summary>
    /// Deactivates this category. Existing seats with this category remain functional,
    /// but new seats cannot be created with this category.
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(
                new Error("SeatCategory.AlreadyInactive", "Category is already inactive"));

        IsActive = false;
        return Result.Success();
    }

    /// <summary>
    /// Reactivates this category.
    /// </summary>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(
                new Error("SeatCategory.AlreadyActive", "Category is already active"));

        IsActive = true;
        return Result.Success();
    }

    /// <summary>
    /// Updates the description of this category.
    /// </summary>
    public Result UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            return Result.Failure(
                new Error("SeatCategory.InvalidDescription", "Description cannot be empty"));

        if (!IsActive)
            return Result.Failure(
                new Error("SeatCategory.InactiveCategory", "Cannot update an inactive category"));

        Description = newDescription.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Determines if a seat with this category would cost a specific amount.
    /// Used for validation during seat creation.
    /// </summary>
    public bool MatchesPrice(Money expectedPrice)
    {
        if (expectedPrice is null)
            return false;

        var effectivePrice = GetEffectivePrice();
        return effectivePrice.Equals(expectedPrice);
    }
}
