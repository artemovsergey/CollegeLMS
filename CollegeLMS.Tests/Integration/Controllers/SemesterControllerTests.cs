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

public class SemesterControllerTests : BaseIntegrationTest
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

        var response = await Client.GetAsync("/api/semesters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<SemesterResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/semesters/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenValid()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.PostAsJsonAsync("/api/semesters", new CreateSemesterRequest
        {
            Name = "Тестовый семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = "Autumn",
            AcademicYear = "2025-2026",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
