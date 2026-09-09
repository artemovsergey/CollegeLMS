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

namespace CollegeLMS.API.Services;

public class ScheduleCorrectionService(AppDbContext db, MaxBotHttpClient maxBot)
    : IScheduleCorrectionService
{
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
            errors.Add(
                Error(3, 1, "structure", "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.».")
            );
            return (default, 0, entries, errors);
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors.Add(
                Error(
                    3,
                    1,
                    "structure",
                    "Указана дата на выходной день. Корректировка применяется к учебным дням."
                )
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

            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == groupName, ct);
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

        return (date, week, entries, errors);
    }

    private static int? ParsePair(XLCellValue pairValue)
    {
        if (pairValue.IsNumber)
            return (int)pairValue.GetNumber();

        var text = pairValue.GetText().Trim();
        return int.TryParse(text, out var n) ? n : null;
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

        if (await IsPairBusyAsync(groupId, day, addPair, week, removed.Id, ct))
        {
            errors.Add(
                Error(
                    row,
                    4,
                    "logic",
                    $"Строка {row}: на {DayName(day)} {week}-й неделе, пара {addPair} уже занята."
                )
            );
            return null;
        }

        return new CorrectionPreviewEntry
        {
            Row = row,
            GroupId = groupId,
            GroupName = groupName,
            ChangeType = ScheduleChangeType.Replace,
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

    public async Task<Result<CorrectionConfirmResult>> ConfirmAsync(
        CorrectionConfirmRequest request,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        if (request.Entries.Count == 0)
            return Result<CorrectionConfirmResult>.Ok(
                new CorrectionConfirmResult { Applied = 0, History = [] }
            );

        var history = new List<ScheduleHistory>();
        var notifChanges = new List<ScheduleChangeDto>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var entry in request.Entries)
            {
                var applied = await ApplyEntryAsync(entry, appliedByUserId, ct);
                history.Add(applied.History);
                if (applied.Change is not null)
                    notifChanges.Add(applied.Change);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // Оповещение MaxBot — fail-safe, после фиксации транзакции
        await maxBot.SendChangesAsync(notifChanges, ct);

        var ids = history.Select(h => h.Id).ToList();
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
                Applied = history.Count,
                History = saved.Select(h => h.ToDto()).ToList(),
            }
        );
    }

    public async Task<Result<PagedResponse<ScheduleHistoryResponse>>> GetHistoryAsync(
        Guid? groupId,
        Guid? teacherId,
        int? week,
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
        var ps = Math.Clamp(pageSize ?? 20, 1, 200);
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

    private async Task<AppliedEntry> ApplyEntryAsync(
        CorrectionPreviewEntry entry,
        Guid appliedByUserId,
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
                var entity = CreateEntry(group.Id, entry, day, utcNow);
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
                    Room = entity.Room,
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
                };

                return new AppliedEntry(history, change);
            }

            case ScheduleChangeType.Remove:
            {
                var target = await db.ScheduleEntries.FirstOrDefaultAsync(
                    e =>
                        e.GroupId == group.Id
                        && e.DayOfWeek == day
                        && e.NumberPair == entry.NumberPair
                        && e.Weeks.Contains(entry.Week),
                    ct
                );
                if (target is null)
                    throw new InvalidOperationException(
                        $"Занятие на {day} {entry.Week}-й неделе, пара {entry.NumberPair} не найдено."
                    );

                target.Weeks = target.Weeks.Where(w => w != entry.Week).ToList();
                if (target.Weeks.Count == 0)
                    db.ScheduleEntries.Remove(target);
                else
                    target.UpdatedAt = utcNow;

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
                };

                return new AppliedEntry(history, change);
            }

            default: // Replace
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
                    ChangeType = ScheduleChangeType.Replace,
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
                    ChangeType = "Replace",
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
                };

                return new AppliedEntry(history, change);
            }
        }
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
            LessonType = LessonType.Practice,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        return entity;
    }
}
