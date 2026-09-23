using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Практики УП/ПП: CRUD и фильтры.</summary>
public class PracticeService(AppDbContext db) : IPracticeService
{
    private const int MinPairNumber = 1;
    private const int MaxPairNumber = 8;
    private const int MaxRoomLength = 20;

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
            new PagedResponse<PracticeResponse>(
                items.Select(item => item.ToDto()).ToList(),
                totalCount,
                p,
                ps
            )
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
            Room = Normalize(request.Room),
            Subgroup = request.Subgroup,
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
        entity.Room = Normalize(request.Room);
        entity.Subgroup = request.Subgroup;
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
        return entity.ToDto();
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
                PairNumbers = (d.PairNumbers ?? []).Distinct().OrderBy(n => n).ToArray(),
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

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

        if (!string.IsNullOrWhiteSpace(request.Room) && request.Room.Trim().Length > MaxRoomLength)
            return ($"Номер кабинета не должен превышать {MaxRoomLength} символов.", 400);

        if (request.Subgroup is <= 0)
            return ("Номер подгруппы должен быть положительным числом.", 400);

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
                return ("Для учебной практики укажите дни с номерами пар.", 400);

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

                var numbers = day.PairNumbers ?? [];
                if (numbers.Count == 0)
                    return ("Укажите хотя бы одну пару в учебном дне.", 400);

                if (numbers.Any(n => n < MinPairNumber || n > MaxPairNumber))
                    return ("Номера пар должны быть от 1 до 8.", 400);

                if (numbers.Distinct().Count() != numbers.Count)
                    return ("Номера пар в дне не должны повторяться.", 400);

                if (!seenDates.Add(date))
                    return ("Даты учебных дней не должны повторяться.", 400);
            }
        }

        // Подгруппы одной УП-группы хранятся отдельными практиками с одинаковым периодом,
        // поэтому «родственные» УП с идентичным периодом не считаются пересечением.
        var overlapQuery = db.Practices.Where(p =>
            p.GroupId == request.GroupId
            && p.DateFrom <= request.DateTo.Date
            && p.DateTo >= request.DateFrom.Date
            && !(
                p.Kind == PracticeKind.Up
                && request.Kind == PracticeKind.Up
                && p.DateFrom == request.DateFrom.Date
                && p.DateTo == request.DateTo.Date
            )
        );
        if (excludeId.HasValue)
            overlapQuery = overlapQuery.Where(p => p.Id != excludeId.Value);

        if (await overlapQuery.AnyAsync(ct))
            return ("У группы уже есть практика в этот период.", 409);

        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
