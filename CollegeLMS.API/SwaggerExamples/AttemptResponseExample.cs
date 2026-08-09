namespace CollegeLMS.API.SwaggerExamples;

public static class AttemptResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            testId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            startedAt = DateTime.UtcNow.AddMinutes(-15),
            completedAt = DateTime.UtcNow,
            status = "Completed",
            score = 34,
            maxScore = 40,
        };
}
