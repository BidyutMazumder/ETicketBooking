namespace Booking.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing a unique identifier for a SeatCategory.
/// Prevents primitive obsession by wrapping Guid in a strongly-typed value object.
/// </summary>
public sealed class SeatCategoryId : IEquatable<SeatCategoryId>
{
    public Guid Value { get; }

    private SeatCategoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("SeatCategoryId cannot be empty");

        Value = value;
    }

    /// <summary>
    /// Creates a new unique SeatCategoryId.
    /// </summary>
    public static SeatCategoryId Create()
    {
        return new SeatCategoryId(Guid.NewGuid());
    }

    /// <summary>
    /// Creates a SeatCategoryId from an existing Guid value.
    /// </summary>
    public static SeatCategoryId From(Guid value)
    {
        return new SeatCategoryId(value);
    }

    public override bool Equals(object? obj)
    {
        return obj is SeatCategoryId other && Equals(other);
    }

    public bool Equals(SeatCategoryId? other)
    {
        return other?.Value == Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static bool operator ==(SeatCategoryId? left, SeatCategoryId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SeatCategoryId? left, SeatCategoryId? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator Guid(SeatCategoryId categoryId)
    {
        return categoryId.Value;
    }
}
