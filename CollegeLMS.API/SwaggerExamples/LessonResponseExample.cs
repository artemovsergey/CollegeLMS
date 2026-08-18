namespace CollegeLMS.API.SwaggerExamples;

public static class LessonResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            courseId = Guid.NewGuid(),
            title = "Введение в C#",
            content = "Лекция по основам языка C#",
            order = 1,
            kind = "Lecture",
            isCurrent = false,
            testId = (Guid?)null,
            testTitle = (string?)null,
        };
}
