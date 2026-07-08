using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleService(AppDbContext db) : IScheduleService
{
    public async Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DayOfWeek? dayOfWeek,
        string? period,
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
