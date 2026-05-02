namespace Identity.Application.Features.Users.Queries.GetUserByEmail;

public sealed class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMapper _mapper;

    public GetUserByEmailQueryHandler(IUserRepository userRepository, IUserMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<UserDto>> Handle(
        GetUserByEmailQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        return Response<UserDto>.Success(_mapper.MapToDto(user));
    }
}
