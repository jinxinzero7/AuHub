using Identity.Application.Commands.Auth;
using Identity.Domain.Entities;

namespace Identity.Application.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }
}
