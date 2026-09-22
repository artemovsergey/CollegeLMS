using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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

    private sealed class ProfileStub(MaxInternalUserDto dto) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var json = JsonSerializer.Serialize(dto);
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private HttpClient ClientWithProfile(MaxInternalUserDto dto)
    {
        var factory = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MaxAuth:BotToken", BotToken);
            builder.ConfigureServices(services =>
                services
                    .AddHttpClient<MaxBotHttpClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new ProfileStub(dto))
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

    [Fact]
    public async Task Login_BotHasNoRecord_ReturnsGuestRoleOther()
    {
        // Бот отвечает 200, но записи user_settings нет: Found=false и Role="student".
        var client = ClientWithProfile(
            new MaxInternalUserDto
            {
                Found = false,
                MaxUserId = MaxUserId,
                Role = "student",
            }
        );

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Other", body.Data!.Profile.Role);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.Data.Token);
        Assert.DoesNotContain(token.Claims, c => c.Type == ClaimTypes.Role);
        Assert.Null(token.Claims.FirstOrDefault(c => c.Type == "groupId"));
        Assert.Null(token.Claims.FirstOrDefault(c => c.Type == "teacherId"));
        Assert.Equal(MaxUserId.ToString(), token.Claims.First(c => c.Type == "max_user_id").Value);
    }

    [Fact]
    public async Task Login_ValidInitData_TokenCarriesMaxStudentClaims()
    {
        var groupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var client = ClientWithProfile(
            new MaxInternalUserDto
            {
                Found = true,
                MaxUserId = MaxUserId,
                Role = "student",
                GroupId = groupId,
                GroupName = "ИС-21-1",
            }
        );

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.Data!.Token);

        var roleClaim = token.Claims.First(c => c.Type == ClaimTypes.Role);
        Assert.Equal("Student", roleClaim.Value);
        Assert.Equal(MaxUserId.ToString(), token.Claims.First(c => c.Type == "max_user_id").Value);
        Assert.Equal(groupId.ToString(), token.Claims.First(c => c.Type == "groupId").Value);
        Assert.Null(token.Claims.FirstOrDefault(c => c.Type == "teacherId"));
    }

    [Fact]
    public async Task Login_ValidInitData_TokenCarriesMaxTeacherClaims()
    {
        var teacherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var client = ClientWithProfile(
            new MaxInternalUserDto
            {
                Found = true,
                MaxUserId = MaxUserId,
                Role = "teacher",
                TeacherId = teacherId,
                TeacherName = "Петров П. П.",
            }
        );

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.Data!.Token);

        Assert.Equal("Teacher", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(teacherId.ToString(), token.Claims.First(c => c.Type == "teacherId").Value);
        Assert.Null(token.Claims.FirstOrDefault(c => c.Type == "groupId"));
    }
}
