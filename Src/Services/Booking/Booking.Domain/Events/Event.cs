namespace Booking.Domain.Events;

using Booking.Domain.Events.Entities;
using Booking.Domain.Events.ValueObjects;

/// <summary>
/// Event aggregate root representing a ticketed event with its associated seats and pricing.
/// This aggregate enforces consistency boundaries ensuring seat uniqueness and inventory integrity.
/// All seat modifications must happen through this aggregate root.
/// </summary>
public sealed class Event : AuditableEntity
{
    private readonly List<Seat> _seats = [];

    private Event(
        Guid id,
        string title,
        string description,
        DateTime startDateTime,
        string venueName,
        bool isPublished) : base(id)
    {
        Title = title;
        Description = description;
        StartDateTime = startDateTime;
        VenueName = venueName;
        IsPublished = isPublished;
    }

    // Required for EF Core
    private Event() { }

    /// <summary>
    /// Gets the event title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the event description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the event start date and time in UTC.
    /// </summary>
    public DateTime StartDateTime { get; private set; }

    /// <summary>
    /// Gets the venue name where the event will be held.
    /// </summary>
    public string VenueName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the event is published (visible to customers).
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// Gets the collection of seats associated with this event.
    /// </summary>
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    /// <summary>
    /// Gets the total number of seats in the event.
    /// </summary>
    public int TotalSeats => _seats.Count;

    /// <summary>
    /// Gets the count of available seats.
    /// </summary>
    public int AvailableSeats => _seats.Count(s => s.Status == SeatStatus.Available);

    /// <summary>
    /// Creates a new Event aggregate using the factory method with validation.
    /// </summary>
    public static Response<Event> Create(
        string title,
        string description,
        DateTime startDateTime,
        string venueName)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Response<Event>.Failure(new Error("Event.InvalidTitle", "Event title cannot be empty"));

        if (string.IsNullOrWhiteSpace(description))
            return Response<Event>.Failure(new Error("Event.InvalidDescription", "Event description cannot be empty"));

        if (startDateTime <= DateTime.UtcNow)
            return Response<Event>.Failure(new Error(
                "Event.InvalidStartDate",
                "Event start date must be in the future"));

        if (string.IsNullOrWhiteSpace(venueName))
            return Response<Event>.Failure(new Error("Event.InvalidVenue", "Event venue name cannot be empty"));

