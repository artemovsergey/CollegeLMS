using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Практики УП/ПП: CRUD, фильтры и импорт из XLSX.</summary>
public class PracticeService(AppDbContext db) : IPracticeService
{
    private const int DefaultPairCount = 6;
    private const int MinPairCount = 1;
    private const int MaxPairCount = 8;

    public async Task<Result<PagedResponse<PracticeResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        PracticeKind? kind,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db
            .Practices.AsNoTracking()
            .Include(p => p.Group)
            .Include(p => p.Teachers)
                .ThenInclude(t => t.Teacher!)
                    .ThenInclude(t => t.User)
            .Include(p => p.Days)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(p => p.GroupId == groupId.Value);
        if (teacherId.HasValue)
            query = query.Where(p => p.Teachers.Any(t => t.TeacherId == teacherId.Value));
        if (kind.HasValue)
            query = query.Where(p => p.Kind == kind.Value);
        if (from.HasValue)
            query = query.Where(p => p.DateTo >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(p => p.DateFrom <= to.Value.Date);

        var totalCount = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);

        var items = await query
            .OrderBy(p => p.DateFrom)
            .ThenBy(p => p.Group!.Name)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        return Result<PagedResponse<PracticeResponse>>.Ok(
            new PagedResponse<PracticeResponse>(items.Select(ToDto).ToList(), totalCount, p, ps)
        );
    }

    public async Task<Result<PracticeResponse>> CreateAsync(
        PracticeRequest request,
        CancellationToken ct
    )
    {
        var error = await ValidateAsync(request, null, ct);
        if (error is not null)
            return Result<PracticeResponse>.Fail(error.Value.Message, error.Value.StatusCode);

        var now = DateTime.UtcNow;
        var entity = new Practice
        {
            Id = Guid.NewGuid(),
            Kind = request.Kind,
            Name = request.Name.Trim(),
            GroupId = request.GroupId,
            DateFrom = request.DateFrom.Date,
            DateTo = request.DateTo.Date,
            Note = Normalize(request.Note),
            CreatedAt = now,
            UpdatedAt = now,
        };
        entity.Teachers = BuildTeachers(request.TeacherIds, now);
        entity.Days = request.Kind == PracticeKind.Up ? BuildDays(request.Days, now) : [];

        db.Practices.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<PracticeResponse>.Ok(await ReloadAsync(entity.Id, ct));
    }

    public async Task<Result<PracticeResponse>> UpdateAsync(
        Guid id,
        PracticeRequest request,
        CancellationToken ct
    )
    {
        var entity = await db
            .Practices.Include(p => p.Teachers)
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return Result<PracticeResponse>.Fail("Практика не найдена.", 404);

        var error = await ValidateAsync(request, id, ct);
        if (error is not null)
            return Result<PracticeResponse>.Fail(error.Value.Message, error.Value.StatusCode);

        var now = DateTime.UtcNow;
        entity.Kind = request.Kind;
        entity.Name = request.Name.Trim();
        entity.GroupId = request.GroupId;
        entity.DateFrom = request.DateFrom.Date;
        entity.DateTo = request.DateTo.Date;
        entity.Note = Normalize(request.Note);
        entity.UpdatedAt = now;

        db.PracticeTeachers.RemoveRange(entity.Teachers);
        entity.Teachers = BuildTeachers(request.TeacherIds, now);

        db.PracticeDays.RemoveRange(entity.Days);
        entity.Days = request.Kind == PracticeKind.Up ? BuildDays(request.Days, now) : [];

        await db.SaveChangesAsync(ct);

        return Result<PracticeResponse>.Ok(await ReloadAsync(entity.Id, ct));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Practices.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return Result.Fail("Практика не найдена.", 404);

        db.Practices.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<PracticeImportPreviewResponse>> PreviewImportAsync(
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
            return Result<PracticeImportPreviewResponse>.Fail(
                "Файл не является корректным XLSX. Сохраните файл в формате .xlsx.",
                400
            );
        }

        using (workbook)
        {
            var ws = workbook.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
                return Result<PracticeImportPreviewResponse>.Fail("В файле нет данных.", 400);

            var rows = ParseRows(ws, lastRow);
            var errors = await ValidateImportRowsAsync(rows, ct);

            return Result<PracticeImportPreviewResponse>.Ok(
                new PracticeImportPreviewResponse
                {
                    TotalRows = rows.Count,
                    Rows = rows,
                    Errors = errors,
                }
            );
        }
    }

    public async Task<Result<PracticeImportConfirmResponse>> ConfirmImportAsync(
        PracticeImportConfirmRequest request,
        CancellationToken ct
    )
    {
        if (request.Rows.Count == 0)
            return Result<PracticeImportConfirmResponse>.Fail("Нет строк для импорта.", 400);

        var errors = await ValidateImportRowsAsync(request.Rows, ct);
        if (errors.Count > 0)
            return Result<PracticeImportConfirmResponse>.Fail(
                string.Join("\n", errors.Select(e => e.Message)),
                400
            );

        var groupIds = await ResolveGroupIdsAsync(request.Rows, ct);
        var teacherIds = await ResolveTeacherIdsAsync(request.Rows, ct);

        var now = DateTime.UtcNow;
        var entities = request
            .Rows.Select(row =>
            {
                var kind = ParseKind(row.Kind)!.Value;
                var from = ParseDate(row.DateFrom)!.Value.Date;
                var to = ParseDate(row.DateTo)!.Value.Date;
                var entity = new Practice
                {
                    Id = Guid.NewGuid(),
                    Kind = kind,
                    Name = row.Name.Trim(),
                    GroupId = groupIds[row.GroupName],
                    DateFrom = from,
                    DateTo = to,
                    Note = Normalize(row.Note),
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                entity.Teachers = BuildTeachers(
                    ParseTeacherNames(row.TeacherName)
                        .Select(n => teacherIds[ScheduleImportService.TeacherLookupKey(n)])
                        .Distinct()
                        .ToList(),
                    now
                );
                entity.Days = kind == PracticeKind.Up ? BuildDefaultDays(from, to, now) : [];
                return entity;
            })
            .ToList();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.Practices.AddRange(entities);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var ids = entities.Select(p => p.Id).ToList();
        var saved = await db
            .Practices.AsNoTracking()
            .Include(p => p.Group)
            .Include(p => p.Teachers)
                .ThenInclude(t => t.Teacher!)
                    .ThenInclude(t => t.User)
            .Include(p => p.Days)
            .Where(p => ids.Contains(p.Id))
            .OrderBy(p => p.DateFrom)
            .ToListAsync(ct);

        return Result<PracticeImportConfirmResponse>.Ok(
            new PracticeImportConfirmResponse
            {
                Imported = saved.Count,
                Practices = saved.Select(ToDto).ToList(),
            }
        );
    }

    private async Task<PracticeResponse> ReloadAsync(Guid id, CancellationToken ct)
    {
        var entity = await db
            .Practices.AsNoTracking()
            .Include(p => p.Group)
            .Include(p => p.Teachers)
                .ThenInclude(t => t.Teacher!)
                    .ThenInclude(t => t.User)
            .Include(p => p.Days)
            .FirstAsync(p => p.Id == id, ct);
        return ToDto(entity);
    }

    private static List<PracticeTeacher> BuildTeachers(List<Guid> teacherIds, DateTime now) =>
        teacherIds
            .Distinct()
            .Select(id => new PracticeTeacher
            {
                Id = Guid.NewGuid(),
                TeacherId = id,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

    private static List<PracticeDay> BuildDays(List<PracticeDayRequest>? days, DateTime now) =>
        (days ?? [])
            .Select(d => new PracticeDay
            {
                Id = Guid.NewGuid(),
                Date = d.Date.Date,
                PairCount = d.PairCount,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

    /// <summary>Для УП по умолчанию пары расставляются по учебным дням (Пн–Пт) периода (6 пар в день).</summary>
    private static List<PracticeDay> BuildDefaultDays(DateTime from, DateTime to, DateTime now)
    {
        var days = new List<PracticeDay>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            days.Add(
                new PracticeDay
                {
                    Id = Guid.NewGuid(),
                    Date = date,
                    PairCount = DefaultPairCount,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }
        return days;
    }

    private async Task<(string Message, int StatusCode)?> ValidateAsync(
        PracticeRequest request,
        Guid? excludeId,
        CancellationToken ct
    )
    {
        if (!Enum.IsDefined(request.Kind))
            return ("Вид практики должен быть УП или ПП.", 400);

        if (string.IsNullOrWhiteSpace(request.Name))
            return ("Укажите название практики.", 400);
        if (request.Name.Trim().Length > 100)
            return ("Название практики не должно превышать 100 символов.", 400);

        if (request.DateFrom == default || request.DateTo == default)
            return ("Укажите период практики.", 400);

        if (request.DateFrom.Date > request.DateTo.Date)
            return ("Дата начала практики не может быть позже даты окончания.", 400);

        if (!StudyWeek.IsInSemester(request.DateFrom) || !StudyWeek.IsInSemester(request.DateTo))
            return ("Период практики должен быть в пределах семестра.", 400);

        var group = await db
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null)
            return ("Группа не найдена.", 400);

        var teacherIds = request.TeacherIds.Distinct().ToList();
        if (teacherIds.Count == 0)
            return ("Укажите хотя бы одного преподавателя.", 400);

        var existingTeachers = await db
            .Teachers.AsNoTracking()
            .Where(t => teacherIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);
        if (existingTeachers.Count != teacherIds.Count)
            return ("Преподаватель не найден.", 400);

        if (request.Kind == PracticeKind.Up)
        {
            var days = request.Days ?? [];
            if (days.Count == 0)
                return ("Для учебной практики укажите дни с числом пар.", 400);

            var seenDates = new HashSet<DateTime>();
            foreach (var day in days)
            {
                if (day.Date == default)
                    return ("Укажите дату учебного дня.", 400);

                var date = day.Date.Date;
                if (date < request.DateFrom.Date || date > request.DateTo.Date)
                    return ("Дата учебного дня вне периода практики.", 400);

                if (!StudyWeek.IsInSemester(date))
                    return ("Дата учебного дня должна быть в пределах семестра.", 400);

                if (day.PairCount < MinPairCount || day.PairCount > MaxPairCount)
                    return ("Число пар в дне должно быть от 1 до 8.", 400);

                if (!seenDates.Add(date))
                    return ("Даты учебных дней не должны повторяться.", 400);
            }
        }

        var overlapQuery = db.Practices.Where(p =>
            p.GroupId == request.GroupId
            && p.DateFrom <= request.DateTo.Date
            && p.DateTo >= request.DateFrom.Date
        );
        if (excludeId.HasValue)
            overlapQuery = overlapQuery.Where(p => p.Id != excludeId.Value);

        if (await overlapQuery.AnyAsync(ct))
            return ("У группы уже есть практика в этот период.", 409);

        return null;
    }

    private static List<PracticeImportRow> ParseRows(IXLWorksheet ws, int lastRow)
    {
        var rows = new List<PracticeImportRow>();
        for (var row = 2; row <= lastRow; row++)
        {
            var kind = ws.Cell(row, 1).GetString().Trim();
            var name = ws.Cell(row, 2).GetString().Trim();
            var group = ws.Cell(row, 3).GetString().Trim();
            var dateFrom = CellText(ws.Cell(row, 4));
            var dateTo = CellText(ws.Cell(row, 5));
            var teacher = ws.Cell(row, 6).GetString().Trim();
            var note = ws.Cell(row, 7).GetString().Trim();

            var isEmpty =
                kind.Length == 0
                && name.Length == 0
                && group.Length == 0
                && dateFrom.Length == 0
                && dateTo.Length == 0
                && teacher.Length == 0;
            if (isEmpty)
                continue;

            rows.Add(
                new PracticeImportRow
                {
                    Row = row,
                    Kind = kind,
                    Name = name,
                    GroupName = group,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    TeacherName = teacher,
                    Note = note.Length == 0 ? null : note,
                }
            );
        }
        return rows;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.Value.IsDateTime)
            return cell.Value.GetDateTime().ToString("dd.MM.yyyy");
        return cell.GetString().Trim();
    }

    private async Task<List<ScheduleValidationError>> ValidateImportRowsAsync(
        List<PracticeImportRow> rows,
        CancellationToken ct
    )
    {
        var errors = new List<ScheduleValidationError>();
        var groups = await db.Groups.AsNoTracking().ToDictionaryAsync(g => g.Name, g => g.Id, ct);
        var teachers = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .ToDictionaryAsync(
                t => ScheduleImportService.TeacherLookupKey(t.User.FullName),
                t => t.Id,
                StringComparer.OrdinalIgnoreCase,
                ct
            );

        var pending = new List<PendingImportPeriod>();

        foreach (var row in rows)
        {
            if (ParseKind(row.Kind) is null)
                errors.Add(
                    Error(row.Row, 1, $"строка {row.Row}: укажите вид практики «УП» или «ПП».")
                );

            if (row.Name.Length == 0)
                errors.Add(Error(row.Row, 2, $"строка {row.Row}: не указано название практики."));
            else if (row.Name.Trim().Length > 100)
                errors.Add(
                    Error(
                        row.Row,
                        2,
                        $"строка {row.Row}: название практики не должно превышать 100 символов."
                    )
                );

            Guid? groupId = null;
            if (row.GroupName.Length == 0)
                errors.Add(Error(row.Row, 3, $"строка {row.Row}: не указана группа."));
            else if (!groups.ContainsKey(row.GroupName))
                errors.Add(
                    Error(row.Row, 3, $"строка {row.Row}: группа «{row.GroupName}» не найдена.")
                );
            else
                groupId = groups[row.GroupName];

            var dateFrom = ParseDate(row.DateFrom);
            var dateTo = ParseDate(row.DateTo);
            if (dateFrom is null)
                errors.Add(
                    Error(
                        row.Row,
                        4,
                        $"строка {row.Row}: неверная дата начала (формат дд.мм.гггг)."
                    )
                );
            if (dateTo is null)
                errors.Add(
                    Error(
                        row.Row,
                        5,
                        $"строка {row.Row}: неверная дата окончания (формат дд.мм.гггг)."
                    )
                );
            if (dateFrom is { } from && dateTo is { } to && from.Date > to.Date)
                errors.Add(
                    Error(row.Row, 5, $"строка {row.Row}: дата окончания раньше даты начала.")
                );

            var teacherNames = ParseTeacherNames(row.TeacherName);
            if (teacherNames.Count == 0)
                errors.Add(Error(row.Row, 6, $"строка {row.Row}: не указан преподаватель."));
            else
                foreach (
                    var name in teacherNames.Where(name =>
                        !teachers.ContainsKey(ScheduleImportService.TeacherLookupKey(name))
                    )
                )
                    errors.Add(
                        Error(row.Row, 6, $"строка {row.Row}: преподаватель «{name}» не найден.")
                    );

            if (
                dateFrom is { } fromDate
                && dateTo is { } toDate
                && fromDate.Date <= toDate.Date
                && groupId is { } resolvedGroupId
            )
            {
                if (!StudyWeek.IsInSemester(fromDate) || !StudyWeek.IsInSemester(toDate))
                    errors.Add(
                        Error(
                            row.Row,
                            4,
                            $"строка {row.Row}: период практики должен быть в пределах семестра."
                        )
                    );

                pending.Add(
                    new PendingImportPeriod(row.Row, resolvedGroupId, fromDate.Date, toDate.Date)
                );
            }
        }

        await ValidateImportOverlapsAsync(pending, errors, ct);

        return errors;
    }

    private async Task ValidateImportOverlapsAsync(
        List<PendingImportPeriod> pending,
        List<ScheduleValidationError> errors,
        CancellationToken ct
    )
    {
        if (pending.Count == 0)
            return;

        var groupIds = pending.Select(p => p.GroupId).Distinct().ToList();
        var minFrom = pending.Min(p => p.DateFrom);
        var maxTo = pending.Max(p => p.DateTo);
        var existing = await db
            .Practices.AsNoTracking()
            .Where(p => groupIds.Contains(p.GroupId) && p.DateFrom <= maxTo && p.DateTo >= minFrom)
            .Select(p => new
            {
                p.GroupId,
                p.DateFrom,
                p.DateTo,
            })
            .ToListAsync(ct);

        foreach (var item in pending)
        {
            if (
                existing.Any(e =>
                    e.GroupId == item.GroupId
                    && e.DateFrom.Date <= item.DateTo
                    && e.DateTo.Date >= item.DateFrom
                )
            )
                errors.Add(
                    Error(
                        item.Row,
                        4,
                        $"строка {item.Row}: у группы уже есть практика в этот период."
                    )
                );
        }

        foreach (var groupRows in pending.GroupBy(p => p.GroupId))
        {
            var ordered = groupRows.OrderBy(p => p.DateFrom).ThenBy(p => p.Row).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                for (var j = i + 1; j < ordered.Count; j++)
                {
                    if (ordered[j].DateFrom > ordered[i].DateTo)
                        break;

                    errors.Add(
                        Error(
                            ordered[j].Row,
                            4,
                            $"строка {ordered[j].Row}: период пересекается со строкой {ordered[i].Row}."
                        )
                    );
                }
            }
        }
    }

    private sealed record PendingImportPeriod(
        int Row,
        Guid GroupId,
        DateTime DateFrom,
        DateTime DateTo
    );

    private async Task<Dictionary<string, Guid>> ResolveGroupIdsAsync(
        List<PracticeImportRow> rows,
        CancellationToken ct
    )
    {
        var names = rows.Select(r => r.GroupName).Distinct().ToList();
        return await db
            .Groups.AsNoTracking()
            .Where(g => names.Contains(g.Name))
            .ToDictionaryAsync(g => g.Name, g => g.Id, ct);
    }

    private async Task<Dictionary<string, Guid>> ResolveTeacherIdsAsync(
        List<PracticeImportRow> rows,
        CancellationToken ct
    )
    {
        var keys = rows.SelectMany(r => ParseTeacherNames(r.TeacherName))
            .Select(ScheduleImportService.TeacherLookupKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var teachers = await db.Teachers.AsNoTracking().Include(t => t.User).ToListAsync(ct);
        return teachers
            .Where(t =>
                keys.Contains(
                    ScheduleImportService.TeacherLookupKey(t.User.FullName),
                    StringComparer.OrdinalIgnoreCase
                )
            )
            .ToDictionary(
                t => ScheduleImportService.TeacherLookupKey(t.User.FullName),
                t => t.Id,
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static List<string> ParseTeacherNames(string raw) =>
        raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    private static PracticeKind? ParseKind(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "УП" or "UP" => PracticeKind.Up,
            "ПП" or "PP" => PracticeKind.Pp,
            _ => null,
        };

    private static readonly Regex DatePattern = new(
        @"^\d{1,2}\.\d{1,2}\.\d{4}$",
        RegexOptions.Compiled
    );

    private static DateTime? ParseDate(string value)
    {
        var v = value.Trim();
        if (!DatePattern.IsMatch(v))
            return null;

        return DateTime.TryParseExact(
            v,
            "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date
        )
            ? date
            : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ScheduleValidationError Error(int row, int column, string message) =>
        new()
        {
            Row = row,
            Column = column,
            Level = "data",
            Message = message,
        };

    private static PracticeResponse ToDto(Practice entity)
    {
        var teachers = entity
            .Teachers.OrderBy(t => t.Teacher?.User?.FullName ?? string.Empty)
            .Select(t => new PracticeTeacherResponse
            {
                Id = t.TeacherId,
                Name = t.Teacher?.User?.FullName ?? string.Empty,
            })
            .ToList();

        return new()
        {
            Id = entity.Id,
            Kind = entity.Kind,
            Name = entity.Name,
            GroupId = entity.GroupId,
            GroupName = entity.Group?.Name ?? string.Empty,
            TeacherIds = teachers.Select(t => t.Id).ToList(),
            Teachers = teachers,
            DateFrom = entity.DateFrom,
            DateTo = entity.DateTo,
            Days = entity
                .Days.OrderBy(d => d.Date)
                .Select(d => new PracticeDayDto { Date = d.Date, PairCount = d.PairCount })
                .ToList(),
            Note = entity.Note,
        };
    }
}
