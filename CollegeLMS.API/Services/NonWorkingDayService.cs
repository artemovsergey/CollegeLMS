using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Нерабочие даты: праздники и выходные.</summary>
public class NonWorkingDayService(AppDbContext db) : INonWorkingDayService
{
    public async Task<Result<PagedResponse<NonWorkingDayResponse>>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db.NonWorkingDays.AsNoTracking().AsQueryable();

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
            .Select(d => new NonWorkingDayResponse
            {
                Id = d.Id,
                DateFrom = d.DateFrom,
                DateTo = d.DateTo,
                Title = d.Title,
            })
            .ToListAsync(ct);

        return Result<PagedResponse<NonWorkingDayResponse>>.Ok(
            new PagedResponse<NonWorkingDayResponse>(items, totalCount, p, ps)
        );
    }

    public async Task<Result<NonWorkingDayResponse>> CreateAsync(
        NonWorkingDayRequest request,
        CancellationToken ct
    )
    {
        var error = Validate(request);
        if (error is not null)
            return Result<NonWorkingDayResponse>.Fail(error, 400);

        var entity = new NonWorkingDay
        {
            Id = Guid.NewGuid(),
            DateFrom = request.DateFrom.Date,
            DateTo = request.DateTo.Date,
            Title = request.Title.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.NonWorkingDays.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<NonWorkingDayResponse>.Ok(ToDto(entity));
    }

    public async Task<Result<NonWorkingDayResponse>> UpdateAsync(
        Guid id,
        NonWorkingDayRequest request,
        CancellationToken ct
    )
    {
        var entity = await db.NonWorkingDays.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result<NonWorkingDayResponse>.Fail("Нерабочая дата не найдена.", 404);

        var error = Validate(request);
        if (error is not null)
            return Result<NonWorkingDayResponse>.Fail(error, 400);

        entity.DateFrom = request.DateFrom.Date;
        entity.DateTo = request.DateTo.Date;
        entity.Title = request.Title.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<NonWorkingDayResponse>.Ok(ToDto(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.NonWorkingDays.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result.Fail("Нерабочая дата не найдена.", 404);

        db.NonWorkingDays.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static string? Validate(NonWorkingDayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "Укажите название нерабочего дня.";

        if (request.Title.Trim().Length > 200)
            return "Название нерабочего дня не должно превышать 200 символов.";

        if (request.DateFrom == default || request.DateTo == default)
            return "Укажите период нерабочего дня.";

        if (request.DateFrom.Date > request.DateTo.Date)
            return "Дата начала не может быть позже даты окончания.";

        return null;
    }

    private static NonWorkingDayResponse ToDto(NonWorkingDay entity) =>
        new()
        {
            Id = entity.Id,
            DateFrom = entity.DateFrom,
            DateTo = entity.DateTo,
            Title = entity.Title,
        };
}
