using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class FavoritesApiTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private string GetUserToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Студент",
            Login = "student",
            PasswordHash = "hash",
            Role = UserRole.Student,
        };
        return tokenService.GenerateAccessToken(user);
    }

    [Fact]
    public async Task Favorites_RequiresAuth()
    {
        var response = await Client.GetAsync("/api/favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddThenListThenDelete()
    {
        var group = GroupFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Groups.Add(group);
            await db.SaveChangesAsync();
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var addResponse = await Client.PostAsJsonAsync(
            "/api/favorites",
            new { targetType = "Group", targetId = group.Id }
        );
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var addBody = JsonSerializer.Deserialize<Result<FavoriteResponse>>(
            await addResponse.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.True(addBody!.IsSuccess);
        Assert.NotNull(addBody.Data!.GroupName);

        var listResponse = await Client.GetAsync("/api/favorites");
        var listBody = JsonSerializer.Deserialize<Result<List<FavoriteResponse>>>(
            await listResponse.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.True(listBody!.IsSuccess);
        Assert.Contains(listBody.Data!, f => f.Id == addBody.Data!.Id);

        var deleteResponse = await Client.DeleteAsync($"/api/favorites/{addBody.Data!.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var list2Response = await Client.GetAsync("/api/favorites");
        var list2Body = JsonSerializer.Deserialize<Result<List<FavoriteResponse>>>(
            await list2Response.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.True(list2Body!.IsSuccess);
        Assert.DoesNotContain(list2Body.Data!, f => f.Id == addBody.Data!.Id);
    }

    [Fact]
    public async Task Add_UnknownGroup_ReturnsNotFound()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var addResponse = await Client.PostAsJsonAsync(
            "/api/favorites",
            new { targetType = "Group", targetId = Guid.NewGuid() }
        );

        Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
    }
}
