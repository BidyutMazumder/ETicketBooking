namespace Identity.Application.Features.Users.Queries.GetAllUsers;
public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagedRes<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMapper _mapper;

    public GetAllUsersQueryHandler(IUserRepository userRepository, IUserMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async ValueTask<PagedRes<UserDto>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = _mapper.MapToDtoList(users).ToList();

        var totalCount = await _userRepository.GetCountAsync(cancellationToken);

        return new PagedRes<UserDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
