using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using CollegeLMS.Tests.Integration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class SearchControllerTests : BaseIntegrationTest
{
    private async Task SeedNewsAsync(params string[] titles)
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

        foreach (var title in titles)
        {
            db.News.Add(
                new News
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Content = $"Содержание: {title}",
                    IsPublished = true,
                    IsDeleted = false,
                    CreatedById = adminId,
                    PublishedAt = DateTime.UtcNow,
                }
            );
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsOkWithEmptyResults()
    {
        var response = await Client.GetAsync("/api/search?q=");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<SearchResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithQuery_ReturnsOk()
    {
        await SeedNewsAsync("День открытых дверей", "Расписание экзаменов");

        var response = await Client.GetAsync("/api/search?q=дверей");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<SearchResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().Contain(i => i.Title == "День открытых дверей");
    }

    [Fact]
    public async Task Search_IncludesStaticPages()
    {
        var response = await Client.GetAsync("/api/search?q=колледж");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<SearchResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().Contain(i => i.Type == "page");
    }

    [Fact]
    public async Task Search_ReturnsPaginationMetadata()
    {
        await SeedNewsAsync("Новость 1", "Новость 2", "Новость 3");

        var response = await Client.GetAsync("/api/search?q=новость&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<SearchResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Search_NoResults_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/search?q=несуществующийзапросxyz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<
            Result<PagedResponse<SearchResponse>>
        >();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }
}
