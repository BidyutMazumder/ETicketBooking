using Shared.Kernel.Domain.Abstractions;
using Shared.Kernel.Domain.Exceptions;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new DomainException("Entity Id cannot be empty.");

        Id = id;
    }

    protected Entity() { } // EF Core

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
