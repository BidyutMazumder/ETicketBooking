namespace Identity.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMapper _mapper;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IUserMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async ValueTask<Response<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        return Response<UserDto>.Success(_mapper.MapToDto(user));
    }
}
