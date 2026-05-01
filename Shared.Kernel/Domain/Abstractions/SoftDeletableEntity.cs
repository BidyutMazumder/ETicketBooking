namespace Shared.Kernel.Domain.Abstractions;

public abstract class SoftDeletableEntity : AuditableEntity
{
    protected SoftDeletableEntity(Guid id) : base(id)
    {
        IsDeleted = false;
    }
    protected SoftDeletableEntity() { }

    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

}
