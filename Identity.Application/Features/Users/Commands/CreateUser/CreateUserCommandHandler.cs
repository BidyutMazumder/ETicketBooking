namespace Identity.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUserMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async ValueTask<Response<UserDto>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            return Response<UserDto>.Failure(new Error("Error.EmailExists", "Email already exists"));

        var email = Email.Create(request.Email);
        var fullName = new FullName(request.FirstName, request.LastName);
        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(email, fullName, passwordHash, role);

        await _userRepository.AddAsync(user, cancellationToken);

        return Response<UserDto>.Success(_mapper.MapToDto(user));
    }
}
