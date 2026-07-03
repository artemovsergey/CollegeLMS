namespace CollegeLMS.API.SwaggerExamples;

public static class AssignmentResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        courseId = Guid.NewGuid(),
        title = "Домашнее задание №1",
        description = "Решить 5 задач по C#",
        dueDate = DateTime.UtcNow.AddDays(7),
        maxScore = 100,
        order = 1,
        submissionCount = 0,
    };
}
