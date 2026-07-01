using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class UserMapper
{
    public static UserResponse ToDto(this User entity)
    {
        return new UserResponse
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role.ToString(),
        };
    }
}
