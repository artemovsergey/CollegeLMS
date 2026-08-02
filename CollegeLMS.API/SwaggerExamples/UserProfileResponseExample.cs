using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class UserProfileResponseExample
{
    public static UserProfileResponse Create() =>
        new()
        {
            User = UserResponseExample.Create(),
            Courses = new List<UserCourseItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "МДК 09.01 Проектирование и разработка веб-приложений",
                },
            },
        };
}
