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

public class CourseControllerTests : BaseIntegrationTest
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
            IsActive = true,
        };
        return tokenService.GenerateAccessToken(admin);
    }

    private string GetStudentToken()
    {
        return GetStudentToken(Guid.NewGuid());
    }

    private string GetStudentToken(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var student = new User
        {
            Id = userId,
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        return tokenService.GenerateAccessToken(student);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsCourses_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var courses = CourseFixture.CreateFaker().Generate(3);
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<CourseResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(3, body.Data!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/courses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teacher = TeacherFixture.CreateFaker().Generate();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-21",
            Course = 2,
        };
        db.Users.Add(teacher.User);
        db.Teachers.Add(teacher);
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/courses",
            new CreateCourseRequest
            {
                Title = "Новый курс",
                Description = "Описание курса",
                GroupId = group.Id,
                TeacherId = teacher.Id,
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<CourseResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Новый курс", body.Data!.Title);
    }

    [Fact]
    public async Task Create_ReturnsForbidden_WhenStudent()
    {
        SetAuthHeader(GetStudentToken());

        var response = await Client.PostAsJsonAsync(
            "/api/courses",
            new CreateCourseRequest { Title = "Курс", GroupId = Guid.NewGuid() }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignGroups_AssignsGroups_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = CourseFixture.CreateFaker().Generate();
        db.Courses.Add(course);
        var group = new Group { Id = Guid.NewGuid(), Name = "ГР-11", Course = 1 };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/courses/{course.Id}/groups",
            new AssignGroupsRequest { GroupIds = new List<Guid> { group.Id } }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCourseGroups_ReturnsGroups_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = CourseFixture.CreateFaker().Generate();
        db.Courses.Add(course);
        var group = new Group { Id = Guid.NewGuid(), Name = "ГР-11", Course = 1 };
        db.Groups.Add(group);
        db.CourseGroups.Add(new CourseGroup
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            GroupId = group.Id,
        });
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/courses/{course.Id}/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProgress_ReturnsProgress_WhenStudent()
    {
        var studentUserId = Guid.NewGuid();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = CourseFixture.CreateFaker().Generate();
        db.Courses.Add(course);
        var group = new Group { Id = Guid.NewGuid(), Name = "ГР-11", Course = 1 };
        db.Groups.Add(group);
        var student = new Student { Id = Guid.NewGuid(), UserId = studentUserId, GroupId = group.Id, RecordBookNumber = "ЗК-001" };
        db.Users.Add(new User { Id = studentUserId, FullName = "Студент", Email = "s@t.ru", PasswordHash = "hash", Role = UserRole.Student, IsActive = true });
        db.Students.Add(student);
        db.CourseGroups.Add(new CourseGroup { Id = Guid.NewGuid(), CourseId = course.Id, GroupId = group.Id });
        await db.SaveChangesAsync();

        SetAuthHeader(GetStudentToken(studentUserId));

        var response = await Client.GetAsync($"/api/my/courses/{course.Id}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
