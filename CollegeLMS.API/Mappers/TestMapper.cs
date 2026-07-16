using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Mappers;

public static class TestMapper
{
    public static TestResponse ToDto(this Test test) =>
        new()
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            TimeLimitMinutes = test.TimeLimitMinutes,
            MaxAttempts = test.MaxAttempts,
            Type = test.Type.ToString(),
            PassingScore = test.PassingScore,
            AutoCheck = test.AutoCheck,
            ShowCorrectAnswers = test.ShowCorrectAnswers,
            ShuffleQuestions = test.ShuffleQuestions,
            ShuffleOptions = test.ShuffleOptions,
            CourseId = test.CourseId,
            CourseTitle = test.Course?.Title ?? string.Empty,
            QuestionCount = test.Questions?.Count ?? 0,
        };

    public static QuestionResponse ToDto(this TestQuestion question) =>
        new()
        {
            Id = question.Id,
            Text = question.Text,
            Type = question.Type.ToString(),
            Options = question.Options,
            CorrectAnswer = question.CorrectAnswer,
            Points = question.Points,
            OrderIndex = question.OrderIndex,
        };

    public static TestAssignmentResponse ToDto(this TestAssignment assignment) =>
        new()
        {
            Id = assignment.Id,
            TestId = assignment.TestId,
            GroupId = assignment.GroupId,
            GroupName = assignment.Group?.Name ?? string.Empty,
            OpenDate = assignment.OpenDate,
            CloseDate = assignment.CloseDate,
            MaxAttempts = assignment.MaxAttempts,
        };

    public static AttemptResponse ToDto(this TestAttempt attempt) =>
        new()
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            Status = attempt.Status.ToString(),
            Score = attempt.Score,
            MaxScore = attempt.MaxScore,
        };

    public static TestQuestionDto ToQuestionDto(this TestQuestion question) =>
        new()
        {
            Id = question.Id,
            Text = question.Text,
            Type = question.Type.ToString(),
            Options = question.Options,
            OrderIndex = question.OrderIndex,
        };
}
