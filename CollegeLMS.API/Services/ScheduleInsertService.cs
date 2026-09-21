using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Специальные события в расписании (не занимают номер пары).</summary>
public class ScheduleInsertService(AppDbContext db) : IScheduleInsertService
{
    public async Task<Result<List<ScheduleInsertResponse>>> GetAllAsync(
        DayOfWeek? dayOfWeek,
        int? course,
        bool activeOnly,
        CancellationToken ct
    )
    {
        var query = db.ScheduleInserts.AsNoTracking().AsQueryable();

        if (dayOfWeek.HasValue)
            query = query.Where(i => i.DayOfWeek == dayOfWeek.Value);
        if (course.HasValue)
            query = query.Where(i => i.Course == null || i.Course == course.Value);
        if (activeOnly)
            query = query.Where(i => i.IsActive);

        var items = await query.OrderBy(i => i.DayOfWeek).ThenBy(i => i.StartTime).ToListAsync(ct);

        return Result<List<ScheduleInsertResponse>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<Result<ScheduleInsertResponse>> CreateAsync(
        ScheduleInsertRequest request,
        CancellationToken ct
    )
    {
        var error = Validate(request);
        if (error is not null)
            return Result<ScheduleInsertResponse>.Fail(error, 400);

        var entity = new ScheduleInsert
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Course = request.Course,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.ScheduleInserts.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<ScheduleInsertResponse>.Ok(ToDto(entity));
    }

    public async Task<Result<ScheduleInsertResponse>> UpdateAsync(
        Guid id,
        ScheduleInsertRequest request,
        CancellationToken ct
    )
    {
        var entity = await db.ScheduleInserts.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return Result<ScheduleInsertResponse>.Fail("Событие не найдено.", 404);

        var error = Validate(request);
        if (error is not null)
            return Result<ScheduleInsertResponse>.Fail(error, 400);

        entity.Title = request.Title.Trim();
        entity.DayOfWeek = request.DayOfWeek;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Course = request.Course;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<ScheduleInsertResponse>.Ok(ToDto(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ScheduleInserts.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return Result.Fail("Событие не найдено.", 404);

        db.ScheduleInserts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static string? Validate(ScheduleInsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "Укажите название события.";

        if (request.Title.Trim().Length > 200)
            return "Название события не должно превышать 200 символов.";

        if (request.DayOfWeek is DayOfWeek.Sunday)
            return "Событие не может быть на воскресенье.";

        if (request.StartTime >= request.EndTime)
            return "Время начала события должно быть раньше времени окончания.";

        if (request.Course is < 1 or > 4)
            return "Курс должен быть от 1 до 4.";

        return null;
    }

    private static ScheduleInsertResponse ToDto(ScheduleInsert entity) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            DayOfWeek = (int)entity.DayOfWeek,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Course = entity.Course,
            IsActive = entity.IsActive,
        };
}
