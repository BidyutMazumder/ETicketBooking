namespace Booking.Domain.Events.ValueObjects;

public sealed class SeatType
{
    public static readonly SeatType VIP = new("VIP");
    public static readonly SeatType Regular = new("Regular");
    public static readonly SeatType Economy = new("Economy");

    private readonly string _value;

    private SeatType(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Value => _value;

    public static bool IsValid(string value)
    {
        return value switch
        {
            "VIP" or "Regular" or "Economy" => true,
            _ => false
        };
    }

    public static SeatType Create(string value)
    {
        if (!IsValid(value))
            throw new DomainException($"Invalid seat type: {value}");

        return value switch
        {
            "VIP" => VIP,
            "Regular" => Regular,
            "Economy" => Economy,
            _ => throw new DomainException($"Invalid seat type: {value}")
        };
    }

    public override bool Equals(object? obj) => obj is SeatType other && _value == other._value;
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value;
}
