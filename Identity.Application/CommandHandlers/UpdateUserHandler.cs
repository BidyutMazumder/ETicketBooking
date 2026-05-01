namespace Identity.Application.CommandHandlers;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UpdateUserHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Response<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        if (!string.IsNullOrEmpty(request.FirstName) || !string.IsNullOrEmpty(request.LastName))
        {
            var newName = new FullName(
                request.FirstName ?? user.Name.FirstName,
                request.LastName ?? user.Name.LastName);
            user.UpdateName(newName);
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            var newRole = Enum.Parse<Role>(request.Role, ignoreCase: true);
            user.UpdateRole(newRole);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Response<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
}