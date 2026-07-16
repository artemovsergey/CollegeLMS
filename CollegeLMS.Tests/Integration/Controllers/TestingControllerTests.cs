using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class TestingControllerTests : BaseIntegrationTest
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

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var course = new Course { Id = Guid.NewGuid(), Title = "Курс", TeacherId = Guid.NewGuid(), GroupId = Guid.NewGuid(), Status = CourseStatus.Draft };
        db.Courses.Add(course);
        db.Tests.Add(new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест 1",
            CourseId = course.Id,
            Type = TestType.SelfStudy,
            Course = course,
        });
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<TestResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/tests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenValid()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var course = new Course { Id = Guid.NewGuid(), Title = "Курс", TeacherId = Guid.NewGuid(), GroupId = Guid.NewGuid(), Status = CourseStatus.Draft };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync("/api/tests", new CreateTestRequest
        {
            Title = "Новый тест",
            CourseId = course.Id,
            Type = "SelfStudy",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
