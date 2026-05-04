namespace Identity.Application.Features.Auth.Commands.RefreshToken;

using Identity.Domain.Users.ValueObject;
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
    }

    public async ValueTask<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // Validate the refresh token to extract user info
        var principal = _jwtProvider.ValidateToken(command.RefreshToken);
        if (principal is null)
            throw new Exception("Invalid or expired refresh token.");

        // Extract user ID from claims
        var userIdClaim = principal.FindFirst("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new Exception("Invalid token claims.");

        // Retrieve user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new Exception("User not found.");

        // Check if the refresh token is active and exists
        var activeToken = user.GetActiveRefreshToken(command.RefreshToken);
        if (activeToken is null)
        {
            // Potential token reuse/theft detected - revoke all tokens
            user.RevokeAllRefreshTokens();
            await _userRepository.UpdateAsync(user, cancellationToken);
            throw new Exception("Refresh token has been revoked or already used. All tokens have been invalidated for security.");
        }

        // Generate new tokens with strict rotation
        var newAccessToken = _jwtProvider.GenerateAccessToken(user.Id, user.Email.Value, user.Role);
        var newRefreshTokenValue = _jwtProvider.GenerateRefreshToken();

        // Create new refresh token and revoke the old one
        var newRefreshToken = RefreshToken.Create(newRefreshTokenValue, TimeSpan.FromDays(15));
        user.UpdateRefreshToken(newRefreshToken);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new AuthResponse(newAccessToken, newRefreshTokenValue, newRefreshToken.ExpiresAt);
    }
}
