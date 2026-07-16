namespace CollegeLMS.API.SwaggerExamples;

public static class TestResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            title = "Контрольная работа по математике",
            description = "Проверка знаний по теме интегралы",
            timeLimitMinutes = 60,
            maxAttempts = 1,
            type = "Control",
            passingScore = 60,
            autoCheck = true,
            showCorrectAnswers = true,
            shuffleQuestions = false,
            shuffleOptions = false,
            courseId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            courseTitle = "Математика",
            questionCount = 10,
        };
}

public static class QuestionResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            text = "Чему равен интеграл от x?",
            type = "SingleChoice",
            options = "x^2/2 + C\nx^2 + C\nx + C\nx^2/2",
            correctAnswer = "x^2/2 + C",
            points = 5,
            orderIndex = 1,
        };
}
