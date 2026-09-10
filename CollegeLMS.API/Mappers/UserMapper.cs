using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Mappers;

public static class UserMapper
{
    public static UserResponse ToDto(this User entity, Guid? teacherId = null)
    {
        var roles = entity.Role.GetRoles();

        return new UserResponse
        {
            Id = entity.Id,
            Login = entity.Login,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = roles.FirstOrDefault().ToString(),
            Roles = roles.Select(role => role.ToString()).ToList(),
            TeacherId = teacherId,
            AvatarUrl = entity.AvatarPath,
        };
    }

    public static ProfileResponse ToProfileDto(this User entity, object? roleData = null)
    {
        var roles = entity.Role.GetRoles();

        var dto = new ProfileResponse
        {
            Id = entity.Id,
            Login = entity.Login,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = roles.FirstOrDefault().ToString(),
            Roles = roles.Select(role => role.ToString()).ToList(),
            AvatarUrl = entity.AvatarPath,
        };

        if (roleData is Teacher teacher)
        {
            dto.TeacherData = new TeacherProfileData
            {
                CyclicalCommission = teacher.CyclicalCommission,
                Position = teacher.Position,
                Category = teacher.Category.ToString(),
            };
        }
        else if (roleData is Student student)
        {
            dto.StudentData = new StudentProfileData
            {
                GroupId = student.GroupId.ToString(),
                GroupName = student.Group?.Name ?? string.Empty,
                RecordBookNumber = student.RecordBookNumber,
            };
        }

        return dto;
    }

    public static User ToEntity(this CreateUserRequest dto)
    {
        var role = dto.Roles is { Count: > 0 }
            ? dto.Roles.Aggregate(UserRole.None, (current, item) => current | item)
            : dto.Role;

        return new User
        {
            Id = Guid.NewGuid(),
            Login = dto.Login,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Role = role,
        };
    }
}
