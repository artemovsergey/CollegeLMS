using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class StudyWeekTests
{
    [Fact]
    public void Now_UsesProvidedTimeZoneAsDate()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        var now = StudyWeek.Now(tz);

        now.TimeOfDay.Should().Be(TimeSpan.Zero);
        now.Should().BeAfter(DateTime.UtcNow.Date.AddDays(-2));
        now.Should().BeBefore(DateTime.UtcNow.Date.AddDays(2));
    }

    [Fact]
    public void ForDate_Boundary_MondayOfNextWeek()
    {
        // Семестр начинается 2026-09-01 (вторник), неделя 1 = 31.08–06.09.
        StudyWeek.ForDate(new DateTime(2026, 8, 31)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 1)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 6)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 7)).Should().Be(2);
        StudyWeek.ForDate(new DateTime(2026, 9, 13)).Should().Be(2);
        StudyWeek.ForDate(new DateTime(2026, 9, 14)).Should().Be(3);
    }
}
