using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models.Max;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Отправка файлов в Max API: multipart-загрузка по /uploads?type=file
/// и сообщение с вложением type=file вместе с inline-клавиатурой.
/// </summary>
public class MaxApiFileTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUris.Add(request.RequestUri!.ToString());
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
    public async Task UploadFileAsync_PostsMultipartWithFileNameAndContentType()
    {
        HttpMethod? method = null;
        string? uri = null;
        string? fieldName = null;
        string? fileName = null;
        string? contentType = null;
        byte[]? uploaded = null;
        var handler = new RecordingHandler(request =>
        {
            method = request.Method;
            uri = request.RequestUri!.ToString();
            var multipart = (MultipartFormDataContent)request.Content!;
            var part = multipart.Single();
            fieldName = part.Headers.ContentDisposition!.Name!.Trim('"');
            fileName =
                part.Headers.ContentDisposition.FileName?.Trim('"')
                ?? part.Headers.ContentDisposition.FileNameStar?.Trim('"');
            contentType = part.Headers.ContentType!.MediaType;
            uploaded = part.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return Json("""{"token":"file-token"}""");
        });
        var client = BuildClient(handler);

        var payload = await client.UploadFileAsync(
            "https://cdn.example/upload/file",
            [1, 2, 3],
            "schedule.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CancellationToken.None
        );

        method.Should().Be(HttpMethod.Post);
        uri.Should().Be("https://cdn.example/upload/file");
        fieldName.Should().Be("data");
        fileName.Should().Be("schedule.xlsx");
        contentType
            .Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        uploaded.Should().Equal([1, 2, 3]);
        payload.GetProperty("token").GetString().Should().Be("file-token");
    }

    [Fact]
    public async Task UploadFileAsync_MultipartFileName_IsAscii()
    {
        string? fieldName = null;
        string? fileName = null;
        var handler = new RecordingHandler(request =>
        {
            var multipart = (MultipartFormDataContent)request.Content!;
            var part = multipart.Single();
            fieldName = part.Headers.ContentDisposition!.Name!.Trim('"');
            fileName =
                part.Headers.ContentDisposition.FileName?.Trim('"')
                ?? part.Headers.ContentDisposition.FileNameStar?.Trim('"');
            return Json("""{"token":"file-token"}""");
        });
        var client = BuildClient(handler);

        const string asciiName = "schedule_2026-09-21_14-35-00.xlsx";

        await client.UploadFileAsync(
            "https://cdn.example/upload/file",
            [1, 2, 3],
            asciiName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CancellationToken.None
        );

        fieldName.Should().Be("data");
        fileName.Should().Be(asciiName);
        fileName.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Fact]
    public async Task UploadImageAsync_UsesPngContentType()
    {
        string? contentType = null;
        var handler = new RecordingHandler(request =>
        {
            var multipart = (MultipartFormDataContent)request.Content!;
            contentType = multipart.Single().Headers.ContentType!.MediaType;
            return Json("""{"token":"img-token"}""");
        });
        var client = BuildClient(handler);

        await client.UploadImageAsync(
            "https://cdn.example/upload/image",
            [1],
            "correction.png",
            CancellationToken.None
        );

        contentType.Should().Be("image/png");
    }

    [Fact]
    public async Task SendDocumentAsync_SendsFileAndKeyboardAttachments()
    {
        string? body = null;
        var handler = new RecordingHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json("{}");
        });
        var client = BuildClient(handler);
        var payload = JsonSerializer.Deserialize<JsonElement>("""{"token":"file-token"}""");
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

        var result = await client.SendDocumentAsync(
            777,
            payload,
            "📎 Расписание",
            buttons,
            CancellationToken.None
        );

        result.Should().NotBeNull();
        body.Should().NotBeNull();
        using var doc = JsonDocument.Parse(body!);
        var attachments = doc.RootElement.GetProperty("attachments");
        attachments.ValueKind.Should().Be(JsonValueKind.Array);
        attachments.GetArrayLength().Should().Be(2);
        attachments[0].GetProperty("type").GetString().Should().Be("file");
        attachments[0]
            .GetProperty("payload")
            .GetProperty("token")
            .GetString()
            .Should()
            .Be("file-token");
        attachments[1].GetProperty("type").GetString().Should().Be("inline_keyboard");
        var button = attachments[1].GetProperty("payload").GetProperty("buttons")[0][0];
        button.GetProperty("text").GetString().Should().Be("📥 Скачать XLSX");
        button
            .GetProperty("url")
            .GetString()
            .Should()
            .Be("https://stvcc.tech/max/api/schedule/export?format=xlsx");
        handler.RequestUris.Should().Equal("https://api.unit.test/messages?chat_id=777");
    }

    [Fact]
    public async Task SendDocumentAsync_NotReady_RetriesThenSucceeds()
    {
        var attempts = 0;
        var handler = new RecordingHandler(_ =>
        {
            attempts++;
            if (attempts < 3)
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        """{"code":"attachment.not.ready"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            return Json("{}");
        });
        var client = BuildClient(handler);
        var payload = JsonSerializer.Deserialize<JsonElement>("""{"token":"file-token"}""");
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "link",
                    Text = "Кнопка",
                    Url = "https://x",
                },
            },
        };

        var result = await client.SendDocumentAsync(
            777,
            payload,
            "caption",
            buttons,
            CancellationToken.None
        );

        result.Should().NotBeNull();
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task SendMessageAsync_NonSuccess_ThrowsWithStatusCodeAndBody()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                """{"code":"access.denied","message":"bot blocked"}""",
                Encoding.UTF8,
                "application/json"
            ),
        });
        var client = BuildClient(handler);

        var act = async () =>
            await client.SendMessageAsync(1, "привет", ct: CancellationToken.None);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.Message.Should().Contain("403");
        ex.Which.Message.Should().Contain("bot blocked");
    }
}
