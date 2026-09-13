using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleContextApiTests : BaseIntegrationTest
{
    private string GetToken(UserRole role, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = userId,
            Email = "user@test.ru",
            FullName = "Пользователь",
            Login = "user",
            PasswordHash = "hash",
            Role = role,
        };
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetContext_RequiresAuth()
    {
        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContext_ReturnsStudentGroup()
    {
        var userId = Guid.NewGuid();
        var group = GroupFixture.CreateFaker().Generate();
        var student = StudentFixture.CreateFaker().Generate();
        student.UserId = userId;
        student.User.Id = userId;
        student.GroupId = group.Id;
        student.Group = group;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GetToken(UserRole.Student, userId));
        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Student", body.Data!.Role);
        Assert.Equal(student.GroupId, body.Data.GroupId);
    }

    [Fact]
    public async Task GetContext_ReturnsTeacher()
    {
        var userId = Guid.NewGuid();
        var teacher = TeacherFixture.CreateFaker().Generate();
        teacher.UserId = userId;
        teacher.User.Id = userId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GetToken(UserRole.Teacher, userId));
        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Equal(teacher.Id, body.Data!.TeacherId);
        Assert.Equal("Teacher", body.Data.Role);
    }

    [Fact]
    public async Task GetContext_ReturnsOther_WhenNoStudentOrTeacher()
    {
        SetAuthHeader(GetToken(UserRole.Admin, Guid.NewGuid()));
        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Other", body.Data!.Role);
    }
}