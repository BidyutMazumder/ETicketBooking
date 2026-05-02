namespace Identity.Application.Features.Users.Commands.UpdateUser;
public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMapper _mapper;

    public UpdateUserCommandHandler(IUserRepository userRepository, IUserMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<UserDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            var firstName = request.FirstName ?? user.Name.FirstName;
            var lastName = request.LastName ?? user.Name.LastName;
            var newFullName = new FullName(firstName, lastName);
            user.UpdateName(newFullName);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var newRole = Enum.Parse<Role>(request.Role, ignoreCase: true);
            user.UpdateRole(newRole);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Response<UserDto>.Success(_mapper.MapToDto(user));
    }
}
