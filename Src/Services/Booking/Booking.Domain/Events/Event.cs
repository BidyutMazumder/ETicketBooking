namespace Booking.Domain.Events;

using Booking.Domain.Events.Entities;

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

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDateTime { get; private set; }
    public string VenueName { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    public static Event Create(string title, string description, DateTime startDateTime, string venueName)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Event title cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Event description cannot be empty");

        if (startDateTime <= DateTime.UtcNow)
            throw new DomainException("Event start date must be in the future");

        if (string.IsNullOrWhiteSpace(venueName))
            throw new DomainException("Event venue name cannot be empty");

        return new Event(
            Guid.NewGuid(),
            title,
            description,
            startDateTime,
            venueName,
            isPublished: false);
    }

    public void AddSeat(Seat seat)
    {
        if (seat is null)
            throw new ArgumentNullException(nameof(seat));

        if (_seats.Any(s => s.Row == seat.Row && s.Number == seat.Number))
            throw new DomainException($"Seat {seat.Row}{seat.Number} already exists in this event");

        _seats.Add(seat);
    }

    public void RemoveSeat(Seat seat)
    {
        if (seat is null)
            throw new ArgumentNullException(nameof(seat));

        _seats.Remove(seat);
    }

    public Seat? GetSeat(Guid seatId)
    {
        return _seats.FirstOrDefault(s => s.Id == seatId);
    }

    public Seat? GetSeat(string row, int number)
    {
        return _seats.FirstOrDefault(s => s.Row == row && s.Number == number);
    }

    public void Publish()
    {
        if (IsPublished)
            throw new DomainException("Event is already published");

        if (_seats.Count == 0)
            throw new DomainException("Cannot publish an event without seats");

        IsPublished = true;
    }

    public void UpdateDetails(string title, string description, string venueName)
    {
        if (IsPublished)
            throw new DomainException("Cannot update details of a published event");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Event title cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Event description cannot be empty");

        if (string.IsNullOrWhiteSpace(venueName))
            throw new DomainException("Event venue name cannot be empty");

        Title = title;
        Description = description;
        VenueName = venueName;
        UpdatedAt = DateTime.UtcNow;
    }
}
