using Identity.Domain.Users.ValueObject;
using Shared.Kernel.Domain.Abstractions;

namespace Identity.Domain.Users;

public sealed class User : SoftDeletableEntity
{
    private User(
        Guid id,
        Email email,
        FullName name,
        string passwordHash,
        Role role) : base(id)
    {
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        Role = role;
    }

    // Required for EF Core
    private User() { }

    public Email Email { get; private set; } = null!;
    public FullName Name { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }

    public static User Create(Email email, FullName name, string passwordHash, Role role)
    {
        var user = new User(Guid.NewGuid(), email, name, passwordHash, role);
        user.RaiseDomainEvent(new Events.UserCreatedDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    public void UpdateName(FullName newName)
    {
        Name = newName;
    }

    public void UpdateRole(Role newRole)
    {
        Role = newRole;
    }

    public void Delete()
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}