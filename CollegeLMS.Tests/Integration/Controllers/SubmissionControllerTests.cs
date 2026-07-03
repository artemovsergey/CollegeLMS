using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class SubmissionControllerTests : BaseIntegrationTest
{
    private string GetToken(UserRole role)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{role}@test.ru",
            FullName = $"{role} User",
            PasswordHash = "hash",
            Role = role,
            IsActive = true,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Submit_ReturnsOk_WhenStudentAuthenticated()
    {
        SetAuthHeader(GetToken(UserRole.Student));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First(u => u.Role == UserRole.Student);

        var groupId = Guid.NewGuid();
        db.Groups.Add(new Group { Id = groupId, Name = "Test", Course = 1 });

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = groupId,
            RecordBookNumber = "RB-001",
        };
        db.Students.Add(student);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            GroupId = groupId,
            Status = CourseStatus.Active,
        };
        db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/assignments/{assignment.Id}/submit",
            new SubmitAssignmentRequest { FilePath = "submissions/test/file.bin", Comment = "Test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<SubmissionResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("submissions/test/file.bin", body.Data!.FilePath);
    }

    [Fact]
    public async Task Submit_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/assignments/{Guid.NewGuid()}/submit",
            new SubmitAssignmentRequest { FilePath = "test.bin" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmissions_ReturnsOk_WhenTeacher()
    {
        SetAuthHeader(GetToken(UserRole.Teacher));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teacherUser = db.Users.First(u => u.Role == UserRole.Teacher);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            Department = "CS",
            Position = "Professor",
        };
        db.Teachers.Add(teacher);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = teacher.Id,
            GroupId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/assignments/{assignment.Id}/submissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<SubmissionResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task Grade_ReturnsOk_WhenTeacher()
    {
        SetAuthHeader(GetToken(UserRole.Teacher));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teacherUser = db.Users.First(u => u.Role == UserRole.Teacher);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            Department = "CS",
            Position = "Professor",
        };
        db.Teachers.Add(teacher);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = teacher.Id,
            GroupId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        db.Courses.Add(course);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Test",
            MaxScore = 100,
        };
        db.Assignments.Add(assignment);

        var submitterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "submitter@test.ru",
            FullName = "Submitter",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(submitterUser);

        var submitterStudent = new Student
        {
            Id = Guid.NewGuid(),
            UserId = submitterUser.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-002",
        };
        db.Students.Add(submitterStudent);

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = submitterStudent.Id,
            FilePath = "test.bin",
            SubmittedAt = DateTime.UtcNow,
        };
        db.AssignmentSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade",
            new GradeSubmissionRequest { Score = 90 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<SubmissionResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(90, body.Data!.Score);
    }

    [Fact]
    public async Task GetMySubmissions_ReturnsOk_WhenStudent()
    {
        SetAuthHeader(GetToken(UserRole.Student));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First(u => u.Role == UserRole.Student);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "RB-001",
        };
        db.Students.Add(student);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Title = "Test",
            MaxScore = 100,
        };
        db.Assignments.Add(assignment);

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            FilePath = "test.bin",
            SubmittedAt = DateTime.UtcNow,
        };
        db.AssignmentSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/my/submissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<SubmissionResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task GetMySubmissions_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/my/submissions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
