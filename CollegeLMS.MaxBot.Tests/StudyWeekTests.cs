using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class StudyWeekTests
{
    [Fact]
    public void MondayOfReturnsPreviousMonday()
    {
        StudyWeek.MondayOf(new DateTime(2026, 9, 1)).Should().Be(new DateTime(2026, 8, 31));
        StudyWeek.MondayOf(new DateTime(2026, 9, 6)).Should().Be(new DateTime(2026, 8, 31));
        StudyWeek.MondayOf(new DateTime(2026, 9, 7)).Should().Be(new DateTime(2026, 9, 7));
        StudyWeek.MondayOf(new DateTime(2026, 9, 13)).Should().Be(new DateTime(2026, 9, 7));
    }

    [Fact]
    public void FirstWeekStartsAtSemesterStart()
    {
        StudyWeek.ForDate(new DateTime(2026, 8, 31)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 1)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 6)).Should().Be(1);
    }

    [Fact]
    public void SecondWeekStartsNextMonday()
    {
        StudyWeek.ForDate(new DateTime(2026, 9, 7)).Should().Be(2);
        StudyWeek.ForDate(new DateTime(2026, 9, 13)).Should().Be(2);
    }

    [Fact]
    public void WeekNeverLessThanOne()
    {
        StudyWeek.ForDate(new DateTime(2026, 8, 1)).Should().Be(1);
    }
}
