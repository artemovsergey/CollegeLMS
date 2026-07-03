using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class DashboardControllerTests : BaseIntegrationTest
{
    private string GetToken(User user)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private User CreateUserWithRole(UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{role}@test.ru",
            FullName = $"{role} User",
            PasswordHash = "hash",
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetTeacherDashboard_ReturnsDashboard_WhenTeacher()
    {
        var teacherUser = CreateUserWithRole(UserRole.Teacher);
        SetAuthHeader(GetToken(teacherUser));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            Department = "ИТ",
            Position = "Преподаватель",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Teachers.Add(teacher);

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-01",
            Course = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Groups.Add(group);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Тестовый курс",
            Description = "Описание",
            TeacherId = teacher.Id,
            GroupId = group.Id,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/teacher/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<TeacherDashboardResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(1, body.Data!.CoursesCount);
    }

    [Fact]
    public async Task GetTeacherDashboard_ReturnsForbidden_WhenNotTeacher()
    {
        var studentUser = CreateUserWithRole(UserRole.Student);
        SetAuthHeader(GetToken(studentUser));

        var response = await Client.GetAsync("/api/teacher/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTeacherDashboard_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/teacher/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsDashboard_WhenStudent()
    {
        var studentUser = CreateUserWithRole(UserRole.Student);
        SetAuthHeader(GetToken(studentUser));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-02",
            Course = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Groups.Add(group);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUser.Id,
            GroupId = group.Id,
            RecordBookNumber = "ЗК-001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/my/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<StudentDashboardResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(0, body.Data!.CoursesCount);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsForbidden_WhenNotStudent()
    {
        var teacherUser = CreateUserWithRole(UserRole.Teacher);
        SetAuthHeader(GetToken(teacherUser));

        var response = await Client.GetAsync("/api/my/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/my/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
