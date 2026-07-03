using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class LoginResponseExample
{
    public static LoginResponse Create() => new()
    {
        Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        User = new UserResponse
        {
            Id = Guid.NewGuid(),
            Email = "user@collegelms.ru",
            FullName = "Иванов Иван Иванович",
            Role = "Teacher",
            IsActive = true,
        },
    };
}
