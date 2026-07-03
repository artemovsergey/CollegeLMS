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
            IsActive = entity.IsActive,
        };
    }

    public static User ToEntity(this CreateUserRequest dto)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Role = dto.Role,
            IsActive = true,
        };
    }
}
