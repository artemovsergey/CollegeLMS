using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleImportService(AppDbContext db, IBellScheduleService bells)
{
    private static readonly Dictionary<string, DayOfWeek> DayMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["понедельник"] = DayOfWeek.Monday,
        ["вторник"] = DayOfWeek.Tuesday,
        ["среда"] = DayOfWeek.Wednesday,
        ["четверг"] = DayOfWeek.Thursday,
        ["пятница"] = DayOfWeek.Friday,
        ["суббота"] = DayOfWeek.Saturday,
    };

    /// <summary>
    /// Время пар по справочнику звонков на 2026/2027 учебный год (распечатанное
    /// «РАСПИСАНИЕ ЗВОНКОВ»). Используется, пока в базе нет профилей звонков.
    /// Понедельник отличается: после поднятия флага (8:15–8:25) и занятия курса
    /// внеучебной деятельности «Разговоры о важном» (8:30–9:15) первая пара
    /// начинается в 9:25, поэтому все пары сдвинуты на 15 минут позже.
    /// В четверг между второй и третьей парами — организационный/классный час
    /// 12:10–12:55 вместо большой перемены.
    /// </summary>
    internal static readonly Dictionary<
        DayOfWeek,
        List<(TimeSpan Start, TimeSpan End)>
    > PairTimeSlots = new()
    {
        [DayOfWeek.Monday] =
        [
            (new(9, 25, 0), new(10, 55, 0)),
            (new(11, 5, 0), new(12, 35, 0)),
            (new(13, 5, 0), new(14, 35, 0)),
            (new(14, 45, 0), new(16, 15, 0)),
            (new(16, 25, 0), new(17, 55, 0)),
            (new(18, 5, 0), new(19, 35, 0)),
        ],
        [DayOfWeek.Tuesday] =
        [
            (new(8, 30, 0), new(10, 0, 0)),
            (new(10, 10, 0), new(11, 40, 0)),
            (new(12, 10, 0), new(13, 40, 0)),
            (new(13, 50, 0), new(15, 20, 0)),
            (new(15, 30, 0), new(17, 0, 0)),
            (new(17, 10, 0), new(18, 40, 0)),
            (new(18, 50, 0), new(20, 20, 0)),
        ],
        [DayOfWeek.Wednesday] =
        [
            (new(8, 30, 0), new(10, 0, 0)),
            (new(10, 10, 0), new(11, 40, 0)),
            (new(12, 10, 0), new(13, 40, 0)),
            (new(13, 50, 0), new(15, 20, 0)),
            (new(15, 30, 0), new(17, 0, 0)),
            (new(17, 10, 0), new(18, 40, 0)),
            (new(18, 50, 0), new(20, 20, 0)),
        ],
        [DayOfWeek.Thursday] =
        [
            (new(8, 30, 0), new(10, 0, 0)),
            (new(10, 10, 0), new(11, 40, 0)),
            (new(13, 0, 0), new(14, 30, 0)),
            (new(14, 40, 0), new(16, 10, 0)),
            (new(16, 20, 0), new(17, 50, 0)),
            (new(18, 0, 0), new(19, 30, 0)),
        ],
        [DayOfWeek.Friday] =
        [
            (new(8, 30, 0), new(10, 0, 0)),
            (new(10, 10, 0), new(11, 40, 0)),
            (new(12, 10, 0), new(13, 40, 0)),
            (new(13, 50, 0), new(15, 20, 0)),
            (new(15, 30, 0), new(17, 0, 0)),
            (new(17, 10, 0), new(18, 40, 0)),
            (new(18, 50, 0), new(20, 20, 0)),
        ],
    };

    internal static (TimeSpan Start, TimeSpan End) GetPairTime(DayOfWeek day, int pairNumber)
    {
        var slots = PairTimeSlots.GetValueOrDefault(day) ?? PairTimeSlots[DayOfWeek.Tuesday];
        var index = Math.Clamp(pairNumber - 1, 0, slots.Count - 1);
        return slots[index];
    }

    /// <summary>Время пар по всем дням недели из профилей звонков (с откатом на базовый профиль).</summary>
    private async Task<
        Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>
    > LoadBellTimesByDayAsync(CancellationToken ct)
    {
        var result = new Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            result[day] = await bells.GetTimeMapAsync(day, ct);
        return result;
    }

    private static Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>> ToDayMap(
        Dictionary<int, (TimeSpan Start, TimeSpan End)>? flat
    )
    {
        var result = new Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>();
        if (flat is null)
            return result;

        foreach (var day in Enum.GetValues<DayOfWeek>())
            result[day] = flat;
        return result;
    }

    private static readonly Dictionary<string, string> SubjectSynonyms = new()
    {
        [@"Физ\.культура"] = "Физкультура",
        [@"Физ\.кул\."] = "Физкультура",
        [@"Ин\.язык\."] = "Ин.язык",
        [@"Ист\.Р\."] = "ИсторияРоссии",
        [@"Матем\.(?!\d)"] = "Математика",
        [@"Охр\.тр\."] = "ОхранаТруда",
        [@"Охрана труда"] = "ОхранаТруда",
        [@"ОсновыЭлект\."] = "ОсновыЭлектр.",
        [@"Эконом\. отр\."] = "ЭкономОтр.",
        [@"Эконом\.отр\."] = "ЭкономОтр.",
        [@"ЭлектротехиЭ\."] = "ЭлектрТех.",
        [@"Электр\.и Э\."] = "ЭлектрТех.",
        [@"Электротех\.(?!и)"] = "ЭлектрТех.",
        [@"Электр\.тех\."] = "ЭлектрТех.",
        [@"Электр\.(?!д|Т|т)"] = "ЭлектрТех.",
        [@"Эл\.тех\."] = "ЭлектрТех.",
        [@"Электробез\."] = "ЭлектрБезопасность",
        [@"ОсновыЭлектрТех\."] = "ОсновыЭлектр.",
    };

    /// <summary>
    /// Помечает занятие без аудитории. Токен нужен формату файла (перед предметом
    /// обязательно стоит токен), но в базу он попадать не должен.
    /// </summary>
    internal const string NoRoomPlaceholder = "—";

    internal static string NormalizeSubject(string subject)
    {
        var v = subject.Trim();

        // Схлопываем повторные пробелы, чтобы «МДК 01.03» и «МДК  01.03» совпадали.
        v = Regex.Replace(v, @"\s+", " ");

        foreach (var (pattern, replacement) in SubjectSynonyms)
            v = Regex.Replace(v, pattern, replacement, RegexOptions.IgnoreCase);

        // Убираем завершающие точки только после цифры: «МДК.01.03.» → «МДК.01.03».
        // Точка в аббревиатурах («ЭлектрТех.») при этом сохраняется.
        v = Regex.Replace(v, @"(?<=\d)\.+$", "");

        return v.Trim();
    }

    internal static string NormalizeTeacherName(string name)
    {
        var v = Regex.Replace(name.Trim(), @"\s+", " ");
        v = Regex.Replace(v, @"([А-Яа-яЁё])\.\s+([А-Яа-яЁё])", "$1.$2");
        v = Regex.Replace(v, @"\.\s*\.", "..");
        return v;
    }

    private static readonly Regex SelfStudyNoteRegex = new(
        @"сам[\s./\-]*р",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Проверяет, помечено ли примечание как самостоятельная работа
    /// («сам.р.», «сам/р», «сам-р», в т.ч. вместе с «вм.X» в любом порядке).
    /// </summary>
    internal static bool IsSelfStudyNote(string? note) =>
        !string.IsNullOrWhiteSpace(note) && SelfStudyNoteRegex.IsMatch(note);

    /// <summary>
    /// Ключ сопоставления групп: регистр, пробелы и разделители не учитываются,
    /// поэтому «ИП 235», «ип-235» и «ИП235» указывают на одну группу.
    /// </summary>
    internal static string GroupLookupKey(string name) =>
        Regex.Replace(name.Trim().ToUpperInvariant(), @"[\s\-–—‑\.]+", "");

    /// <summary>
    /// Ключ сопоставления преподавателей: регистр и «ё/е» не учитываются,
    /// поэтому варианты «петров П.П.», «Петров П.П.» и «Пётр П.П.» находят одного пользователя.
    /// </summary>
    internal static string TeacherLookupKey(string name)
    {
        var v = NormalizeTeacherName(name);
        return v.ToLowerInvariant().Replace('ё', 'е');
    }

    /// <summary>
    /// Ключ сопоставления предметов: регистр, пробелы, точки и прочие разделители
    /// не учитываются, поэтому «ОБП и ЗР», «ОБПиЗР» и «ОБП. и ЗР» — один предмет.
    /// </summary>
    internal static string SubjectLookupKey(string subject)
    {
        var v = Regex.Replace(NormalizeSubject(subject), @"[^\p{L}\p{Nd}]+", "");
        return v.ToLowerInvariant().Replace('ё', 'е');
    }

    /// <summary>
    /// Варианты ФИО из одной ячейки: «Кривцова/Степаненко» — два преподавателя через слеш.
    /// </summary>
    internal static List<string> TeacherNameVariants(string name) =>
        name.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeTeacherName)
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Фамилия (первое слово) — для сопоставления «Кривцова» с «Кривцова С.Н.».</summary>
    internal static string SurnameKey(string fullName)
    {
        var trimmed = fullName.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        var surname = spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
        return surname.ToLowerInvariant().Replace('ё', 'е');
    }

    /// <summary>
    /// Ищет преподавателя по имени (в т.ч. «A/B»): сначала точное совпадение полного ФИО
    /// по варианту, затем — единственная подходящая фамилия.
    /// </summary>
    internal static Guid? ResolveTeacherId(
        string? name,
        IReadOnlyDictionary<string, Guid> byFullName,
        IReadOnlyDictionary<string, List<Guid>> bySurname
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        Guid? surnameMatch = null;
        foreach (var variant in TeacherNameVariants(name))
        {
            if (byFullName.TryGetValue(variant, out var id))
                return id;

            if (
                surnameMatch is null
                && bySurname.TryGetValue(SurnameKey(variant), out var ids)
                && ids.Count == 1
            )
                surnameMatch = ids[0];
        }

        return surnameMatch;
    }

    private static ScheduleValidationError Error(
        string sheet,
        int row,
        int column,
        string level,
        string message
    ) =>
        new()
        {
            Sheet = sheet,
            Row = row,
            Column = column,
            Level = level,
            Message = $"Лист {sheet}, строка {row}, столбец {column}: {message}",
        };

    public (
        List<SchedulePreviewEntry> Entries,
        List<ScheduleValidationError> Errors
    ) ParseScheduleMatrix(
        IXLWorkbook workbook,
        Dictionary<int, (TimeSpan Start, TimeSpan End)>? bellTimes = null
    ) => ParseScheduleMatrix(workbook, ToDayMap(bellTimes));

    /// <summary>Разбор матрицы расписания с временем пар по каждому дню недели.</summary>
    public (
        List<SchedulePreviewEntry> Entries,
        List<ScheduleValidationError> Errors
    ) ParseScheduleMatrix(
        IXLWorkbook workbook,
        Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>? bellTimes
    )
    {
        var ws = workbook.Worksheet(1);
        var sheet = ws.Name;
        var entries = new List<SchedulePreviewEntry>();
        var errors = new List<ScheduleValidationError>();
        bellTimes ??= [];

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 3;
        var groupColumns = new Dictionary<int, string>();
        for (int col = 3; col <= lastCol; col++)
        {
            var name = ws.Cell(5, col).GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                groupColumns[col] = name;
        }

        if (groupColumns.Count == 0)
            errors.Add(Error(sheet, 5, 3, "structure", "не найдены названия групп"));

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var dayBlocks = new List<(int StartRow, DayOfWeek Day)>();
        for (int row = 1; row <= lastRow; row++)
        {
            var rawA = ws.Cell(row, 1).GetString().Trim();
            if (DayMap.TryGetValue(rawA.ToUpperInvariant(), out var day))
            {
                dayBlocks.Add((row, day));
            }
            else if (!string.IsNullOrEmpty(rawA) && ws.Cell(row, 2).Value.IsNumber)
            {
                errors.Add(
                    Error(sheet, row, 1, "structure", $"неизвестный день недели \"{rawA}\"")
                );
            }
        }

        if (dayBlocks.Count == 0)
            errors.Add(Error(sheet, 0, 1, "structure", "не найдены дни недели"));

        for (int bi = 0; bi < dayBlocks.Count; bi++)
        {
            var (dayStart, day) = dayBlocks[bi];
            int dayEnd = bi + 1 < dayBlocks.Count ? dayBlocks[bi + 1].StartRow : lastRow + 1;

            var pairRows = new List<int>();
            for (int r = dayStart; r < dayEnd; r++)
            {
                var bVal = ws.Cell(r, 2).Value;
                if (bVal.IsNumber)
                    pairRows.Add(r);
            }

            for (int pi = 0; pi < pairRows.Count; pi++)
            {
                int pairRow = pairRows[pi];
                int pairNum = (int)ws.Cell(pairRow, 2).GetDouble();

                if (pairNum < 1 || pairNum > 8)
                {
                    errors.Add(
                        Error(sheet, pairRow, 2, "data", $"номер пары {pairNum} вне диапазона 1–8")
                    );
                    continue;
                }

                int nextPairRow = pi + 1 < pairRows.Count ? pairRows[pi + 1] : dayEnd;

                foreach (var (col, groupName) in groupColumns)
                {
                    for (int r = pairRow; r < nextPairRow; r++)
                    {
                        var cellText = ws.Cell(r, col).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(cellText))
                            continue;

                        var parsed = ParseSubjectCell(cellText);
                        var hasErrors = false;

                        if (string.IsNullOrEmpty(parsed.Subject))
                        {
                            errors.Add(
                                Error(
                                    sheet,
                                    r,
                                    col,
                                    "data",
                                    $"не удалось распознать предмет из \"{cellText}\""
                                )
                            );
                            hasErrors = true;
                        }

                        // Аудитория обязательна. Молча создавать пару без неё нельзя:
                        // так в базу попадали «пары» из строк вида «Ин.язык» старого
                        // формата, где предмет и недели стояли отдельными строками.
                        if (parsed.RoomMissing)
                        {
                            errors.Add(
                                Error(
                                    sheet,
                                    r,
                                    col,
                                    "data",
                                    $"не удалось распознать аудиторию в \"{cellText}\""
                                )
                            );
                            hasErrors = true;
                        }

                        if (parsed.Weeks.Count == 0)
                        {
                            errors.Add(Error(sheet, r, col, "data", "не указаны недели"));
                            hasErrors = true;
                        }

                        if (parsed.Weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
                        {
                            var badWeek = parsed.Weeks.First(w =>
                                w < 1 || w > StudyWeek.TotalWeeks
                            );
                            errors.Add(
                                Error(
                                    sheet,
                                    r,
                                    col,
                                    "data",
                                    $"неделя {badWeek} вне семестра (1–{StudyWeek.TotalWeeks})"
                                )
                            );
                            hasErrors = true;
                        }

                        if (hasErrors)
                            continue;

                        var dayTimes = bellTimes.GetValueOrDefault(day);
                        foreach (var part in parsed.Parts)
                        {
                            var (start, end) =
                                dayTimes is not null && dayTimes.TryGetValue(pairNum, out var t)
                                    ? t
                                    : GetPairTime(day, pairNum);
                            entries.Add(
                                new SchedulePreviewEntry
                                {
                                    GroupName = groupName,
                                    Day = day.ToString(),
                                    Pair = pairNum,
                                    Subject = NormalizeSubject(parsed.Subject),
                                    Room = part.Room,
                                    TeacherName = part.Teacher,
                                    Weeks = parsed.Weeks,
                                    StartTime = start,
                                    EndTime = end,
                                }
                            );
                        }
                    }
                }
            }
        }

        return (entries, errors);
    }

    private static readonly Regex SubjectCellRegex = new(
        @"^(?<room>[^\s]+)\s+(?<body>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex HallRoomRegex = new(
        @"^(ч\.\s*з|с\.\s*з)\.?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex TeacherTailRegex = new(
        @"[А-Яа-яёЁ][А-Яа-яёЁ]+\s+[А-Яа-яёЁ]\.?\s*[А-Яа-яёЁ]\.?(?:\s*/\s*[А-Яа-яёЁ][А-Яа-яёЁ]+\s+[А-Яа-яёЁ]\.?\s*[А-Яа-яёЁ]\.?)*$",
        RegexOptions.Compiled
    );

    /// <summary>Аудитория + преподаватель одного варианта объединённой ячейки.</summary>
    private readonly record struct SubjectCellPart(string Room, string Teacher);

    /// <summary>Результат разбора ячейки предмета.</summary>
    private sealed class SubjectCellParse
    {
        public string Subject { get; init; } = string.Empty;
        public List<int> Weeks { get; init; } = [];
        public List<SubjectCellPart> Parts { get; init; } = [];

        /// <summary>В ячейке нет токена аудитории — разбирать её как пару нельзя.</summary>
        public bool RoomMissing { get; init; }
    }

    /// <summary>
    /// Разбор ячейки предмета. Кроме обычного «ауд. Предмет (недели) Преподаватель»
    /// поддерживается объединённый компонент на несколько преподавателей:
    /// «302/413 Ин.язык (1-6,8,10) Рахимова А.Л./Степаненко О.А.» — это один предмет
    /// с одними неделями, но своя аудитория и преподаватель у каждой подгруппы.
    /// </summary>
    private static SubjectCellParse ParseSubjectCell(string cell)
    {
        var text = cell.Trim();
        if (string.IsNullOrEmpty(text) || text == ".")
            return new SubjectCellParse();

        // «ч.з.»/«с.з.» — аудитория задаётся словом, а не номером.
        var hall = HallRoomRegex.Match(text);
        if (hall.Success)
            return BuildSubjectCell(hall.Value.Replace(" ", string.Empty), text[hall.Length..]);

        var match = SubjectCellRegex.Match(text);
        if (match.Success)
            return BuildSubjectCell(match.Groups["room"].Value, match.Groups["body"].Value);

        // Ячейка без пробела: аудитории в ней нет. Такой разбор допустим только
        // для явного прочерка — выгрузки помечают им занятия без аудитории,
        // иначе прочерк не должен попадать в базу как настоящая аудитория.
        if (text == NoRoomPlaceholder)
            return BuildSubjectCell(string.Empty, text[NoRoomPlaceholder.Length..].Trim());

        return new SubjectCellParse { Subject = text, RoomMissing = true };
    }

    /// <summary>Отделяет от тела ячейки недели и преподавателя(-ей).</summary>
    private static SubjectCellParse BuildSubjectCell(string room, string body)
    {
        // Прочерк — явная пометка «аудитория не указана», а не номер аудитории.
        var roomMissing = false;
        if (room == NoRoomPlaceholder)
            room = string.Empty;

        var weeks = new List<int>();
        var weeksMatch = Regex.Match(body, @"\(([^)]+)\)");
        if (weeksMatch.Success)
        {
            weeks = ParseWeeks(weeksMatch.Groups[1].Value);
            body = Regex.Replace(body, @"\([^)]*\)", "").Trim();
        }

        // Отрезаем по длине совпадения, а не по длине нормализованного имени:
        // нормализация может убрать пробелы («А. Л.» → «А.Л.»).
        var teacherMatch = TeacherTailRegex.Match(body);
        var teacher = teacherMatch.Success
            ? NormalizeTeacherName(teacherMatch.Value)
            : string.Empty;
        if (teacherMatch.Success)
            body = body[..^teacherMatch.Length].TrimEnd();

        return new SubjectCellParse
        {
            Subject = body.Trim(),
            Weeks = weeks,
            Parts = BuildParts(room, teacher),
            RoomMissing = false,
        };
    }

    /// <summary>
    /// Сопоставляет список аудиторий со списком преподавателей. Если один из списков
    /// одноэлементный, он применяется ко всем вариантам; иначе берётся меньший список.
    /// </summary>
    private static List<SubjectCellPart> BuildParts(string roomRaw, string teacherRaw)
    {
        var rooms = SplitVariants(roomRaw, NormalizeRoomName);
        var teachers = SplitVariants(teacherRaw, NormalizeTeacherName);
        if (rooms.Count == 0)
            rooms.Add(string.Empty);
        if (teachers.Count == 0)
            teachers.Add(string.Empty);

        while (rooms.Count < teachers.Count)
            rooms.Add(rooms[0]);
        while (teachers.Count < rooms.Count)
            teachers.Add(teachers[0]);

        return rooms.Zip(teachers, (room, teacher) => new SubjectCellPart(room, teacher)).ToList();
    }

    /// <summary>Аудитория: без лишних пробелов, «ч.з» приводим к «ч.з.».</summary>
    private static string NormalizeRoomName(string name)
    {
        var value = Regex.Replace(name.Trim(), @"\s+", string.Empty);
        if (value.Equals("ч.з", StringComparison.OrdinalIgnoreCase))
            return "ч.з.";
        if (value.Equals("с.з", StringComparison.OrdinalIgnoreCase))
            return "с.з.";
        return value;
    }

    /// <summary>Делит «302/413» или «Рахимова А.Л./Степаненко О.А.» на варианты.</summary>
    private static List<string> SplitVariants(string raw, Func<string, string> normalize) =>
        raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(normalize)
            .Where(v => v.Length > 0)
            .ToList();

    private static readonly Regex WeekPartRegex = new(
        @"^(?<from>\d{1,2})(?:\s*[-–—]\s*(?<to>\d{1,2}))?$",
        RegexOptions.Compiled
    );

    private static List<int> ParseWeeks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var weeks = new List<int>();

        foreach (var rawPart in text.Split([',', ';', 'и', 'И'], StringSplitOptions.TrimEntries))
        {
            var part = rawPart.Trim('(', ')', ' ', '.');
            if (part.Length == 0)
                continue;

            var match = WeekPartRegex.Match(part);
            if (!match.Success)
                continue;

            var from = int.Parse(match.Groups["from"].Value);
            if (match.Groups["to"].Success)
            {
                var to = int.Parse(match.Groups["to"].Value);
                for (int i = from; i <= to; i++)
                    weeks.Add(i);
            }
            else
            {
                weeks.Add(from);
            }
        }

        return weeks.Distinct().OrderBy(x => x).ToList();
    }

    public async Task<PreviewResult> PreviewAsync(Stream fileStream, CancellationToken ct)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return new PreviewResult
            {
                IsSuccess = false,
                ErrorMessage = "Не удалось прочитать файл. Убедитесь, что это XLSX-файл.",
            };
        }

        using (workbook)
        {
            var bellTimes = await LoadBellTimesByDayAsync(ct);

            List<SchedulePreviewEntry> entries;
            List<ScheduleValidationError> errors;
            try
            {
                (entries, errors) = ParseScheduleMatrix(workbook, bellTimes);
            }
            catch (Exception ex)
            {
                entries = [];
                errors =
                [
                    new ScheduleValidationError
                    {
                        Sheet = string.Empty,
                        Row = 0,
                        Column = 0,
                        Level = "structure",
                        Message = $"Не удалось прочитать лист: {ex.Message}",
                    },
                ];
            }

            return new PreviewResult
            {
                IsSuccess = true,
                Preview = new SchedulePreviewResponse
                {
                    TotalEntries = entries.Count,
                    Entries = entries,
                    Errors = errors,
                },
            };
        }
    }

    private static List<ScheduleValidationError> ValidateConfirmEntries(
        List<SchedulePreviewEntry> entries
    )
    {
        var errors = new List<ScheduleValidationError>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Pair < 1 || entry.Pair > 8)
                errors.Add(ConfirmError(i, "номер пары должен быть от 1 до 8"));
            if (!Enum.TryParse<DayOfWeek>(entry.Day, true, out _))
                errors.Add(ConfirmError(i, $"неизвестный день недели \"{entry.Day}\""));
            if (string.IsNullOrWhiteSpace(entry.GroupName))
                errors.Add(ConfirmError(i, "не указана группа"));
            if (string.IsNullOrWhiteSpace(entry.Subject))
                errors.Add(ConfirmError(i, "не указан предмет"));
            // Аудитория в паре необязательна: занятия, добавленные корректировкой,
            // её не имеют, а формат файла допускает прочерк на их месте.
            var weeks = entry.Weeks ?? [];
            if (weeks.Count == 0 || weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
                errors.Add(
                    ConfirmError(i, $"недели должны быть в диапазоне 1–{StudyWeek.TotalWeeks}")
                );
        }
        return errors;
    }

    private static ScheduleValidationError ConfirmError(int index, string message) =>
        Error("импорт", index + 2, 0, "data", message);

    /// <summary>
    /// Подбирает свободные login/email: при совпадении с существующими или уже
    /// сгенерированными значениями добавляет суффикс «-2», «-3» и так далее.
    /// </summary>
    private static (string Login, string Email) GenerateUniqueCredentials(
        string baseLogin,
        HashSet<string> takenLogins,
        HashSet<string> takenEmails
    )
    {
        for (var suffix = 1; ; suffix++)
        {
            var login = suffix == 1 ? baseLogin : $"{baseLogin}-{suffix}";
            var email = $"{login}@temp.local";
            if (takenLogins.Contains(login) || takenEmails.Contains(email))
                continue;

            takenLogins.Add(login);
            takenEmails.Add(email);
            return (login, email);
        }
    }

    private static int ParseCourse(string groupName)
    {
        var digits = new string(groupName.Where(char.IsDigit).ToArray());
        var course = digits.Length > 0 ? digits[0] - '0' : 1;
        return Math.Clamp(course, 1, 4);
    }

    public async Task<ConfirmResult> ConfirmAsync(
        ConfirmImportRequest request,
        CancellationToken ct
    )
    {
        var entries = request.Entries ?? [];

        if (entries.Count == 0)
        {
            return new ConfirmResult
            {
                IsSuccess = false,
                Errors =
                [
                    new ScheduleValidationError
                    {
                        Sheet = "импорт",
                        Row = 0,
                        Column = 0,
                        Level = "structure",
                        Message = "Нет позиций для импорта",
                    },
                ],
            };
        }

        var errors = ValidateConfirmEntries(entries);
        if (errors.Count > 0)
        {
            return new ConfirmResult { IsSuccess = false, Errors = errors };
        }

        var bellTimes = await LoadBellTimesByDayAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var existingEntries = await db.ScheduleEntries.ToListAsync(ct);
            var existingHistory = await db.ScheduleHistory.ToListAsync(ct);
            db.ScheduleEntries.RemoveRange(existingEntries);
            db.ScheduleHistory.RemoveRange(existingHistory);

            var uniqueGroups = entries.Select(e => e.GroupName).Distinct().ToList();

            var createdGroups = 0;
            var existingGroups = await db
                .Groups.Where(g => uniqueGroups.Contains(g.Name))
                .ToDictionaryAsync(g => g.Name, g => g.Id, ct);

            var groupMap = new Dictionary<string, Guid>(existingGroups);
            foreach (var name in uniqueGroups.Where(n => !existingGroups.ContainsKey(n)))
            {
                var group = new Entities.Group
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Course = ParseCourse(name),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                db.Groups.Add(group);
                groupMap[name] = group.Id;
                createdGroups++;
            }

            // «А.Л. Рахимова/О.А. Степаненко» — ячейка может нести нескольких преподавателей,
            // каждый из них должен попасть в справочник отдельной записью.
            var uniqueTeachers = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.TeacherName))
                .SelectMany(e => TeacherNameVariants(e.TeacherName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var createdTeachers = 0;
            var teacherMap = new Dictionary<string, Guid>();

            if (uniqueTeachers.Count > 0)
            {
                var allUsers = await db
                    .Users.Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Login,
                        u.Email,
                        u.Role,
                    })
                    .ToListAsync(ct);

                var userMap = new Dictionary<string, Guid>();
                foreach (var item in allUsers.Where(u => u.Role == UserRole.Teacher))
                    userMap.TryAdd(TeacherLookupKey(item.FullName), item.Id);

                var takenLogins = new HashSet<string>(
                    allUsers.Select(u => u.Login),
                    StringComparer.OrdinalIgnoreCase
                );
                var takenEmails = new HashSet<string>(
                    allUsers.Select(u => u.Email),
                    StringComparer.OrdinalIgnoreCase
                );

                var profileMap = (
                    await db.Teachers.Select(t => new { t.Id, t.UserId }).ToListAsync(ct)
                ).ToDictionary(t => t.UserId, t => t.Id);

                foreach (var name in uniqueTeachers)
                {
                    var key = TeacherLookupKey(name);
                    if (userMap.TryGetValue(key, out var userId))
                    {
                        if (profileMap.TryGetValue(userId, out var profileId))
                        {
                            teacherMap[key] = profileId;
                            continue;
                        }

                        var profile = new Teacher
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            CyclicalCommission = "Не указана",
                            Position = "Преподаватель",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };
                        db.Teachers.Add(profile);
                        profileMap[userId] = profile.Id;
                        teacherMap[key] = profile.Id;
                        createdTeachers++;
                        continue;
                    }

                    var (login, email) = GenerateUniqueCredentials(
                        name.Replace(" ", ".").ToLowerInvariant(),
                        takenLogins,
                        takenEmails
                    );

                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = name,
                        Login = login,
                        Email = email,
                        PasswordHash = "",
                        Role = UserRole.Teacher,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.Users.Add(user);

                    var teacher = new Teacher
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CyclicalCommission = "Не указана",
                        Position = "Преподаватель",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.Teachers.Add(teacher);
                    userMap[key] = user.Id;
                    teacherMap[key] = teacher.Id;
                    createdTeachers++;
                }
            }

            await db.SaveChangesAsync(ct);

            var entriesToAdd = new List<ScheduleEntry>();
            foreach (var entry in entries)
            {
                var groupId = groupMap[entry.GroupName];

                Guid? teacherId = null;
                if (!string.IsNullOrWhiteSpace(entry.TeacherName))
                {
                    foreach (var variant in TeacherNameVariants(entry.TeacherName))
                    {
                        if (teacherMap.TryGetValue(TeacherLookupKey(variant), out var tid))
                        {
                            teacherId = tid;
                            break;
                        }
                    }
                }

                Enum.TryParse<DayOfWeek>(entry.Day, true, out var dayOfWeek);
                var (startTime, endTime) =
                    bellTimes.GetValueOrDefault(dayOfWeek) is { } dayTimes
                    && dayTimes.TryGetValue(entry.Pair, out var t)
                        ? t
                        : GetPairTime(dayOfWeek, entry.Pair);

                entriesToAdd.Add(
                    new ScheduleEntry
                    {
                        Id = Guid.NewGuid(),
                        GroupId = groupId,
                        TeacherId = teacherId,
                        Subject = NormalizeSubject(entry.Subject),
                        Room = entry.Room,
                        DayOfWeek = dayOfWeek,
                        NumberPair = entry.Pair,
                        StartTime = startTime,
                        EndTime = endTime,
                        Weeks = entry.Weeks ?? [],
                        LessonType = LessonType.None,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
            }

            db.ScheduleEntries.AddRange(entriesToAdd);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var allEntries = await db
                .ScheduleEntries.Include(e => e.Group)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.User)
                .OrderBy(e => e.DayOfWeek)
                .ThenBy(e => e.NumberPair)
                .ToListAsync(ct);

            return new ConfirmResult
            {
                IsSuccess = true,
                Imported = entriesToAdd.Count,
                Groups = createdGroups,
                Teachers = createdTeachers,
                Schedule = allEntries.Select(e => e.ToDto()).ToList(),
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

public class PreviewResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public SchedulePreviewResponse? Preview { get; set; }
}
