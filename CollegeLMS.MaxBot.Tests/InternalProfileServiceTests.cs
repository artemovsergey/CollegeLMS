using System.Net;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

public class InternalProfileServiceTests
{
    private sealed class StubHandler(Func<string, string> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var url = request.RequestUri!.ToString();
            var body = respond(url.Contains("/api/groups") ? "groups" : "teachers");
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private static CollegeLmsApiClient BuildApi(string groups, string teachers) =>
        new(
            new HttpClient(new StubHandler(kind => kind == "groups" ? groups : teachers))
            {
                BaseAddress = new Uri("http://api.unit.test"),
            },
            NullLogger<CollegeLmsApiClient>.Instance
        );

    private static MaxBotDbContext BuildDb() =>
        new(
            new DbContextOptionsBuilder<MaxBotDbContext>()
                .UseInMemoryDatabase($"maxbot_{Guid.NewGuid()}")
                .Options
        );

    [Fact]
    public async Task GetAsync_ReturnsProfileWithResolvedNames()
    {
        var groupId = Guid.NewGuid();
        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = 42,
                Role = "student",
                GroupId = groupId,
            }
        );
        await db.SaveChangesAsync();

        var api = BuildApi(
            $$"""{"isSuccess":true,"data":[{"id":"{{groupId}}","name":"ИС-21-1"}]}""",
            """{"isSuccess":true,"data":[]}"""
        );
        var service = new InternalProfileService(db, api);

        var profile = await service.GetAsync(42, CancellationToken.None);

        profile.Found.Should().BeTrue();
        profile.Role.Should().Be("student");
        profile.GroupId.Should().Be(groupId);
        profile.GroupName.Should().Be("ИС-21-1");
    }

    [Fact]
    public async Task GetAsync_UnknownUser_ReturnsNotFound()
    {
        await using var db = BuildDb();
        var api = BuildApi("""{"isSuccess":true,"data":[]}""", """{"isSuccess":true,"data":[]}""");
        var service = new InternalProfileService(db, api);

        var profile = await service.GetAsync(999, CancellationToken.None);

        profile.Found.Should().BeFalse();
        profile.Role.Should().Be("student");
    }

    [Fact]
    public void InternalSecret_GuardRejectsWrongValue()
    {
        WebhookSecretValidator.IsValid("secret", "other").Should().BeFalse();
        WebhookSecretValidator.IsValid("secret", "secret").Should().BeTrue();
    }
}
