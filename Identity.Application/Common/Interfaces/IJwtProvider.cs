namespace Identity.Application.Common.Interfaces;

public interface IJwtProvider
{
    string GenerateAccessToken(Guid userId, string email, Role role);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
