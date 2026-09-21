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
    /// <summary>Понедельник недели 1 — нижняя граница семестра во всех видах.</summary>
    private static DateTime SemesterMonday => StudyWeek.MondayOf(StudyWeek.SemesterStart);

    /// <summary>Последний день семестра (включительно).</summary>
    private static DateTime SemesterLastDay => SemesterMonday.AddDays(StudyWeek.TotalWeeks * 7 - 1);

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
        var data = await LoadRangeAsync(groupId, teacherId, room, target, target, ct);
        return Result<ScheduleDayViewResponse>.Ok(BuildDay(data, target, week));
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
        var monday = SemesterMonday.AddDays((effectiveWeek - 1) * 7);
        var data = await LoadRangeAsync(groupId, teacherId, room, monday, monday.AddDays(5), ct);

        var days = new List<ScheduleDayViewResponse>();
        for (var i = 0; i < 6; i++) // Пн–Сб
            days.Add(BuildDay(data, monday.AddDays(i), effectiveWeek));
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

        var days = new List<ScheduleMonthDayResponse>();
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var week = StudyWeek.WeekOf(date);
            var nwd = nonWorking.FirstOrDefault(d => d.DateFrom <= date && d.DateTo >= date);
            var coversPractice = practiceList
                .Where(p => p.DateFrom <= date && p.DateTo >= date)
                .ToList();
            var isSunday = date.DayOfWeek == DayOfWeek.Sunday;
            var isOutOfSemester = date < SemesterMonday || date > SemesterLastDay;
            var pairCount =
                (isSunday || nwd is not null || coversPractice.Count > 0 || isOutOfSemester)
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
                    IsOutOfSemester = isOutOfSemester,
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

        var data = await LoadRangeAsync(
            groupId,
            teacherId,
            null,
            SemesterMonday,
            SemesterLastDay,
            ct
        );

        var weeks = new List<ScheduleSemesterWeekResponse>();
        for (var week = 1; week <= StudyWeek.TotalWeeks; week++)
        {
            var weekStart = SemesterMonday.AddDays((week - 1) * 7);
            var days = new List<ScheduleDayViewResponse>();
            for (var i = 0; i < 6; i++)
                days.Add(BuildDay(data, weekStart.AddDays(i), week));
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

    /// <summary>Однократная загрузка слоёв на диапазон дат (без запросов на каждый день).</summary>
    private async Task<ScheduleRangeData> LoadRangeAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime from,
        DateTime to,
        CancellationToken ct
    )
    {
        var nonWorking = await db
            .NonWorkingDays.AsNoTracking()
            .Where(d => d.DateFrom <= to && d.DateTo >= from)
            .ToListAsync(ct);

        var practiceResult = await practices.GetAllAsync(
            groupId,
            teacherId,
            null,
            from,
            to,
            1,
            100,
            ct
        );
        var practiceList = practiceResult.Data?.Items ?? [];

        var entriesQuery = db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();
        if (groupId.HasValue)
            entriesQuery = entriesQuery.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue)
            entriesQuery = entriesQuery.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room))
            entriesQuery = entriesQuery.Where(s => s.Room == room);

        var entries = await entriesQuery.OrderBy(s => s.NumberPair).ToListAsync(ct);
        var changeTags = await ScheduleChangeTags.BuildAsync(db, entries, null, ct);
        var bellTimes = await bells.GetTimeMapAsync(ct);

        int? course = null;
        if (groupId.HasValue)
            course = await db
                .Groups.AsNoTracking()
                .Where(g => g.Id == groupId.Value)
                .Select(g => (int?)g.Course)
                .FirstOrDefaultAsync(ct);
        var insertResult = await inserts.GetAllAsync(null, course, activeOnly: true, ct);
        var insertsByDay = (insertResult.Data ?? [])
            .GroupBy(i => (DayOfWeek)i.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new ScheduleRangeData(
            nonWorking,
            practiceList,
            entries,
            changeTags,
            bellTimes,
            insertsByDay
        );
    }

    /// <summary>Сборка дня из предзагруженных слоёв (без обращений к БД).</summary>
    private static ScheduleDayViewResponse BuildDay(
        ScheduleRangeData data,
        DateTime target,
        int week
    )
    {
        if (target.DayOfWeek == DayOfWeek.Sunday)
            return Empty(target, week, isSunday: true);

        var nonWorking = data.NonWorking.FirstOrDefault(d =>
            d.DateFrom <= target && d.DateTo >= target
        );
        if (nonWorking is not null)
            return new ScheduleDayViewResponse
            {
                Date = target,
                Week = week,
                DayOfWeek = (int)target.DayOfWeek,
                IsNonWorking = true,
                NonWorkingTitle = nonWorking.Title,
            };

        if (target < SemesterMonday || target > SemesterLastDay)
            return new ScheduleDayViewResponse
            {
                Date = target,
                Week = week,
                DayOfWeek = (int)target.DayOfWeek,
            };

        var practiceList = data
            .Practices.Where(p => p.DateFrom <= target && p.DateTo >= target)
            .ToList();
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
            Inserts = data.InsertsByDay.GetValueOrDefault(target.DayOfWeek) ?? [],
            Entries = BuildEntries(data, target, week),
        };
    }

    private static List<ScheduleResponse> BuildEntries(
        ScheduleRangeData data,
        DateTime target,
        int week
    )
    {
        return data
            .Entries.Where(e => e.DayOfWeek == target.DayOfWeek && e.Weeks.Contains(week))
            .Select(e =>
            {
                var tags = data
                    .ChangeTags.GetValueOrDefault((e.GroupId, e.DayOfWeek, e.NumberPair))
                    ?.Where(t => t.Week == week)
                    .ToList();
                var dto = e.ToDto(tags);
                if (data.BellTimes.TryGetValue(e.NumberPair, out var time))
                {
                    dto.StartTime = time.Start;
                    dto.EndTime = time.End;
                }
                return dto;
            })
            .ToList();
    }

    private static ScheduleDayViewResponse Empty(DateTime target, int week, bool isSunday) =>
        new()
        {
            Date = target,
            Week = week,
            DayOfWeek = (int)target.DayOfWeek,
            IsSunday = isSunday,
        };

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

    private sealed record ScheduleRangeData(
        List<NonWorkingDay> NonWorking,
        List<PracticeResponse> Practices,
        List<ScheduleEntry> Entries,
        Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>> ChangeTags,
        Dictionary<int, (TimeSpan Start, TimeSpan End)> BellTimes,
        Dictionary<DayOfWeek, List<ScheduleInsertResponse>> InsertsByDay
    );
}