        return Response<Event>.Success(
            new Event(
                Guid.NewGuid(),
                title,
                description,
                startDateTime,
                venueName,
                isPublished: false));
    }

    /// <summary>
    /// Adds a single seat to the event with invariant checking.
    /// Ensures seat numbers are unique within the event.
    /// </summary>
    public Result AddSeat(Seat seat)
    {
        if (seat is null)
            return Result.Failure(new Error("Event.NullSeat", "Seat cannot be null"));

        // Check for duplicate seat
        if (_seats.Any(s => s.Row == seat.Row && s.Number == seat.Number))
            return Result.Failure(new Error(
                "Event.DuplicateSeat",
                $"Seat {seat.Row}{seat.Number} already exists in this event"));

        _seats.Add(seat);
        return Result.Success();
    }

    /// <summary>
    /// Removes a seat from the event.
    /// </summary>
    public Result RemoveSeat(Seat seat)
    {
        if (seat is null)
            return Result.Failure(new Error("Event.NullSeat", "Seat cannot be null"));

        var removed = _seats.Remove(seat);

        return removed
            ? Result.Success()
            : Result.Failure(new Error(
                "Event.SeatNotFound",
                "The specified seat was not found in this event"));
    }

    /// <summary>
    /// Retrieves a seat by its ID.
    /// </summary>
    public Seat? GetSeat(Guid seatId)
    {
        return _seats.FirstOrDefault(s => s.Id == seatId);
    }

    /// <summary>
    /// Retrieves a seat by its row and number.
    /// </summary>
    public Seat? GetSeatByRowAndNumber(string row, int number)
    {
        return _seats.FirstOrDefault(s => s.Row == row && s.Number == number);
    }

    /// <summary>
    /// Generates seats in bulk according to a SeatingPlan configuration.
    /// This method enforces the aggregate root's consistency boundary by creating all seats atomically.
    /// </summary>
    public Result GenerateSeats(SeatingPlan seatingPlan)
    {
        if (seatingPlan is null)
            return Result.Failure(new Error("Event.NullSeatingPlan", "Seating plan cannot be null"));

        if (_seats.Count > 0)
            return Result.Failure(new Error(
                "Event.SeatsAlreadyGenerated",
                "Seats have already been generated for this event"));

        var generatedSeats = new List<Seat>();

        // Validate all seats can be created before adding any
        foreach (var planRow in seatingPlan.Rows)
        {
            for (int seatNumber = planRow.StartNumber; seatNumber <= planRow.EndNumber; seatNumber++)
            {
                var seatResult = Seat.Create(
                    planRow.Row,
                    seatNumber,
                    planRow.SeatType,
                    planRow.Price);

                if (seatResult.IsFailure)
                    return Result.Failure(seatResult.Error);

                generatedSeats.Add(seatResult.Value);
            }
        }

        // All validations passed, add all seats to the aggregate
        foreach (var seat in generatedSeats)
        {
            var addResult = AddSeat(seat);
            if (addResult.IsFailure)
                return addResult;
        }

        return Result.Success();
    }

    /// <summary>
    /// Publishes the event, making it visible to customers.
    /// </summary>
    public Result PublishEvent()
    {
        if (IsPublished)
            return Result.Failure(new Error(
                "Event.AlreadyPublished",
                "This event is already published"));

        if (_seats.Count == 0)
            return Result.Failure(new Error(
                "Event.NoSeats",
                "Cannot publish an event without any seats configured"));

        IsPublished = true;
        return Result.Success();
    }

    /// <summary>
    /// Unpublishes the event, making it invisible to customers.
    /// </summary>
    public Result UnpublishEvent()
    {
        if (!IsPublished)
            return Result.Failure(new Error(
                "Event.NotPublished",
                "This event is not currently published"));

        IsPublished = false;
        return Result.Success();
    }

    /// <summary>
    /// Updates event details with validation.
    /// </summary>
    public Result UpdateDetails(string title, string description, string venueName)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(new Error("Event.InvalidTitle", "Event title cannot be empty"));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(new Error("Event.InvalidDescription", "Event description cannot be empty"));

        if (string.IsNullOrWhiteSpace(venueName))
            return Result.Failure(new Error("Event.InvalidVenue", "Event venue name cannot be empty"));

        Title = title;
        Description = description;
        VenueName = venueName;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Gets the total revenue from sold seats.
    /// </summary>
    public Money GetTotalRevenue()
    {
        var soldSeats = _seats.Where(s => s.Status == SeatStatus.Sold).ToList();

        if (soldSeats.Count == 0)
        {
            // Return zero in the currency of the first seat if available
            var firstSeat = _seats.FirstOrDefault();
            if (firstSeat is not null)
            {
                return Money.Zero(firstSeat.Price.Currency);
            }
            throw new DomainException("Cannot calculate revenue for an event without seats");
        }

        var total = soldSeats[0].Price;
        for (int i = 1; i < soldSeats.Count; i++)
        {
            total = total + soldSeats[i].Price;
        }

        return total;
    }

    /// <summary>
    /// Gets revenue from sold seats by seat type.
    /// </summary>
    public Dictionary<string, Money> GetRevenueByType()
    {
        var revenueByType = new Dictionary<string, Money>();

        var soldByType = _seats
            .Where(s => s.Status == SeatStatus.Sold)
            .GroupBy(s => s.Type.Value);

        foreach (var group in soldByType)
        {
            var total = group.First().Price;
            foreach (var seat in group.Skip(1))
            {
                total = total + seat.Price;
            }
            revenueByType[group.Key] = total;
        }

        return revenueByType;
    }
}
