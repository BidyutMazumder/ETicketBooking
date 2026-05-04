namespace Identity.Application.Features.Auth.Commands.Login;

using Identity.Domain.Users.ValueObject;
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly Shared.Kernel.Domain.Abstractions.IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public LoginCommandHandler(
        IUserRepository userRepository,
        Shared.Kernel.Domain.Abstractions.IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async ValueTask<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Retrieve user by email
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
            throw new Exception($"User with email '{command.Email}' not found.");

        // Verify password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new Exception("Invalid credentials.");

        // Generate tokens
        var accessToken = _jwtProvider.GenerateAccessToken(user.Id, user.Email.Value, user.Role);
        var refreshTokenValue = _jwtProvider.GenerateRefreshToken();

        // Create and store refresh token (15 days expiration)
        var refreshToken = RefreshToken.Create(refreshTokenValue, TimeSpan.FromDays(15));
        user.UpdateRefreshToken(refreshToken);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new AuthResponse(accessToken, refreshTokenValue, refreshToken.ExpiresAt);
    }
}
