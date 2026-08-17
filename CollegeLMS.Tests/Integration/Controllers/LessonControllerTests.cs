using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class LessonControllerTests : BaseIntegrationTest
{
    private string GetAdminToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.ru",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
        };
        return tokenService.GenerateAccessToken(admin);
    }

    private string GetTeacherToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var teacher = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.ru",
            FullName = "Teacher",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
        };
        return tokenService.GenerateAccessToken(teacher);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsLessons_WhenAuthenticated()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);
        var lessons = LessonFixture.CreateFaker().Generate(3);
        foreach (var l in lessons)
            l.CourseId = course.Id;
        db.Lessons.AddRange(lessons);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/courses/{course.Id}/lessons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<LessonResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(3, body.Data!.Count);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/courses/{course.Id}/lessons",
            new CreateLessonRequest
            {
                Title = "Новое занятие",
                Content = "Контент",
                Kind = "Lecture",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<LessonResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Новое занятие", body.Data!.Title);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/lessons");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reorder_ReturnsOk_WhenTeacherOwner()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.ru",
            FullName = "Owner",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
        };
        db.Users.Add(user);
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "Цикловая комиссия",
            Category = TeacherCategory.None,
        };
        db.Teachers.Add(teacher);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = teacher.Id,
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        db.Courses.Add(course);
        db.Lessons.AddRange(a, b);
        await db.SaveChangesAsync();

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            tokenService.GenerateAccessToken(user)
        );

        var response = await Client.PutAsJsonAsync(
            $"/api/courses/{course.Id}/lessons/reorder",
            new ReorderLessonsRequest { LessonIds = [b.Id, a.Id] }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task Reorder_ReturnsForbidden_WhenNotOwner()
    {
        SetAuthHeader(GetTeacherToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        db.Courses.Add(course);
        db.Lessons.Add(a);
        await db.SaveChangesAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/courses/{course.Id}/lessons/reorder",
            new ReorderLessonsRequest { LessonIds = [a.Id] }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}