using System.Net;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models.Max;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Отправка сообщений с inline-клавиатурой: кнопки open_app должны содержать
/// web_app и не отправлять url, а отказ MAX API по клавиатуре не должен
/// оставлять пользователя без ответа.
/// </summary>
public class MaxApiKeyboardTests
{
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

    private static MaxApiClient BuildClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.unit.test") },
            NullLogger<MaxApiClient>.Instance
        );

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    [Fact]
    public async Task SendInlineKeyboardAsync_OpenAppButton_SendsWebAppWithoutUrl()
    {
        string? body = null;
        var handler = new RecordingHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json("{}");
        });
        var client = BuildClient(handler);
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "open_app",
                    Text = "📱 Открыть расписание",
                    WebApp = "teacher_scc_bot",
                    Payload = "today",
                },
            },
        };

        await client.SendInlineKeyboardAsync(
            777,
            "Главное меню",
            buttons,
            ct: CancellationToken.None
        );

        body.Should().NotBeNull();
        using var doc = JsonDocument.Parse(body!);
        var button = doc
            .RootElement.GetProperty("attachments")[0]
            .GetProperty("payload")
            .GetProperty("buttons")[0][0];

        button.GetProperty("type").GetString().Should().Be("open_app");
        button.GetProperty("text").GetString().Should().Be("📱 Открыть расписание");
        button.GetProperty("web_app").GetString().Should().Be("teacher_scc_bot");
        button.GetProperty("payload").GetString().Should().Be("today");
        button.TryGetProperty("url", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendInlineKeyboardAsync_LinkButton_SendsUrlWithoutWebApp()
    {
        string? body = null;
        var handler = new RecordingHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json("{}");
        });
        var client = BuildClient(handler);
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "link",
                    Text = "📥 Скачать XLSX",
                    Url = "https://stvcc.tech/max/api/schedule/export?format=xlsx",
                },
            },
        };

        await client.SendInlineKeyboardAsync(777, "Файл", buttons, ct: CancellationToken.None);

        body.Should().NotBeNull();
        using var doc = JsonDocument.Parse(body!);
        var button = doc
            .RootElement.GetProperty("attachments")[0]
            .GetProperty("payload")
            .GetProperty("buttons")[0][0];

        button
            .GetProperty("url")
            .GetString()
            .Should()
            .Be("https://stvcc.tech/max/api/schedule/export?format=xlsx");
        button.TryGetProperty("web_app", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendInlineKeyboardAsync_KeyboardRejected_FallsBackToPlainText()
    {
        var attempts = 0;
        var handler = new RecordingHandler(_ =>
        {
            attempts++;
            return attempts == 1
                ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        """{"code":"proto.payload","message":"Field 'webApp' cannot be null"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
                : Json("{}");
        });
        var client = BuildClient(handler);
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "open_app",
                    Text = "📱 Открыть расписание",
                    WebApp = "teacher_scc_bot",
                    Payload = "today",
                },
            },
        };

        await client.SendInlineKeyboardAsync(
            777,
            "Главное меню",
            buttons,
            ct: CancellationToken.None
        );

        attempts.Should().Be(2);
        handler.RequestUris.Should().HaveCount(2);
        handler.RequestUris[0].Should().Be(handler.RequestUris[1]);
        handler.RequestBodies[0].Should().Contain("attachments");
        handler.RequestBodies[1].Should().NotContain("attachments");
        using var fallback = JsonDocument.Parse(handler.RequestBodies[1]);
        fallback.RootElement.GetProperty("text").GetString().Should().Be("Главное меню");
    }
}
