using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Профили звонков: пары и большая перемена с привязкой к дням недели и датам.</summary>
public class BellScheduleService(AppDbContext db) : IBellScheduleService
{
    private const string DefaultProfileName = "Обычный";

    private static readonly TimeSpan DayStart = new(7, 0, 0);
    private static readonly TimeSpan DayEnd = new(21, 0, 0);

    // Кэш справочника на время жизни scoped-сервиса: резолв на сотни дат без лишних запросов.
    private List<BellProfile>? _profiles;
    private List<BellProfileDate>? _profileDates;
    private List<BellSlot>? _slots;
    private List<BigBreak>? _bigBreaks;

    public async Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct)
    {
        await EnsureCacheAsync(ct);
        var profile = DefaultProfile();
        return Result<BellScheduleResponse>.Ok(
            profile is null ? new BellScheduleResponse() : ToScheduleDto(profile.Id)
        );
    }

    public async Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    )
    {
        var error = ValidateSlots(request.Slots, request.BigBreak);
        if (error is not null)
            return Result<BellScheduleResponse>.Fail(error, 400);

        var now = DateTime.UtcNow;
        var profile = await db.BellProfiles.FirstOrDefaultAsync(p => p.IsDefault, ct);
        if (profile is null)
        {
            profile = new BellProfile
            {
                Id = Guid.NewGuid(),
                Name = DefaultProfileName,
                IsDefault = true,
                DaysOfWeek = [],
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.BellProfiles.Add(profile);
        }

        var existingSlots = await db
            .BellSlots.Where(s => s.ProfileId == profile.Id)
            .ToListAsync(ct);
        db.BellSlots.RemoveRange(existingSlots);
        var existingBreaks = await db
            .BigBreaks.Where(b => b.ProfileId == profile.Id)
            .ToListAsync(ct);
        db.BigBreaks.RemoveRange(existingBreaks);

        var slots = request
            .Slots.OrderBy(s => s.NumberPair)
            .Select(s => new BellSlot
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                NumberPair = s.NumberPair,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();
        db.BellSlots.AddRange(slots);

        if (request.BigBreak is { } b)
        {
            db.BigBreaks.Add(
                new BigBreak
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    AfterPair = b.AfterPair,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        await db.SaveChangesAsync(ct);
        InvalidateCache();

        await EnsureCacheAsync(ct);
        return Result<BellScheduleResponse>.Ok(ToScheduleDto(profile.Id));
    }

    public async Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DayOfWeek day,
        CancellationToken ct
    )
    {
        await EnsureCacheAsync(ct);
        return BuildTimeMap(ResolveByDayOfWeek(ToIso(day)));
    }

    public async Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    )
    {
        await EnsureCacheAsync(ct);
        return BuildTimeMap(ResolveProfile(date, substituteDayOfWeek));
    }

    public async Task<Result<List<BellProfileResponse>>> GetProfilesAsync(CancellationToken ct)
    {
        await EnsureCacheAsync(ct);
        var profiles = _profiles!
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToProfileDto)
            .ToList();
        return Result<List<BellProfileResponse>>.Ok(profiles);
    }

    public async Task<Result<BellProfileResponse>> CreateProfileAsync(
        BellProfileRequest request,
        CancellationToken ct
    )
    {
        var error = ValidateProfile(request);
        if (error is not null)
            return Result<BellProfileResponse>.Fail(error, 400);

        var name = request.Name.Trim();
        var nameTaken = await db.BellProfiles.AnyAsync(p => p.Name == name, ct);
        if (nameTaken)
            return Result<BellProfileResponse>.Fail(
                "Профиль с таким названием уже существует.",
                400
            );

        var now = DateTime.UtcNow;
        var profile = new BellProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDefault = false,
            DaysOfWeek = request.DaysOfWeek.Distinct().OrderBy(d => d).ToArray(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.BellProfiles.Add(profile);
        AddChildren(profile.Id, request, now);
        await db.SaveChangesAsync(ct);
        InvalidateCache();

        await EnsureCacheAsync(ct);
        return Result<BellProfileResponse>.Ok(ToProfileDto(profile));
    }

    public async Task<Result<BellProfileResponse>> UpdateProfileAsync(
        Guid id,
        BellProfileRequest request,
        CancellationToken ct
    )
    {
        var profile = await db.BellProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null)
            return Result<BellProfileResponse>.Fail("Профиль звонков не найден.", 404);

        var error = ValidateProfile(request);
        if (error is not null)
            return Result<BellProfileResponse>.Fail(error, 400);

        var name = request.Name.Trim();
        var nameTaken = await db.BellProfiles.AnyAsync(p => p.Name == name && p.Id != id, ct);
        if (nameTaken)
            return Result<BellProfileResponse>.Fail(
                "Профиль с таким названием уже существует.",
                400
            );

        var now = DateTime.UtcNow;
        profile.Name = name;
        profile.DaysOfWeek = request.DaysOfWeek.Distinct().OrderBy(d => d).ToArray();
        profile.UpdatedAt = now;

        var existingSlots = await db.BellSlots.Where(s => s.ProfileId == id).ToListAsync(ct);
        db.BellSlots.RemoveRange(existingSlots);
        var existingBreaks = await db.BigBreaks.Where(b => b.ProfileId == id).ToListAsync(ct);
        db.BigBreaks.RemoveRange(existingBreaks);
        var existingDates = await db.BellProfileDates.Where(d => d.ProfileId == id).ToListAsync(ct);
        db.BellProfileDates.RemoveRange(existingDates);

        AddChildren(id, request, now);
        await db.SaveChangesAsync(ct);
        InvalidateCache();

        await EnsureCacheAsync(ct);
        return Result<BellProfileResponse>.Ok(ToProfileDto(profile));
    }

    public async Task<Result> DeleteProfileAsync(Guid id, CancellationToken ct)
    {
        var profile = await db.BellProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null)
            return Result.Fail("Профиль звонков не найден.", 404);

        if (profile.IsDefault)
            return Result.Fail("Базовый профиль удалить нельзя.", 400);

        var slots = await db.BellSlots.Where(s => s.ProfileId == id).ToListAsync(ct);
        db.BellSlots.RemoveRange(slots);
        var breaks = await db.BigBreaks.Where(b => b.ProfileId == id).ToListAsync(ct);
        db.BigBreaks.RemoveRange(breaks);
        var dates = await db.BellProfileDates.Where(d => d.ProfileId == id).ToListAsync(ct);
        db.BellProfileDates.RemoveRange(dates);
        db.BellProfiles.Remove(profile);
        await db.SaveChangesAsync(ct);
        InvalidateCache();

        return Result.Ok();
    }

    public Task<Result<BellProfileResponse>> GetResolvedAsync(
        DateTime date,
        CancellationToken ct
    ) => GetResolvedAsync(date, null, ct);

    public async Task<Result<BellProfileResponse>> GetResolvedAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    )
    {
        await EnsureCacheAsync(ct);
        var profile = ResolveProfile(date, substituteDayOfWeek);
        return Result<BellProfileResponse>.Ok(
            profile is null ? new BellProfileResponse() : ToProfileDto(profile)
        );
    }

    // ─────────────────────────── Резолв профиля ───────────────────────────

    private BellProfile? DefaultProfile() => _profiles!.FirstOrDefault(p => p.IsDefault);

    /// <summary>Профиль по дню недели (1=Пн … 7=Вс) с откатом на базовый.</summary>
    private BellProfile? ResolveByDayOfWeek(int isoDay)
    {
        var byDay = _profiles!
            .Where(p => !p.IsDefault)
            .FirstOrDefault(p => p.DaysOfWeek.Contains(isoDay));
        return byDay ?? DefaultProfile();
    }

    /// <summary>Приоритет: диапазон даты → подмена дня недели → день недели даты → базовый.</summary>
    private BellProfile? ResolveProfile(DateTime date, int? substituteDayOfWeek)
    {
        var target = date.Date;
        var byDate = _profileDates!
            .Where(d => d.DateFrom.Date <= target && d.DateTo.Date >= target)
            .OrderBy(d => d.DateFrom)
            .Select(d => _profiles!.FirstOrDefault(p => p.Id == d.ProfileId))
            .FirstOrDefault(p => p is not null);
        if (byDate is not null)
            return byDate;

        if (substituteDayOfWeek is >= 1 and <= 7)
        {
            var bySubstitute = _profiles!
                .Where(p => !p.IsDefault)
                .FirstOrDefault(p => p.DaysOfWeek.Contains(substituteDayOfWeek.Value));
            if (bySubstitute is not null)
                return bySubstitute;
        }

        return ResolveByDayOfWeek(ToIso(date.DayOfWeek));
    }

    private Dictionary<int, (TimeSpan Start, TimeSpan End)> BuildTimeMap(BellProfile? profile)
    {
        if (profile is null)
            return new();

        return _slots!
            .Where(s => s.ProfileId == profile.Id)
            .ToDictionary(s => s.NumberPair, s => (s.StartTime, s.EndTime));
    }

    private static int ToIso(DayOfWeek day) => day == DayOfWeek.Sunday ? 7 : (int)day;

    // ─────────────────────────── Запись дочерних сущностей ───────────────────────────

    private void AddChildren(Guid profileId, BellProfileRequest request, DateTime now)
    {
        foreach (var s in request.Slots.OrderBy(s => s.NumberPair))
        {
            db.BellSlots.Add(
                new BellSlot
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    NumberPair = s.NumberPair,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        if (request.BigBreak is { } b)
        {
            db.BigBreaks.Add(
                new BigBreak
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    AfterPair = b.AfterPair,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        foreach (var d in request.Dates)
        {
            db.BellProfileDates.Add(
                new BellProfileDate
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    DateFrom = d.DateFrom.Date,
                    DateTo = d.DateTo.Date,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }
    }

    // ─────────────────────────── Валидация ───────────────────────────

    private static string? ValidateProfile(BellProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Укажите название профиля звонков.";

        if (request.Name.Trim().Length > 100)
            return "Название профиля не должно превышать 100 символов.";

        var daysError = ValidateDays(request.DaysOfWeek);
        if (daysError is not null)
            return daysError;

        var slotsError = ValidateSlots(request.Slots, request.BigBreak);
        if (slotsError is not null)
            return slotsError;

        foreach (var d in request.Dates)
        {
            if (d.DateFrom == default || d.DateTo == default)
                return "Укажите период действия профиля звонков.";

            if (d.DateFrom.Date > d.DateTo.Date)
                return "Дата начала не может быть позже даты окончания.";
        }

        return null;
    }

    private static string? ValidateDays(List<int> days)
    {
        if (days.Any(d => d is < 1 or > 7))
            return "День недели должен быть от 1 (Пн) до 7 (Вс).";

        if (days.Distinct().Count() != days.Count)
            return "Дни недели не должны повторяться.";

        return null;
    }

    private static string? ValidateSlots(List<BellSlotRequest> slots, BigBreakRequest? bigBreak)
    {
        if (slots.Count == 0)
            return "Укажите хотя бы одну пару в справочнике звонков.";

        if (slots.Count > 8)
            return "В справочнике не может быть больше 8 пар.";

        var duplicates = slots.GroupBy(s => s.NumberPair).FirstOrDefault(g => g.Count() > 1);
        if (duplicates is not null)
            return $"Номер пары {duplicates.Key} указан несколько раз.";

        foreach (var slot in slots)
        {
            if (slot.NumberPair < 1 || slot.NumberPair > 8)
                return "Номер пары должен быть от 1 до 8.";

            if (slot.StartTime < DayStart || slot.EndTime > DayEnd)
                return "Время пар должно быть в диапазоне 07:00–21:00.";

            if (slot.StartTime >= slot.EndTime)
                return $"Для пары {slot.NumberPair} время начала должно быть раньше времени окончания.";
        }

        var ordered = slots.OrderBy(s => s.NumberPair).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].StartTime < ordered[i - 1].StartTime)
                return "Время пар должно идти по возрастанию.";

            if (ordered[i].StartTime < ordered[i - 1].EndTime)
                return $"Пары {ordered[i - 1].NumberPair} и {ordered[i].NumberPair} пересекаются по времени.";
        }

        if (bigBreak is not null)
        {
            if (bigBreak.AfterPair < 1 || bigBreak.AfterPair > 8)
                return "Номер пары для большой перемены должен быть от 1 до 8.";

            if (bigBreak.StartTime < DayStart || bigBreak.EndTime > DayEnd)
                return "Время большой перемены должно быть в диапазоне 07:00–21:00.";

            if (bigBreak.StartTime >= bigBreak.EndTime)
                return "Время начала большой перемены должно быть раньше времени окончания.";
        }

        return null;
    }

    // ─────────────────────────── Маппинг ───────────────────────────

    private BellScheduleResponse ToScheduleDto(Guid profileId) =>
        new()
        {
            Slots = _slots!
                .Where(s => s.ProfileId == profileId)
                .OrderBy(s => s.NumberPair)
                .Select(ToSlotDto)
                .ToList(),
            BigBreak = _bigBreaks!.FirstOrDefault(b => b.ProfileId == profileId) is { } b
                ? ToBigBreakDto(b)
                : null,
        };

    private BellProfileResponse ToProfileDto(BellProfile profile) =>
        new()
        {
            Id = profile.Id,
            Name = profile.Name,
            IsDefault = profile.IsDefault,
            DaysOfWeek = profile.DaysOfWeek.ToList(),
            Slots = _slots!
                .Where(s => s.ProfileId == profile.Id)
                .OrderBy(s => s.NumberPair)
                .Select(ToSlotDto)
                .ToList(),
            BigBreak = _bigBreaks!.FirstOrDefault(b => b.ProfileId == profile.Id) is { } b
                ? ToBigBreakDto(b)
                : null,
            Dates = _profileDates!
                .Where(d => d.ProfileId == profile.Id)
                .OrderBy(d => d.DateFrom)
                .Select(d => new BellProfileDateResponse
                {
                    Id = d.Id,
                    DateFrom = d.DateFrom,
                    DateTo = d.DateTo,
                })
                .ToList(),
        };

    private static BellSlotResponse ToSlotDto(BellSlot slot) =>
        new()
        {
            Id = slot.Id,
            NumberPair = slot.NumberPair,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
        };

    private static BigBreakResponse ToBigBreakDto(BigBreak bigBreak) =>
        new()
        {
            Id = bigBreak.Id,
            AfterPair = bigBreak.AfterPair,
            StartTime = bigBreak.StartTime,
            EndTime = bigBreak.EndTime,
        };

    // ─────────────────────────── Кэш ───────────────────────────

    private async Task EnsureCacheAsync(CancellationToken ct)
    {
        if (_profiles is not null)
            return;

        _profiles = await db.BellProfiles.AsNoTracking().ToListAsync(ct);
        _profileDates = await db.BellProfileDates.AsNoTracking().ToListAsync(ct);
        _slots = await db.BellSlots.AsNoTracking().ToListAsync(ct);
        _bigBreaks = await db.BigBreaks.AsNoTracking().ToListAsync(ct);
    }

    private void InvalidateCache()
    {
        _profiles = null;
        _profileDates = null;
        _slots = null;
        _bigBreaks = null;
    }
}
