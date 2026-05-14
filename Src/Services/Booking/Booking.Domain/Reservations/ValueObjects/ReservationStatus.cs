namespace Booking.Domain.Reservations.ValueObjects;

public sealed class ReservationStatus
{
    public static readonly ReservationStatus Pending = new("Pending");
    public static readonly ReservationStatus Confirmed = new("Confirmed");
    public static readonly ReservationStatus Cancelled = new("Cancelled");

    private readonly string _value;

    private ReservationStatus(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Value => _value;

    public static bool IsValid(string value)
    {
        return value switch
        {
            "Pending" or "Confirmed" or "Cancelled" => true,
            _ => false
        };
    }

    public static ReservationStatus Create(string value)
    {
        if (!IsValid(value))
            throw new DomainException($"Invalid reservation status: {value}");

        return value switch
        {
            "Pending" => Pending,
            "Confirmed" => Confirmed,
            "Cancelled" => Cancelled,
            _ => throw new DomainException($"Invalid reservation status: {value}")
        };
    }

    public override bool Equals(object? obj) => obj is ReservationStatus other && _value == other._value;
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value;
}
