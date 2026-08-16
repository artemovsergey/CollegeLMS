using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class UserResponseExample
{
    public static UserResponse Create() =>
        new()
        {
            Id = Guid.NewGuid(),
            Login = "ivanov",
            Email = "user@collegelms.ru",
            FullName = "Иванов Иван Иванович",
            Role = "Teacher",
            AvatarUrl = "/uploads/avatars/11f4a1c2-0000-4000-8000-000000000001.jpg",
        };
}
