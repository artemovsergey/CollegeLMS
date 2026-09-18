using System.Net;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>Новые методы клиента CollegeLMS: нерабочие дни, вставки, практики.</summary>
public class CollegeLmsApiClientReferenceTests
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

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    [Fact]
    public async Task GetNonWorkingDaysAsync_ParsesItemsAndBuildsUrl()
    {
        string? url = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json(
                    """{"isSuccess":true,"data":{"items":[{"id":"11111111-1111-1111-1111-111111111111","dateFrom":"2026-09-07T00:00:00","dateTo":"2026-09-07T00:00:00","title":"Праздник"}],"totalCount":1,"page":1,"pageSize":100}}"""
                );
            })
        );

        var result = await client.GetNonWorkingDaysAsync(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            CancellationToken.None
        );

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Праздник");
        url.Should().Be("/api/non-working-days?from=2026-09-01&to=2026-09-30&page=1&pageSize=100");
    }

    [Fact]
    public async Task GetNonWorkingDaysAsync_NonSuccess_ReturnsEmpty()
    {
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))
        );

        var result = await client.GetNonWorkingDaysAsync(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            CancellationToken.None
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInsertsAsync_WithCourse_BuildsUrlAndParses()
    {
        string? url = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json(
                    """{"isSuccess":true,"data":[{"id":"22222222-2222-2222-2222-222222222222","title":"Линейка","dayOfWeek":3,"startTime":"08:00:00","endTime":"08:30:00","course":2,"isActive":true}]}"""
                );
            })
        );

        var result = await client.GetInsertsAsync(3, 2, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Линейка");
        url.Should().Be("/api/schedule/inserts?dayOfWeek=3&activeOnly=true&course=2");
    }

    [Fact]
    public async Task GetInsertsAsync_WithoutCourse_OmitsCourseParam()
    {
        string? url = null;
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json("""{"isSuccess":true,"data":[]}""");
            })
        );

        await client.GetInsertsAsync(1, null, CancellationToken.None);

        url.Should().Be("/api/schedule/inserts?dayOfWeek=1&activeOnly=true");
    }

    [Fact]
    public async Task GetPracticesAsync_BuildsUrlWithFiltersAndParses()
    {
        string? url = null;
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json(
                    """{"isSuccess":true,"data":{"items":[{"id":"33333333-3333-3333-3333-333333333333","kind":"Up","groupId":"44444444-4444-4444-4444-444444444444","groupName":"ПО-262","teacherId":"55555555-5555-5555-5555-555555555555","teacherName":"Марченко И.А.","dateFrom":"2026-09-07T00:00:00","dateTo":"2026-09-11T00:00:00","organization":"ООО Ромашка"}],"totalCount":1,"page":1,"pageSize":100}}"""
                );
            })
        );

        var result = await client.GetPracticesAsync(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            groupId,
            teacherId,
            CancellationToken.None
        );

        result.Should().ContainSingle();
        result[0].Kind.Should().Be("Up");
        url.Should()
            .Be(
                $"/api/practices?from=2026-09-01&to=2026-09-30&page=1&pageSize=100&groupId={groupId}&teacherId={teacherId}"
            );
    }
}
