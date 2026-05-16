namespace Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Money value object representing a monetary amount with currency.
/// This ensures type safety and prevents bugs where different currencies might be accidentally added together.
/// Immutable and provides operator overloading for arithmetic operations.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency cannot be empty");

        if (currency.Length != 3)
            throw new DomainException("Currency code must be 3 characters (ISO 4217)");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code (ISO 4217, uppercase).
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a Money instance with validation.
    /// </summary>
    public static Money Create(decimal amount, string currency)
    {
        return new Money(amount, currency);
    }

    /// <summary>
    /// Creates a zero Money instance for the given currency.
    /// </summary>
    public static Money Zero(string currency)
    {
        return new Money(0, currency);
    }

    /// <summary>
    /// Adds two Money values. Both must have the same currency.
    /// </summary>
    public static Money operator +(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot add money with different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Subtracts two Money values. Both must have the same currency.
    /// </summary>
    public static Money operator -(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot subtract money with different currencies: {left.Currency} and {right.Currency}");

        var result = left.Amount - right.Amount;

        if (result < 0)
            throw new DomainException("Money amount cannot be negative");

        return new Money(result, left.Currency);
    }

    /// <summary>
    /// Multiplies a Money value by a decimal factor.
    /// </summary>
    public static Money operator *(Money money, decimal factor)
    {
        if (money is null)
            throw new ArgumentNullException(nameof(money));

        if (factor < 0)
            throw new DomainException("Multiplication factor cannot be negative");

        return new Money(money.Amount * factor, money.Currency);
    }

    /// <summary>
    /// Multiplies a Money value by a decimal factor.
    /// </summary>
    public static Money operator *(decimal factor, Money money)
    {
        return money * factor;
    }

    /// <summary>
    /// Compares if this Money is greater than another Money value.
    /// </summary>
    public static bool operator >(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Amount > right.Amount;
    }

    /// <summary>
    /// Compares if this Money is less than another Money value.
    /// </summary>
    public static bool operator <(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Amount < right.Amount;
    }

    /// <summary>
    /// Compares if this Money is greater than or equal to another Money value.
    /// </summary>
    public static bool operator >=(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Amount >= right.Amount;
    }

    /// <summary>
    /// Compares if this Money is less than or equal to another Money value.
    /// </summary>
    public static bool operator <=(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));
        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new DomainException(
                $"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Amount <= right.Amount;
    }

    /// <summary>
    /// Compares two Money instances for equality.
    /// </summary>
    public bool Equals(Money? other)
    {
        if (other is null)
            return false;

        return Amount == other.Amount && Currency == other.Currency;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Money instance.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Money money && Equals(money);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    /// <summary>
    /// Returns a string representation of the Money instance.
    /// </summary>
    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }
}
