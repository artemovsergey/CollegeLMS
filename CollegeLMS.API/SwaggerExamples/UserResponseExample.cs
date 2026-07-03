using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class UserResponseExample
{
    public static UserResponse Create() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@collegelms.ru",
        FullName = "Иванов Иван Иванович",
        Role = "Teacher",
        IsActive = true,
    };
}
