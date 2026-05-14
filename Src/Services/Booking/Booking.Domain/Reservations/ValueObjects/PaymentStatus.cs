namespace Booking.Domain.Reservations.ValueObjects;

public sealed class PaymentStatus
{
    public static readonly PaymentStatus Pending = new("Pending");
    public static readonly PaymentStatus Paid = new("Paid");
    public static readonly PaymentStatus Failed = new("Failed");
    public static readonly PaymentStatus Refunded = new("Refunded");

    private readonly string _value;

    private PaymentStatus(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Value => _value;

    public static bool IsValid(string value)
    {
        return value switch
        {
            "Pending" or "Paid" or "Failed" or "Refunded" => true,
            _ => false
        };
    }

    public static PaymentStatus Create(string value)
    {
        if (!IsValid(value))
            throw new DomainException($"Invalid payment status: {value}");

        return value switch
        {
            "Pending" => Pending,
            "Paid" => Paid,
            "Failed" => Failed,
            "Refunded" => Refunded,
            _ => throw new DomainException($"Invalid payment status: {value}")
        };
    }

    public override bool Equals(object? obj) => obj is PaymentStatus other && _value == other._value;
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value;
}
