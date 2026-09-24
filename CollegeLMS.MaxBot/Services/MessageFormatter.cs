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
        var kind = PracticeKindLabel(practice);
        var line = $"{kind}: {practice.GroupName} · {PracticeTeachers(practice)}";
        line += $" (с {practice.DateFrom:dd.MM.yyyy} по {practice.DateTo:dd.MM.yyyy})";
        return line;
    }

    /// <summary>Уведомление о начале практики (день начала периода).</summary>
    public static string FormatPracticeStarted(PracticeDto practice) =>
        FormatPracticeEvent(practice, "🎓", "Началась практика");

    /// <summary>Уведомление об окончании практики (день окончания периода).</summary>
    public static string FormatPracticeFinished(PracticeDto practice) =>
        FormatPracticeEvent(practice, "🏁", "Закончилась практика");

    private static string FormatPracticeEvent(PracticeDto practice, string icon, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{icon} *{title} {PracticeDisplayName(practice)}*");
        sb.AppendLine(
            $"{PracticeKindLabel(practice)}: {practice.GroupName} · {PracticeTeachers(practice)} (с {practice.DateFrom:dd.MM.yyyy} по {practice.DateTo:dd.MM.yyyy})"
        );
        return sb.ToString().TrimEnd();
    }

    /// <summary>Метка вида практики: УП (учебная) или ПП (производственная).</summary>
    private static string PracticeKindLabel(PracticeDto practice) =>
        practice.Kind.Equals("Pp", StringComparison.OrdinalIgnoreCase) ? "ПП" : "УП";

    /// <summary>Название практики; при отсутствии — метка вида (legacy-контракт).</summary>
    private static string PracticeDisplayName(PracticeDto practice) =>
        string.IsNullOrWhiteSpace(practice.Name)
            ? PracticeKindLabel(practice)
            : practice.Name.Trim();

    /// <summary>ФИО преподавателей практики через запятую (новый и legacy-контракты).</summary>
    private static string PracticeTeachers(PracticeDto practice)
    {
        var names = practice.TeacherNameList();
        return names.Count > 0 ? string.Join(", ", names) : "—";
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
        !string.IsNullOrWhiteSpace(note)
        && System.Text.RegularExpressions.Regex.IsMatch(
            note,
            @"сам[\s./\-]*р",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

    /// <summary>Заголовок по типу изменения: добавлена / снята / замена.</summary>
    private static void AppendChangeMarkers(System.Text.StringBuilder sb, ScheduleResponse entry)
    {
        foreach (var tag in entry.ChangeTags)
        {
            var isSelfStudy = IsSelfStudyNote(tag.Note);

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
    /// Сводное уведомление об изменениях: карточки, как в веб-приложении.
    /// Одно сообщение на подписчика, без ссылок.
    /// </summary>
    public static string FormatCorrectionDigest(List<ScheduleRevision> revisions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔔 **Изменения в расписании**");

        foreach (var r in revisions)
        {
            sb.AppendLine();
            sb.AppendLine(FormatRevisionCard(r));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Карточка одной позиции изменения — как карточка в веб-приложении.</summary>
    private static string FormatRevisionCard(ScheduleRevision r)
    {
        var sb = new System.Text.StringBuilder();

        var badges = new List<string> { $"**{FormatChangeCardLabel(r.ChangeType)}**" };
        if (IsSelfStudyNote(r.Note))
            badges.Add("🟣 Сам.р.");
        if (DateForRevision(r) is { } date)
            badges.Add($"📅 {FormatDayMonthYear(date)}");
        badges.Add($"{r.DayOfWeek}, {r.Week}-я неделя");
        sb.AppendLine(string.Join(" · ", badges));

        var details = new List<string> { $"🏫 **{r.GroupName}**", $"🕐 {FormatPairLabel(r)}" };
        if (!string.IsNullOrWhiteSpace(r.Room))
            details.Add($"📍 ауд. {r.Room}");
        sb.AppendLine(string.Join(" · ", details));

        sb.AppendLine($"📖 {FormatSubjectLine(r)}");

        var teacher = r.TeacherName ?? r.RemovedTeacherName;
        sb.AppendLine(
            $"👤 {(string.IsNullOrWhiteSpace(teacher) ? "Преподаватель не указан" : teacher)}"
        );

        if (!string.IsNullOrWhiteSpace(r.Note))
            sb.AppendLine($"📝 Примечание: {r.Note}");

        return sb.ToString();
    }

    /// <summary>Пара: «пара N» или «пара X → Y» при переносе/замене.</summary>
    private static string FormatPairLabel(ScheduleRevision r)
    {
        var isMoveOrReplace = r.ChangeType is "Replace" or "Move";
        if (isMoveOrReplace && r.RemovedNumberPair is { } from && from != r.NumberPair)
            return $"пара {from} → {r.NumberPair}";

        return $"пара {r.NumberPair}";
    }

    /// <summary>Строка предмета: снятие, замена/перенос со зачёркиванием или новый предмет.</summary>
    private static string FormatSubjectLine(ScheduleRevision r)
    {
        var removed = r.RemovedSubject;
        if (r.ChangeType == "Remove")
            return $"**{(string.IsNullOrWhiteSpace(removed) ? r.Subject : removed)}**";

        if (r.ChangeType is "Replace" or "Move" && !string.IsNullOrWhiteSpace(removed))
            return $"~~{removed}~~ → **{r.Subject}**";

        return $"**{r.Subject}**";
    }

    private static string FormatChangeCardLabel(string changeType)
    {
        return changeType switch
        {
            "Add" => "Добавлено",
            "Remove" => "Снято",
            "Replace" => "Замена",
            "Move" => "Перенос",
            _ => "Изменено",
        };
    }

    private static readonly string[] MonthNames =
    [
        "",
        "января",
        "февраля",
        "марта",
        "апреля",
        "мая",
        "июня",
        "июля",
        "августа",
        "сентября",
        "октября",
        "ноября",
        "декабря",
    ];

    private static string FormatDayMonthYear(DateTime date) =>
        $"{date.Day} {MonthNames[date.Month]} {date.Year}";

    /// <summary>Дата занятия по номеру недели и дню недели (индекс 1..7, Пн=1).</summary>
    public static DateTime? DateForRevision(ScheduleRevision revision)
    {
        var dayIndex = revision.DayOfWeek is null ? 0 : ParseDayOfWeek(revision.DayOfWeek);
        if (dayIndex == 0)
            dayIndex = 7;

        var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        return monday.AddDays((revision.Week - 1) * 7 + (dayIndex - 1));
    }
}
