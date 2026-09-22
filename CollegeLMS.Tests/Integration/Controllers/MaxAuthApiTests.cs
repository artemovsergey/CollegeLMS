using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class MaxAuthApiTests : BaseIntegrationTest
{
    private const string BotToken = "test-bot-token";
    private const long MaxUserId = 555000111;

    private sealed class BotStub(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var json = JsonSerializer.Serialize(
                new MaxInternalUserDto
                {
                    Found = true,
                    MaxUserId = MaxUserId,
                    Role = "student",
                    GroupId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GroupName = "ИС-21-1",
                }
            );
            return Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private static string BuildInitData(long userId, DateTimeOffset authDate)
    {
        var pairs = new Dictionary<string, string>
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "q1",
            ["start_param"] = "today",
            ["user"] = JsonSerializer.Serialize(
                new
                {
                    id = userId,
                    first_name = "Иван",
                    last_name = "Иванов",
                }
            ),
        };
        var launch = string.Join("\n", pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(BotToken)
        );
        var hash = Convert
            .ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
            .ToLowerInvariant();
        return string.Join(
                "&",
                pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
            ) + $"&hash={hash}";
    }

    private HttpClient ClientWithBot(HttpStatusCode status)
    {
        var factory = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MaxAuth:BotToken", BotToken);
            builder.ConfigureServices(services =>
                services
                    .AddHttpClient<MaxBotHttpClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new BotStub(status))
            );
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidInitData_ReturnsTokenAndProfile()
    {
        var client = ClientWithBot(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.False(string.IsNullOrEmpty(body.Data!.Token));
        Assert.Equal("Student", body.Data.Profile.Role);
        Assert.Equal("ИС-21-1", body.Data.Profile.GroupName);
        Assert.Equal(MaxUserId, body.Data.Profile.MaxUserId);
    }

    [Fact]
    public async Task Login_BotUnavailable_StillReturnsTokenAsGuest()
    {
        var client = ClientWithBot(HttpStatusCode.InternalServerError);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.False(string.IsNullOrEmpty(body.Data!.Token));
        Assert.Equal("Other", body.Data.Profile.Role);
    }

    [Fact]
    public async Task Login_TamperedHash_Returns401()
    {
        var client = ClientWithBot(HttpStatusCode.OK);
        var initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) + "tampered";

        var response = await client.PostAsJsonAsync("/api/auth/max", new { initData });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_StaleAuthDate_Returns401()
    {
        var client = ClientWithBot(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow.AddHours(-2)) }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
