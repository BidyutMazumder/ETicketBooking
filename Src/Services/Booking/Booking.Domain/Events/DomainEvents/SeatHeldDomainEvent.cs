namespace Booking.Domain.Events.DomainEvents;

public sealed record SeatHeldDomainEvent(
    Guid EventId,
    Guid SeatId,
    Guid UserId,
    DateTime HeldUntilUtc) : IDomainEvent;
