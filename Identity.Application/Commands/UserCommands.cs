namespace Identity.Application.Commands;

public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role
) : IRequest<Response<UserDto>>;

public sealed record UpdateUserCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Role
) : IRequest<Response<UserDto>>;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Response<bool>>;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;