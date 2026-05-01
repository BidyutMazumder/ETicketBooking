namespace Identity.Domain.Users.Events;

public sealed record UserCreatedDomainEvent(Guid UserId, string Email) : IDomainEvent;