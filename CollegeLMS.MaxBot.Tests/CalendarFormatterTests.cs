using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class CalendarFormatterTests
{
    [Fact]
    public void MonthTitle_FormatsRussianMonth()
    {
        CalendarFormatter.MonthTitle(new DateTime(2026, 9, 1)).Should().Be("Сентябрь 2026");
        CalendarFormatter.MonthTitle(new DateTime(2027, 1, 1)).Should().Be("Январь 2027");
    }

    [Fact]
    public void BuildGrid_Sep2026_FirstButtonIsFirstOfMonth()
    {
        // Сентябрь 2026: 1-е — вторник. Первая строка начинается со вторника.
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 9, 1));

        var all = buttons.SelectMany(x => x).ToList();
        all.Should().HaveCount(30);
        all[0].Payload.Should().Be("day:2026-09-01");
        all[0].Text.Should().Be("1");
    }

    [Fact]
    public void BuildGrid_DaysGroupedByWeek()
    {
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 9, 1));

        buttons.Should().NotBeEmpty();
        foreach (var row in buttons)
            row.Should().NotBeEmpty();
        // ceil((offset + 30) / 7) для сентября 2026, offset=1 → 5 строк
        buttons.Should().HaveCount(5);
    }

    [Fact]
    public void BuildGrid_DecemberHas31Days()
    {
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 12, 1));
        buttons.SelectMany(x => x).Should().HaveCount(31);
    }

    [Fact]
    public void Bounds_ControlArrows()
    {
        var semesterStart = new DateTime(2026, 9, 1);
        var maxMonth = new DateTime(2026, 12, 1);

        CalendarFormatter.CanGoPrev(semesterStart).Should().BeFalse();
        CalendarFormatter.CanGoPrev(new DateTime(2026, 10, 1)).Should().BeTrue();
        CalendarFormatter.CanGoNext(new DateTime(2026, 12, 1), maxMonth).Should().BeFalse();
        CalendarFormatter.CanGoNext(new DateTime(2026, 11, 1), maxMonth).Should().BeTrue();
    }
}
