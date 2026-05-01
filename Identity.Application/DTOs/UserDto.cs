namespace Identity.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record CreateUserDto(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role
);

public sealed record UpdateUserDto(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Role
);

public sealed record DeleteUserDto(Guid Id);

public sealed record LoginDto(string Email, string Password);

public sealed record AuthResultDto(
    bool Success,
    string? Token,
    string? Error,
    UserDto? User
);