namespace Identity.Application.Features.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<Response<UserDto>>;

public sealed record GetUserByEmailQuery(string Email) : IRequest<Response<UserDto>>;

public sealed record GetAllUsersQuery(int Page = 1, int PageSize = 10) : IRequest<PagedRes<UserDto>>;
