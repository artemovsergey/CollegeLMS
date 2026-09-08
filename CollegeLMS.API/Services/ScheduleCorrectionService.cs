using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleCorrectionService(AppDbContext db, MaxBotHttpClient maxBot)
{
    private static readonly Regex DatePattern =
        new(@"на\s+(\d{1,2})\.(\d{1,2})\.(\d{4})", RegexOptions.Compiled);

    private static readonly string[] DayNames =
        ["", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

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

    private async Task<(DateTime Date, int Week, List<CorrectionPreviewEntry> Entries, List<ScheduleValidationError> Errors)>
        ParseWorkbookAsync(XLWorkbook workbook, CancellationToken ct)
    {
        var ws = workbook.Worksheet(1);
        var errors = new List<ScheduleValidationError>();
        var entries = new List<CorrectionPreviewEntry>();

        // --- Уровень 1: структура файла ---
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 7)
        {
            errors.Add(Error(0, 0, "structure", "В файле нет данных."));
            return (default, 0, entries, errors);
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
            return (default, 0, entries, errors);
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
            errors.Add(Error(3, 1, "structure", "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.»."));
            return (default, 0, entries, errors);
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors.Add(
                Error(3, 1, "structure", "Указана дата на выходной день. Корректировка применяется к учебным дням.")
            );
            return (default, 0, entries, errors);
        }

        var a5 = ws.Cell(5, 1).GetString().Trim();
        if (!string.Equals(a5, "Группа", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error(5, 1, "structure", "Не найден заголовок «Группа» в ячейке A5."));
            return (default, 0, entries, errors);
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
            return (default, 0, entries, errors);
        }

        var week = StudyWeek.ForDate(date);

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
                    Error(row, 6, "data", $"Строка {row}: не указан/некорректен № пары (F). Ожидается число 1–8.")
                );
                continue;
            }

            var hasRemove = removeSubject.Length > 0 || removeTeacher.Length > 0;
            var hasAdd = addSubject.Length > 0 || addTeacher.Length > 0;

            if ((removeSubject.Length > 0) != (removeTeacher.Length > 0))
            {
                errors.Add(
                    Error(row, 3, "data", $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот).")
                );
                continue;
            }

            if (hasAdd && (addSubject.Length > 0) != (addTeacher.Length > 0))
            {
                errors.Add(
                    Error(row, 5, "data", $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот).")
                );
                continue;
            }

            if (!hasRemove && !hasAdd)
            {
                errors.Add(
                    Error(row, 4, "data", $"Строка {row}: не заполнены ни «Снимается», ни «Вводится».")
                );
                continue;
            }

            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == groupName, ct);
            if (group is null)
            {
                errors.Add(Error(row, 1, "data", $"Строка {row}: группа «{groupName}» не найдена в системе."));
                continue;
            }

            Guid? removeTeacherId = null;
            if (removeTeacher.Length > 0)
            {
                var t = await FindTeacherAsync(removeTeacher, ct);
                if (t is null)
                {
                    errors.Add(
                        Error(row, 3, "data", $"Строка {row}: преподаватель «{removeTeacher}» не найден.")
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
                        Error(row, 5, "data", $"Строка {row}: преподаватель «{addTeacher}» не найден.")
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

            if (changeType == ScheduleChangeType.Add)
            {
                if (await IsPairBusyAsync(group.Id, date.DayOfWeek, pair, week, null, ct))
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} уже занята."
                        )
                    );
                    continue;
                }

                var subject = addSubject.Length > 0
                    ? ScheduleImportService.NormalizeSubject(addSubject)
                    : string.Empty;

                entries.Add(
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
            else if (changeType == ScheduleChangeType.Remove)
            {
                var target = await db
                    .ScheduleEntries.AsNoTracking()
                    .Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(
                        e =>
                            e.GroupId == group.Id
                            && e.DayOfWeek == date.DayOfWeek
                            && e.NumberPair == pair
                            && e.Weeks.Contains(week),
                        ct
                    );

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
                    continue;
                }

                entries.Add(
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
            else // Replace
            {
                var removed = await db
                    .ScheduleEntries.AsNoTracking()
                    .Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(
                        e =>
                            e.GroupId == group.Id
                            && e.DayOfWeek == date.DayOfWeek
                            && e.Weeks.Contains(week)
                            && e.Subject == ScheduleImportService.NormalizeSubject(removeSubject)
                            && (
                                removeTeacherId.HasValue
                                    ? e.TeacherId == removeTeacherId.Value
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
                            $"Строка {row}: занятие на {DayName(date.DayOfWeek)} {week}-й неделе не найдено."
                        )
                    );
                    continue;
                }

                if (removed.NumberPair == pair)
                {
                    errors.Add(
                        Error(row, 4, "logic", $"Строка {row}: пара снятия и ввода одинакова.")
                    );
                    continue;
                }

                if (await IsPairBusyAsync(group.Id, date.DayOfWeek, pair, week, removed.Id, ct))
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} уже занята."
                        )
                    );
                    continue;
                }

                entries.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupId = group.Id,
                        GroupName = group.Name,
                        ChangeType = changeType,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = pair,
                        Subject = ScheduleImportService.NormalizeSubject(addSubject),
                        TeacherId = addTeacherId,
                        TeacherName = Normalize(addTeacher),
                        RemovedSubject = removed.Subject,
                        RemovedTeacherId = removed.TeacherId,
                        RemovedTeacherName = removed.Teacher?.User?.FullName,
                        RemovedNumberPair = removed.NumberPair,
                        Note = note,
                    }
                );
            }
        }

        return (date, week, entries, errors);
    }

    private static int? ParsePair(XLCellValue pairValue)
    {
        if (pairValue.IsNumber)
            return (int)pairValue.GetNumber();

        var text = pairValue.GetText().Trim();
        return int.TryParse(text, out var n) ? n : null;
    }

    private async Task<Guid?> FindTeacherAsync(string name, CancellationToken ct)
    {
        var normalized = ScheduleImportService.NormalizeTeacherName(name);
        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.User.FullName == normalized, ct);
        return teacher?.Id;
    }

    private static string Normalize(string name) =>
        ScheduleImportService.NormalizeTeacherName(name);

    private async Task<bool> IsPairBusyAsync(
        Guid groupId,
        DayOfWeek day,
        int pair,
        int week,
        Guid? excludeEntryId,
        CancellationToken ct
    )
    {
        var query = db.ScheduleEntries.Where(e =>
            e.GroupId == groupId
            && e.DayOfWeek == day
            && e.NumberPair == pair
            && e.Weeks.Contains(week)
        );

        if (excludeEntryId.HasValue)
            query = query.Where(e => e.Id != excludeEntryId.Value);

        return await query.AnyAsync(ct);
    }
}