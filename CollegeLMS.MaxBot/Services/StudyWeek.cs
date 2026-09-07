namespace CollegeLMS.MaxBot.Services;

public static class StudyWeek
{
    /// <summary>Начало семестра (МСК). Совпадает с SEMESTER_START во фронтенде.</summary>
    public static DateTime SemesterStart { get; } = new(2026, 9, 1);

    public static DateTime MondayOf(DateTime date)
    {
        var day = (int)date.DayOfWeek; // 0 = Воскресенье
        var offset = day == 0 ? 6 : day - 1;
        return date.Date.AddDays(-offset);
    }

    public static int ForDate(DateTime date)
    {
        var diffWeeks = (int)((MondayOf(date) - MondayOf(SemesterStart)).TotalDays / 7);
        return Math.Max(1, diffWeeks + 1);
    }

    public static int Current(TimeZoneInfo tz)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return ForDate(now);
    }
}
