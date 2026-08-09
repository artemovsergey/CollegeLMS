namespace CollegeLMS.API.SwaggerExamples;

public static class TestResultResponseExample
{
    public static object Create() =>
        new
        {
            attemptId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            score = 34,
            maxScore = 40,
            percentage = 85,
            passed = true,
            completedAt = DateTime.UtcNow,
            answerReviews = new[]
            {
                new
                {
                    questionId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    questionText = "Чему равен интеграл от x?",
                    givenAnswer = "x^2/2 + C",
                    correctAnswer = "x^2/2 + C",
                    isCorrect = true,
                    points = 5,
                },
            },
        };
}
