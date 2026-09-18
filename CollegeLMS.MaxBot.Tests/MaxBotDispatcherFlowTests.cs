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

    [Fact]
    public async Task ConfirmCorrectionAsync_SendsBearerAndIdempotencyKey()
    {
        string? auth = null;
        string? idem = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                auth = request.Headers.Authorization?.ToString();
                idem = request.Headers.Contains("Idempotency-Key")
                    ? string.Join(",", request.Headers.GetValues("Idempotency-Key"))
                    : "";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"isSuccess":true,"data":{"applied":1,"history":[]}}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        );

        var entry = new CorrectionEntryDto
        {
            Row = 1,
            GroupId = Guid.NewGuid(),
            GroupName = "ИС-21",
            ChangeType = "Add",
            DayOfWeek = 1,
            Week = 1,
            NumberPair = 1,
        };

        var result = await client.ConfirmCorrectionAsync(entry, "tok-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Applied.Should().Be(1);
        auth.Should().Be(new AuthenticationHeaderValue("Bearer", "tok-1").ToString());
        Guid.TryParse(idem, out _).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmCorrectionAsync_ServerError_ReturnsNull()
    {
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden))
        );

        var entry = new CorrectionEntryDto
        {
            Row = 1,
            GroupId = Guid.NewGuid(),
            GroupName = "ИС-21",
            ChangeType = "Add",
            DayOfWeek = 1,
            Week = 1,
            NumberPair = 1,
        };

        var result = await client.ConfirmCorrectionAsync(entry, "tok-1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetScheduleMetaAsync_ParsesTotalWeeks()
    {
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"isSuccess":true,"data":{"semesterStart":"2026-09-01","totalWeeks":16,"currentWeek":2,"currentDate":"2026-09-15"}}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            })
        );

        var meta = await client.GetScheduleMetaAsync(CancellationToken.None);

        meta.Should().NotBeNull();
        meta!.TotalWeeks.Should().Be(16);
        meta.CurrentWeek.Should().Be(2);
    }

    [Fact]
    public async Task CreateCorrectionBatchAsync_SendsBearerAndDate()
    {
        var batchId = Guid.NewGuid();
        string? auth = null;
        string? body = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                auth = request.Headers.Authorization?.ToString();
                body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(
                            new { isSuccess = true, data = new { id = batchId } }
                        ),
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        );

        var result = await client.CreateCorrectionBatchAsync(
            new DateTime(2026, 9, 15),
            "tok-batch",
            CancellationToken.None
        );

        result.Should().NotBeNull();
        result!.Id.Should().Be(batchId);
        auth.Should().Be(new AuthenticationHeaderValue("Bearer", "tok-batch").ToString());
        body.Should().Contain("2026-09-15");
    }

    [Fact]
    public async Task AddCorrectionPositionAsync_UsesBatchPositionRoute()
    {
        var batchId = Guid.NewGuid();
        Uri? requestUri = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                requestUri = request.RequestUri;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"isSuccess":true,"data":{}}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        );

        var added = await client.AddCorrectionPositionAsync(
            batchId,
            new CreateCorrectionPositionDto
            {
                ChangeType = "Move",
                GroupId = Guid.NewGuid(),
                GroupName = "ИС-21",
                NumberPair = 4,
                Note = "вм.4",
            },
            "tok-batch",
            CancellationToken.None
        );

        added.Should().BeTrue();
        requestUri!
            .AbsolutePath.Should()
            .Be($"/api/schedule/correction/batches/{batchId}/positions");
    }

    [Fact]
    public async Task ApplyCorrectionBatchAsync_SendsProvidedIdempotencyKey()
    {
        var batchId = Guid.NewGuid();
        string? idempotencyKey = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                idempotencyKey = string.Join(",", request.Headers.GetValues("Idempotency-Key"));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(
                            new { isSuccess = true, data = new { applied = 1, batchId } }
                        ),
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        );

        var result = await client.ApplyCorrectionBatchAsync(
            batchId,
            "tok-batch",
            "idem-123",
            CancellationToken.None
        );

        result.Should().NotBeNull();
        result!.Applied.Should().Be(1);
        result.BatchId.Should().Be(batchId);
        idempotencyKey.Should().Be("idem-123");
    }
}
