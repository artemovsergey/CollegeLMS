using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class TestingService(AppDbContext db) : ITestingService
{
    public async Task<Result<List<TestResponse>>> GetAllAsync(
        Guid? courseId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var query = db.Tests.AsNoTracking()
            .Include(t => t.Course)
            .Include(t => t.Questions)
            .AsQueryable();

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null)
                return Result<List<TestResponse>>.Fail("Преподаватель не найден", 404);
            query = query.Where(t => t.Course.TeacherId == teacher.Id);
        }

        if (courseId.HasValue)
            query = query.Where(t => t.CourseId == courseId.Value);

        var tests = await query.OrderBy(t => t.Title).ToListAsync(ct);
        return Result<List<TestResponse>>.Ok(tests.Select(t => t.ToDto()).ToList());
    }

    public async Task<Result<TestResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var test = await db.Tests.AsNoTracking()
            .Include(t => t.Course)
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (test is null)
            return Result<TestResponse>.Fail("Тест не найден", 404);

        return Result<TestResponse>.Ok(test.ToDto());
    }

    public async Task<Result<TestResponse>> CreateAsync(
        CreateTestRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        if (!Enum.TryParse<TestType>(request.Type, out var testType))
            return Result<TestResponse>.Fail("Некорректный тип теста", 400);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Result<TestResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<TestResponse>.Fail("У вас нет прав на создание теста в этом курсе", 403);
        }

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            TimeLimitMinutes = request.TimeLimitMinutes,
            MaxAttempts = request.MaxAttempts,
            Type = testType,
            PassingScore = request.PassingScore,
            CourseId = request.CourseId,
        };
        db.Tests.Add(test);
        await db.SaveChangesAsync(ct);

        test = await db.Tests.Include(t => t.Course).Include(t => t.Questions)
            .FirstAsync(t => t.Id == test.Id, ct);

        return Result<TestResponse>.Ok(test.ToDto());
    }

    public async Task<Result<TestResponse>> UpdateAsync(
        Guid id,
        UpdateTestRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.Include(t => t.Course).Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (test is null)
            return Result<TestResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<TestResponse>.Fail("У вас нет прав на редактирование этого теста", 403);

        if (!Enum.TryParse<TestType>(request.Type, out var testType))
            return Result<TestResponse>.Fail("Некорректный тип теста", 400);

        test.Title = request.Title;
        test.Description = request.Description;
        test.TimeLimitMinutes = request.TimeLimitMinutes;
        test.MaxAttempts = request.MaxAttempts;
        test.Type = testType;
        test.PassingScore = request.PassingScore;
        test.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<TestResponse>.Ok(test.ToDto());
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (test is null)
            return Result.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на удаление этого теста", 403);

        db.Tests.Remove(test);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<List<QuestionResponse>>> GetQuestionsAsync(
        Guid testId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<List<QuestionResponse>>.Fail("Тест не найден", 404);

        var questions = await db.TestQuestions.AsNoTracking()
            .Where(q => q.TestId == testId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);

        return Result<List<QuestionResponse>>.Ok(questions.Select(q => q.ToDto()).ToList());
    }

    public async Task<Result<QuestionResponse>> CreateQuestionAsync(
        Guid testId,
        CreateQuestionRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<QuestionResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<QuestionResponse>.Fail("У вас нет прав на редактирование этого теста", 403);

        if (!Enum.TryParse<QuestionType>(request.Type, out var questionType))
            return Result<QuestionResponse>.Fail("Некорректный тип вопроса", 400);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = request.Text,
            Type = questionType,
            Options = request.Options,
            CorrectAnswer = request.CorrectAnswer,
            Points = request.Points,
            OrderIndex = request.OrderIndex,
            TestId = testId,
        };
        db.TestQuestions.Add(question);
        await db.SaveChangesAsync(ct);

        return Result<QuestionResponse>.Ok(question.ToDto());
    }

    public async Task<Result<QuestionResponse>> UpdateQuestionAsync(
        Guid testId,
        Guid questionId,
        UpdateQuestionRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<QuestionResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<QuestionResponse>.Fail("У вас нет прав на редактирование этого теста", 403);

        var question = await db.TestQuestions.FirstOrDefaultAsync(q => q.Id == questionId && q.TestId == testId, ct);
        if (question is null)
            return Result<QuestionResponse>.Fail("Вопрос не найден", 404);

        if (!Enum.TryParse<QuestionType>(request.Type, out var questionType))
            return Result<QuestionResponse>.Fail("Некорректный тип вопроса", 400);

        question.Text = request.Text;
        question.Type = questionType;
        question.Options = request.Options;
        question.CorrectAnswer = request.CorrectAnswer;
        question.Points = request.Points;
        question.OrderIndex = request.OrderIndex;
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<QuestionResponse>.Ok(question.ToDto());
    }

    public async Task<Result> DeleteQuestionAsync(
        Guid testId,
        Guid questionId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на редактирование этого теста", 403);

        var question = await db.TestQuestions.FirstOrDefaultAsync(q => q.Id == questionId && q.TestId == testId, ct);
        if (question is null)
            return Result.Fail("Вопрос не найден", 404);

        db.TestQuestions.Remove(question);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<TestAssignmentResponse>> AssignTestAsync(
        Guid testId,
        AssignTestRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<TestAssignmentResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<TestAssignmentResponse>.Fail("У вас нет прав на назначение теста", 403);

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<TestAssignmentResponse>.Fail("Группа не найдена", 404);

        var assignment = new TestAssignment
        {
            Id = Guid.NewGuid(),
            TestId = testId,
            GroupId = request.GroupId,
            OpenDate = request.OpenDate,
            CloseDate = request.CloseDate,
            MaxAttempts = request.MaxAttempts,
        };
        db.TestAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        assignment = await db.TestAssignments.Include(a => a.Group)
            .FirstAsync(a => a.Id == assignment.Id, ct);

        return Result<TestAssignmentResponse>.Ok(assignment.ToDto());
    }

    public async Task<Result<List<TestAssignmentResponse>>> GetAssignmentsAsync(
        Guid testId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<List<TestAssignmentResponse>>.Fail("Тест не найден", 404);

        var assignments = await db.TestAssignments.AsNoTracking()
            .Include(a => a.Group)
            .Where(a => a.TestId == testId)
            .OrderBy(a => a.OpenDate)
            .ToListAsync(ct);

        return Result<List<TestAssignmentResponse>>.Ok(assignments.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result> RemoveAssignmentAsync(
        Guid testId,
        Guid assignmentId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на редактирование этого теста", 403);

        var assignment = await db.TestAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TestId == testId, ct);
        if (assignment is null)
            return Result.Fail("Назначение не найдено", 404);

        db.TestAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<TestResponse>> UpdateSettingsAsync(
        Guid testId,
        TestSettingsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.Include(t => t.Course).Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<TestResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<TestResponse>.Fail("У вас нет прав на редактирование этого теста", 403);

        if (request.PassingScore.HasValue)
            test.PassingScore = request.PassingScore.Value;
        if (request.AutoCheck.HasValue)
            test.AutoCheck = request.AutoCheck.Value;
        if (request.ShowCorrectAnswers.HasValue)
            test.ShowCorrectAnswers = request.ShowCorrectAnswers.Value;
        if (request.ShuffleQuestions.HasValue)
            test.ShuffleQuestions = request.ShuffleQuestions.Value;
        if (request.ShuffleOptions.HasValue)
            test.ShuffleOptions = request.ShuffleOptions.Value;
        test.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<TestResponse>.Ok(test.ToDto());
    }

    public async Task<Result<StartTestResponse>> StartTestAsync(
        Guid testId,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var test = await db.Tests.AsNoTracking()
            .Include(t => t.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<StartTestResponse>.Fail("Тест не найден", 404);

        if (test.Questions.Count == 0)
            return Result<StartTestResponse>.Fail("Тест не содержит вопросов", 400);

        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<StartTestResponse>.Fail("Студент не найден", 404);

        var canAccess = await db.TestAssignments.AsNoTracking()
            .AnyAsync(a => a.TestId == testId && a.GroupId == student.GroupId
                && a.OpenDate <= DateTime.UtcNow && a.CloseDate >= DateTime.UtcNow, ct);
        if (!canAccess)
            return Result<StartTestResponse>.Fail("Тест не доступен для вашей группы", 403);

        var attemptCount = await db.TestAttempts
            .CountAsync(a => a.TestId == testId && a.StudentId == student.Id, ct);
        if (attemptCount >= test.MaxAttempts)
            return Result<StartTestResponse>.Fail("Превышено количество попыток", 409);

        var attempt = new TestAttempt
        {
            Id = Guid.NewGuid(),
            TestId = testId,
            StudentId = student.Id,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
            MaxScore = test.Questions.Sum(q => q.Points),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        var questions = test.ShuffleQuestions
            ? test.Questions.OrderBy(_ => Random.Shared.Next()).ToList()
            : test.Questions.ToList();

        return Result<StartTestResponse>.Ok(new StartTestResponse
        {
            AttemptId = attempt.Id,
            StartedAt = attempt.StartedAt,
            TimeLimitMinutes = test.TimeLimitMinutes,
            Questions = questions.Select(q => new TestQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type.ToString(),
                Options = q.ShuffleOptions() ?? q.Options,
                OrderIndex = q.OrderIndex,
            }).ToList(),
        });
    }

    public async Task<Result<AttemptResponse>> SubmitAnswersAsync(
        Guid testId,
        Guid attemptId,
        SubmitAnswersRequest request,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var attempt = await db.TestAttempts
            .Include(a => a.Test)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.TestId == testId, ct);
        if (attempt is null)
            return Result<AttemptResponse>.Fail("Попытка не найдена", 404);

        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null || attempt.StudentId != student.Id)
            return Result<AttemptResponse>.Fail("Это не ваша попытка", 403);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result<AttemptResponse>.Fail("Попытка уже завершена", 400);

        var elapsed = DateTime.UtcNow - attempt.StartedAt;
        if (elapsed.TotalMinutes > attempt.Test.TimeLimitMinutes)
        {
            attempt.Status = AttemptStatus.TimedOut;
            attempt.CompletedAt = attempt.StartedAt.AddMinutes(attempt.Test.TimeLimitMinutes);
            await db.SaveChangesAsync(ct);
            return Result<AttemptResponse>.Fail("Время вышло", 408);
        }

        var questions = await db.TestQuestions.AsNoTracking()
            .Where(q => q.TestId == testId)
            .ToListAsync(ct);

        int score = 0;
        foreach (var answer in request.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question is null) continue;

            bool isCorrect = false;
            if (attempt.Test.AutoCheck && question.Type != QuestionType.OpenAnswer)
            {
                isCorrect = string.Equals(
                    answer.GivenAnswer.Trim(),
                    question.CorrectAnswer.Trim(),
                    StringComparison.OrdinalIgnoreCase
                );
                if (isCorrect)
                    score += question.Points;
            }

            db.TestAnswers.Add(new TestAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attemptId,
                QuestionId = answer.QuestionId,
                GivenAnswer = answer.GivenAnswer,
                IsCorrect = isCorrect,
            });
        }

        attempt.Status = AttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.Score = score;
        attempt.MaxScore = questions.Sum(q => q.Points);

        await db.SaveChangesAsync(ct);

        return Result<AttemptResponse>.Ok(attempt.ToDto());
    }

    public async Task<Result<List<AttemptResponse>>> GetMyAttemptsAsync(
        Guid testId,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<List<AttemptResponse>>.Fail("Студент не найден", 404);

        var attempts = await db.TestAttempts.AsNoTracking()
            .Where(a => a.TestId == testId && a.StudentId == student.Id)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(ct);

        return Result<List<AttemptResponse>>.Ok(attempts.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<TestResultResponse>> GetMyResultAsync(
        Guid testId,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<TestResultResponse>.Fail("Студент не найден", 404);

        var attempt = await db.TestAttempts.AsNoTracking()
            .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
            .Include(a => a.Test)
            .Where(a => a.TestId == testId && a.StudentId == student.Id)
            .OrderByDescending(a => a.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (attempt is null)
            return Result<TestResultResponse>.Fail("Результаты не найдены", 404);

        var showAnswers = attempt.Test.ShowCorrectAnswers;

        return Result<TestResultResponse>.Ok(new TestResultResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score,
            MaxScore = attempt.MaxScore,
            Percentage = attempt.MaxScore > 0 ? attempt.Score * 100 / attempt.MaxScore : 0,
            Passed = attempt.Score >= attempt.Test.PassingScore,
            CompletedAt = attempt.CompletedAt ?? attempt.StartedAt,
            AnswerReviews = attempt.Answers.Select(a => new AnswerReviewDto
            {
                QuestionId = a.QuestionId,
                QuestionText = a.Question?.Text ?? string.Empty,
                GivenAnswer = a.GivenAnswer,
                CorrectAnswer = showAnswers ? (a.Question?.CorrectAnswer ?? string.Empty) : string.Empty,
                IsCorrect = a.IsCorrect,
                Points = a.Question?.Points ?? 0,
            }).ToList(),
        });
    }

    public async Task<Result<List<AttemptResponse>>> GetAllMyResultsAsync(
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<List<AttemptResponse>>.Fail("Студент не найден", 404);

        var attempts = await db.TestAttempts.AsNoTracking()
            .Include(a => a.Test)
            .Where(a => a.StudentId == student.Id && a.Status == AttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync(ct);

        return Result<List<AttemptResponse>>.Ok(attempts.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<TestStatsResponse>> GetStatsAsync(
        Guid testId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var test = await db.Tests.AsNoTracking()
            .Include(t => t.Course)
            .FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null)
            return Result<TestStatsResponse>.Fail("Тест не найден", 404);

        if (!await CanEditTest(test, currentUserId, currentUserRole, ct))
            return Result<TestStatsResponse>.Fail("У вас нет прав на просмотр статистики", 403);

        var attempts = await db.TestAttempts.AsNoTracking()
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Student).ThenInclude(s => s.Group)
            .Where(a => a.TestId == testId && a.Status == AttemptStatus.Completed)
            .ToListAsync(ct);

        var scores = attempts.Select(a => a.Score).OrderBy(s => s).ToList();
        var count = scores.Count;
        double median = count > 0
            ? count % 2 == 1
                ? scores[count / 2]
                : (scores[count / 2 - 1] + scores[count / 2]) / 2.0
            : 0;

        return Result<TestStatsResponse>.Ok(new TestStatsResponse
        {
            TotalAttempts = count,
            PassedCount = attempts.Count(a => a.Score >= test.PassingScore),
            FailedCount = attempts.Count(a => a.Score < test.PassingScore),
            AverageScore = count > 0 ? attempts.Average(a => a.Score) : 0,
            MedianScore = median,
            MaxScore = count > 0 ? scores.Max() : 0,
            MinScore = count > 0 ? scores.Min() : 0,
            StudentResults = attempts.Select(a => new StudentResultDto
            {
                StudentName = a.Student?.User?.FullName ?? string.Empty,
                GroupName = a.Student?.Group?.Name ?? string.Empty,
                Score = a.Score,
                MaxScore = a.MaxScore,
                Passed = a.Score >= test.PassingScore,
            }).ToList(),
        });
    }

    private async Task<bool> CanEditTest(
        Test test,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        if (currentUserRole == "Admin") return true;
        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            return teacher is not null && test.CourseId != Guid.Empty
                && await db.Courses.AnyAsync(c => c.Id == test.CourseId && c.TeacherId == teacher.Id, ct);
        }
        return false;
    }
}

internal static class TestQuestionExtensions
{
    public static string? ShuffleOptions(this TestQuestion question)
    {
        if (string.IsNullOrEmpty(question.Options))
            return question.Options;

        var options = question.Options.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        if (options.Count <= 1) return question.Options;

        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        return string.Join("\n", options);
    }
}
