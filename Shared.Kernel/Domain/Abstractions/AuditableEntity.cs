namespace Shared.Kernel.Domain.Abstractions;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity(Guid id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }

    protected AuditableEntity() { }

    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public string? LastModifiedBy { get; protected set; }

    public virtual void MarkUpdated(string? performedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = performedBy;
    }

    public virtual void MarkCreated(string? performedBy = null)
    {
        LastModifiedBy = performedBy;
    }
}