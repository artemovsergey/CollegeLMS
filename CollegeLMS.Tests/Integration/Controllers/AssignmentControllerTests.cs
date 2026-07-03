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

public class AssignmentControllerTests : BaseIntegrationTest
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
    public async Task GetAll_ReturnsAssignments_WhenAuthenticated()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);
        var assignments = AssignmentFixture.CreateFaker().Generate(3);
        foreach (var a in assignments)
            a.CourseId = course.Id;
        db.Assignments.AddRange(assignments);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/courses/{course.Id}/assignments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<AssignmentResponse>>>(response);
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
            GroupId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/courses/{course.Id}/assignments",
            new CreateAssignmentRequest { Title = "Новое задание", Description = "Описание", MaxScore = 100 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<AssignmentResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Новое задание", body.Data!.Title);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/assignments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
