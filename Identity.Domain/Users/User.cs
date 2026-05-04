using Identity.Domain.Users.ValueObject;
using Shared.Kernel.Domain.Abstractions;

namespace Identity.Domain.Users;

public sealed class User : SoftDeletableEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];

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
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

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

    public void UpdateRefreshToken(RefreshToken newRefreshToken)
    {
        // Revoke all existing active tokens
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            _refreshTokens.Remove(token);
            _refreshTokens.Add(token.Revoke());
        }

        // Add new refresh token
        _refreshTokens.Add(newRefreshToken);
    }

    public void RevokeAllRefreshTokens()
    {
        for (int i = 0; i < _refreshTokens.Count; i++)
        {
            if (_refreshTokens[i].IsActive)
            {
                _refreshTokens[i] = _refreshTokens[i].Revoke();
            }
        }
    }

    public RefreshToken? GetActiveRefreshToken(string token)
    {
        return _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);
    }
}