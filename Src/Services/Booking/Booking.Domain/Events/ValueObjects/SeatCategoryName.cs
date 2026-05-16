namespace Booking.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing a seat category name.
/// Encapsulates validation and normalization of category names.
/// Prevents primitive obsession by wrapping string with validation.
/// </summary>
public sealed class SeatCategoryName : IEquatable<SeatCategoryName>
{
    public const int MaxLength = 100;
    public const int MinLength = 2;

    public string Value { get; }

    private SeatCategoryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Seat category name cannot be empty");

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            throw new DomainException($"Seat category name must be at least {MinLength} characters");

        if (trimmed.Length > MaxLength)
            throw new DomainException($"Seat category name must not exceed {MaxLength} characters");

        Value = trimmed;
    }

    /// <summary>
    /// Creates a SeatCategoryName with validation.
    /// </summary>
    public static SeatCategoryName Create(string value)
    {
        return new SeatCategoryName(value);
    }

    /// <summary>
    /// Attempts to create a SeatCategoryName, returning a Result instead of throwing.
    /// </summary>
    public static Response<SeatCategoryName> TryCreate(string value)
    {
        try
        {
            return Response<SeatCategoryName>.Success(new SeatCategoryName(value));
        }
        catch (DomainException ex)
        {
            return Response<SeatCategoryName>.Failure(
                new Error("SeatCategoryName.Invalid", ex.Message));
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is SeatCategoryName other && Equals(other);
    }

    public bool Equals(SeatCategoryName? other)
    {
        return other?.Value == Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(SeatCategoryName? left, SeatCategoryName? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SeatCategoryName? left, SeatCategoryName? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator string(SeatCategoryName name)
    {
        return name.Value;
    }
}
