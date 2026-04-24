using Shared.Kernel.Domain.Exceptions;

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

    public virtual void Delete(string? performedBy = null)
    {
        if (IsDeleted)
            throw new DomainException("Entity is already deleted.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        LastModifiedBy = performedBy;
    }

    public virtual void Restore(string? performedBy = null)
    {
        if (!IsDeleted)
            throw new DomainException("Entity is not deleted.");

        IsDeleted = false;
        DeletedAt = null;
        LastModifiedBy = performedBy;
    }
}
