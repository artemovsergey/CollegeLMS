namespace CollegeLMS.API.SwaggerExamples;

public static class LectureResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            courseId = Guid.NewGuid(),
            title = "Введение в C#",
            content = "Лекция по основам языка C#",
            order = 1,
        };
}
