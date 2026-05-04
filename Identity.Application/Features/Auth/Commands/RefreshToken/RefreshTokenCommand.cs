namespace Identity.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResponse>;
