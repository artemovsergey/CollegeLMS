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

            AppendChangeMarkers(sb, e);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Строка практики УП/ПП для расписания дня (заменяет пары).</summary>
    public static string FormatPracticeLine(PracticeDto practice)
    {
        var kind = practice.Kind.Equals("Pp", StringComparison.OrdinalIgnoreCase) ? "ПП" : "УП";
        var line = $"{kind}: {practice.GroupName} · {practice.TeacherName}";
        if (!string.IsNullOrWhiteSpace(practice.Organization))
            line += $" · {practice.Organization}";
        line += $" (с {practice.DateFrom:dd.MM.yyyy} по {practice.DateTo:dd.MM.yyyy})";
        return line;
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

    /// <summary>
    /// День расписания из серверного вида view=day: практика заменяет пары,
    /// вставки идут перед парами, нерабочий день и воскресенье — отдельными строками.
    /// </summary>
    public static string FormatDaySchedule(
        ScheduleDayViewDto day,
        string entityName,
        bool showGroup
    )
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 *{FormatLongDate(day.Date)}* — {entityName}");

        if (day.IsSunday)
        {
            sb.AppendLine();
            sb.AppendLine("Расписания нет — выходной!");
            return sb.ToString().TrimEnd();
        }

        if (day.IsNonWorking)
        {
            sb.AppendLine();
            sb.AppendLine(FormatNonWorkingLine(day.NonWorkingTitle));
            return sb.ToString().TrimEnd();
        }

        if (day.Practices.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🎓 *Практика*");
            sb.AppendLine(FormatPracticeLine(day.Practices[0]));
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();
        if (day.Inserts.Count > 0)
        {
            AppendInsertLines(sb, day.Inserts);
            sb.AppendLine();
        }

        if (day.Entries.Count == 0)
        {
            sb.AppendLine("Пар нет.");
            return sb.ToString().TrimEnd();
        }

        foreach (var e in day.Entries.OrderBy(x => x.NumberPair))
        {
            sb.AppendLine($"*{e.NumberPair}.* {LessonTypeIcon(e.LessonType)} {e.Subject}");
            sb.AppendLine($"    🕐 {e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}  📍 {e.Room}");

            if (showGroup && e.GroupName.Length > 0)
                sb.AppendLine($"    🏫 {e.GroupName}");

            if (e.TeacherName is not null)
                sb.AppendLine($"    👨‍🏫 {e.TeacherName}");

            AppendChangeMarkers(sb, e);
            sb.AppendLine("────────");
        }

        if (day.BigBreak is { } bigBreak)
            sb.AppendLine(BigBreakLine(bigBreak));

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Неделя расписания из серверного вида view=week: заголовок с номером недели
    /// и диапазоном дат, блоки дней Пн–Сб со слоями.
    /// </summary>
    public static string FormatWeekSchedule(
        ScheduleWeekViewDto week,
        string entityName,
        bool showGroup
    )
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(
            $"📅 *Неделя {week.Week} · {FormatShortDate(week.WeekStart)}–{FormatShortDate(week.WeekStart.AddDays(6))}* — {entityName}"
        );
        sb.AppendLine();

        foreach (var day in week.Days.OrderBy(d => DayIndex(d.DayOfWeek)))
        {
            var date =
                day.Date != default ? day.Date : DateForWeekDay(week.WeekStart, day.DayOfWeek);
            sb.AppendLine($"*{DayNames[DayIndex(day.DayOfWeek)]}, {FormatShortDate(date)}*");

            if (day.IsNonWorking)
            {
                sb.AppendLine(FormatNonWorkingLine(day.NonWorkingTitle));
            }
            else if (day.IsSunday)
            {
                sb.AppendLine("Расписания нет — выходной!");
            }
            else if (day.Practices.Count > 0)
            {
                sb.AppendLine("🎓 *Практика*");
                sb.AppendLine(FormatPracticeLine(day.Practices[0]));
            }
            else
            {
                if (day.Inserts.Count > 0)
                    AppendInsertLines(sb, day.Inserts);

                if (day.Entries.Count == 0)
                {
                    sb.AppendLine("Пар нет.");
                }
                else
                {
                    foreach (var e in day.Entries.OrderBy(x => x.NumberPair))
                    {
                        var pairLine =
                            $"*{e.NumberPair}.* {LessonTypeIcon(e.LessonType)} {e.Subject} ({e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}, {e.Room})";
                        if (showGroup && e.GroupName.Length > 0)
                            pairLine += $" — {e.GroupName}";
                        sb.AppendLine(pairLine);
                        AppendChangeMarkers(sb, e);
                    }

                    if (day.BigBreak is { } bigBreak)
                        sb.AppendLine(BigBreakLine(bigBreak));
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string LessonTypeIcon(string lessonType) =>
        lessonType switch
        {
            "Lecture" => "📖",
            "Practice" => "✏️",
            "Lab" => "🔬",
            "Exam" => "📝",
            _ => "📚",
        };

    private static string FormatNonWorkingLine(string? title) =>
        string.IsNullOrWhiteSpace(title) ? "🎉 Нерабочий день" : $"🎉 Нерабочий день: {title}";

    private static string BigBreakLine(BigBreakDto bigBreak) =>
        $"☕ Большая перемена {bigBreak.StartTime:hh\\:mm}–{bigBreak.EndTime:hh\\:mm} (после {bigBreak.AfterPair} пары)";

    private static void AppendInsertLines(
        System.Text.StringBuilder sb,
        List<ScheduleInsertDto> inserts
    )
    {
        foreach (var ins in inserts.OrderBy(x => x.StartTime))
        {
            sb.AppendLine($"{ins.StartTime:hh\\:mm}–{ins.EndTime:hh\\:mm} {ins.Title}");
        }
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

    /// <summary>Примечание «сам.р.» — признак самостоятельной работы.</summary>
    private static bool IsSelfStudyNote(string? note) =>
        string.Equals(note?.Trim(), "сам.р.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Заголовок по типу изменения: добавлена / снята / замена.</summary>
    private static void AppendChangeMarkers(System.Text.StringBuilder sb, ScheduleResponse entry)
    {
        foreach (var tag in entry.ChangeTags)
        {
            var isSelfStudy = tag.ChangeType == "Remove" && IsSelfStudyNote(tag.Note);

            var marker = isSelfStudy
                ? "🟣 сам.р. (самостоятельная работа)"
                : tag.ChangeType switch
                {
                    "Add" => "🟢 добавлено",
                    "Remove" => "🔴 снято",
                    "Move" => tag.RemovedNumberPair.HasValue
                        ? $"🔄 перенос с пары {tag.RemovedNumberPair}"
                        : "🔄 перенос",
                    _ => "🔵 замена",
                };
            sb.AppendLine($"    ⚠️ {marker} (нед. {tag.Week})");
        }
    }

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

    /// <summary>Формат уведомления об изменении расписания с deep link на дату.</summary>
    public static string FormatChangeNotification(ScheduleRevision revision, string miniAppUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔔 *Изменение в расписании*");
        sb.AppendLine();
        sb.AppendLine(
            $"{revision.GroupName} · {revision.DayOfWeek} · Нед. {revision.Week} · Пара {revision.NumberPair}"
        );
        sb.AppendLine();
        sb.AppendLine(
            $"📖 {revision.Subject} — {FormatChangeNotificationTitle(revision.ChangeType)}"
        );

        if (revision.TeacherName is not null)
            sb.AppendLine($"👨‍🏫 Преподаватель: {revision.TeacherName}");

        if (revision.Note is not null)
            sb.AppendLine($"📝 Примечание: {revision.Note}");

        if (DateForRevision(revision) is { } date)
            sb.AppendLine(
                $"📅 Открыть на дату: {MiniAppUrlBuilder.Build(miniAppUrl, "day", date)}"
            );

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Сводное уведомление об изменениях: дата корректировки и все позиции,
    /// затрагивающие выбор подписчика. Одно сообщение на подписчика.
    /// </summary>
    public static string FormatCorrectionDigest(
        DateTime? correctionDate,
        List<ScheduleRevision> revisions,
        string? miniAppUrl
    )
    {
        var date = correctionDate ?? (revisions.Count > 0 ? DateForRevision(revisions[0]) : null);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔔 *Изменения в расписании*");
        sb.AppendLine();
        if (date is { } d)
            sb.AppendLine($"📅 Дата: {d:dd.MM.yyyy}");
        sb.AppendLine();

        foreach (var r in revisions)
        {
            sb.AppendLine($"• {r.GroupName} · {r.DayOfWeek} · Нед. {r.Week} · Пара {r.NumberPair}");
            sb.AppendLine($"  📖 {r.Subject} — {FormatChangeNotificationTitle(r.ChangeType)}");
            if (r.TeacherName is not null)
                sb.AppendLine($"  👨‍🏫 {r.TeacherName}");
            if (r.Note is not null)
                sb.AppendLine($"  📝 {r.Note}");
            if (IsSelfStudyNote(r.Note))
                sb.AppendLine("  🟣 сам.р. (самостоятельная работа)");
            sb.AppendLine();
        }

        if (miniAppUrl is not null && date is { } linkDate)
            sb.AppendLine($"📅 Открыть: {MiniAppUrlBuilder.Build(miniAppUrl, "day", linkDate)}");

        return sb.ToString().TrimEnd();
    }

    /// <summary>Дата занятия по номеру недели и дню недели (индекс 1..7, Пн=1).</summary>
    private static DateTime? DateForRevision(ScheduleRevision revision)
    {
        var dayIndex = revision.DayOfWeek is null ? 0 : ParseDayOfWeek(revision.DayOfWeek);
        if (dayIndex == 0)
            dayIndex = 7;

        var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        return monday.AddDays((revision.Week - 1) * 7 + (dayIndex - 1));
    }

    /// <summary>
    /// Нумерованный список изменений для подписчика (пагинация 20/стр.).
    /// Показывает дату занятия и deep link на дату последнего изменения.
    /// </summary>
    public static string FormatMyChanges(
        List<ScheduleRevision> revisions,
        int page,
        int totalPages,
        int pageSize = 20,
        string? miniAppUrl = null
    )
    {
        if (revisions.Count == 0)
            return "📭 *Мои изменения*\n\nИзменений пока нет.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🔄 *Мои изменения* (стр. {page + 1} из {Math.Max(1, totalPages)})");
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
            if (DateForRevision(r) is { } lessonDate)
                sb.AppendLine($"   🗓 {FormatShortDate(lessonDate)}");
            sb.AppendLine($"   🕐 {r.CreatedAt:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
            index++;
        }

        if (
            miniAppUrl is not null
            && revisions.Count > 0
            && DateForRevision(revisions[0]) is { } lastDate
        )
            sb.AppendLine(
                $"📅 Открыть дату последнего изменения: {MiniAppUrlBuilder.Build(miniAppUrl, "day", lastDate)}"
            );

        return sb.ToString().TrimEnd();
    }
}
