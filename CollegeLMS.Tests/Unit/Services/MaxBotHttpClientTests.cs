using System.Net;
using System.Text;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

public class MaxBotHttpClientTests
{
    private sealed class CapturingHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public string? Secret { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Last = request;
            Secret = request.Headers.TryGetValues("X-Internal-Secret", out var v)
                ? v.First()
                : null;
            if (request.Content is not null)
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static MaxBotHttpClient Build(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://maxbot.unit.test") },
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["MaxBot:InternalSecret"] = "s3cr3t" }
                )
                .Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );

    [Fact]
    public async Task GetInternalUserAsync_SendsSecretAndParses()
    {
        var groupId = Guid.NewGuid();
        var handler = new CapturingHandler(
            $$"""{"found":true,"maxUserId":42,"role":"student","groupId":"{{groupId}}","groupName":"ИС-21-1","teacherId":null,"teacherName":null}"""
        );

        var dto = await Build(handler).GetInternalUserAsync(42, CancellationToken.None);

        handler.Last!.RequestUri!.AbsolutePath.Should().Be("/maxbot/internal/users/42");
        handler.Secret.Should().Be("s3cr3t");
        dto!.Found.Should().BeTrue();
        dto.GroupName.Should().Be("ИС-21-1");
    }

    [Fact]
    public async Task GetInternalUserAsync_BotError_ReturnsNull()
    {
        var errorHandler = new StubHandler(HttpStatusCode.InternalServerError, "{}");
        var dto = await Build(errorHandler).GetInternalUserAsync(42, CancellationToken.None);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task SetSelectionAsync_PostsSelectionWithSecret()
    {
        var groupId = Guid.NewGuid();
        var handler = new CapturingHandler(
            $$"""{"found":true,"maxUserId":42,"role":"student","groupId":"{{groupId}}","groupName":"ИС-21-1","teacherId":null,"teacherName":null}"""
        );

        var dto = await Build(handler).SetSelectionAsync(42, groupId, null, CancellationToken.None);

        handler.Last!.Method.Should().Be(HttpMethod.Post);
        handler.Last.RequestUri!.AbsolutePath.Should().Be("/maxbot/internal/selection");
        handler.Secret.Should().Be("s3cr3t");
        handler.Body.Should().Contain(groupId.ToString());
        dto!.GroupName.Should().Be("ИС-21-1");
    }

    [Fact]
    public async Task SetSelectionAsync_BotError_ReturnsNull()
    {
        var errorHandler = new StubHandler(HttpStatusCode.ServiceUnavailable, "{}");
        var dto = await Build(errorHandler)
            .SetSelectionAsync(42, null, Guid.NewGuid(), CancellationToken.None);

        dto.Should().BeNull();
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
    }
}
