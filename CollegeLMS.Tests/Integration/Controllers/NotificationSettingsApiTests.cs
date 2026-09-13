using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class NotificationSettingsApiTests : BaseIntegrationTest
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
    public async Task Get_ReturnsDefaults()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var response = await Client.GetAsync("/api/notifications/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonSerializer.Deserialize<Result<NotificationSettingsResponse>>(
            await response.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.True(body!.IsSuccess);
        Assert.True(body.Data!.Enabled);
        Assert.Equal("07:30", body.Data.Time);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, body.Data.Days.ToArray());
    }

    [Fact]
    public async Task Update_ValidatesTimeBounds()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var bad = await Client.PutAsJsonAsync(
            "/api/notifications/settings",
            new
            {
                enabled = true,
                time = "09:00",
                days = new[] { 1 },
            }
        );
        var badBody = JsonSerializer.Deserialize<Result<NotificationSettingsResponse>>(
            await bad.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.False(badBody!.IsSuccess);

        var notStep = await Client.PutAsJsonAsync(
            "/api/notifications/settings",
            new
            {
                enabled = true,
                time = "07:33",
                days = new[] { 1 },
            }
        );
        var notStepBody = JsonSerializer.Deserialize<Result<NotificationSettingsResponse>>(
            await notStep.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.False(notStepBody!.IsSuccess);
    }

    [Fact]
    public async Task Update_RejectsInvalidDays()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var bad = await Client.PutAsJsonAsync(
            "/api/notifications/settings",
            new
            {
                enabled = true,
                time = "07:45",
                days = new[] { 0, 8 },
            }
        );

        var body = JsonSerializer.Deserialize<Result<NotificationSettingsResponse>>(
            await bad.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.False(body!.IsSuccess);
    }

    [Fact]
    public async Task Update_SetsNextNotifyAt()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetUserToken()
        );

        var ok = await Client.PutAsJsonAsync(
            "/api/notifications/settings",
            new
            {
                enabled = true,
                time = "07:45",
                days = new[] { 1, 3, 5 },
            }
        );
        var okBody = JsonSerializer.Deserialize<Result<NotificationSettingsResponse>>(
            await ok.Content.ReadAsStringAsync(),
            JsonOptions
        );
        Assert.True(okBody!.IsSuccess);
        Assert.Equal("07:45", okBody.Data!.Time);
        Assert.Equal(new[] { 1, 3, 5 }, okBody.Data!.Days.ToArray());
        Assert.NotNull(okBody.Data.NextNotifyAt);
    }
}
