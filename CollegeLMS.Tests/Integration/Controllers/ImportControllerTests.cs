using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ImportControllerTests : BaseIntegrationTest
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
    public async Task GetActiveImport_Returns404_WhenNoRunningJobs()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync("/api/import/wordpress/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveImport_ReturnsRunningJob()
    {
        SetAuthHeader(GetAdminToken());

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            Status = "running",
            Total = 10,
            Processed = 3,
        };
        using (var db = CreateDbContext())
        {
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/import/wordpress/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<Result<ImportProgressDto>>(response);
        result!.IsSuccess.Should().BeTrue();
        result.Data!.ImportId.Should().Be(job.Id.ToString());
        result.Data.Status.Should().Be("running");
        result.Data.Processed.Should().Be(3);
        result.Data.Total.Should().Be(10);
    }

    [Fact]
    public async Task GetImportStatus_Returns404_WhenNotFound()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/import/wordpress/status/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetImportStatus_ReturnsJobWithResult()
    {
        SetAuthHeader(GetAdminToken());

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            Status = "completed",
            Total = 5,
            Processed = 5,
            CategoriesCreated = 2,
            PostsImported = 3,
            PostsSkipped = 2,
            CompletedAt = DateTime.UtcNow,
        };
        using (var db = CreateDbContext())
        {
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/import/wordpress/status/{job.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<Result<ImportProgressDto>>(response);
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("completed");
        result.Data.Processed.Should().Be(5);
        result.Data.Result.Should().NotBeNull();
        result.Data.Result!.CategoriesCreated.Should().Be(2);
        result.Data.Result.PostsImported.Should().Be(3);
        result.Data.Result.PostsSkipped.Should().Be(2);
    }

    [Fact]
    public async Task ImportWordPress_CreatesJobAndExposesStatus()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.PostAsync("/api/import/wordpress", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var start = await DeserializeAsync<Result<string>>(response);
        start!.IsSuccess.Should().BeTrue();
        start.Data.Should().NotBeNullOrEmpty();

        var statusResponse = await Client.GetAsync($"/api/import/wordpress/status/{start.Data}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await DeserializeAsync<Result<ImportProgressDto>>(statusResponse);
        status!.IsSuccess.Should().BeTrue();
        status.Data.Should().NotBeNull();
    }
}
