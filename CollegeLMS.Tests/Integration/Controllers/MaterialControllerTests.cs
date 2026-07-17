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

public class MaterialControllerTests : BaseIntegrationTest
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
    public async Task GetByCourse_ReturnsMaterials_WhenAuthenticated()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        db.Courses.Add(course);
        var materials = MaterialFixture.CreateFaker().Generate(2);
        foreach (var m in materials)
            m.CourseId = course.Id;
        db.CourseMaterials.AddRange(materials);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/courses/{course.Id}/materials");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<MaterialResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(2, body.Data!.Count);
    }

    [Fact]
    public async Task GetByCourse_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/materials");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByCourse_ReturnsNotFound_WhenCourseMissing()
    {
        SetAuthHeader(GetAdminToken());
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/materials");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        db.Courses.Add(course);
        var material = MaterialFixture.CreateFaker().Generate();
        material.Course = course;
        db.CourseMaterials.Add(material);
        await db.SaveChangesAsync();

        var response = await Client.DeleteAsync($"/api/materials/{material.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());
        var response = await Client.DeleteAsync($"/api/materials/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());
        var response = await Client.GetAsync($"/api/materials/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
