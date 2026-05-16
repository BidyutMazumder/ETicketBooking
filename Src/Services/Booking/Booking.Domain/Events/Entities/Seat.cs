using Booking.Domain.Events.ValueObjects;
namespace Booking.Domain.Events.Entities;

/// <summary>
/// Seat entity within an Event aggregate.
/// Represents a physical seat with its location, category, pricing, and reservation status.
/// This entity is immutable once created and can only be modified through explicit domain methods.
/// </summary>
public sealed class Seat : Entity
{
    private Seat(
        Guid id,
        string row,
        int number,
        SeatType type,
        Money price,
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

    /// <summary>
    /// Gets the row identifier (e.g., "A", "B", "1", etc.).
    /// </summary>
    public string Row { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the seat number within the row.
    /// </summary>
    public int Number { get; private set; }

    /// <summary>
    /// Gets the seat type/category (e.g., VIP, Regular, Economy).
    /// </summary>
    public SeatType Type { get; private set; } = null!;

    /// <summary>
    /// Gets the price of the seat as a Money value object.
    /// Ensures currency consistency and type safety.
    /// </summary>
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Gets the current reservation status of the seat.
    /// </summary>
    public SeatStatus Status { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC time until which the seat is held for reservation.
    /// </summary>
    public DateTime? HeldUntilUtc { get; private set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Creates a new Seat entity with validation using the Result pattern.
    /// </summary>
    public static Response<Seat> Create(string row, int number, SeatType type, Money price)
    {
        if (string.IsNullOrWhiteSpace(row))
            return Response<Seat>.Failure(new Error("Seat.InvalidRow", "Seat row cannot be empty"));

        if (number <= 0)
            return Response<Seat>.Failure(new Error("Seat.InvalidNumber", "Seat number must be greater than zero"));

        if (type is null)
            return Response<Seat>.Failure(new Error("Seat.NullType", "Seat type cannot be null"));

        if (price is null)
            return Response<Seat>.Failure(new Error("Seat.NullPrice", "Seat price cannot be null"));

        return Response<Seat>.Success(
            new Seat(
                Guid.NewGuid(),
                row,
                number,
                type,
                price,
                SeatStatus.Available));
    }

    public Result Hold(TimeSpan duration)
    {
        if (Status == SeatStatus.Held || Status == SeatStatus.Reserved || Status == SeatStatus.Sold)
            return Result.Failure(new Error(
                "Seat.CannotHold",
                $"Cannot hold a seat that is {Status.Value}"));

        if (duration <= TimeSpan.Zero)
            return Result.Failure(new Error(
                "Seat.InvalidHoldDuration",
                "Hold duration must be greater than zero"));

        Status = SeatStatus.Held;
        HeldUntilUtc = DateTime.UtcNow.Add(duration);

        return Result.Success();
    }

    /// <summary>
    /// Releases a held seat back to Available status.
    /// </summary>
    public Result Release()
    {
        if (Status != SeatStatus.Held)
            return Result.Failure(new Error(
                "Seat.CannotRelease",
                "Only held seats can be released"));

        Status = SeatStatus.Available;
        HeldUntilUtc = null;

        return Result.Success();
    }

    /// <summary>
    /// Reserves a held seat, moving it to Reserved status.
    /// </summary>
    public Result Reserve()
    {
        if (Status != SeatStatus.Held)
            return Result.Failure(new Error(
                "Seat.CannotReserve",
                "Only held seats can be reserved"));

        Status = SeatStatus.Reserved;
        HeldUntilUtc = null;

        return Result.Success();
    }

    /// <summary>
    /// Marks a reserved seat as Sold.
    /// </summary>
    public Result Sell()
    {
        if (Status != SeatStatus.Reserved)
            return Result.Failure(new Error(
                "Seat.CannotSell",
                "Only reserved seats can be sold"));

        Status = SeatStatus.Sold;

        return Result.Success();
    }

    /// <summary>
    /// Checks if the hold period has expired for a held seat.
    /// </summary>
    public bool IsHoldExpired()
    {
        return Status == SeatStatus.Held && HeldUntilUtc.HasValue && HeldUntilUtc < DateTime.UtcNow;
    }
}
