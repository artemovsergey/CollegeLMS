using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using Group = CollegeLMS.API.Entities.Group;

namespace CollegeLMS.API.Services;

public class ScheduleCorrectionService(
    AppDbContext db,
    MaxBotHttpClient maxBot,
    CorrectionApplyEngine engine
) : IScheduleCorrectionService
{
    private readonly string templatesPath = Path.Combine("..", "import", "schedule");

    private static bool IsSelfStudyNote(string? note) =>
        string.Equals(note?.Trim(), "сам.р.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Есть ли на дату рабочий день (override) — разрешает корректировку на выходной.</summary>
    private async Task<bool> HasWorkingOverrideAsync(DateTime date, CancellationToken ct) =>
        await db
            .WorkingDayOverrides.AsNoTracking()
            .AnyAsync(d => d.DateFrom <= date.Date && d.DateTo >= date.Date, ct);

    public async Task<Result<DocumentDownloadResult>> ExportManualAsync(
        ManualCorrectionExportRequest request,
        CancellationToken ct
    )
    {
        if (request.CorrectionDate == default || request.Rows.Count == 0)
            return Result<DocumentDownloadResult>.Fail(
                "Укажите дату и хотя бы одну корректировку.",
                400
            );

        if (
            request.CorrectionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            && !await HasWorkingOverrideAsync(request.CorrectionDate, ct)
        )
            return Result<DocumentDownloadResult>.Fail(
                "Корректировка не может быть на выходной день.",
                400
            );

        var templatePath = Path.GetFullPath(Path.Combine(templatesPath, "Корректировка.xlsx"));
        if (!File.Exists(templatePath))
            return Result<DocumentDownloadResult>.Fail(
                "Шаблон корректировки отсутствует на сервере.",
                404
            );

        await using var template = File.OpenRead(templatePath);
        using var workbook = new XLWorkbook(template);
        var sheet = workbook.Worksheet(1);
        sheet.Cell(3, 1).Value =
            $"Корректировка на {request.CorrectionDate:dd.MM.yyyy} г. ({DayName(request.CorrectionDate.DayOfWeek)})";

        var lastRow = Math.Max(sheet.LastRowUsed()?.RowNumber() ?? 7, 7);
        if (lastRow >= 7)
            sheet.Range(7, 1, lastRow, 7).Clear(XLClearOptions.Contents);

        for (var index = 0; index < request.Rows.Count; index++)
        {
            var row = 7 + index;
            if (row > lastRow)
                sheet.Row(row).Style = sheet.Row(7).Style;

            var item = request.Rows[index];
            sheet.Cell(row, 1).Value = item.GroupName.Trim();
            sheet.Cell(row, 2).Value = item.RemovedSubject?.Trim() ?? string.Empty;
            sheet.Cell(row, 3).Value = item.RemovedTeacherName?.Trim() ?? string.Empty;
            sheet.Cell(row, 4).Value = item.AddedSubject?.Trim() ?? string.Empty;
            sheet.Cell(row, 5).Value = item.AddedTeacherName?.Trim() ?? string.Empty;
            sheet.Cell(row, 6).Value = item.NumberPair;
            sheet.Cell(row, 7).Value = item.Note?.Trim() ?? string.Empty;
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        var timestamp = DateTime.UtcNow.ToString("dd.MM.yyyy_HH-mm-ss");
        return Result<DocumentDownloadResult>.Ok(
            new DocumentDownloadResult
            {
                Content = output.ToArray(),
                FileName = $"Корректировка_{timestamp}.xlsx",
            }
        );
    }

    private static readonly Regex DatePattern = new(
        @"на\s+(\d{1,2})\.(\d{1,2})\.(\d{4})",
        RegexOptions.Compiled
    );

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

    private static string DayName(DayOfWeek day) => DayNames[(int)day];

    public async Task<Result<CorrectionPreviewResponse>> PreviewAsync(
        Stream fileStream,
        CancellationToken ct
    )
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return Result<CorrectionPreviewResponse>.Fail(
                "Файл не является корректным XLSX. Сохраните файл в формате .xlsx.",
                400
            );
        }

        using (workbook)
        {
            var parsed = await ParseWorkbookAsync(workbook, ct);

            return Result<CorrectionPreviewResponse>.Ok(
                new CorrectionPreviewResponse
                {
                    CorrectionDate = parsed.Date,
                    Week = parsed.Week,
                    DayOfWeek = (int)parsed.Date.DayOfWeek,
                    TotalEntries = parsed.Entries.Count,
                    Entries = parsed.Entries,
                    AllEntries = parsed.AllEntries,
                    Errors = parsed.Errors,
                }
            );
        }
    }

    private static ScheduleValidationError Error(
        int row,
        int column,
        string level,
        string message
    ) =>
        new()
        {
            Row = row,
            Column = column,
            Level = level,
            Message = message,
        };

    private async Task<(
        DateTime Date,
        int Week,
        List<CorrectionPreviewEntry> Entries,
        List<CorrectionPreviewEntry> AllEntries,
        List<ScheduleValidationError> Errors
    )> ParseWorkbookAsync(XLWorkbook workbook, CancellationToken ct)
    {
        var ws = workbook.Worksheet(1);
        var errors = new List<ScheduleValidationError>();
        var entries = new List<CorrectionPreviewEntry>();

        // --- Уровень 1: структура файла ---
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 7)
        {
            errors.Add(Error(0, 0, "structure", "В файле нет данных."));
            return (default, 0, entries, [], errors);
        }

        var a3 = ws.Cell(3, 1).GetString().Trim();
        var dateMatch = DatePattern.Match(a3);
        if (!dateMatch.Success)
        {
            errors.Add(
                Error(
                    3,
                    1,
                    "structure",
                    string.IsNullOrWhiteSpace(a3)
                        ? "Не найдена дата корректировки в ячейке A3 (например, «на 01.09.2026 г.»)."
                        : "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.»."
                )
            );
            return (default, 0, entries, [], errors);
        }

        DateTime date;
        try
        {
            date = new DateTime(
                int.Parse(dateMatch.Groups[3].Value),
                int.Parse(dateMatch.Groups[2].Value),
                int.Parse(dateMatch.Groups[1].Value)
            );
        }
        catch
        {
            errors.Add(
                Error(3, 1, "structure", "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.».")
            );
            return (default, 0, entries, [], errors);
        }

        if (
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            && !await HasWorkingOverrideAsync(date, ct)
        )
        {
            errors.Add(
                Error(
                    3,
                    1,
                    "structure",
                    "Указана дата на выходной день. Корректировка применяется к учебным дням."
                )
            );
            return (default, 0, entries, [], errors);
        }

        var a5 = ws.Cell(5, 1).GetString().Trim();
        if (!string.Equals(a5, "Группа", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error(5, 1, "structure", "Не найден заголовок «Группа» в ячейке A5."));
            return (default, 0, entries, [], errors);
        }

        var b5 = ws.Cell(5, 2).GetString();
        var d5 = ws.Cell(5, 4).GetString();
        var hasRemoveHeader = b5.Contains("Снимается", StringComparison.OrdinalIgnoreCase);
        var hasAddHeader = d5.Contains("Вводится", StringComparison.OrdinalIgnoreCase);
        if (!hasRemoveHeader && !hasAddHeader)
        {
            errors.Add(
                Error(
                    5,
                    2,
                    "structure",
                    "Не найдены колонки «Снимается по расписанию» или «Вводится в расписание»."
                )
            );
            return (default, 0, entries, [], errors);
        }

        var week = StudyWeek.ForDate(date);
        var allGroups = await db.Groups.AsNoTracking().ToListAsync(ct);

        for (int row = 7; row <= lastRow; row++)
        {
            var groupName = ws.Cell(row, 1).GetString().Trim();
            var removeSubject = ws.Cell(row, 2).GetString().Trim();
            var removeTeacher = ws.Cell(row, 3).GetString().Trim();
            var addSubject = ws.Cell(row, 4).GetString().Trim();
            var addTeacher = ws.Cell(row, 5).GetString().Trim();
            var pairValue = ws.Cell(row, 6).Value;
            var note = ws.Cell(row, 7).GetString().Trim();

            var pairNumber = ParsePair(pairValue);

            var anyContent =
                groupName.Length > 0
                || removeSubject.Length > 0
                || removeTeacher.Length > 0
                || addSubject.Length > 0
                || addTeacher.Length > 0
                || pairNumber is not null;

            if (!anyContent)
                continue;

            // --- Уровень 2: данные строки ---
            if (groupName.Length == 0)
            {
                errors.Add(Error(row, 1, "data", $"Строка {row}: не указана группа."));
                continue;
            }

            if (pairNumber is not { } pair || pair < 1 || pair > 8)
            {
                errors.Add(
                    Error(
                        row,
                        6,
                        "data",
                        $"Строка {row}: не указан/некорректен № пары (F). Ожидается число 1–8."
                    )
                );
                continue;
            }

            var hasRemove = removeSubject.Length > 0 || removeTeacher.Length > 0;
            var hasAdd = addSubject.Length > 0 || addTeacher.Length > 0;

            if ((removeSubject.Length > 0) != (removeTeacher.Length > 0))
            {
                errors.Add(
                    Error(
                        row,
                        3,
                        "data",
                        $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот)."
                    )
                );
                continue;
            }

            if (hasAdd && (addSubject.Length > 0) != (addTeacher.Length > 0))
            {
                errors.Add(
                    Error(
                        row,
                        5,
                        "data",
                        $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот)."
                    )
                );
                continue;
            }

            if (!hasRemove && !hasAdd)
            {
                errors.Add(
                    Error(
                        row,
                        4,
                        "data",
                        $"Строка {row}: не заполнены ни «Снимается», ни «Вводится»."
                    )
                );
                continue;
            }

            var (group, ambiguous) = ResolveGroup(groupName, allGroups);
            if (ambiguous)
            {
                errors.Add(
                    Error(
                        row,
                        1,
                        "data",
                        $"Строка {row}: группа «{groupName}» неоднозначна — совпадает с несколькими группами в системе."
                    )
                );
                continue;
            }

            if (group is null)
            {
                errors.Add(
                    Error(
                        row,
                        1,
                        "data",
                        $"Строка {row}: группа «{groupName}» не найдена в системе."
                    )
                );
                continue;
            }

            Guid? removeTeacherId = null;
            if (removeTeacher.Length > 0)
            {
                var t = await FindTeacherAsync(removeTeacher, ct);
                if (t is null)
                {
                    errors.Add(
                        Error(
                            row,
                            3,
                            "data",
                            $"Строка {row}: преподаватель «{removeTeacher}» не найден."
                        )
                    );
                    continue;
                }
                removeTeacherId = t;
            }

            Guid? addTeacherId = null;
            if (addTeacher.Length > 0)
            {
                var t = await FindTeacherAsync(addTeacher, ct);
                if (t is null)
                {
                    errors.Add(
                        Error(
                            row,
                            5,
                            "data",
                            $"Строка {row}: преподаватель «{addTeacher}» не найден."
                        )
                    );
                    continue;
                }
                addTeacherId = t;
            }

            // --- Уровень 3: бизнес-логика ---
            var changeType = (hasRemove, hasAdd) switch
            {
                (true, false) => ScheduleChangeType.Remove,
                (false, true) => ScheduleChangeType.Add,
                _ => ScheduleChangeType.Replace,
            };

            var noteOldPair = ParseNoteOldPair(note);
            var rowEntries = new List<CorrectionPreviewEntry>();

            if (changeType == ScheduleChangeType.Add)
            {
                var subject =
                    addSubject.Length > 0
                        ? ScheduleImportService.NormalizeSubject(addSubject)
                        : string.Empty;

                // Примечание «вм.X» → перенос: снять занятие с пары X, ввести на пару F.
                if (noteOldPair is { } movePair)
                {
                    var moved = await BuildReplaceAsync(
                        row,
                        group.Name,
                        group.Id,
                        date.DayOfWeek,
                        week,
                        movePair,
                        subject,
                        addTeacherId,
                        pair,
                        subject,
                        addTeacherId,
                        Normalize(addTeacher),
                        note,
                        errors,
                        ct
                    );
                    if (moved is not null)
                        rowEntries.Add(moved);
                }
                else
                {
                    rowEntries.Add(
                        new CorrectionPreviewEntry
                        {
                            Row = row,
                            GroupId = group.Id,
                            GroupName = group.Name,
                            ChangeType = changeType,
                            DayOfWeek = (int)date.DayOfWeek,
                            Week = week,
                            NumberPair = pair,
                            Subject = subject,
                            TeacherId = addTeacherId,
                            TeacherName = Normalize(addTeacher),
                            Note = note,
                        }
                    );
                }
            }
            else if (changeType == ScheduleChangeType.Remove)
            {
                var target = await FindEntryAtPairAsync(group.Id, date.DayOfWeek, week, pair, ct);

                if (target is null)
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: занятие на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} не найдено."
                        )
                    );
                }
                else
                {
                    rowEntries.Add(
                        new CorrectionPreviewEntry
                        {
                            Row = row,
                            GroupId = group.Id,
                            GroupName = group.Name,
                            ChangeType = changeType,
                            DayOfWeek = (int)date.DayOfWeek,
                            Week = week,
                            NumberPair = pair,
                            RemovedSubject = target.Subject,
                            RemovedTeacherId = target.TeacherId,
                            RemovedTeacherName = target.Teacher?.User?.FullName,
                            RemovedNumberPair = target.NumberPair,
                            Note = note,
                        }
                    );
                }
            }
            else // Replace: на паре F снимается B/C и вводится D/E
            {
                var swap = await BuildReplaceAsync(
                    row,
                    group.Name,
                    group.Id,
                    date.DayOfWeek,
                    week,
                    pair,
                    removeSubject,
                    removeTeacherId,
                    pair,
                    addSubject,
                    addTeacherId,
                    Normalize(addTeacher),
                    note,
                    errors,
                    ct
                );

                if (noteOldPair is { } movePair)
                {
                    // Примечание «вм.X» → дополнительно снять D/E со старой пары X.
                    var removedOld = await BuildRemoveEntryAsync(
                        row,
                        group.Name,
                        group.Id,
                        date.DayOfWeek,
                        week,
                        movePair,
                        addSubject,
                        addTeacherId,
                        Normalize(addTeacher),
                        note,
                        errors,
                        ct
                    );
                    if (swap is not null && removedOld is not null)
                    {
                        rowEntries.Add(swap);
                        rowEntries.Add(removedOld);
                    }
                }
                else if (swap is not null)
                {
                    rowEntries.Add(swap);
                }
            }

            entries.AddRange(rowEntries);
        }

        var allEntries = await BuildAllEntriesAsync(ws, lastRow, date, week, ct);

        return (date, week, entries, allEntries, errors);
    }

    /// <summary>
    /// Best-effort строки для импорта в пакет: каждая непустая строка файла
    /// сохраняется (даже с ошибками данных), группы и преподаватели
    /// сопоставляются по имени, если найдены.
    /// </summary>
    private async Task<List<CorrectionPreviewEntry>> BuildAllEntriesAsync(
        IXLWorksheet ws,
        int lastRow,
        DateTime date,
        int week,
        CancellationToken ct
    )
    {
        var all = new List<CorrectionPreviewEntry>();

        for (int row = 7; row <= lastRow; row++)
        {
            var groupName = ws.Cell(row, 1).GetString().Trim();
            var removeSubject = ws.Cell(row, 2).GetString().Trim();
            var removeTeacher = ws.Cell(row, 3).GetString().Trim();
            var addSubject = ws.Cell(row, 4).GetString().Trim();
            var addTeacher = ws.Cell(row, 5).GetString().Trim();
            var pairValue = ParsePair(ws.Cell(row, 6).Value);
            var pair = pairValue ?? 0;
            var note = ws.Cell(row, 7).GetString().Trim();

            var anyContent =
                groupName.Length > 0
                || removeSubject.Length > 0
                || removeTeacher.Length > 0
                || addSubject.Length > 0
                || addTeacher.Length > 0
                || pairValue is not null;

            if (!anyContent)
                continue;

            var hasRemove = removeSubject.Length > 0 || removeTeacher.Length > 0;
            var hasAdd = addSubject.Length > 0 || addTeacher.Length > 0;
            var changeType = (hasRemove, hasAdd) switch
            {
                (true, false) => ScheduleChangeType.Remove,
                (false, true) => ScheduleChangeType.Add,
                _ => ScheduleChangeType.Replace,
            };

            var oldPair = ParseNoteOldPair(note);

            if (changeType == ScheduleChangeType.Add && oldPair is { } movePair)
            {
                // «вм.X»: перенос — снять D/E со старой пары X, ввести на пару F.
                all.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupName = groupName,
                        ChangeType = ScheduleChangeType.Move,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = pair,
                        Subject = ScheduleImportService.NormalizeSubject(addSubject),
                        TeacherName = Normalize(addTeacher),
                        RemovedSubject = ScheduleImportService.NormalizeSubject(addSubject),
                        RemovedTeacherName = Normalize(addTeacher),
                        RemovedNumberPair = movePair,
                        Note = note,
                    }
                );
                continue;
            }

            all.Add(
                new CorrectionPreviewEntry
                {
                    Row = row,
                    GroupName = groupName,
                    ChangeType = changeType,
                    DayOfWeek = (int)date.DayOfWeek,
                    Week = week,
                    NumberPair = pair,
                    Subject =
                        changeType != ScheduleChangeType.Remove && addSubject.Length > 0
                            ? ScheduleImportService.NormalizeSubject(addSubject)
                            : null,
                    TeacherName =
                        changeType != ScheduleChangeType.Remove && addTeacher.Length > 0
                            ? Normalize(addTeacher)
                            : null,
                    RemovedSubject =
                        changeType != ScheduleChangeType.Add && removeSubject.Length > 0
                            ? ScheduleImportService.NormalizeSubject(removeSubject)
                            : null,
                    RemovedTeacherName =
                        changeType != ScheduleChangeType.Add && removeTeacher.Length > 0
                            ? Normalize(removeTeacher)
                            : null,
                    RemovedNumberPair = changeType
                        is ScheduleChangeType.Replace
                            or ScheduleChangeType.Move
                        ? pair
                        : null,
                    Note = note,
                }
            );

            if (changeType == ScheduleChangeType.Replace && oldPair is { } movedPair)
            {
                // Дополнительно снять D/E со старой пары X.
                all.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupName = groupName,
                        ChangeType = ScheduleChangeType.Remove,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = movedPair,
                        RemovedSubject = ScheduleImportService.NormalizeSubject(addSubject),
                        RemovedTeacherName = Normalize(addTeacher),
                        RemovedNumberPair = movedPair,
                        Note = note,
                    }
                );
            }
        }

        await ResolveReferencesAsync(all, ct);
        return all;
    }

    /// <summary>Сопоставляет группы и преподавателей best-effort строк по имени.</summary>
    private async Task ResolveReferencesAsync(
        List<CorrectionPreviewEntry> entries,
        CancellationToken ct
    )
    {
        if (entries.Count == 0)
            return;

        var groups = await db.Groups.AsNoTracking().ToListAsync(ct);
        foreach (var entry in entries)
        {
            if (entry.GroupId != Guid.Empty || string.IsNullOrWhiteSpace(entry.GroupName))
                continue;

            var (group, ambiguous) = ResolveGroup(entry.GroupName, groups);
            if (group is not null)
            {
                entry.GroupId = group.Id;
                entry.GroupName = group.Name;
            }
            // При неоднозначности GroupId не проставляем — ошибка всплывёт при применении.
        }

        var teacherNames = entries
            .SelectMany(e => new[] { e.TeacherName, e.RemovedTeacherName })
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        if (teacherNames.Count > 0)
        {
            var teachers = await db.Teachers.AsNoTracking().Include(t => t.User).ToListAsync(ct);
            var (byFullName, bySurname) = BuildTeacherLookups(teachers);

            foreach (var entry in entries)
            {
                if (entry.TeacherId is null && !string.IsNullOrWhiteSpace(entry.TeacherName))
                {
                    entry.TeacherId = ScheduleImportService.ResolveTeacherId(
                        entry.TeacherName,
                        byFullName,
                        bySurname
                    );
                }

                if (
                    entry.RemovedTeacherId is null
                    && !string.IsNullOrWhiteSpace(entry.RemovedTeacherName)
                )
                {
                    entry.RemovedTeacherId = ScheduleImportService.ResolveTeacherId(
                        entry.RemovedTeacherName,
                        byFullName,
                        bySurname
                    );
                }
            }
        }
    }

    /// <summary>
    /// Ищет группу по имени: сначала точное совпадение (без учёта регистра),
    /// затем по нормализованному ключу («ИП 235» → «ИП235»). Если ключу
    /// соответствуют несколько разных групп — возвращает признак неоднозначности.
    /// </summary>
    private static (Group? Group, bool Ambiguous) ResolveGroup(
        string name,
        IReadOnlyList<Group> groups
    )
    {
        var exact = groups.FirstOrDefault(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)
        );
        if (exact is not null)
            return (exact, false);

        var key = ScheduleImportService.GroupLookupKey(name);
        var matches = groups
            .Where(g => ScheduleImportService.GroupLookupKey(g.Name) == key)
            .ToList();
        return matches.Count switch
        {
            1 => (matches[0], false),
            > 1 => (null, true),
            _ => (null, false),
        };
    }

    private static readonly Regex TextPairPattern = new(
        @"^(\d{1,2})\s*п?\.?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static int? ParsePair(XLCellValue pairValue)
    {
        if (pairValue.IsNumber)
            return (int)pairValue.GetNumber();

        if (!pairValue.IsText)
            return null;

        var text = pairValue.GetText().Trim();
        if (int.TryParse(text, out var n))
            return n;

        var match = TextPairPattern.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static readonly Regex NotePairPattern = new(
        @"вм\.?\s*(\d{1,2})\s*п?",
        RegexOptions.Compiled
    );

    private static int? ParseNoteOldPair(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var match = NotePairPattern.Match(note);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private async Task<ScheduleEntry?> FindEntryAtPairAsync(
        Guid groupId,
        DayOfWeek day,
        int week,
        int pair,
        CancellationToken ct
    ) =>
        await db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(
                e =>
                    e.GroupId == groupId
                    && e.DayOfWeek == day
                    && e.NumberPair == pair
                    && e.Weeks.Contains(week),
                ct
            );

    private async Task<CorrectionPreviewEntry?> BuildReplaceAsync(
        int row,
        string groupName,
        Guid groupId,
        DayOfWeek day,
        int week,
        int removedPair,
        string removedSubject,
        Guid? removedTeacherId,
        int addPair,
        string addSubject,
        Guid? addTeacherId,
        string addTeacherName,
        string note,
        List<ScheduleValidationError> errors,
        CancellationToken ct
    )
    {
        var removed = await db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(
                e =>
                    e.GroupId == groupId
                    && e.DayOfWeek == day
                    && e.NumberPair == removedPair
                    && e.Weeks.Contains(week)
                    && e.Subject == ScheduleImportService.NormalizeSubject(removedSubject)
                    && (
                        removedTeacherId.HasValue
                            ? e.TeacherId == removedTeacherId.Value
                            : e.TeacherId == null
                    ),
                ct
            );

        if (removed is null)
        {
            errors.Add(
                Error(
                    row,
                    4,
                    "logic",
                    $"Строка {row}: занятие на {DayName(day)} {week}-й неделе, пара {removedPair} не найдено."
                )
            );
            return null;
        }

        return new CorrectionPreviewEntry
        {
            Row = row,
            GroupId = groupId,
            GroupName = groupName,
            ChangeType =
                removed.NumberPair != addPair
                    ? ScheduleChangeType.Move
                    : ScheduleChangeType.Replace,
            DayOfWeek = (int)day,
            Week = week,
            NumberPair = addPair,
            Subject = ScheduleImportService.NormalizeSubject(addSubject),
            TeacherId = addTeacherId,
            TeacherName = addTeacherName,
            RemovedSubject = removed.Subject,
            RemovedTeacherId = removed.TeacherId,
            RemovedTeacherName = removed.Teacher?.User?.FullName,
            RemovedNumberPair = removed.NumberPair,
            Note = note,
        };
    }

    private async Task<CorrectionPreviewEntry?> BuildRemoveEntryAsync(
        int row,
        string groupName,
        Guid groupId,
        DayOfWeek day,
        int week,
        int pair,
        string subject,
        Guid? teacherId,
        string teacherName,
        string note,
        List<ScheduleValidationError> errors,
        CancellationToken ct
    )
    {
        var target = await db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(
                e =>
                    e.GroupId == groupId
                    && e.DayOfWeek == day
                    && e.NumberPair == pair
                    && e.Weeks.Contains(week)
                    && e.Subject == ScheduleImportService.NormalizeSubject(subject)
                    && (teacherId.HasValue ? e.TeacherId == teacherId.Value : e.TeacherId == null),
                ct
            );

        if (target is null)
        {
            errors.Add(
                Error(
                    row,
                    4,
                    "logic",
                    $"Строка {row}: занятие на {DayName(day)} {week}-й неделе, пара {pair} не найдено."
                )
            );
            return null;
        }

        return new CorrectionPreviewEntry
        {
            Row = row,
            GroupId = groupId,
            GroupName = groupName,
            ChangeType = ScheduleChangeType.Remove,
            DayOfWeek = (int)day,
            Week = week,
            NumberPair = pair,
            Subject = ScheduleImportService.NormalizeSubject(subject),
            TeacherId = teacherId,
            TeacherName = teacherName,
            RemovedSubject = target.Subject,
            RemovedTeacherId = target.TeacherId,
            RemovedTeacherName = target.Teacher?.User?.FullName,
            RemovedNumberPair = target.NumberPair,
            Note = note,
        };
    }

    private async Task<Guid?> FindTeacherAsync(string name, CancellationToken ct)
    {
        var teachers = await db.Teachers.AsNoTracking().Include(t => t.User).ToListAsync(ct);
        var (byFullName, bySurname) = BuildTeacherLookups(teachers);
        return ScheduleImportService.ResolveTeacherId(name, byFullName, bySurname);
    }

    private static (
        Dictionary<string, Guid> ByFullName,
        Dictionary<string, List<Guid>> BySurname
    ) BuildTeacherLookups(List<Teacher> teachers)
    {
        var byFullName = teachers
            .GroupBy(
                t => ScheduleImportService.NormalizeTeacherName(t.User.FullName),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var bySurname = teachers
            .GroupBy(
                t => ScheduleImportService.SurnameKey(t.User.FullName),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t.Id).Distinct().ToList(),
                StringComparer.OrdinalIgnoreCase
            );
        return (byFullName, bySurname);
    }

    private static string Normalize(string name) =>
        ScheduleImportService.NormalizeTeacherName(name);

    public async Task<Result<CorrectionConfirmResult>> ConfirmAsync(
        CorrectionConfirmRequest request,
        string idempotencyKey,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        var existing = await db
            .CorrectionConfirmations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
            return Result<CorrectionConfirmResult>.Fail(
                $"Корректировка с ключом идемпотентности «{idempotencyKey}» уже была применена.",
                409
            );

        // Сохраняем ключ идемпотентности до транзакции, чтобы повторный вызов
        // с тем же ключом был отклонён (защита от гонки).
        db.CorrectionConfirmations.Add(
            new CorrectionConfirmation
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                HistoryCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync(ct);

        var notifChanges = new List<ScheduleChangeDto>();

        if (request.Entries.Count > 0)
        {
            var applyResult = await ApplyEntriesAsync(request.Entries, appliedByUserId, null, ct);
            if (!applyResult.IsSuccess)
                return Result<CorrectionConfirmResult>.Fail(
                    applyResult.ErrorMessage!,
                    applyResult.StatusCode
                );
            notifChanges = applyResult.Data!;
        }

        // Обновляем количество записей в подтверждении
        var confirmation = await db.CorrectionConfirmations.FirstOrDefaultAsync(
            c => c.IdempotencyKey == idempotencyKey,
            ct
        );
        if (confirmation is not null)
            confirmation.HistoryCount = notifChanges.Count;
        await db.SaveChangesAsync(ct);

        // Оповещение MaxBot — fail-safe, после фиксации транзакции
        if (notifChanges.Count > 0)
            await maxBot.SendChangesAsync(notifChanges, ct);

        if (notifChanges.Count == 0)
            return Result<CorrectionConfirmResult>.Ok(
                new CorrectionConfirmResult { Applied = 0, History = [] }
            );

        var ids = notifChanges.Select(h => h.Id).ToList();
        var saved = await db
            .ScheduleHistory.AsNoTracking()
            .Include(h => h.Group)
            .Include(h => h.Teacher!)
                .ThenInclude(t => t.User)
            .Where(h => ids.Contains(h.Id))
            .OrderBy(h => h.AppliedAt)
            .ToListAsync(ct);

        return Result<CorrectionConfirmResult>.Ok(
            new CorrectionConfirmResult
            {
                Applied = notifChanges.Count,
                History = saved.Select(h => h.ToDto()).ToList(),
            }
        );
    }

    /// <summary>
    /// Применяет список позиций корректировки в одной транзакции.
    /// Возвращает изменения для уведомления (с Id строки истории).
    /// </summary>
    public async Task<Result<List<ScheduleChangeDto>>> ApplyEntriesAsync(
        List<CorrectionPreviewEntry> entries,
        Guid appliedByUserId,
        DateTime? correctionDate,
        CancellationToken ct
    )
    {
        if (entries.Count == 0)
            return Result<List<ScheduleChangeDto>>.Ok([]);

        var validationErrors = await ValidateEntriesAsync(entries, ct);
        if (validationErrors.Count > 0)
            return Result<List<ScheduleChangeDto>>.Fail(string.Join("; ", validationErrors), 400);

        var changes = new List<ScheduleChangeDto>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var entry in entries)
            {
                try
                {
                    var applied = await ApplyEntryAsync(entry, appliedByUserId, correctionDate, ct);
                    if (applied.Change is not null)
                        changes.Add(applied.Change);
                }
                catch (InvalidOperationException ex)
                {
                    await tx.RollbackAsync(ct);
                    return Result<List<ScheduleChangeDto>>.Fail(
                        $"Ошибка в строке {entry.Row}: {ex.Message}",
                        400
                    );
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return Result<List<ScheduleChangeDto>>.Ok(changes);
    }

    public async Task<Result<CorrectionDayResponse>> GetDayAsync(
        Guid groupId,
        DateTime date,
        Guid? batchId,
        CancellationToken ct
    )
    {
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return Result<CorrectionDayResponse>.Fail("Группа не найдена.", 404);

        var day = date.Date.DayOfWeek;
        var week = StudyWeek.WeekOf(date);

        var entries = await engine.BuildEffectiveEntriesAsync(groupId, day, week, batchId, ct);

        var history = await db
            .ScheduleHistory.AsNoTracking()
            .Where(h => h.GroupId == groupId && h.DayOfWeek == day && h.Week == week)
            .ToListAsync(ct);
        var tagsByPair = history
            .GroupBy(h => h.NumberPair)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.OrderBy(h => h.AppliedAt)
                        .Select(h => new ChangeTag
                        {
                            ChangeType = h.ChangeType,
                            Week = h.Week,
                            RemovedNumberPair = h.RemovedNumberPair,
                            RemovedSubject = h.RemovedSubject,
                            Note = h.Note,
                        })
                        .ToList()
            );

        var response = new CorrectionDayResponse
        {
            Date = date.Date,
            Week = week,
            DayOfWeek = (int)day,
            GroupId = group.Id,
            GroupName = group.Name,
            Entries = entries
                .OrderBy(e => e.NumberPair)
                .ThenBy(e => e.Subject)
                .Select(e => new CorrectionDayEntry
                {
                    NumberPair = e.NumberPair,
                    Subject = e.Subject,
                    Room = e.Room,
                    TeacherId = e.TeacherId,
                    TeacherName = e.TeacherName,
                    Note = e.Note,
                    IsSelfStudy = e.IsSelfStudy,
                    PendingChangeType = ParseChangeType(e.PendingChangeType),
                    ChangeTags = tagsByPair.GetValueOrDefault(e.NumberPair, []),
                })
                .ToList(),
        };

        return Result<CorrectionDayResponse>.Ok(response);
    }

    private static ScheduleChangeType? ParseChangeType(string? value) =>
        Enum.TryParse<ScheduleChangeType>(value, ignoreCase: true, out var parsed) ? parsed : null;

    public async Task<Result<PagedResponse<ScheduleHistoryResponse>>> GetHistoryAsync(
        Guid? groupId,
        Guid? teacherId,
        int? week,
        DateTime? date,
        DateTime? from,
        DateTime? to,
        ScheduleChangeType? changeType,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db
            .ScheduleHistory.AsNoTracking()
            .Include(h => h.Group)
            .Include(h => h.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(h => h.GroupId == groupId.Value);

        if (teacherId.HasValue)
            query = query.Where(h =>
                h.TeacherId == teacherId.Value || h.RemovedTeacherId == teacherId.Value
            );

        if (week.HasValue)
            query = query.Where(h => h.Week == week.Value);

        if (date.HasValue)
        {
            var lessonWeek = StudyWeek.WeekOf(date.Value);
            var lessonDay = date.Value.DayOfWeek;
            query = query.Where(h => h.Week == lessonWeek && h.DayOfWeek == lessonDay);
        }

        if (from.HasValue)
            query = query.Where(h => h.AppliedAt >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(h => h.AppliedAt < to.Value.Date.AddDays(1));

        if (changeType.HasValue)
            query = query.Where(h => h.ChangeType == changeType.Value);

        // Сортировка и пагинация выполняются в памяти: EF Core InMemory-провайдер
        // возвращает пустой список при Include + Skip/Take (баг тестового провайдера).
        // Для Postgres таблица истории небольшая, поэтому оверхед незначителен.
        var items = await query.ToListAsync(ct);
        items = items
            .OrderByDescending(h => h.AppliedAt)
            .ThenByDescending(h => h.CreatedAt)
            .ToList();

        var total = items.Count;
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);
        items = items.Skip((p - 1) * ps).Take(ps).ToList();

        return Result<PagedResponse<ScheduleHistoryResponse>>.Ok(
            new PagedResponse<ScheduleHistoryResponse>(
                items.Select(h => h.ToDto()).ToList(),
                total,
                p,
                ps
            )
        );
    }

    private record AppliedEntry(ScheduleHistory History, ScheduleChangeDto? Change);

    private async Task<List<string>> ValidateEntriesAsync(
        List<CorrectionPreviewEntry> entries,
        CancellationToken ct
    )
    {
        var errors = new List<string>();

        foreach (var entry in entries)
        {
            if (IsSelfStudyNote(entry.Note) && entry.ChangeType != ScheduleChangeType.Remove)
            {
                errors.Add("Примечание «сам.р.» допустимо только для снятия.");
                continue;
            }

            var groupExists = await db
                .Groups.AsNoTracking()
                .AnyAsync(g => g.Id == entry.GroupId, ct);
            if (!groupExists)
            {
                errors.Add($"Группа «{entry.GroupName}» не найдена.");
                continue;
            }

            var day = (DayOfWeek)entry.DayOfWeek;
            var dayName = DayName(day);

            switch (entry.ChangeType)
            {
                case ScheduleChangeType.Remove:
                {
                    var removeQuery = db
                        .ScheduleEntries.AsNoTracking()
                        .Where(e =>
                            e.GroupId == entry.GroupId
                            && e.DayOfWeek == day
                            && e.NumberPair == entry.NumberPair
                            && e.Weeks.Contains(entry.Week)
                        );

                    if (!string.IsNullOrEmpty(entry.RemovedSubject))
                        removeQuery = removeQuery.Where(e =>
                            e.Subject
                            == ScheduleImportService.NormalizeSubject(entry.RemovedSubject)
                        );
                    if (entry.RemovedTeacherId.HasValue)
                        removeQuery = removeQuery.Where(e =>
                            e.TeacherId == entry.RemovedTeacherId.Value
                        );

                    var target = await removeQuery.FirstOrDefaultAsync(ct);
                    if (target is null)
                        errors.Add(
                            $"Занятие на {dayName} {entry.Week}-й неделе, пара {entry.NumberPair} не найдено (снимаемое занятие)."
                        );
                    break;
                }

                case ScheduleChangeType.Replace:
                case ScheduleChangeType.Move:
                {
                    var removed = await db
                        .ScheduleEntries.AsNoTracking()
                        .FirstOrDefaultAsync(
                            e =>
                                e.GroupId == entry.GroupId
                                && e.DayOfWeek == day
                                && e.NumberPair == (entry.RemovedNumberPair ?? entry.NumberPair)
                                && e.Weeks.Contains(entry.Week)
                                && (
                                    entry.RemovedTeacherId.HasValue
                                        ? e.TeacherId == entry.RemovedTeacherId.Value
                                        : e.TeacherId == null
                                )
                                && e.Subject == (entry.RemovedSubject ?? string.Empty),
                            ct
                        );
                    if (removed is null)
                        errors.Add(
                            $"Занятие на {dayName} {entry.Week}-й неделе, пара {entry.RemovedNumberPair ?? entry.NumberPair} не найдено (снимаемое занятие)."
                        );
                    break;
                }

                default:
                    break;
            }
        }

        return errors;
    }

    private async Task<AppliedEntry> ApplyEntryAsync(
        CorrectionPreviewEntry entry,
        Guid appliedByUserId,
        DateTime? correctionDate,
        CancellationToken ct
    )
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == entry.GroupId, ct);
        if (group is null)
            throw new InvalidOperationException($"Группа {entry.GroupId} не найдена.");

        var day = (DayOfWeek)entry.DayOfWeek;
        var utcNow = DateTime.UtcNow;

        switch (entry.ChangeType)
        {
            case ScheduleChangeType.Add:
            {
                var entity = IsSelfStudyNote(entry.Note)
                    ? null
                    : CreateEntry(group.Id, entry, day, utcNow);
                if (entity is not null)
                    db.ScheduleEntries.Add(entity);

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Add,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = entry.TeacherId,
                    Subject = entry.Subject ?? string.Empty,
                    Room = entity?.Room ?? string.Empty,
                    DayOfWeek = day,
                    NumberPair = entry.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    Id = history.Id,
                    ChangeType = "Add",
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = entry.TeacherId,
                    TeacherName = entry.TeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = entry.NumberPair,
                    Subject = entry.Subject ?? string.Empty,
                    Note = entry.Note,
                    CorrectionDate = correctionDate,
                };

                return new AppliedEntry(history, change);
            }

            case ScheduleChangeType.Remove:
            {
                var removeQuery = db.ScheduleEntries.Where(e =>
                    e.GroupId == group.Id
                    && e.DayOfWeek == day
                    && e.NumberPair == entry.NumberPair
                    && e.Weeks.Contains(entry.Week)
                );

                if (!string.IsNullOrEmpty(entry.RemovedSubject))
                    removeQuery = removeQuery.Where(e =>
                        e.Subject == ScheduleImportService.NormalizeSubject(entry.RemovedSubject)
                    );
                if (entry.RemovedTeacherId.HasValue)
                    removeQuery = removeQuery.Where(e =>
                        e.TeacherId == entry.RemovedTeacherId.Value
                    );

                var target = await removeQuery.FirstOrDefaultAsync(ct);
                if (target is null)
                    throw new InvalidOperationException(
                        $"Занятие на {day} {entry.Week}-й неделе, пара {entry.NumberPair} не найдено."
                    );

                // «сам.р.» при снятии: пара остаётся в расписании, неделя не удаляется.
                // Участвует в учебном процессе, студенты видят бейдж и могут не приходить.
                if (!IsSelfStudyNote(entry.Note))
                {
                    target.Weeks = target.Weeks.Where(w => w != entry.Week).ToList();
                    if (target.Weeks.Count == 0)
                        db.ScheduleEntries.Remove(target);
                    else
                        target.UpdatedAt = utcNow;
                }
                else
                {
                    target.UpdatedAt = utcNow;
                }

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Remove,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = target.TeacherId,
                    Subject = target.Subject,
                    Room = target.Room,
                    DayOfWeek = day,
                    NumberPair = target.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    Id = history.Id,
                    ChangeType = "Remove",
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = target.TeacherId,
                    TeacherName = entry.RemovedTeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = target.NumberPair,
                    Subject = target.Subject,
                    Note = entry.Note,
                    CorrectionDate = correctionDate,
                };

                return new AppliedEntry(history, change);
            }

            case ScheduleChangeType.Replace:
            case ScheduleChangeType.Move:
            {
                var removed = await db.ScheduleEntries.FirstOrDefaultAsync(
                    e =>
                        e.GroupId == group.Id
                        && e.DayOfWeek == day
                        && e.NumberPair == (entry.RemovedNumberPair ?? entry.NumberPair)
                        && e.Weeks.Contains(entry.Week)
                        && (
                            entry.RemovedTeacherId.HasValue
                                ? e.TeacherId == entry.RemovedTeacherId.Value
                                : e.TeacherId == null
                        )
                        && e.Subject == (entry.RemovedSubject ?? string.Empty),
                    ct
                );
                if (removed is null)
                    throw new InvalidOperationException(
                        $"Занятие на {day} {entry.Week}-й неделе не найдено."
                    );

                removed.Weeks = removed.Weeks.Where(w => w != entry.Week).ToList();
                if (removed.Weeks.Count == 0)
                    db.ScheduleEntries.Remove(removed);
                else
                    removed.UpdatedAt = utcNow;

                var entity = CreateEntry(group.Id, entry, day, utcNow);
                db.ScheduleEntries.Add(entity);

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = entry.ChangeType,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = entry.TeacherId,
                    Subject = entry.Subject ?? string.Empty,
                    Room = entity.Room,
                    DayOfWeek = day,
                    NumberPair = entry.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    RemovedSubject = removed.Subject,
                    RemovedTeacherId = removed.TeacherId,
                    RemovedRoom = removed.Room,
                    RemovedNumberPair = removed.NumberPair,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    Id = history.Id,
                    ChangeType = entry.ChangeType.ToString(),
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = entry.TeacherId,
                    TeacherName = entry.TeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = entry.NumberPair,
                    Subject = entry.Subject ?? string.Empty,
                    Note = entry.Note,
                    RemovedSubject = removed.Subject,
                    RemovedTeacherName = entry.RemovedTeacherName,
                    RemovedNumberPair = removed.NumberPair,
                    CorrectionDate = correctionDate,
                };

                return new AppliedEntry(history, change);
            }

            default:
                throw new InvalidOperationException(
                    $"Неизвестный тип изменения расписания: {entry.ChangeType}"
                );
        }

        throw new InvalidOperationException(
            $"Неизвестный тип изменения расписания: {entry.ChangeType}"
        );
    }

    private static ScheduleEntry CreateEntry(
        Guid groupId,
        CorrectionPreviewEntry entry,
        DayOfWeek day,
        DateTime utcNow
    )
    {
        var (start, end) = ScheduleImportService.GetPairTime(day, entry.NumberPair);

        var entity = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = entry.TeacherId,
            Subject = ScheduleImportService.NormalizeSubject(entry.Subject ?? string.Empty),
            Room = string.Empty,
            DayOfWeek = day,
            NumberPair = entry.NumberPair,
            StartTime = start,
            EndTime = end,
            Weeks = new List<int> { entry.Week },
            LessonType = LessonType.None,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        return entity;
    }
}
