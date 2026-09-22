using System.Net;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

public class InternalSelectionServiceTests
{
    private sealed class ApiStubHandler(Func<string, string> respond) : HttpMessageHandler
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

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUris.Add(request.RequestUri!.ToString());
            RequestBodies.Add(
                request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty
            );
            return Task.FromResult(respond(request));
        }
    }

    private static MaxBotDbContext BuildDb() =>
        new(
            new DbContextOptionsBuilder<MaxBotDbContext>()
                .UseInMemoryDatabase($"maxbot_{Guid.NewGuid()}")
                .Options
        );

    private static CollegeLmsApiClient BuildApi(string groups, string teachers) =>
        new(
            new HttpClient(new ApiStubHandler(kind => kind == "groups" ? groups : teachers))
            {
                BaseAddress = new Uri("http://api.unit.test"),
            },
            NullLogger<CollegeLmsApiClient>.Instance
        );

    private static MaxApiClient BuildMax(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.unit.test") },
            NullLogger<MaxApiClient>.Instance
        );

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static InternalSelectionService BuildService(
        MaxBotDbContext db,
        CollegeLmsApiClient api,
        MaxApiClient max
    ) =>
        new(
            db,
            new InternalProfileService(db, api),
            max,
            Microsoft.Extensions.Options.Options.Create(new MaxBotOptions()),
            NullLogger<InternalSelectionService>.Instance
        );

    [Fact]
    public async Task SetAsync_NewUser_CreatesSettingsAndResolvesName()
    {
        var groupId = Guid.NewGuid();
        await using var db = BuildDb();
        var handler = new RecordingHandler(_ => Json("{}"));
        var service = BuildService(
            db,
            BuildApi(
                $$"""{"isSuccess":true,"data":[{"id":"{{groupId}}","name":"ИС-21-1"}]}""",
                """{"isSuccess":true,"data":[]}"""
            ),
            BuildMax(handler)
        );

        var profile = await service.SetAsync(42, groupId, null, CancellationToken.None);

        profile.Found.Should().BeTrue();
        profile.Role.Should().Be("student");
        profile.GroupId.Should().Be(groupId);
        profile.GroupName.Should().Be("ИС-21-1");

        var saved = await db.UserSettings.SingleAsync(x => x.MaxUserId == 42);
        saved.GroupId.Should().Be(groupId);
        saved.Role.Should().Be("student");

        // Чат ещё не известен (пользователь не запускал бота) — сообщение не отправляем.
        handler.RequestUris.Should().BeEmpty();
    }

    [Fact]
    public async Task SetAsync_ChangedSelection_SendsMenuToChat()
    {
        var chatId = 777L;
        var oldGroup = Guid.NewGuid();
        var newGroup = Guid.NewGuid();

        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = chatId,
                Role = "student",
                GroupId = oldGroup,
            }
        );
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(_ => Json("{}"));
        var service = BuildService(
            db,
            BuildApi(
                $$"""{"isSuccess":true,"data":[{"id":"{{newGroup}}","name":"ИС-21-1"}]}""",
                """{"isSuccess":true,"data":[]}"""
            ),
            BuildMax(handler)
        );

        var profile = await service.SetAsync(42, newGroup, null, CancellationToken.None);

        profile.GroupId.Should().Be(newGroup);
        handler.RequestUris.Should().ContainSingle();
        handler.RequestUris[0].Should().Contain($"chat_id={chatId}");

        using var body = JsonDocument.Parse(handler.RequestBodies[0]);
        var text = body.RootElement.GetProperty("text").GetString();
        text.Should().Contain("Группа: ИС-21-1");
    }

    [Fact]
    public async Task SetAsync_SameSelection_DoesNotSendMessage()
    {
        var groupId = Guid.NewGuid();

        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = 777,
                Role = "student",
                GroupId = groupId,
            }
        );
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(_ => Json("{}"));
        var service = BuildService(
            db,
            BuildApi(
                $$"""{"isSuccess":true,"data":[{"id":"{{groupId}}","name":"ИС-21-1"}]}""",
                """{"isSuccess":true,"data":[]}"""
            ),
            BuildMax(handler)
        );

        await service.SetAsync(42, groupId, null, CancellationToken.None);

        handler.RequestUris.Should().BeEmpty();
    }

    [Fact]
    public async Task SetAsync_TeacherSelection_SwitchesRoleAndClearsGroup()
    {
        var teacherId = Guid.NewGuid();

        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = 777,
                Role = "student",
                GroupId = Guid.NewGuid(),
            }
        );
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(_ => Json("{}"));
        var service = BuildService(
            db,
            BuildApi(
                """{"isSuccess":true,"data":[]}""",
                $$"""{"isSuccess":true,"data":[{"id":"{{teacherId}}","fullName":"Иванов И. И."}]}"""
            ),
            BuildMax(handler)
        );

        var profile = await service.SetAsync(42, null, teacherId, CancellationToken.None);

        profile.Role.Should().Be("teacher");
        profile.TeacherId.Should().Be(teacherId);
        profile.TeacherName.Should().Be("Иванов И. И.");
        profile.GroupId.Should().BeNull();

        var saved = await db.UserSettings.SingleAsync(x => x.MaxUserId == 42);
        saved.Role.Should().Be("teacher");
        saved.GroupId.Should().BeNull();
        saved.TeacherId.Should().Be(teacherId);

        handler.RequestUris.Should().ContainSingle();
        using var body = JsonDocument.Parse(handler.RequestBodies[0]);
        body.RootElement.GetProperty("text")
            .GetString()
            .Should()
            .Contain("Преподаватель: Иванов И. И.");
    }

    [Fact]
    public async Task SetAsync_WhenChatDeliveryFails_StillSavesSelection()
    {
        var groupId = Guid.NewGuid();

        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = 777,
                Role = "student",
            }
        );
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(_ => new HttpResponseMessage(
            HttpStatusCode.InternalServerError
        ));
        var service = BuildService(
            db,
            BuildApi(
                $$"""{"isSuccess":true,"data":[{"id":"{{groupId}}","name":"ИС-21-1"}]}""",
                """{"isSuccess":true,"data":[]}"""
            ),
            BuildMax(handler)
        );

        var profile = await service.SetAsync(42, groupId, null, CancellationToken.None);

        profile.Found.Should().BeTrue();
        var saved = await db.UserSettings.SingleAsync(x => x.MaxUserId == 42);
        saved.GroupId.Should().Be(groupId);
    }
}
