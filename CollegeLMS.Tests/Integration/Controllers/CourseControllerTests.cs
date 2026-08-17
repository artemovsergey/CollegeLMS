using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class CourseControllerTests : BaseIntegrationTest
{
    private static User MakeUser(string login, UserRole role) =>
        new()
        {
            Id = Guid.NewGuid(),
            Login = login,
            Email = $"{login}@test.ru",
            FullName = "Тест Тестович",
            PasswordHash = "hash",
            Role = role,
        };

    private static Teacher MakeTeacher(Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CyclicalCommission = "ИТ",
            Position = "Преподаватель",
            Category = TeacherCategory.None,
        };

    [Fact]
    public async Task Duplicate_CopiesCourseAndMakesDraft()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var user = MakeUser("dupowner", UserRole.Teacher);
        var teacher = MakeTeacher(user.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "МДК 09.01",
            Description = "Описание",
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.Add(user);
        db.Teachers.Add(teacher);
        db.Courses.Add(course);
        db.Lessons.Add(
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Занятие 1",
                Content = "Текст",
                Order = 1,
                Kind = LessonKind.Lecture,
            }
        );
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await Client.PostAsync($"/api/courses/{course.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<CourseResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.Contains("(копия)", body.Data.Title);
        Assert.False(body.Data.IsActive);

        var copy = await db.Courses.FirstAsync(c => c.Id == body.Data.Id);
        Assert.Equal(CourseStatus.Draft, copy.Status);
        Assert.Equal(1, await db.Lessons.CountAsync(l => l.CourseId == copy.Id));
    }

    [Fact]
    public async Task SetActive_Forbidden_ForCoAuthor()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("activeowner", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var coAuthor = MakeUser("activecoauthor", UserRole.Teacher);
        var coAuthorTeacher = MakeTeacher(coAuthor.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, coAuthor);
        db.Teachers.AddRange(ownerTeacher, coAuthorTeacher);
        db.Courses.Add(course);
        db.CourseAuthors.Add(
            new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                TeacherId = coAuthorTeacher.Id,
            }
        );
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(coAuthor);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await Client.PatchAsJsonAsync(
            $"/api/courses/{course.Id}/active",
            new UpdateCourseActiveRequest { IsActive = false }
        );
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsCourses_WhereCoAuthor()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("listowner", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var teacher2 = MakeUser("listteacher", UserRole.Teacher);
        var teacher2Entity = MakeTeacher(teacher2.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Соавторский",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, teacher2);
        db.Teachers.AddRange(ownerTeacher, teacher2Entity);
        db.Courses.Add(course);
        db.CourseAuthors.Add(
            new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                TeacherId = teacher2Entity.Id,
            }
        );
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(teacher2);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await Client.GetAsync("/api/courses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<List<CourseResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!);
        Assert.Equal(course.Id, body.Data![0].Id);
    }

    [Fact]
    public async Task Duplicate_Forbidden_ForForeignTeacher()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("dupowner2", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var foreign = MakeUser("dupforeign", UserRole.Teacher);
        var foreignTeacher = MakeTeacher(foreign.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, foreign);
        db.Teachers.AddRange(ownerTeacher, foreignTeacher);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(foreign);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await Client.PostAsync($"/api/courses/{course.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}