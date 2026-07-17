using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class TestingServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly TestingService _sut;

    public TestingServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new TestingService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoTests()
    {
        var result = await _sut.GetAllAsync(null, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTests_WhenAdmin()
    {
        var tests = TestFixture.CreateFaker().Generate(3);
        _db.Tests.AddRange(tests);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCourseId()
    {
        var courseId = Guid.NewGuid();
        var tests = TestFixture.CreateFaker().Generate(3);
        tests[0].CourseId = courseId;
        tests[0].Course!.Id = courseId;
        _db.Tests.AddRange(tests);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTest_WhenFound()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(test.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(test.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesTest_WhenAdmin()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateTestRequest
            {
                Title = "Новый тест",
                Description = "Описание",
                CourseId = course.Id,
                Type = "SelfStudy",
                TimeLimitMinutes = 30,
                MaxAttempts = 2,
                PassingScore = 70,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новый тест");
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenInvalidType()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateTestRequest
            {
                Title = "Тест",
                CourseId = course.Id,
                Type = "InvalidType",
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTest_WhenAdmin()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Старый тест",
            Description = "Старое описание",
            TimeLimitMinutes = 30,
            MaxAttempts = 1,
            Type = TestType.SelfStudy,
            PassingScore = 60,
            CourseId = course.Id,
            Course = course,
        };
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            test.Id,
            new UpdateTestRequest
            {
                Title = "Обновлённый тест",
                Description = "Новое описание",
                Type = "Control",
                TimeLimitMinutes = 90,
                MaxAttempts = 1,
                PassingScore = 80,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess, $"Status={result.StatusCode}, Error={result.ErrorMessage}");
        result.Data!.Title.Should().Be("Обновлённый тест");
        result.Data.Type.Should().Be("Control");
    }

    [Fact]
    public async Task DeleteAsync_RemovesTest_WhenAdmin()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(test.Id, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Tests.AnyAsync(t => t.Id == test.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetQuestionsAsync_ReturnsQuestions_WhenTestExists()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = "Вопрос 1",
            Type = QuestionType.SingleChoice,
            Options = "A\nB\nC",
            CorrectAnswer = "A",
            Points = 10,
            OrderIndex = 1,
            TestId = test.Id,
        };
        _db.TestQuestions.Add(question);
        await _db.SaveChangesAsync();

        var result = await _sut.GetQuestionsAsync(test.Id, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Text.Should().Be("Вопрос 1");
    }

    [Fact]
    public async Task CreateQuestionAsync_CreatesQuestion_WhenAdmin()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateQuestionAsync(
            test.Id,
            new CreateQuestionRequest
            {
                Text = "Новый вопрос",
                Type = "SingleChoice",
                Options = "A\nB\nC",
                CorrectAnswer = "A",
                Points = 5,
                OrderIndex = 1,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Text.Should().Be("Новый вопрос");
    }

    [Fact]
    public async Task AssignTestAsync_AssignsTest_WhenAdmin()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.AssignTestAsync(
            test.Id,
            new AssignTestRequest
            {
                GroupId = group.Id,
                OpenDate = DateTime.UtcNow.AddDays(-1),
                CloseDate = DateTime.UtcNow.AddDays(30),
                MaxAttempts = 2,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task AssignTestAsync_ReturnsNotFound_WhenGroupMissing()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.AssignTestAsync(
            test.Id,
            new AssignTestRequest
            {
                    OpenDate = DateTime.UtcNow,
                CloseDate = DateTime.UtcNow.AddDays(30),
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAssignmentsAsync_ReturnsAssignments()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        _db.TestAssignments.Add(
            new TestAssignment
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                GroupId = group.Id,
                OpenDate = DateTime.UtcNow,
                CloseDate = DateTime.UtcNow.AddDays(7),
                MaxAttempts = 1,
                Group = group,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAssignmentsAsync(test.Id, Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesSettings()
    {
        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateSettingsAsync(
            test.Id,
            new TestSettingsRequest
            {
                ShuffleQuestions = true,
                AutoCheck = true,
                ShowCorrectAnswers = true,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.ShuffleQuestions.Should().BeTrue();
    }

    [Fact]
    public async Task StartTestAsync_ReturnsQuestions_WhenValid()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);

        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = group.Id,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        test.CourseId = course.Id;
        test.Course = course;
        test.MaxAttempts = 3;
        _db.Tests.Add(test);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = "Вопрос 1",
            Type = QuestionType.SingleChoice,
            Options = "A\nB\nC",
            CorrectAnswer = "A",
            Points = 10,
            OrderIndex = 1,
            TestId = test.Id,
        };
        _db.TestQuestions.Add(question);

        _db.TestAssignments.Add(
            new TestAssignment
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                GroupId = group.Id,
                OpenDate = DateTime.UtcNow.AddDays(-1),
                CloseDate = DateTime.UtcNow.AddDays(30),
                MaxAttempts = 3,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.StartTestAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Questions.Should().HaveCount(1);
    }

    [Fact]
    public async Task StartTestAsync_ReturnsFail_WhenMaxAttemptsExceeded()
    {
        var groupId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = groupId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            MaxAttempts = 1,
            TimeLimitMinutes = 60,
            PassingScore = 60,
            Type = TestType.SelfStudy,
            CourseId = Guid.NewGuid(),
        };
        _db.Tests.Add(test);

        _db.TestQuestions.Add(
            new TestQuestion
            {
                Id = Guid.NewGuid(),
                Text = "Q1",
                Type = QuestionType.SingleChoice,
                CorrectAnswer = "A",
                Points = 10,
                OrderIndex = 1,
                TestId = test.Id,
            }
        );

        _db.TestAssignments.Add(
            new TestAssignment
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                GroupId = groupId,
                OpenDate = DateTime.UtcNow.AddDays(-1),
                CloseDate = DateTime.UtcNow.AddDays(1),
                MaxAttempts = 1,
            }
        );

        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                Status = AttemptStatus.Completed,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.StartTestAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task SubmitAnswersAsync_ComputesScore_WhenAutoCheck()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        test.AutoCheck = true;
        test.TimeLimitMinutes = 60;
        _db.Tests.Add(test);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = "Q1",
            Type = QuestionType.SingleChoice,
            CorrectAnswer = "A",
            Points = 10,
            OrderIndex = 1,
            TestId = test.Id,
        };
        _db.TestQuestions.Add(question);

        var attempt = new TestAttempt
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            StudentId = student.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            Status = AttemptStatus.InProgress,
            Test = test,
        };
        _db.TestAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAnswersAsync(
            test.Id,
            attempt.Id,
            new SubmitAnswersRequest
            {
                Answers = new List<AnswerDto>
                {
                    new() { QuestionId = question.Id, GivenAnswer = "A" },
                },
            },
            studentUserId,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Score.Should().Be(10);
    }

    [Fact]
    public async Task SubmitAnswersAsync_ReturnsFail_WhenTimedOut()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        test.TimeLimitMinutes = 10;
        _db.Tests.Add(test);

        var attempt = new TestAttempt
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            StudentId = student.Id,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            Status = AttemptStatus.InProgress,
            Test = test,
        };
        _db.TestAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAnswersAsync(
            test.Id,
            attempt.Id,
            new SubmitAnswersRequest { Answers = new List<AnswerDto>() },
            studentUserId,
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(408);
    }

    [Fact]
    public async Task GetMyAttemptsAsync_ReturnsAttempts()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        _db.Tests.Add(test);

        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow,
                Status = AttemptStatus.Completed,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyAttemptsAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyResultAsync_ReturnsResult()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            PassingScore = 5,
            ShowCorrectAnswers = true,
            TimeLimitMinutes = 60,
            MaxAttempts = 1,
            Type = TestType.SelfStudy,
            CourseId = Guid.NewGuid(),
        };
        _db.Tests.Add(test);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = "Q1",
            Type = QuestionType.SingleChoice,
            CorrectAnswer = "A",
            Points = 10,
            OrderIndex = 1,
            TestId = test.Id,
        };
        _db.TestQuestions.Add(question);

        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Status = AttemptStatus.Completed,
                Score = 10,
                MaxScore = 10,
                Test = test,
                Answers = new List<TestAnswer>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AttemptId = Guid.NewGuid(),
                        QuestionId = question.Id,
                        GivenAnswer = "A",
                        IsCorrect = true,
                        Question = question,
                    },
                },
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyResultAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Percentage.Should().Be(100);
        result.Data.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStats()
    {
        var adminId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = adminId,
                FullName = "Admin",
                Email = "a@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );

        var test = TestFixture.CreateFaker().Generate();
        test.PassingScore = 50;
        _db.Tests.Add(test);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RecordBookNumber = "ЗК-001",
        };
        student.User = new User
        {
            Id = student.UserId,
            FullName = "Студент",
            Email = "s@t.ru",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        student.Group = new Group
        {
            Id = student.GroupId,
            Name = "ГР-11",
            Course = 1,
        };

        _db.Students.Add(student);
        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                Status = AttemptStatus.Completed,
                Score = 80,
                MaxScore = 100,
                Student = student,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetStatsAsync(test.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalAttempts.Should().Be(1);
        result.Data.PassedCount.Should().Be(1);
    }
}
