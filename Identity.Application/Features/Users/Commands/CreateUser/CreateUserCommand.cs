namespace Identity.Application.Features.Users.Commands.CreateUser;
public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role
) : IRequest<Response<UserDto>>;
