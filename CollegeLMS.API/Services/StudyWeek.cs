namespace CollegeLMS.API.Services;

public static class StudyWeek
{
    /// <summary>Начало семестра. Совпадает с MaxBot StudyWeek.SemesterStart и фронтендом.</summary>
    public static DateTime SemesterStart { get; } = new(2026, 9, 1);

    public static DateTime MondayOf(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        var offset = day == 0 ? 6 : day - 1;
        return date.Date.AddDays(-offset);
    }

    public static int WeekOf(DateTime date)
    {
        var diffWeeks = (int)((MondayOf(date) - MondayOf(SemesterStart)).TotalDays / 7);
        return Math.Max(1, diffWeeks + 1);
    }

    public static int ForDate(DateTime date) => WeekOf(date);

    public static bool IsInSemester(DateTime date)
    {
        if (date.Date < SemesterStart.Date)
            return false;

        return WeekOf(date) <= TotalWeeks;
    }

    /// <summary>Количество учебных недель в семестре.</summary>
    public static int TotalWeeks { get; } = 17;
}
