using System.Globalization;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Серверные виды расписания: день, неделя, календарь месяца, семестр.</summary>
public class ScheduleViewService(
    AppDbContext db,
    IBellScheduleService bells,
    IPracticeService practices,
    IScheduleInsertService inserts
) : IScheduleViewService
{
    public async Task<Result<ScheduleDayViewResponse>> GetDayAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime? date,
        CancellationToken ct
    )
    {
        var target = (date ?? DateTime.UtcNow).Date;
        var week = Math.Clamp(StudyWeek.WeekOf(target), 1, StudyWeek.TotalWeeks);
        return Result<ScheduleDayViewResponse>.Ok(
            await BuildDayAsync(groupId, teacherId, room, target, week, ct)
        );
    }

    public async Task<Result<ScheduleWeekViewResponse>> GetWeekAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        int? week,
        DateTime? date,
        CancellationToken ct
    )
    {
        var derived = week ?? StudyWeek.WeekOf(date ?? DateTime.UtcNow);
        var effectiveWeek = Math.Clamp(derived, 1, StudyWeek.TotalWeeks);
        var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays((effectiveWeek - 1) * 7);
        var days = new List<ScheduleDayViewResponse>();
        for (var i = 0; i < 6; i++) // Пн–Сб
            days.Add(
                await BuildDayAsync(groupId, teacherId, room, monday.AddDays(i), effectiveWeek, ct)
            );
        return Result<ScheduleWeekViewResponse>.Ok(
            new ScheduleWeekViewResponse
            {
                Week = effectiveWeek,
                WeekStart = monday,
                Days = days,
            }
        );
    }

    public async Task<Result<ScheduleMonthViewResponse>> GetMonthAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? month,
        CancellationToken ct
    )
    {
        DateTime first;
        if (string.IsNullOrWhiteSpace(month))
            first = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        else if (
            !DateTime.TryParseExact(
                month + "-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out first
            )
        )
            return Result<ScheduleMonthViewResponse>.Fail("Неверный формат месяца.", 400);

        var last = first.AddMonths(1).AddDays(-1);
        var nonWorking = await db
            .NonWorkingDays.AsNoTracking()
            .Where(d => d.DateFrom <= last && d.DateTo >= first)
            .ToListAsync(ct);
        var practiceResult = await practices.GetAllAsync(
            groupId,
            teacherId,
            null,
            first,
            last,
            1,
            100,
            ct
        );
        var practiceList = practiceResult.Data?.Items ?? [];
        var entries = await GetMonthEntriesAsync(groupId, teacherId, room, ct); // все записи фильтра, без недельного среза
        var semesterEnd = StudyWeek
            .MondayOf(StudyWeek.SemesterStart)
            .AddDays(StudyWeek.TotalWeeks * 7 - 1);

        var days = new List<ScheduleMonthDayResponse>();
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var week = StudyWeek.WeekOf(date);
            var nwd = nonWorking.FirstOrDefault(d => d.DateFrom <= date && d.DateTo >= date);
            var coversPractice = practiceList
                .Where(p => p.DateFrom <= date && p.DateTo >= date)
                .ToList();
            var isSunday = date.DayOfWeek == DayOfWeek.Sunday;
            var pairCount =
                (isSunday || nwd is not null || coversPractice.Count > 0)
                    ? 0
                    : entries.Count(e => e.DayOfWeek == date.DayOfWeek && e.Weeks.Contains(week));
            days.Add(
                new ScheduleMonthDayResponse
                {
                    Date = date,
                    DayOfWeek = (int)date.DayOfWeek,
                    IsSunday = isSunday,
                    IsNonWorking = nwd is not null,
                    NonWorkingTitle = nwd?.Title,
                    PracticeKinds = coversPractice.Select(p => p.Kind).Distinct().ToList(),
                    IsOutOfSemester = date < StudyWeek.SemesterStart.Date || date > semesterEnd,
                    PairCount = pairCount,
                }
            );
        }
        return Result<ScheduleMonthViewResponse>.Ok(
            new ScheduleMonthViewResponse
            {
                Year = first.Year,
                Month = first.Month,
                Days = days,
            }
        );
    }

    public async Task<Result<ScheduleSemesterViewResponse>> GetSemesterAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        if (groupId.HasValue == teacherId.HasValue)
            return Result<ScheduleSemesterViewResponse>.Fail(
                "Укажите одну группу или одного преподавателя.",
                400
            );

        var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        var weeks = new List<ScheduleSemesterWeekResponse>();
        for (var week = 1; week <= StudyWeek.TotalWeeks; week++)
        {
            var weekStart = monday.AddDays((week - 1) * 7);
            var days = new List<ScheduleDayViewResponse>();
            for (var i = 0; i < 6; i++)
                days.Add(
                    await BuildDayAsync(groupId, teacherId, null, weekStart.AddDays(i), week, ct)
                );
            weeks.Add(
                new ScheduleSemesterWeekResponse
                {
                    Week = week,
                    WeekStart = weekStart,
                    Days = days,
                }
            );
        }
        return Result<ScheduleSemesterViewResponse>.Ok(
            new ScheduleSemesterViewResponse { TotalWeeks = StudyWeek.TotalWeeks, Weeks = weeks }
        );
    }

    private async Task<ScheduleDayViewResponse> BuildDayAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime target,
        int week,
        CancellationToken ct
    )
    {
        if (target.DayOfWeek == DayOfWeek.Sunday)
            return Empty(target, week, isSunday: true);

        var nonWorking = await db
            .NonWorkingDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom <= target && d.DateTo >= target, ct);
        if (nonWorking is not null)
            return new ScheduleDayViewResponse
            {
                Date = target,
                Week = week,
                DayOfWeek = (int)target.DayOfWeek,
                IsNonWorking = true,
                NonWorkingTitle = nonWorking.Title,
            };

        var practiceResult = await practices.GetAllAsync(
            groupId,
            teacherId,
            null,
            target,
            target,
            1,
            50,
            ct
        );
        var practiceList = practiceResult.Data?.Items ?? [];
        if (practiceList.Count > 0)
            return new ScheduleDayViewResponse
            {
                Date = target,
                Week = week,
                DayOfWeek = (int)target.DayOfWeek,
                Practices = practiceList,
            };

        return new ScheduleDayViewResponse
        {
            Date = target,
            Week = week,
            DayOfWeek = (int)target.DayOfWeek,
            Inserts = await GetInsertsAsync(target.DayOfWeek, groupId, ct),
            Entries = await GetEntriesAsync(groupId, teacherId, room, target, week, ct),
        };
    }

    private static ScheduleDayViewResponse Empty(DateTime target, int week, bool isSunday) =>
        new()
        {
            Date = target,
            Week = week,
            DayOfWeek = (int)target.DayOfWeek,
            IsSunday = isSunday,
        };

    private async Task<List<ScheduleInsertResponse>> GetInsertsAsync(
        DayOfWeek day,
        Guid? groupId,
        CancellationToken ct
    )
    {
        int? course = null;
        if (groupId.HasValue)
            course = await db
                .Groups.AsNoTracking()
                .Where(g => g.Id == groupId.Value)
                .Select(g => (int?)g.Course)
                .FirstOrDefaultAsync(ct);
        var result = await inserts.GetAllAsync(day, course, activeOnly: true, ct);
        return result.Data ?? [];
    }

    private async Task<List<ScheduleResponse>> GetEntriesAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime date,
        int week,
        CancellationToken ct
    )
    {
        var query = db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .Where(s =>
                s.DayOfWeek == date.DayOfWeek
                && s.Weeks.Contains(week)
                && date >= StudyWeek.MondayOf(StudyWeek.SemesterStart)
                && date
                    < StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(StudyWeek.TotalWeeks * 7)
            );
        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room))
            query = query.Where(s => s.Room == room);

        var items = await query.OrderBy(s => s.NumberPair).ToListAsync(ct);
        var tags = await ScheduleChangeTags.BuildAsync(db, items, week, ct);
        var bellTimes = await bells.GetTimeMapAsync(ct);
        return items
            .Select(s =>
            {
                var dto = s.ToDto(tags.GetValueOrDefault((s.GroupId, s.DayOfWeek, s.NumberPair)));
                if (bellTimes.TryGetValue(s.NumberPair, out var time))
                {
                    dto.StartTime = time.Start;
                    dto.EndTime = time.End;
                }
                return dto;
            })
            .ToList();
    }

    private async Task<List<ScheduleEntry>> GetMonthEntriesAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        CancellationToken ct
    )
    {
        var query = db.ScheduleEntries.AsNoTracking().AsQueryable();
        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room))
            query = query.Where(s => s.Room == room);

        return await query.ToListAsync(ct);
    }
}
