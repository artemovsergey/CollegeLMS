using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class SubmissionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SubmissionService _sut;

    public SubmissionServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new SubmissionService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Submit_CreatesSubmission_WhenValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(user);

        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "Test Group",
                Course = 1,
            }
        );

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = groupId,
            RecordBookNumber = "RB-001",
        };
        _db.Students.Add(student);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);
        _db.CourseGroups.Add(new CourseGroup { Id = Guid.NewGuid(), CourseId = course.Id, GroupId = groupId });

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAsync(
            assignment.Id,
            new SubmitAssignmentRequest
            {
                FilePath = "submissions/test/file.bin",
                Comment = "Готово",
            },
            user.Id,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.FilePath.Should().Be("submissions/test/file.bin");
        result.Data.Comment.Should().Be("Готово");
    }

    [Fact]
    public async Task Submit_ReturnsForbidden_WhenNotEnrolled()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(user);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-001",
        };
        _db.Students.Add(student);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAsync(
            assignment.Id,
            new SubmitAssignmentRequest { FilePath = "submissions/test/file.bin" },
            user.Id,
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Submit_ReturnsNotFound_WhenAssignmentMissing()
    {
        var result = await _sut.SubmitAsync(
            Guid.NewGuid(),
            new SubmitAssignmentRequest { FilePath = "test.bin" },
            Guid.NewGuid(),
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Submit_UpdatesExistingSubmission()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(user);

        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "Test",
                Course = 1,
            }
        );

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = groupId,
            RecordBookNumber = "RB-001",
        };
        _db.Students.Add(student);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);
        _db.CourseGroups.Add(new CourseGroup { Id = Guid.NewGuid(), CourseId = course.Id, GroupId = groupId });

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        _db.Assignments.Add(assignment);

        var existing = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            FilePath = "old/path.bin",
            SubmittedAt = DateTime.UtcNow.AddDays(-1),
        };
        _db.AssignmentSubmissions.Add(existing);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAsync(
            assignment.Id,
            new SubmitAssignmentRequest { FilePath = "new/path.bin", Comment = "Обновлено" },
            user.Id,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.FilePath.Should().Be("new/path.bin");
    }

    [Fact]
    public async Task Grade_SetsScore_WhenValid()
    {
        var teacherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.ru",
            FullName = "Teacher",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            IsActive = true,
        };
        _db.Users.Add(teacherUser);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            CyclicalCommission = "CS",
            Position = "Professor",
        };
        _db.Teachers.Add(teacher);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        _db.Assignments.Add(assignment);

        var submitterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "submitter@test.ru",
            FullName = "Submitter",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(submitterUser);

        var submitterStudent = new Student
        {
            Id = Guid.NewGuid(),
            UserId = submitterUser.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-002",
        };
        _db.Students.Add(submitterStudent);

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = submitterStudent.Id,
            FilePath = "test.bin",
            SubmittedAt = DateTime.UtcNow,
        };
        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var result = await _sut.GradeAsync(
            submission.Id,
            new GradeSubmissionRequest { Score = 85 },
            teacherUser.Id,
            "Teacher",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Score.Should().Be(85);
    }

    [Fact]
    public async Task Grade_ReturnsError_WhenScoreExceedsMax()
    {
        var teacherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.ru",
            FullName = "Teacher",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            IsActive = true,
        };
        _db.Users.Add(teacherUser);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            CyclicalCommission = "CS",
            Position = "Professor",
        };
        _db.Teachers.Add(teacher);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 50,
        };
        _db.Assignments.Add(assignment);

        var submitterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "submitter@test.ru",
            FullName = "Submitter",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(submitterUser);

        var submitterStudent = new Student
        {
            Id = Guid.NewGuid(),
            UserId = submitterUser.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-002",
        };
        _db.Students.Add(submitterStudent);

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = submitterStudent.Id,
            FilePath = "test.bin",
            SubmittedAt = DateTime.UtcNow,
        };
        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var result = await _sut.GradeAsync(
            submission.Id,
            new GradeSubmissionRequest { Score = 100 },
            teacherUser.Id,
            "Teacher",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetMySubmissions_ReturnsSubmissions_ForStudent()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        _db.Users.Add(user);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-001",
        };
        _db.Students.Add(student);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Title = "Test",
            MaxScore = 100,
        };
        _db.Assignments.Add(assignment);

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            FilePath = "test.bin",
            SubmittedAt = DateTime.UtcNow,
        };
        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var result = await _sut.GetMySubmissionsAsync(user.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }
}
