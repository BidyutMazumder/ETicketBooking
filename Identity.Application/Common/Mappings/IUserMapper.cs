namespace Identity.Application.Common.Mappings;

public interface IUserMapper
{
    UserDto MapToDto(User user);
    IEnumerable<UserDto> MapToDtoList(IEnumerable<User> users);
}
