namespace Booking.Domain.Events.ValueObjects;

public sealed class SeatStatus
{
    public static readonly SeatStatus Available = new("Available");
    public static readonly SeatStatus Held = new("Held");
    public static readonly SeatStatus Reserved = new("Reserved");
    public static readonly SeatStatus Sold = new("Sold");

    private readonly string _value;

    private SeatStatus(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Value => _value;

    public static bool IsValid(string value)
    {
        return value switch
        {
            "Available" or "Held" or "Reserved" or "Sold" => true,
            _ => false
        };
    }

    public static SeatStatus Create(string value)
    {
        if (!IsValid(value))
            throw new DomainException($"Invalid seat status: {value}");

        return value switch
        {
            "Available" => Available,
            "Held" => Held,
            "Reserved" => Reserved,
            "Sold" => Sold,
            _ => throw new DomainException($"Invalid seat status: {value}")
        };
    }

    public override bool Equals(object? obj) => obj is SeatStatus other && _value == other._value;
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value;
}
