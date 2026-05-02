namespace Identity.Application.Common.Mappings;

public sealed class UserMapper : IUserMapper
{
    public UserDto MapToDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email.Value,
            FirstName: user.Name.FirstName,
            LastName: user.Name.LastName,
            Role: user.Role.ToString(),
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            IsDeleted: user.IsDeleted
        );
    }

    public IEnumerable<UserDto> MapToDtoList(IEnumerable<User> users)
    {
        return users.Select(MapToDto);
    }
}
