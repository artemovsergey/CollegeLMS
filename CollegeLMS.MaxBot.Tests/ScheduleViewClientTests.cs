using System.Net;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Клиент CollegeLMS: серверные виды расписания (view=day / view=week) со слоями
/// практик, вставок и пар. Ошибка запроса отличается от валидного пустого дня.
/// </summary>
public class ScheduleViewClientTests
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
    public async Task GetDayViewAsync_Success_ParsesLayers()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        string? url = null;
        var body =
            """{"isSuccess":true,"data":{"date":"2026-09-15T00:00:00","week":5,"dayOfWeek":2,"isSunday":false,"isNonWorking":true,"nonWorkingTitle":"Праздник","practices":[{"id":"11111111-1111-1111-1111-111111111111","kind":"Up","groupId":"22222222-2222-2222-2222-222222222222","groupName":"ПО-262","teacherId":"33333333-3333-3333-3333-333333333333","teacherName":"Марченко И.А.","dateFrom":"2026-09-07T00:00:00","dateTo":"2026-09-11T00:00:00","organization":"ООО Ромашка","note":null}],"inserts":[{"id":"44444444-4444-4444-4444-444444444444","title":"Линейка","dayOfWeek":2,"startTime":"08:00:00","endTime":"08:30:00","course":2,"isActive":true}],"entries":[{"id":"55555555-5555-5555-5555-555555555555","groupId":"66666666-6666-6666-6666-666666666666","groupName":"ИС-21","teacherId":null,"teacherName":null,"subject":"Математика","room":"201","dayOfWeek":2,"numberPair":1,"startTime":"08:30:00","endTime":"10:00:00","weeks":[5],"lessonType":"Lecture","changeTags":[]}]}}""";
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json(body);
            })
        );

        var day = await client.GetDayViewAsync(
            new DateTime(2026, 9, 15),
            groupId,
            teacherId,
            CancellationToken.None
        );

        day.Should().NotBeNull();
        day!.Date.Should().Be(new DateTime(2026, 9, 15));
        day.Week.Should().Be(5);
        day.DayOfWeek.Should().Be(2);
        day.IsNonWorking.Should().BeTrue();
        day.NonWorkingTitle.Should().Be("Праздник");
        day.Practices.Should().ContainSingle();
        day.Practices[0].Kind.Should().Be("Up");
        day.Practices[0].GroupName.Should().Be("ПО-262");
        day.Inserts.Should().ContainSingle();
        day.Inserts[0].Title.Should().Be("Линейка");
        day.Entries.Should().ContainSingle();
        day.Entries[0].Subject.Should().Be("Математика");
        day.Entries[0].Room.Should().Be("201");
        url.Should()
            .Be($"/api/schedule?view=day&date=2026-09-15&groupId={groupId}&teacherId={teacherId}");
    }

    [Fact]
    public async Task GetDayViewAsync_BadRequest_ReturnsNull()
    {
        var client = BuildClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest))
        );

        var day = await client.GetDayViewAsync(
            new DateTime(2026, 9, 15),
            null,
            null,
            CancellationToken.None
        );

        day.Should().BeNull();
    }

    [Fact]
    public async Task GetDayViewAsync_SuccessFalse_ReturnsNull()
    {
        var client = BuildClient(
            new StubHandler(_ => Json("""{"isSuccess":false,"errorMessage":"Ошибка"}"""))
        );

        var day = await client.GetDayViewAsync(
            new DateTime(2026, 9, 15),
            null,
            null,
            CancellationToken.None
        );

        day.Should().BeNull();
    }

    [Fact]
    public async Task GetDayViewAsync_EmptyEntries_ReturnsDayWithEmptyEntries()
    {
        var body =
            """{"isSuccess":true,"data":{"date":"2026-09-15T00:00:00","week":5,"dayOfWeek":2,"isSunday":true,"isNonWorking":false,"nonWorkingTitle":null,"practices":[],"inserts":[],"entries":[]}}""";
        var client = BuildClient(new StubHandler(_ => Json(body)));

        var day = await client.GetDayViewAsync(
            new DateTime(2026, 9, 15),
            null,
            null,
            CancellationToken.None
        );

        day.Should().NotBeNull();
        day!.Entries.Should().BeEmpty();
        day.Practices.Should().BeEmpty();
        day.Inserts.Should().BeEmpty();
        day.IsSunday.Should().BeTrue();
    }

    [Fact]
    public async Task GetWeekViewAsync_Success_ParsesSixDays()
    {
        string? url = null;
        var days = string.Join(
            ",",
            Enumerable
                .Range(0, 6)
                .Select(i =>
                    $$$"""{"date":"2026-09-{{{14 + i}}}T00:00:00","week":5,"dayOfWeek":{{{i + 1}}},"isSunday":false,"isNonWorking":false,"nonWorkingTitle":null,"practices":[],"inserts":[],"entries":[]}"""
                )
        );
        var body =
            $$$"""{"isSuccess":true,"data":{"week":5,"weekStart":"2026-09-14T00:00:00","days":[{{{days}}}]}}""";
        var client = BuildClient(
            new StubHandler(request =>
            {
                url = request.RequestUri!.PathAndQuery;
                return Json(body);
            })
        );

        var week = await client.GetWeekViewAsync(5, null, null, CancellationToken.None);

        week.Should().NotBeNull();
        week!.Week.Should().Be(5);
        week.WeekStart.Should().Be(new DateTime(2026, 9, 14));
        week.Days.Should().HaveCount(6);
        week.Days[0].DayOfWeek.Should().Be(1);
        week.Days[5].DayOfWeek.Should().Be(6);
        url.Should().Be("/api/schedule?view=week&week=5");
    }
}
