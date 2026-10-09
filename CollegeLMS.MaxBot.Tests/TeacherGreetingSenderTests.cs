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

/// <summary>
/// Рассылка поздравления преподавателям: отбор аудитории по роли teacher,
/// один upload на всех и отказоустойчивость при сбое в одном чате.
/// </summary>
public class TeacherGreetingSenderTests
{
    private sealed class FakeMaxHandler : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Uri, string? Body)> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => Json("{}");

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!.ToString(), body));
            return Responder(request);
        }
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    /// <summary>Ожидаемый happy-path: uploads → uploadUrl → по одному сообщению на чат.</summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> HappyPath(
        Func<HttpRequestMessage, bool>? isFail = null
    ) =>
        request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/uploads?type=image"))
                return Json("""{"url":"https://cdn.example/upload/1"}""");
            if (uri.StartsWith("https://cdn.example/upload/1"))
                return Json("""{"token":"tok"}""");
            if (uri.Contains("/messages?chat_id="))
                return isFail?.Invoke(request) == true
                    ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            """{"code":"chat.blocked"}""",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                    : Json("{}");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

    private static MaxBotDbContext BuildDb() =>
        new(
            new DbContextOptionsBuilder<MaxBotDbContext>()
                .UseInMemoryDatabase($"maxbot_{Guid.NewGuid()}")
                .Options
        );

    private static UserSettings Teacher(long chatId, Guid teacherId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = 1000 + chatId,
            MaxChatId = chatId,
            Role = "teacher",
            TeacherId = teacherId,
        };

    private static UserSettings Student(long chatId, Guid groupId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = 1000 + chatId,
            MaxChatId = chatId,
            Role = "student",
            GroupId = groupId,
        };

    private static TeacherGreetingSender CreateSender(FakeMaxHandler handler, MaxBotDbContext db) =>
        new(
            db,
            new MaxApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://max.example") },
                NullLogger<MaxApiClient>.Instance
            ),
            NullLogger<TeacherGreetingSender>.Instance
        );

    [Fact]
    public async Task SendAsync_NoTeachers_SkipsUploadAndReturnsEmptyReport()
    {
        var handler = new FakeMaxHandler();
        await using var db = BuildDb();
        db.UserSettings.Add(Student(10, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var sender = CreateSender(handler, db);

        var report = await sender.SendAsync(new byte[] { 1 }, "текст", default);

        report.Should().Be(new TeacherGreetingSender.Report(0, 0, 0));
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_UploadsOnceAndSendsToEveryTeacherChat()
    {
        var handler = new FakeMaxHandler { Responder = HappyPath() };
        await using var db = BuildDb();
        db.UserSettings.Add(Teacher(11, Guid.NewGuid()));
        db.UserSettings.Add(Teacher(22, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var sender = CreateSender(handler, db);

        var report = await sender.SendAsync(new byte[] { 1, 2, 3 }, "🌸 С Днём учителя!", default);

        report.Should().Be(new TeacherGreetingSender.Report(2, 2, 0));

        // Один upload на всю рассылку, дальше по одному сообщению на чат.
        handler.Requests.Count(r => r.Uri.Contains("/uploads?type=image")).Should().Be(1);
        handler.Requests.Count(r => r.Uri.Contains("https://cdn.example/upload/1")).Should().Be(1);

        var messages = handler.Requests.Where(r => r.Uri.Contains("/messages?chat_id=")).ToList();
        messages.Should().HaveCount(2);
        messages.Select(m => m.Uri).Should().Contain("https://max.example/messages?chat_id=11");
        messages.Select(m => m.Uri).Should().Contain("https://max.example/messages?chat_id=22");
        messages.Should().OnlyContain(m => m.Body!.Contains("\"type\":\"image\""));

        // Текст уходит в JSON с escape не-ASCII, поэтому сравниваем разобранное поле.
        messages
            .Select(m => JsonDocument.Parse(m.Body!).RootElement.GetProperty("text").GetString())
            .Should()
            .AllBe("🌸 С Днём учителя!");
    }

    [Fact]
    public async Task SendAsync_SkipsStudentsAndDuplicateTeacherChats()
    {
        var handler = new FakeMaxHandler { Responder = HappyPath() };
        await using var db = BuildDb();
        db.UserSettings.Add(Student(10, Guid.NewGuid()));
        db.UserSettings.Add(Teacher(11, Guid.NewGuid()));
        // Тот же чат преподавателя — настройка от другого MAX-аккаунта, дублировать нельзя.
        db.UserSettings.Add(Teacher(11, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var sender = CreateSender(handler, db);

        var report = await sender.SendAsync(new byte[] { 1 }, "текст", default);

        report.Should().Be(new TeacherGreetingSender.Report(1, 1, 0));
        handler.Requests.Count(r => r.Uri.Contains("/messages?chat_id=")).Should().Be(1);
        handler
            .Requests.Where(r => r.Uri.Contains("/messages?chat_id="))
            .Single()
            .Uri.Should()
            .EndWith("chat_id=11");
    }

    [Fact]
    public async Task SendAsync_OneChatFails_ContinuesAndReportsFailure()
    {
        var handler = new FakeMaxHandler
        {
            Responder = HappyPath(r => r.RequestUri!.ToString().EndsWith("chat_id=11")),
        };
        await using var db = BuildDb();
        db.UserSettings.Add(Teacher(11, Guid.NewGuid()));
        db.UserSettings.Add(Teacher(22, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var sender = CreateSender(handler, db);

        var report = await sender.SendAsync(new byte[] { 1 }, "текст", default);

        report.Should().Be(new TeacherGreetingSender.Report(2, 1, 1));
        handler.Requests.Where(r => r.Uri.Contains("/messages?chat_id=22")).Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_UsesAsciiFileName_GreetingPng()
    {
        var handler = new FakeMaxHandler { Responder = HappyPath() };
        await using var db = BuildDb();
        db.UserSettings.Add(Teacher(11, Guid.NewGuid()));
        await db.SaveChangesAsync();
        var sender = CreateSender(handler, db);

        await sender.SendAsync(new byte[] { 1 }, "текст", default);

        // Имя файла входит в multipart — MAX ждёт ASCII-имя.
        handler
            .Requests.Single(r => r.Uri.Contains("https://cdn.example/upload/1"))
            .Body.Should()
            .Contain("greeting.png");
    }
}
