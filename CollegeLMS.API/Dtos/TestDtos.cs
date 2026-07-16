namespace CollegeLMS.API.Dtos;

public class CreateTestRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public string Type { get; set; } = "SelfStudy";
    public int PassingScore { get; set; } = 60;
    public Guid CourseId { get; set; }
}

public class UpdateTestRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public string Type { get; set; } = "SelfStudy";
    public int PassingScore { get; set; } = 60;
}

public class TestResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public string Type { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public bool AutoCheck { get; set; }
    public bool ShowCorrectAnswers { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
}

public class CreateQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "SingleChoice";
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
}

public class UpdateQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "SingleChoice";
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
}

public class QuestionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; }
    public int OrderIndex { get; set; }
}

public class AssignTestRequest
{
    public Guid GroupId { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public int MaxAttempts { get; set; } = 1;
}

public class TestAssignmentResponse
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public int MaxAttempts { get; set; }
}

public class TestSettingsRequest
{
    public int? PassingScore { get; set; }
    public bool? AutoCheck { get; set; }
    public bool? ShowCorrectAnswers { get; set; }
    public bool? ShuffleQuestions { get; set; }
    public bool? ShuffleOptions { get; set; }
}

public class StartTestResponse
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAt { get; set; }
    public int TimeLimitMinutes { get; set; }
    public List<TestQuestionDto> Questions { get; set; } = new();
}

public class TestQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class SubmitAnswersRequest
{
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid QuestionId { get; set; }
    public string GivenAnswer { get; set; } = string.Empty;
}

public class AttemptResponse
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
}

public class TestResultResponse
{
    public Guid AttemptId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<AnswerReviewDto> AnswerReviews { get; set; } = new();
}

public class AnswerReviewDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string GivenAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
}

public class TestStatsResponse
{
    public int TotalAttempts { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public double AverageScore { get; set; }
    public double MedianScore { get; set; }
    public int MaxScore { get; set; }
    public int MinScore { get; set; }
    public List<StudentResultDto> StudentResults { get; set; } = new();
}

public class StudentResultDto
{
    public string StudentName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public bool Passed { get; set; }
}
