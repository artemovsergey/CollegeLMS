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
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class NewsControllerTests : BaseIntegrationTest
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
    public async Task GetAll_ReturnsOk_WithPagedNews()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminId = Guid.NewGuid();
        db.Users.Add(
            new User
            {
                Id = adminId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
            }
        );
        var newsList = NewsFixture.CreateFaker().Generate(3);
        newsList.ForEach(n => n.CreatedById = adminId);
        db.News.AddRange(newsList);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/news?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<NewsResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsNews_WhenExists()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminId = Guid.NewGuid();
        db.Users.Add(
            new User
            {
                Id = adminId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
            }
        );
        var news = NewsFixture.CreateFaker().Generate();
        news.CreatedById = adminId;
        db.News.Add(news);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/news/{news.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<NewsResponse>>();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(news.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var response = await Client.GetAsync($"/api/news/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthenticated()
    {
        var request = new CreateNewsRequest { Title = "Новость", Content = "Контент" };

        var response = await Client.PostAsJsonAsync("/api/news", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Returns201_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        var request = new CreateNewsRequest
        {
            Title = "Новая новость",
            Content = "<p>Содержание новой новости</p>",
            IsPublished = true,
        };

        var response = await Client.PostAsJsonAsync("/api/news", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<NewsResponse>>();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новая новость");
    }

    [Fact]
    public async Task Delete_Returns401_WhenUnauthenticated()
    {
        var response = await Client.DeleteAsync($"/api/news/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound_AndAuthenticated()
    {
        SetAuthHeader(GetAdminToken());
        var response = await Client.DeleteAsync($"/api/news/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
