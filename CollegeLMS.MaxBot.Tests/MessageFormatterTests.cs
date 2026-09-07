using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MessageFormatterTests
{
    [Theory]
    [InlineData("пн", 1)]
    [InlineData("ПОНЕДЕЛЬНИК", 1)]
    [InlineData("вт", 2)]
    [InlineData("ср", 3)]
    [InlineData("чт", 4)]
    [InlineData("пт", 5)]
    [InlineData("сб", 6)]
    [InlineData("вс", 0)]
    [InlineData("неизвестно", 0)]
    public void ParseDayOfWeekMapsToApiConvention(string input, int expected)
    {
        MessageFormatter.ParseDayOfWeek(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 1)]
    [InlineData(DayOfWeek.Tuesday, 2)]
    [InlineData(DayOfWeek.Wednesday, 3)]
    [InlineData(DayOfWeek.Thursday, 4)]
    [InlineData(DayOfWeek.Friday, 5)]
    [InlineData(DayOfWeek.Saturday, 6)]
    [InlineData(DayOfWeek.Sunday, 0)]
    public void ToApiDayMatchesCSharpDayOfWeek(DayOfWeek day, int expected)
    {
        MessageFormatter.ToApiDay(day).Should().Be(expected);
    }
}
