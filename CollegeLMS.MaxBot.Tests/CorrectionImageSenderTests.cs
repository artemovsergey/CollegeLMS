using System.Net;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Отправка PNG-картинки корректировки в канал Max (UC-SCH-28/29):
/// пустой канал, upload → send и повтор при attachment.not.ready.
/// </summary>
public class CorrectionImageSenderTests
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

    private static CorrectionImageSender CreateSender(FakeMaxHandler handler, string channel) =>
        new(
            new MaxApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://max.example") },
                NullLogger<MaxApiClient>.Instance
            ),
            Options.Create(new MaxBotOptions { CorrectionChannelId = channel }),
            NullLogger<CorrectionImageSender>.Instance
        );

    [Fact]
    public async Task SendAsync_EmptyChannel_SkipsWithoutExceptionAndRequests()
    {
        var handler = new FakeMaxHandler();
        var sender = CreateSender(handler, "");

        Func<Task> act = () => sender.SendAsync(new byte[] { 1, 2, 3 }, "caption", default);

        await act.Should().NotThrowAsync();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithChannel_UploadsThenSendsImage()
    {
        var handler = new FakeMaxHandler
        {
            Responder = request =>
            {
                var uri = request.RequestUri!.ToString();
                if (uri.Contains("/uploads?type=image"))
                    return Json("""{"url":"https://cdn.example/upload/1"}""");
                if (uri.StartsWith("https://cdn.example/upload/1"))
                    return Json("""{"token":"tok"}""");
                if (uri.Contains("/messages?chat_id="))
                    return Json("{}");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            },
        };
        var sender = CreateSender(handler, "777");

        await sender.SendAsync(new byte[] { 1, 2, 3 }, "🔔 caption", default);

        handler.Requests.Should().HaveCount(3);
        handler.Requests[0].Uri.Should().Contain("/uploads?type=image");
        handler.Requests[1].Uri.Should().Contain("https://cdn.example/upload/1");
        handler.Requests[2].Uri.Should().Contain("/messages?chat_id=777");
        handler.Requests[2].Body.Should().Contain("caption");
        handler.Requests[2].Body.Should().Contain("\"type\":\"image\"");
    }

    [Fact]
    public async Task SendAsync_AttachmentNotReady_RetriesMessage()
    {
        var messageAttempts = 0;
        var handler = new FakeMaxHandler
        {
            Responder = request =>
            {
                var uri = request.RequestUri!.ToString();
                if (uri.Contains("/uploads?type=image"))
                    return Json("""{"url":"https://cdn.example/upload/1"}""");
                if (uri.StartsWith("https://cdn.example/upload/1"))
                    return Json("""{"token":"tok"}""");

                messageAttempts++;
                if (messageAttempts == 1)
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            """{"code":"attachment.not.ready"}""",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    };
                return Json("{}");
            },
        };
        var sender = CreateSender(handler, "777");

        await sender.SendAsync(new byte[] { 1, 2, 3 }, "caption", default);

        messageAttempts.Should().Be(2);
    }
}
