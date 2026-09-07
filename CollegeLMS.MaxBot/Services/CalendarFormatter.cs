using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

public static class CalendarFormatter
{
    private static readonly string[] MonthNames =
    [
        "Январь",
        "Февраль",
        "Март",
        "Апрель",
        "Май",
        "Июнь",
        "Июль",
        "Август",
        "Сентябрь",
        "Октябрь",
        "Ноябрь",
        "Декабрь",
    ];

    public static string MonthTitle(DateTime month) =>
        $"{MonthNames[month.Month - 1]} {month.Year}";

    public static List<List<MaxButton>> BuildGrid(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        var offset = ((int)start.DayOfWeek + 6) % 7; // Пн = 0, Вс = 6

        var grid = new List<List<MaxButton>>();
        var row = new List<MaxButton>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            if ((offset + day - 1) % 7 == 0 && row.Count > 0)
            {
                grid.Add(row);
                row = new List<MaxButton>();
            }

            var date = start.AddDays(day - 1);
            row.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = day.ToString(),
                    Payload = CallbackPayload.Day(date),
                }
            );
        }

        if (row.Count > 0)
            grid.Add(row);

        return grid;
    }

    public static bool CanGoPrev(DateTime month)
    {
        var thisFirst = new DateTime(month.Year, month.Month, 1);
        var firstSemester = new DateTime(
            StudyWeek.SemesterStart.Year,
            StudyWeek.SemesterStart.Month,
            1
        );
        return thisFirst > firstSemester;
    }

    public static bool CanGoNext(DateTime month, DateTime maxMonth)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var maxFirst = new DateTime(maxMonth.Year, maxMonth.Month, 1);
        return first < maxFirst;
    }
}