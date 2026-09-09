using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DispatcherDashboardService(AppDbContext db) : IDispatcherDashboardService
{
    public async Task<Result<DispatcherDashboardResponse>> GetDailyAsync(
        DateTime date,
        CancellationToken ct
    )
    {
        var day = date.DayOfWeek;
        var week = StudyWeek.ForDate(date);

        var entries = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .Where(s => s.DayOfWeek == day && s.Weeks.Contains(week))
            .OrderBy(s => s.NumberPair)
            .ThenBy(s => s.StartTime)
            .ToListAsync(ct);

        var slots = new List<DispatcherPairSlot>();
        var usedPairs = entries.Select(e => e.NumberPair).Distinct().OrderBy(x => x).ToList();
        foreach (var p in usedPairs)
        {
            var (start, end) = ScheduleImportService.GetPairTime(day, p);
            var slotEntries = entries
                .Where(e => e.NumberPair == p)
                .Select(e => e.ToDispatcherEntry())
                .ToList();
            slots.Add(
                new DispatcherPairSlot
                {
                    NumberPair = p,
                    StartTime = start,
                    EndTime = end,
                    Entries = slotEntries,
                }
            );
        }

        var teachers = entries
            .Where(e => e.TeacherId.HasValue)
            .GroupBy(e => e.TeacherId!.Value)
            .Select(g => new DispatcherTeacherStatus
            {
                TeacherId = g.Key,
                TeacherName = g.First().Teacher?.User?.FullName ?? "—",
                TotalPairs = g.Count(),
                Entries = g.Select(e => e.ToDispatcherEntry()).ToList(),
            })
            .OrderBy(t => t.TeacherName)
            .ToList();

        return Result<DispatcherDashboardResponse>.Ok(
            new DispatcherDashboardResponse
            {
                Date = date,
                Week = week,
                DayOfWeek = (int)day,
                Slots = slots,
                Teachers = teachers,
            }
        );
    }
}
