namespace Identity.Domain.Users.ValueObject;

public sealed record RefreshToken
{
    public string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; init; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    public RefreshToken(string token, DateTime expiresAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        RevokedAt = null;
    }

    public static RefreshToken Create(string token, TimeSpan expirationDuration)
    {
        return new RefreshToken(token, DateTime.UtcNow.Add(expirationDuration));
    }

    public RefreshToken Revoke()
    {
        return this with { RevokedAt = DateTime.UtcNow };
    }
}
