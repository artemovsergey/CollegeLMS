using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Справочник звонков: время пар и большая перемена.</summary>
public class BellScheduleService(AppDbContext db) : IBellScheduleService
{
    private static readonly TimeSpan DayStart = new(7, 0, 0);
    private static readonly TimeSpan DayEnd = new(21, 0, 0);

    public async Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct)
    {
        var slots = await db.BellSlots.AsNoTracking().OrderBy(s => s.NumberPair).ToListAsync(ct);
        var bigBreak = await db.BigBreaks.AsNoTracking().FirstOrDefaultAsync(ct);
        return Result<BellScheduleResponse>.Ok(ToDto(slots, bigBreak));
    }

    public async Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    )
    {
        var error = Validate(request);
        if (error is not null)
            return Result<BellScheduleResponse>.Fail(error, 400);

        var existingSlots = await db.BellSlots.ToListAsync(ct);
        db.BellSlots.RemoveRange(existingSlots);

        var existingBreak = await db.BigBreaks.ToListAsync(ct);
        db.BigBreaks.RemoveRange(existingBreak);

        var now = DateTime.UtcNow;
        var slots = request
            .Slots.OrderBy(s => s.NumberPair)
            .Select(s => new BellSlot
            {
                Id = Guid.NewGuid(),
                NumberPair = s.NumberPair,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();
        db.BellSlots.AddRange(slots);

        BigBreak? bigBreak = null;
        if (request.BigBreak is { } b)
        {
            bigBreak = new BigBreak
            {
                Id = Guid.NewGuid(),
                AfterPair = b.AfterPair,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.BigBreaks.Add(bigBreak);
        }

        await db.SaveChangesAsync(ct);

        return Result<BellScheduleResponse>.Ok(ToDto(slots, bigBreak));
    }

    public async Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        CancellationToken ct
    ) =>
        await db
            .BellSlots.AsNoTracking()
            .ToDictionaryAsync(s => s.NumberPair, s => (s.StartTime, s.EndTime), ct);

    private static string? Validate(UpdateBellScheduleRequest request)
    {
        if (request.Slots.Count == 0)
            return "Укажите хотя бы одну пару в справочнике звонков.";

        if (request.Slots.Count > 8)
            return "В справочнике не может быть больше 8 пар.";

        var duplicates = request
            .Slots.GroupBy(s => s.NumberPair)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicates is not null)
            return $"Номер пары {duplicates.Key} указан несколько раз.";

        foreach (var slot in request.Slots)
        {
            if (slot.NumberPair < 1 || slot.NumberPair > 8)
                return "Номер пары должен быть от 1 до 8.";

            if (slot.StartTime < DayStart || slot.EndTime > DayEnd)
                return "Время пар должно быть в диапазоне 07:00–21:00.";

            if (slot.StartTime >= slot.EndTime)
                return $"Для пары {slot.NumberPair} время начала должно быть раньше времени окончания.";
        }

        var ordered = request.Slots.OrderBy(s => s.NumberPair).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].StartTime < ordered[i - 1].StartTime)
                return "Время пар должно идти по возрастанию.";

            if (ordered[i].StartTime < ordered[i - 1].EndTime)
                return $"Пары {ordered[i - 1].NumberPair} и {ordered[i].NumberPair} пересекаются по времени.";
        }

        if (request.BigBreak is { } bigBreak)
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

    private static BellScheduleResponse ToDto(List<BellSlot> slots, BigBreak? bigBreak) =>
        new()
        {
            Slots = slots
                .OrderBy(s => s.NumberPair)
                .Select(s => new BellSlotResponse
                {
                    Id = s.Id,
                    NumberPair = s.NumberPair,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                })
                .ToList(),
            BigBreak = bigBreak is null
                ? null
                : new BigBreakResponse
                {
                    Id = bigBreak.Id,
                    AfterPair = bigBreak.AfterPair,
                    StartTime = bigBreak.StartTime,
                    EndTime = bigBreak.EndTime,
                },
        };
}
