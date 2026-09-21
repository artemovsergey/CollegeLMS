using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Рабочие дни (рабочие субботы и переносы) — зеркало нерабочих дней.</summary>
public class WorkingDayService(AppDbContext db) : IWorkingDayService
{
    private const string NonWorkingConflictMessage = "Дата уже отмечена как нерабочая.";

    public async Task<Result<PagedResponse<WorkingDayResponse>>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db.WorkingDayOverrides.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(d => d.DateTo >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(d => d.DateFrom <= to.Value.Date);

        var totalCount = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);

        var items = await query
            .OrderBy(d => d.DateFrom)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(d => new WorkingDayResponse
            {
                Id = d.Id,
                DateFrom = d.DateFrom,
                DateTo = d.DateTo,
                SubstituteDayOfWeek = d.SubstituteDayOfWeek,
                Title = d.Title,
            })
            .ToListAsync(ct);

        return Result<PagedResponse<WorkingDayResponse>>.Ok(
            new PagedResponse<WorkingDayResponse>(items, totalCount, p, ps)
        );
    }

    public async Task<Result<WorkingDayResponse>> CreateAsync(
        WorkingDayRequest request,
        CancellationToken ct
    )
    {
        var error = Validate(request);
        if (error is not null)
            return Result<WorkingDayResponse>.Fail(error, 400);

        var conflict = await FindNonWorkingConflictAsync(
            request.DateFrom.Date,
            request.DateTo.Date,
            ct
        );
        if (conflict is not null)
            return Result<WorkingDayResponse>.Fail(NonWorkingConflictMessage, 409);

        var entity = new WorkingDayOverride
        {
            Id = Guid.NewGuid(),
            DateFrom = request.DateFrom.Date,
            DateTo = request.DateTo.Date,
            SubstituteDayOfWeek = request.SubstituteDayOfWeek,
            Title = request.Title.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.WorkingDayOverrides.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<WorkingDayResponse>.Ok(ToDto(entity));
    }

    public async Task<Result<WorkingDayResponse>> UpdateAsync(
        Guid id,
        WorkingDayRequest request,
        CancellationToken ct
    )
    {
        var entity = await db.WorkingDayOverrides.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result<WorkingDayResponse>.Fail("Рабочий день не найден.", 404);

        var error = Validate(request);
        if (error is not null)
            return Result<WorkingDayResponse>.Fail(error, 400);

        var conflict = await FindNonWorkingConflictAsync(
            request.DateFrom.Date,
            request.DateTo.Date,
            ct
        );
        if (conflict is not null)
            return Result<WorkingDayResponse>.Fail(NonWorkingConflictMessage, 409);

        entity.DateFrom = request.DateFrom.Date;
        entity.DateTo = request.DateTo.Date;
        entity.SubstituteDayOfWeek = request.SubstituteDayOfWeek;
        entity.Title = request.Title.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<WorkingDayResponse>.Ok(ToDto(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.WorkingDayOverrides.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result.Fail("Рабочий день не найден.", 404);

        db.WorkingDayOverrides.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    /// <summary>Пересечение диапазона с нерабочим днём.</summary>
    private async Task<NonWorkingDay?> FindNonWorkingConflictAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct
    ) =>
        await db
            .NonWorkingDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom <= to && d.DateTo >= from, ct);

    private static string? Validate(WorkingDayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "Укажите название рабочего дня.";

        if (request.Title.Trim().Length > 200)
            return "Название рабочего дня не должно превышать 200 символов.";

        if (request.DateFrom == default || request.DateTo == default)
            return "Укажите период рабочего дня.";

        if (request.DateFrom.Date > request.DateTo.Date)
            return "Дата начала не может быть позже даты окончания.";

        if (request.SubstituteDayOfWeek is { } day && (day < 1 || day > 5))
            return "День недели для подмены должен быть от 1 (Пн) до 5 (Пт).";

        return null;
    }

    private static WorkingDayResponse ToDto(WorkingDayOverride entity) =>
        new()
        {
            Id = entity.Id,
            DateFrom = entity.DateFrom,
            DateTo = entity.DateTo,
            SubstituteDayOfWeek = entity.SubstituteDayOfWeek,
            Title = entity.Title,
        };
}
