namespace Identity.Application.Mappings;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Value))
            .ForMember(d => d.FirstName, o => o.MapFrom(s => s.Name.FirstName))
            .ForMember(d => d.LastName, o => o.MapFrom(s => s.Name.LastName))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));
    }
}