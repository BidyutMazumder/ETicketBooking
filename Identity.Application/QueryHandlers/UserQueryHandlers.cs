namespace Identity.Application.QueryHandlers;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByIdHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Response<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        return Response<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
}

public sealed class GetUserByEmailHandler : IRequestHandler<GetUserByEmailQuery, Response<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByEmailHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Response<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Response<UserDto>.Failure(new Error("Error.UserNotFound", "User not found"));

        return Response<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
}

public sealed class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, PagedRes<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAllUsersHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<PagedRes<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = users.Select(u => _mapper.Map<UserDto>(u)).ToList();
        
        return new PagedRes<UserDto>(dtos, users.Count, request.Page, request.PageSize);
    }
}