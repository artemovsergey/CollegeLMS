using System.Net;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models.Max;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

public class MaxApiSubscriptionTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
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
    public async Task GetSubscriptionsAsync_ReturnsSubscriptionList()
    {
        var handler = new RecordingHandler(_ =>
            Json(
                """
                {"subscriptions":[{"url":"https://stvcc.tech/maxbot/webhook","update_types":["message_created","bot_started"],"secret":"s3cret"}]}
                """
            )
        );
        var client = BuildClient(handler);

        var subscriptions = await client.GetSubscriptionsAsync(CancellationToken.None);

        subscriptions.Should().NotBeNull();
        subscriptions!.Should().ContainSingle();
        subscriptions[0].Url.Should().Be("https://stvcc.tech/maxbot/webhook");
        subscriptions[0]
            .UpdateTypes.Should()
            .BeEquivalentTo(new[] { "message_created", "bot_started" });
        subscriptions[0].Secret.Should().Be("s3cret");

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.AbsolutePath.Should().Be("/subscriptions");
    }

    [Fact]
    public async Task SubscribeWebhookAsync_SendsUrlTypesAndSecret()
    {
        var handler = new RecordingHandler(_ => Json("""{"success":true}"""));
        var client = BuildClient(handler);

        var result = await client.SubscribeWebhookAsync(
            "https://stvcc.tech/maxbot/webhook",
            ["message_created", "message_callback", "bot_started"],
            "topsecret",
            CancellationToken.None
        );

        result.Should().BeTrue();

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.Should().Be("/subscriptions");

        var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        body.RootElement.GetProperty("url")
            .GetString()
            .Should()
            .Be("https://stvcc.tech/maxbot/webhook");
        body.RootElement.GetProperty("update_types")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Should()
            .BeEquivalentTo(new[] { "message_created", "message_callback", "bot_started" });
        body.RootElement.GetProperty("secret").GetString().Should().Be("topsecret");
    }

    [Fact]
    public async Task SubscribeWebhookAsync_WithoutSecret_OmitsSecretField()
    {
        var handler = new RecordingHandler(_ => Json("""{"success":true}"""));
        var client = BuildClient(handler);

        await client.SubscribeWebhookAsync(
            "https://stvcc.tech/maxbot/webhook",
            ["message_created"],
            ct: CancellationToken.None
        );

        var body = JsonDocument.Parse(
            await handler.Requests.Single().Content!.ReadAsStringAsync()
        );
        body.RootElement.TryGetProperty("secret", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeWebhookAsync_SuccessFalse_ReturnsFalse()
    {
        var handler = new RecordingHandler(_ =>
            Json("""{"success":false,"message":"invalid url"}""")
        );
        var client = BuildClient(handler);

        var result = await client.SubscribeWebhookAsync(
            "https://stvcc.tech/maxbot/webhook",
            ["message_created"],
            "topsecret",
            CancellationToken.None
        );

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnsubscribeWebhookAsync_EscapesUrl()
    {
        var handler = new RecordingHandler(_ => Json("""{"success":true}"""));
        var client = BuildClient(handler);

        var result = await client.UnsubscribeWebhookAsync(
            "https://stvcc.tech/maxbot/webhook",
            CancellationToken.None
        );

        result.Should().BeTrue();

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri!.AbsolutePath.Should().Be("/subscriptions");
        request.RequestUri.Query.Should().Be("?url=https%3A%2F%2Fstvcc.tech%2Fmaxbot%2Fwebhook");
    }
}
