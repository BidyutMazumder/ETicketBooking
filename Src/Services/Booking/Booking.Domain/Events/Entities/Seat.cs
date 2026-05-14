using Booking.Domain.Events.ValueObjects;
namespace Booking.Domain.Events.Entities;

public sealed class Seat : Entity
{
    private Seat(
        Guid id,
        string row,
        int number,
        SeatType type,
        decimal price,
        SeatStatus status) : base(id)
    {
        Row = row;
        Number = number;
        Type = type;
        Price = price;
        Status = status;
    }

    // Required for EF Core
    private Seat() { }

    public string Row { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public SeatType Type { get; private set; } = null!;
    public decimal Price { get; private set; }
    public SeatStatus Status { get; private set; } = null!;
    public DateTime? HeldUntilUtc { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static Seat Create(string row, int number, SeatType type, decimal price)
    {
        if (string.IsNullOrWhiteSpace(row))
            throw new DomainException("Seat row cannot be empty");

        if (number <= 0)
            throw new DomainException("Seat number must be greater than zero");

        if (price < 0)
            throw new DomainException("Seat price cannot be negative");

        return new Seat(
            Guid.NewGuid(),
            row,
            number,
            type,
            price,
            SeatStatus.Available);
    }

    public void Hold(TimeSpan duration)
    {
        if (Status == SeatStatus.Held || Status == SeatStatus.Reserved || Status == SeatStatus.Sold)
            throw new DomainException($"Cannot hold a seat that is {Status.Value}");

        Status = SeatStatus.Held;
        HeldUntilUtc = DateTime.UtcNow.Add(duration);
    }

    public void Release()
    {
        if (Status != SeatStatus.Held)
            throw new DomainException("Only held seats can be released");

        Status = SeatStatus.Available;
        HeldUntilUtc = null;
    }

    public void Reserve()
    {
        if (Status != SeatStatus.Held)
            throw new DomainException("Only held seats can be reserved");

        Status = SeatStatus.Reserved;
        HeldUntilUtc = null;
    }

    public void Sell()
    {
        if (Status != SeatStatus.Reserved)
            throw new DomainException("Only reserved seats can be sold");

        Status = SeatStatus.Sold;
    }

    public bool IsHoldExpired()
    {
        return Status == SeatStatus.Held && HeldUntilUtc < DateTime.UtcNow;
    }
}
