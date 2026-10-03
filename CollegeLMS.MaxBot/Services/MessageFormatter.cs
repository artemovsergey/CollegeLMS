using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.Shared;

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

    /// <summary>«сам.р.» без «+» — пара только для информирования, в расписание не встаёт.</summary>
    private static bool IsInformationalNote(string? note) =>
        IsSelfStudyNote(note)
        && (
            note is null
            || !System.Text.RegularExpressions.Regex.IsMatch(
                note,
                @"сам[\s./\-]*р[\s.]*\+",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            )
        );

    /// <summary>Пара «откуда» из примечания: «вм.4 п.».</summary>
    private static int? MovePair(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            note,
            @"вм\.?\s*(\d{1,2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Пометка под изменением пары. Исходов всего два — добавлено и снято,
    /// поэтому замена и перенос подписываются как добавление, а подробности
    /// («вместо…», «с пары N») идут следом.
    /// </summary>
    private static void AppendChangeMarkers(System.Text.StringBuilder sb, ScheduleResponse entry)
    {
        foreach (var tag in entry.ChangeTags)
        {
            var isSelfStudy = IsSelfStudyNote(tag.Note);
            var isRemove = tag.ChangeType == "Remove";

            var marker =
                isSelfStudy && isRemove ? "🟣 сам.р. (самостоятельная работа)"
                : isRemove ? "🔴 снято"
                : "🟢 добавлено";

            var details = new List<string>();
            if (isSelfStudy && !isRemove)
                details.Add(IsInformationalNote(tag.Note) ? "только информация" : "сам.р.");
            if (!isRemove && tag.RemovedSubject is not null)
                details.Add($"вместо: {tag.RemovedSubject}");
            if (!isRemove && MovePair(tag.Note) is { } from)
                details.Add($"с пары {from}");

            var suffix = details.Count > 0 ? " — " + string.Join(", ", details) : string.Empty;
            sb.AppendLine($"    ⚠️ {marker}{suffix} (нед. {tag.Week})");
        }
    }

    public static string FormatChangeNotificationTitle(string changeType)
    {
        return changeType switch
        {
            "Remove" => "снята",
            "Add" or "Replace" or "Move" => "добавлена",
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
            badges.Add(IsInformationalNote(r.Note) ? "🟣 Сам.р. (только информация)" : "🟣 Сам.р.");
        if (DateForRevision(r) is { } date)
            badges.Add($"📅 {FormatDayMonthYear(date)}");
        badges.Add($"{r.DayOfWeek}, {r.Week}-я неделя");
        sb.AppendLine(string.Join(" · ", badges));

        var details = new List<string> { $"🏫 **{r.GroupName}**", $"🕐 {FormatPairLabel(r)}" };
        if (!string.IsNullOrWhiteSpace(r.Room))
            details.Add($"📍 ауд. {r.Room}");
        sb.AppendLine(string.Join(" · ", details));

        // Преподаватель идёт вместе с предметом — отдельной строкой не нужен.
        sb.AppendLine($"📖 {FormatSubjectLine(r)}");

        var teacher = r.TeacherName ?? r.RemovedTeacherName;
        if (string.IsNullOrWhiteSpace(teacher) && r.ChangeType != "Remove")
            sb.AppendLine("👤 Преподаватель не указан");

        var movedFrom = MovePair(r.Note) ?? r.RemovedNumberPair;
        if (
            r.ChangeType != "Remove"
            && movedFrom is { } from
            && from != r.NumberPair
            && !string.IsNullOrWhiteSpace(r.RemovedSubject)
            && IsSameLesson(r.RemovedSubject, r.Subject)
        )
            sb.AppendLine(
                $"🔀 Перенос: освобождается пара {from} ({LessonLine(r.RemovedSubject, r.RemovedTeacherName ?? r.TeacherName)})"
            );

        if (!string.IsNullOrWhiteSpace(r.Note))
            sb.AppendLine($"📝 Примечание: {r.Note}");

        return sb.ToString();
    }

    /// <summary>Одно и то же ли занятие (предмет без учёта регистра и пробелов).</summary>
    private static bool IsSameLesson(string? left, string? right)
    {
        static string Key(string? value) =>
            string.Join(" ", (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();

        return Key(left) == Key(right);
    }

    /// <summary>Пара: «пара N» или «пара X => Y», если занятие переехало.</summary>
    private static string FormatPairLabel(ScheduleRevision r)
    {
        var moved = MovePair(r.Note) ?? r.RemovedNumberPair;
        if (r.ChangeType != "Remove" && moved is { } from && from != r.NumberPair)
            return $"пара {from} {ReplaceArrow} {r.NumberPair}";

        return $"пара {r.NumberPair}";
    }

    /// <summary>
    /// Строка занятия: «Предмет Преподаватель», при замене и переносе —
    /// «старое =&gt; новое». Зачёркивания в MAX выглядят плохо, поэтому формат
    /// тот же, что в веб-приложении: стрелка без перечёркивания.
    /// </summary>
    private static string FormatSubjectLine(ScheduleRevision r)
    {
        if (r.ChangeType == "Remove")
        {
            var removed = string.IsNullOrWhiteSpace(r.RemovedSubject)
                ? r.Subject
                : r.RemovedSubject;
            return $"**{LessonLine(removed, r.TeacherName ?? r.RemovedTeacherName)}**";
        }

        if (r.ChangeType != "Remove" && !string.IsNullOrWhiteSpace(r.RemovedSubject))
        {
            var from = LessonLine(r.RemovedSubject, r.RemovedTeacherName ?? r.TeacherName);
            var to = LessonLine(r.Subject, r.TeacherName);

            // При переносе снимается то же самое занятие: стрелка вывела бы
            // «Математика => Математика», поэтому занятие печатаем один раз, а
            // освобождаемую пару — отдельной строкой.
            if (IsSameLesson(r.RemovedSubject, r.Subject))
                return $"**{to}**";

            return $"{from} {ReplaceArrow} **{to}**";
        }

        return $"**{LessonLine(r.Subject, r.TeacherName)}**";
    }

    /// <summary>
    /// Знак изменения. Раньше константа была объявлена здесь, а на вебе для
    /// переноса пары стоял другой знак — теперь оба берут общий словарь.
    /// </summary>
    private const string ReplaceArrow = ChangeVocabulary.ChangeArrow;

    /// <summary>«Предмет Преподаватель» одной строкой — как в веб-приложении.</summary>
    private static string LessonLine(string? subject, string? teacher)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(subject))
            parts.Add(subject.Trim());
        if (!string.IsNullOrWhiteSpace(teacher))
            parts.Add(teacher.Trim());
        return parts.Count == 0 ? "—" : string.Join(" ", parts);
    }

    private static string FormatChangeCardLabel(string changeType) =>
        ChangeVocabulary.Label(changeType);

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
