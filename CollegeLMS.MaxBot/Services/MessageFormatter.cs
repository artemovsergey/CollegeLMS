using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;

namespace CollegeLMS.MaxBot.Services;

public static class MessageFormatter
{
    private static readonly string[] DayNames =
    [
        "",
        "Понедельник",
        "Вторник",
        "Среда",
        "Четверг",
        "Пятница",
        "Суббота",
        "Воскресенье",
    ];

    private static readonly string[] DayAbbr = ["", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];

    private static readonly string[] ShortDayNames = ["", "пн", "вт", "ср", "чт", "пт", "сб", "вс"];

    public static string FormatDaySchedule(
        List<ScheduleResponse> entries,
        int dayOfWeek,
        string entityName
    )
    {
        if (entries.Count == 0)
            return $"📋 *{DayNames[dayOfWeek]}*\n\nРасписания нет — выходной!";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 *{DayNames[dayOfWeek]}* — {entityName}");
        sb.AppendLine();

        foreach (var e in entries.OrderBy(x => x.NumberPair))
        {
            var type = e.LessonType switch
            {
                "Lecture" => "📖",
                "Practice" => "✏️",
                "Lab" => "🔬",
                "Exam" => "📝",
                _ => "📚",
            };

            sb.AppendLine($"*{e.NumberPair}.* {type} {e.Subject}");
            sb.AppendLine($"    🕐 {e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}  📍 {e.Room}");

            if (e.TeacherName is not null)
                sb.AppendLine($"    👨‍🏫 {e.TeacherName}");

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatWeekSchedule(List<ScheduleResponse> entries, string entityName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 *Расписание на неделю* — {entityName}");
        sb.AppendLine();

        var grouped = entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key);

        foreach (var group in grouped)
        {
            sb.AppendLine($"*{DayNames[group.Key]}*");
            foreach (var e in group.OrderBy(x => x.NumberPair))
            {
                var type = e.LessonType switch
                {
                    "Lecture" => "📖",
                    "Practice" => "✏️",
                    "Lab" => "🔬",
                    "Exam" => "📝",
                    _ => "📚",
                };
                sb.AppendLine(
                    $"*{e.NumberPair}.* {type} {e.Subject} ({e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}, {e.Room})"
                );
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static int DayIndex(int apiDay) => apiDay == 0 ? 7 : apiDay;

    public static int DayOffset(int apiDay) => apiDay == 0 ? 6 : apiDay - 1;

    public static DateTime DateForWeekDay(DateTime weekStart, int apiDay) =>
        weekStart.AddDays(DayOffset(apiDay));

    public static string FormatShortDate(DateTime date) => date.ToString("dd.MM");

    public static string FormatLongDate(DateTime date) =>
        $"{DayNames[DayIndex((int)date.DayOfWeek)]}, {FormatShortDate(date)}";

    public static string DayLabelForDate(DateTime date) => DayNames[DayIndex((int)date.DayOfWeek)];

    public static string DayAbbrForDate(DateTime date) => DayAbbr[DayIndex((int)date.DayOfWeek)];

    public static string FormatDaySchedule(
        List<ScheduleResponse> entries,
        DateTime date,
        string entityName,
        bool showGroup = false
    )
    {
        var header = $"📋 *{FormatLongDate(date)}* — {entityName}";
        if (entries.Count == 0)
            return $"{header}\n\nРасписания нет — выходной!";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine();

        foreach (var e in entries.OrderBy(x => x.NumberPair))
        {
            var type = e.LessonType switch
            {
                "Lecture" => "📖",
                "Practice" => "✏️",
                "Lab" => "🔬",
                "Exam" => "📝",
                _ => "📚",
            };

            sb.AppendLine($"*{e.NumberPair}.* {type} {e.Subject}");
            sb.AppendLine($"    🕐 {e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}  📍 {e.Room}");

            if (showGroup && e.GroupName.Length > 0)
                sb.AppendLine($"    🏫 {e.GroupName}");

            if (e.TeacherName is not null)
                sb.AppendLine($"    👨‍🏫 {e.TeacherName}");

            sb.AppendLine("────────");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatWeekSchedule(
        List<ScheduleResponse> entries,
        DateTime weekStart,
        string entityName,
        bool showGroup = false
    )
    {
        var weekEnd = weekStart.AddDays(6);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(
            $"📅 *Неделя {FormatShortDate(weekStart)}–{FormatShortDate(weekEnd)}* — {entityName}"
        );
        sb.AppendLine();

        var grouped = entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key);

        foreach (var group in grouped)
        {
            var date = DateForWeekDay(weekStart, group.Key);
            sb.AppendLine($"*{DayNames[DayIndex(group.Key)]}, {FormatShortDate(date)}*");
            foreach (var e in group.OrderBy(x => x.NumberPair))
            {
                var type = e.LessonType switch
                {
                    "Lecture" => "📖",
                    "Practice" => "✏️",
                    "Lab" => "🔬",
                    "Exam" => "📝",
                    _ => "📚",
                };
                var pairLine =
                    $"*{e.NumberPair}.* {type} {e.Subject} ({e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}, {e.Room})";
                if (showGroup && e.GroupName.Length > 0)
                    pairLine += $" — {e.GroupName}";
                sb.AppendLine(pairLine);
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string GetDayLabel(int dayOfWeek) => DayNames[dayOfWeek];

    public static string GetShortDayLabel(int dayOfWeek) => DayAbbr[dayOfWeek];

    public static string GetMinDayLabel(int dayOfWeek) => ShortDayNames[dayOfWeek];

    public const string SundayMessage = "🎉 Сегодня воскресенье — выходной!";

    /// <summary>День недели для API: значение C# DayOfWeek (Пн=1 … Сб=6, Вс=0).</summary>
    public static int ToApiDay(DayOfWeek day) => (int)day;

    public static int ParseDayOfWeek(string? text)
    {
        return text?.ToLower().Trim() switch
        {
            "пн" or "понедельник" or "monday" => 1,
            "вт" or "вторник" or "tuesday" => 2,
            "ср" or "среда" or "wednesday" => 3,
            "чт" or "четверг" or "thursday" => 4,
            "пт" or "пятница" or "friday" => 5,
            "сб" or "суббота" or "saturday" => 6,
            _ => 0,
        };
    }

    /// <summary>Заголовок по типу изменения: добавлена / снята / замена.</summary>
    public static string FormatChangeNotificationTitle(string changeType)
    {
        return changeType switch
        {
            "Add" => "добавлена",
            "Remove" => "снята",
            "Replace" => "замена",
            _ => "изменена",
        };
    }

    /// <summary>Формат уведомления об изменении расписания.</summary>
    public static string FormatChangeNotification(ScheduleRevision revision)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔔 *Изменение в расписании*");
        sb.AppendLine();
        sb.AppendLine(
            $"{revision.GroupName} · {revision.DayOfWeek} · Нед. {revision.Week} · Пара {revision.NumberPair}"
        );
        sb.AppendLine();
        sb.AppendLine($"📖 {revision.Subject} — {FormatChangeNotificationTitle(revision.ChangeType)}");

        if (revision.TeacherName is not null)
            sb.AppendLine($"👨‍🏫 Преподаватель: {revision.TeacherName}");

        if (revision.Note is not null)
            sb.AppendLine($"📝 Примечание: {revision.Note}");

        return sb.ToString().TrimEnd();
    }

    /// <summary>Нумерованный список изменений для подписчика (пагинация 20/стр.).</summary>
    public static string FormatMyChanges(
        List<ScheduleRevision> revisions,
        int page,
        int pageSize = 20
    )
    {
        if (revisions.Count == 0)
            return "📭 *Мои изменения*\n\nИзменений пока нет.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🔄 *Мои изменения* (стр. {page + 1})");
        sb.AppendLine();

        var index = page * pageSize + 1;
        foreach (var r in revisions)
        {
            sb.AppendLine(
                $"{index}. {r.GroupName} · {r.DayOfWeek} · Нед. {r.Week} · Пара {r.NumberPair}"
            );
            sb.AppendLine($"   📖 {r.Subject} ({FormatChangeNotificationTitle(r.ChangeType)})");
            if (r.TeacherName is not null)
                sb.AppendLine($"   👨‍🏫 {r.TeacherName}");
            sb.AppendLine($"   🕐 {r.CreatedAt:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
            index++;
        }

        return sb.ToString().TrimEnd();
    }
}
