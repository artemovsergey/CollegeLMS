using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class CourseDocumentControllerTests : BaseIntegrationTest
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

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsDocuments_WhenAuthenticated()
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
        db.CourseDocuments.AddRange(
            new CourseDocument
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                FileName = "a.pdf",
                FilePath = "documents/1/a.pdf",
                ContentType = "application/pdf",
                SizeBytes = 100,
            },
            new CourseDocument
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                FileName = "b.pdf",
                FilePath = "documents/1/b.pdf",
                ContentType = "application/pdf",
                SizeBytes = 200,
            }
        );
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/courses/{course.Id}/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<CourseDocumentResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(2, body.Data!.Count);
    }

    [Fact]
    public async Task Upload_ReturnsOk_WhenAdmin()
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

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "док.pdf");

        var response = await Client.PostAsync($"/api/courses/{course.Id}/documents", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<CourseDocumentResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("док.pdf", body.Data!.FileName);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());
        var response = await Client.GetAsync($"/api/courses/{Guid.NewGuid()}/documents/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}