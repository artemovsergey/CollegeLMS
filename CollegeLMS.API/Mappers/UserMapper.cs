using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class UserMapper
{
    public static UserResponse ToDto(this User entity, Guid? teacherId = null)
    {
        return new UserResponse
        {
            Id = entity.Id,
            Login = entity.Login,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role.ToString(),
            TeacherId = teacherId,
            AvatarUrl = entity.AvatarPath,
        };
    }

    public static ProfileResponse ToProfileDto(this User entity, object? roleData = null)
    {
        var dto = new ProfileResponse
        {
            Id = entity.Id,
            Login = entity.Login,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role.ToString(),
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
        return new User
        {
            Id = Guid.NewGuid(),
            Login = dto.Login,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Role = dto.Role,
        };
    }
}
