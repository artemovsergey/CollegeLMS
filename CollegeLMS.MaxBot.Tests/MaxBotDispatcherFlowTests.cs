using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class MaxBotDispatcherFlowTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond(request));
    }

    private static CollegeLmsApiClient BuildClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.unit.test") },
            NullLogger<CollegeLmsApiClient>.Instance
        );

    [Fact]
    public async Task DispatcherLoginAsync_Unauthorized_ReturnsNull()
    {
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))
        );

        var token = await client.DispatcherLoginAsync("wrong", CancellationToken.None);

        token.Should().BeNull();
    }

    [Fact]
    public async Task DispatcherLoginAsync_Success_ReturnsToken()
    {
        var body =
            """{"isSuccess":true,"data":{"token":"tok-123","expiresAt":"2026-01-01T00:00:00Z"}}""";
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            })
        );

        var token = await client.DispatcherLoginAsync("secret", CancellationToken.None);

        token.Should().Be("tok-123");
    }

    [Fact]
    public async Task GetScheduleXlsxAsync_SendsBearerToken()
    {
        string? authHeader = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                authHeader = request.Headers.Authorization?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3]),
                };
            })
        );

        var bytes = await client.GetScheduleXlsxAsync(null, "tok-123", CancellationToken.None);

        bytes.Should().Equal([1, 2, 3]);
        authHeader.Should().Be(new AuthenticationHeaderValue("Bearer", "tok-123").ToString());
    }

    [Fact]
    public void BuildScheduleExportXlsxUrl_IncludesGroupId()
    {
        var groupId = Guid.NewGuid();

        var url = MiniAppUrlBuilder.BuildScheduleExportXlsxUrl("https://stvcc.tech/max", groupId);

        url.Should()
            .Be($"https://stvcc.tech/max/api/schedule/export?format=xlsx&groupId={groupId}");
    }

    [Fact]
    public void BuildScheduleExportXlsxUrl_WithoutGroup_OmissesGroupParam()
    {
        var url = MiniAppUrlBuilder.BuildScheduleExportXlsxUrl("https://stvcc.tech/max", null);

        url.Should().Be("https://stvcc.tech/max/api/schedule/export?format=xlsx");
    }
}
