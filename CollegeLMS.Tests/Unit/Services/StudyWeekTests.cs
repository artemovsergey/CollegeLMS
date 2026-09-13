using CollegeLMS.API.Services;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class StudyWeekTests
{
    [Theory]
    [InlineData("2026-09-01", "2026-08-31")] // Пн
    [InlineData("2026-09-06", "2026-08-31")] // Вс
    public void MondayOf_ReturnsMonday(string date, string expected)
    {
        var result = StudyWeek.MondayOf(DateTime.Parse(date));

        result.Date.Should().Be(DateTime.Parse(expected));
    }

    [Theory]
    [InlineData("2026-09-01", 1)]
    [InlineData("2026-09-07", 2)]
    [InlineData("2026-12-14", 16)]
    public void WeekOf_ReturnsExpected(string date, int expected)
    {
        StudyWeek.WeekOf(DateTime.Parse(date)).Should().Be(expected);
    }

    [Theory]
    [InlineData("2026-09-01", true)]
    [InlineData("2026-08-31", false)]
    [InlineData("2026-12-21", false)]
    public void IsInSemester_ReturnsExpected(string date, bool expected)
    {
        StudyWeek.IsInSemester(DateTime.Parse(date)).Should().Be(expected);
    }
}