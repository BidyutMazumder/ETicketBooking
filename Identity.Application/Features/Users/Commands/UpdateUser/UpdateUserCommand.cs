namespace Identity.Application.Features.Users.Commands.UpdateUser;
public sealed record UpdateUserCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Role
) : IRequest<Response<UserDto>>;
