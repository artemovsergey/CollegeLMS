namespace CollegeLMS.API.SwaggerExamples;

public static class TestStatsResponseExample
{
    public static object Create() =>
        new
        {
            totalAttempts = 12,
            passedCount = 9,
            failedCount = 3,
            averageScore = 31.5,
            medianScore = 33.0,
            maxScore = 40,
            minScore = 12,
            studentResults = new[]
            {
                new
                {
                    studentName = "Иванов Иван Иванович",
                    groupName = "ИСП-31",
                    score = 34,
                    maxScore = 40,
                    passed = true,
                },
            },
        };
}
