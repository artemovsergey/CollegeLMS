using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MessageFormatterTests
{
    private static List<ScheduleResponse> Entries() =>
        [
            new ScheduleResponse
            {
                DayOfWeek = 1,
                NumberPair = 1,
                Subject = "Математика",
                Room = "405",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                TeacherName = "Иванов И.И.",
                LessonType = "Lecture",
            },
        ];

    [Fact]
    public void FormatDaySchedule_IncludesDateInHeader()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Entries(),
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
        text.Should().Contain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_ShowGroup_IncludesGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_WithoutShowGroup_OmitsGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И."
        );

        text.Should().NotContain("ИС-21");
    }

    [Fact]
    public void FormatWeekSchedule_ShowGroup_IncludesGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatWeekSchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_Empty_ShowsHoliday()
    {
        var text = MessageFormatter.FormatDaySchedule([], new DateTime(2026, 9, 7), "Группа 101");

        text.Should().Contain("Расписания нет — выходной!");
    }

    [Fact]
    public void FormatWeekSchedule_HeaderHasDateRange()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(Entries(), weekStart, "Группа 101");

        text.Should().Contain("07.09–13.09");
        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
    }

    [Fact]
    public void FormatWeekSchedule_SundayEntries_HeaderUsesIndex7()
    {
        var weekStart = new DateTime(2026, 9, 7);
        var entries = new List<ScheduleResponse> { Entries()[0] with { DayOfWeek = 0 } };

        var text = MessageFormatter.FormatWeekSchedule(entries, weekStart, "Группа 101");

        text.Should().Contain("Воскресенье, 13.09");
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    public void DayIndex_MapsApiDayToLabelIndex(int apiDay, int expected)
    {
        MessageFormatter.DayIndex(apiDay).Should().Be(expected);
    }

    [Fact]
    public void DateForWeekDay_MapsApiDayToDate()
    {
        var weekStart = new DateTime(2026, 9, 7);

        MessageFormatter.DateForWeekDay(weekStart, 1).Should().Be(new DateTime(2026, 9, 7));
        MessageFormatter.DateForWeekDay(weekStart, 0).Should().Be(new DateTime(2026, 9, 13));
    }
}
