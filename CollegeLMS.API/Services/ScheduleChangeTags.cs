using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Построитель бейджей корректировок для пар расписания.</summary>
internal static class ScheduleChangeTags
{
    public static async Task<
        Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>>
    > BuildAsync(AppDbContext db, List<ScheduleEntry> items, int? week, CancellationToken ct)
    {
        var groupIds = items.Select(s => s.GroupId).Distinct().ToList();
        if (groupIds.Count == 0)
            return new Dictionary<(Guid, DayOfWeek, int), List<ChangeTag>>();

        var historyQuery = db
            .ScheduleHistory.AsNoTracking()
            .Where(h => groupIds.Contains(h.GroupId));

        if (week.HasValue)
            historyQuery = historyQuery.Where(h => h.Week == week.Value);

        var history = await historyQuery.ToListAsync(ct);

        // Дедупликация: Add+Remove за одну неделю на одном слоте аннулируют друг друга
        var filtered = history
            .GroupBy(h => (h.GroupId, h.DayOfWeek, h.NumberPair))
            .Select(g =>
            {
                var sorted = g.OrderBy(h => h.AppliedAt).ToList();
                var remaining = new List<ScheduleHistory>();
                var consumed = new HashSet<int>();

                for (var i = 0; i < sorted.Count; i++)
                {
                    if (consumed.Contains(i))
                        continue;

                    var h = sorted[i];
                    if (h.ChangeType == Entities.Enums.ScheduleChangeType.Add)
                    {
                        var matchingRemove = sorted
                            .Select((x, idx) => (x, idx))
                            .FirstOrDefault(pair =>
                                !consumed.Contains(pair.idx)
                                && pair.idx > i
                                && pair.x.ChangeType == Entities.Enums.ScheduleChangeType.Remove
                                && pair.x.Week == h.Week
                            );

                        if (matchingRemove.x is not null)
                        {
                            consumed.Add(i);
                            consumed.Add(matchingRemove.idx);
                            continue;
                        }
                    }
                    remaining.Add(h);
                }

                return (g.Key, remaining);
            })
            .ToList();

        return filtered
            .Where(x => x.remaining.Count > 0)
            .ToDictionary(
                x => x.Key,
                x =>
                    x.remaining.Select(h => new ChangeTag
                        {
                            ChangeType = h.ChangeType,
                            Week = h.Week,
                            RemovedNumberPair = h.RemovedNumberPair,
                            RemovedSubject = h.RemovedSubject,
                            Note = h.Note,
                        })
                        .ToList()
            );
    }
}
