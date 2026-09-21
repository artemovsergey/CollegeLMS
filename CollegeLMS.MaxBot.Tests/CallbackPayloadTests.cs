using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class CallbackPayloadTests
{
    [Fact]
    public void Parse_DayPayload_SplitsParts()
    {
        var p = CallbackPayload.Parse("day:2026-09-08");

        p.Should().NotBeNull();
        p!.Action.Should().Be("day");
        p.Param1.Should().Be("2026-09-08");
        p.Param2.Should().BeNull();
    }

    [Fact]
    public void Parse_ThreePartPayload_SplitsAll()
    {
        var p = CallbackPayload.Parse("page:groups:2");

        p.Should().NotBeNull();
        p!.Action.Should().Be("page");
        p.Param1.Should().Be("groups");
        p.Param2.Should().Be("2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Blank_ReturnsNull(string? payload)
    {
        CallbackPayload.Parse(payload).Should().BeNull();
    }

    [Fact]
    public void FactoryMethods_ProduceExpectedPayload()
    {
        var d = new DateTime(2026, 9, 8);

        CallbackPayload.Day(d).Should().Be("day:2026-09-08");
        CallbackPayload.DayPrev(d).Should().Be("dayprev:2026-09-08");
        CallbackPayload.DayNext(d).Should().Be("daynext:2026-09-08");
        CallbackPayload.Week(d).Should().Be("week:2026-09-08");
        CallbackPayload.WeekPrev(d).Should().Be("weekprev:2026-09-08");
        CallbackPayload.WeekNext(d).Should().Be("weeknext:2026-09-08");
        CallbackPayload.Cal(d).Should().Be("cal:2026-09");
        CallbackPayload.CalPrev(d).Should().Be("calprev:2026-09");
        CallbackPayload.CalNext(d).Should().Be("calnext:2026-09");
        CallbackPayload.RetryDay(d).Should().Be("dayretry:2026-09-08");
        CallbackPayload.RetryWeek(d).Should().Be("weekretry:2026-09-08");
        CallbackPayload.RetryCal(d).Should().Be("calretry:2026-09");
    }

    [Fact]
    public void RetryPayloads_ParseBackWithSameContext()
    {
        var p = CallbackPayload.Parse(CallbackPayload.RetryDay(new DateTime(2026, 9, 8)));

        p!.Action.Should().Be("dayretry");
        CallbackPayload.TryParseDate(p.Param1).Should().Be(new DateTime(2026, 9, 8));

        var w = CallbackPayload.Parse(CallbackPayload.RetryWeek(new DateTime(2026, 9, 7)));

        w!.Action.Should().Be("weekretry");
        CallbackPayload.TryParseDate(w.Param1).Should().Be(new DateTime(2026, 9, 7));

        var c = CallbackPayload.Parse(CallbackPayload.RetryCal(new DateTime(2026, 9, 1)));

        c!.Action.Should().Be("calretry");
        CallbackPayload.TryParseMonth(c.Param1).Should().Be(new DateTime(2026, 9, 1));
    }

    [Fact]
    public void TryParseDate_ValidAndInvalid()
    {
        CallbackPayload.TryParseDate("2026-09-08").Should().Be(new DateTime(2026, 9, 8));
        CallbackPayload.TryParseDate("nope").Should().BeNull();
        CallbackPayload.TryParseDate(null).Should().BeNull();
    }

    [Fact]
    public void TryParseMonth_ValidAndInvalid()
    {
        CallbackPayload.TryParseMonth("2026-09").Should().Be(new DateTime(2026, 9, 1));
        CallbackPayload.TryParseMonth("2026").Should().BeNull();
    }
}
