using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleService(
    AppDbContext db,
    ScheduleExportService exportService,
    ScheduleImportService importService
) : IScheduleService
{
    public async Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DayOfWeek? dayOfWeek,
        string? period,
        string? view,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);

        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);

        if (!string.IsNullOrEmpty(room))
            query = query.Where(s => s.Room == room);

        if (dayOfWeek.HasValue)
            query = query.Where(s => s.DayOfWeek == dayOfWeek.Value);

        var today = DateTime.UtcNow;
        if (period == "day")
            query = query.Where(s => s.DayOfWeek == today.DayOfWeek);

        query = query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime);

        var totalCount = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);
        var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync(ct);

        return Result<PagedResponse<ScheduleResponse>>.Ok(
            new PagedResponse<ScheduleResponse>(
                items.Select(s => s.ToDto()).ToList(),
                totalCount,
                p,
                ps
            )
        );
    }

    public async Task<Result<ScheduleResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entry = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entry is null)
            return Result<ScheduleResponse>.Fail("Запись расписания не найдена", 404);

        return Result<ScheduleResponse>.Ok(entry.ToDto());
    }

    public async Task<Result<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct
    )
    {
        if (request.StartTime >= request.EndTime)
            return Result<ScheduleResponse>.Fail(
                "Время начала должно быть раньше времени окончания",
                400
            );

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<ScheduleResponse>.Fail("Группа не найдена", 404);

        if (request.TeacherId.HasValue)
        {
            var teacherExists = await db.Teachers.AnyAsync(
                t => t.Id == request.TeacherId.Value,
                ct
            );
            if (!teacherExists)
                return Result<ScheduleResponse>.Fail("Преподаватель не найден", 404);
        }

        var overlap = await CheckOverlapAsync(
            null,
            request.GroupId,
            request.TeacherId,
            request.Room,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        var entry = request.ToEntity();
        db.ScheduleEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var saved = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstAsync(s => s.Id == entry.Id, ct);

        return Result<ScheduleResponse>.Ok(saved.ToDto());
    }

    public async Task<Result<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken ct
    )
    {
        if (request.StartTime >= request.EndTime)
            return Result<ScheduleResponse>.Fail(
                "Время начала должно быть раньше времени окончания",
                400
            );

        var entry = await db.ScheduleEntries.FindAsync([id], ct);
        if (entry is null)
            return Result<ScheduleResponse>.Fail("Запись расписания не найдена", 404);

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<ScheduleResponse>.Fail("Группа не найдена", 404);

        if (request.TeacherId.HasValue)
        {
            var teacherExists = await db.Teachers.AnyAsync(
                t => t.Id == request.TeacherId.Value,
                ct
            );
            if (!teacherExists)
                return Result<ScheduleResponse>.Fail("Преподаватель не найден", 404);
        }

        var overlap = await CheckOverlapAsync(
            id,
            request.GroupId,
            request.TeacherId,
            request.Room,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        entry.GroupId = request.GroupId;
        entry.TeacherId = request.TeacherId;
        entry.Subject = request.Subject;
        entry.Room = request.Room;
        entry.DayOfWeek = request.DayOfWeek;
        entry.StartTime = request.StartTime;
        entry.EndTime = request.EndTime;
        entry.LessonType = Enum.Parse<LessonType>(request.LessonType);
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var saved = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstAsync(s => s.Id == entry.Id, ct);

        return Result<ScheduleResponse>.Ok(saved.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.ScheduleEntries.FindAsync([id], ct);
        if (entry is null)
            return Result.Fail("Запись расписания не найдена", 404);

        db.ScheduleEntries.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<ExportResult>> ExportScheduleAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? period,
        ExportFormat format,
        CancellationToken ct
    )
    {
        return await exportService.ExportAsync(groupId, teacherId, room, period, format, ct);
    }

    public async Task<Result<ScheduleImportResult>> ImportScheduleAsync(
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file == null || file.Length == 0)
            return Result<ScheduleImportResult>.Fail("Файл не выбран или пуст", 400);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return Result<ScheduleImportResult>.Fail("Поддерживается только формат XLSX", 400);

        if (file.Length > 10 * 1024 * 1024)
            return Result<ScheduleImportResult>.Fail("Файл слишком большой. Максимум 10MB.", 400);

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        return await importService.ImportAsync(stream, ct);
    }

    public async Task<Result<CalendarResponse>> GetCalendarAsync(
        Guid? groupId, Guid? teacherId, string? room, CancellationToken ct)
    {
        var query = db.ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!).ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue) query = query.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue) query = query.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room)) query = query.Where(s => s.Room == room);

        var entries = await query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync(ct);

        var days = new List<CalendarDayResponse>();
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var dayEntries = entries.Where(s => s.DayOfWeek == day).Select(s => s.ToDto()).ToList();
            if (dayEntries.Count > 0 || groupId.HasValue || teacherId.HasValue || !string.IsNullOrEmpty(room))
            {
                days.Add(new CalendarDayResponse
                {
                    Day = GetRussianDayName(day),
                    DayOfWeek = (int)day,
                    Entries = dayEntries,
                });
            }
        }

        var today = DateTime.UtcNow;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        return Result<CalendarResponse>.Ok(new CalendarResponse
        {
            WeekStart = weekStart,
            Days = days,
        });
    }

    private static string GetRussianDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понедельник",
        DayOfWeek.Tuesday => "Вторник",
        DayOfWeek.Wednesday => "Среда",
        DayOfWeek.Thursday => "Четверг",
        DayOfWeek.Friday => "Пятница",
        DayOfWeek.Saturday => "Суббота",
        DayOfWeek.Sunday => "Воскресенье",
        _ => day.ToString(),
    };

    private async Task<string?> CheckOverlapAsync(
        Guid? excludeId,
        Guid groupId,
        Guid? teacherId,
        string room,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        CancellationToken ct
    )
    {
        var baseQuery = db.ScheduleEntries.Where(s =>
            s.DayOfWeek == dayOfWeek && startTime < s.EndTime && endTime > s.StartTime
        );

        if (excludeId.HasValue)
            baseQuery = baseQuery.Where(s => s.Id != excludeId.Value);

        if (await baseQuery.AnyAsync(s => s.GroupId == groupId, ct))
            return "У группы уже есть занятие в это время";

        if (teacherId.HasValue)
        {
            if (await baseQuery.AnyAsync(s => s.TeacherId == teacherId.Value, ct))
                return "У преподавателя уже есть занятие в это время";
        }

        if (await baseQuery.AnyAsync(s => s.Room == room, ct))
            return "Аудитория уже занята в это время";

        return null;
    }
}
