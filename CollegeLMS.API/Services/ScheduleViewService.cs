using System.Globalization;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
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
        var rangeResult = await LoadRangeAsync(groupId, teacherId, room, target, target, ct);
        if (!rangeResult.IsSuccess)
            return Result<ScheduleDayViewResponse>.Fail(
                rangeResult.ErrorMessage ?? "Ошибка загрузки данных",
                rangeResult.StatusCode
            );
        return Result<ScheduleDayViewResponse>.Ok(BuildDay(rangeResult.Data!, target, week));
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
        var rangeResult = await LoadRangeAsync(
            groupId,
            teacherId,
            room,
            monday,
            monday.AddDays(6),
            ct
        );
        if (!rangeResult.IsSuccess)
            return Result<ScheduleWeekViewResponse>.Fail(
                rangeResult.ErrorMessage ?? "Ошибка загрузки данных",
                rangeResult.StatusCode
            );
        var data = rangeResult.Data!;

        var days = new List<ScheduleDayViewResponse>();
        for (var i = 0; i < 7; i++)
        {
            var target = monday.AddDays(i);
            if (!ShouldIncludeDay(data, target, effectiveWeek))
                continue;
            days.Add(BuildDay(data, target, effectiveWeek));
        }

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
        var workingDays = await db
            .WorkingDayOverrides.AsNoTracking()
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
        if (!practiceResult.IsSuccess)
            return Result<ScheduleMonthViewResponse>.Fail(
                practiceResult.ErrorMessage ?? "Ошибка загрузки данных",
                practiceResult.StatusCode
            );
        var practiceList = practiceResult.Data?.Items ?? [];
        var entries = await GetMonthEntriesAsync(groupId, teacherId, room, ct); // все записи фильтра, без недельного среза

        var days = new List<ScheduleMonthDayResponse>();
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var week = StudyWeek.WeekOf(date);
            var nwd = nonWorking.FirstOrDefault(d => d.DateFrom <= date && d.DateTo >= date);
            var overrideDay = workingDays.FirstOrDefault(d =>
                d.DateFrom.Date <= date && d.DateTo.Date >= date
            );
            var coversPractice = practiceList
                .Where(p => p.DateFrom <= date && p.DateTo >= date)
                .ToList();
            var isSunday = date.DayOfWeek == DayOfWeek.Sunday && overrideDay is null;
            var isOutOfSemester = date < SemesterMonday || date > SemesterLastDay;
            var effectiveDay = overrideDay?.SubstituteDayOfWeek is int s and >= 1 and <= 7
                ? (DayOfWeek)s
                : date.DayOfWeek;
            var upDay = coversPractice
                .Where(p => p.Kind == PracticeKind.Up)
                .SelectMany(p => p.Days)
                .FirstOrDefault(d => d.Date.Date == date);
            var pairCount =
                upDay is not null && !isSunday && nwd is null && !isOutOfSemester
                    ? upDay.PairNumbers.Count
                    : (
                        isSunday || nwd is not null || coversPractice.Count > 0 || isOutOfSemester
                            ? 0
                            : entries.Count(e =>
                                e.DayOfWeek == effectiveDay && e.Weeks.Contains(week)
                            )
                    );
            days.Add(
                new ScheduleMonthDayResponse
                {
                    Date = date,
                    DayOfWeek = (int)date.DayOfWeek,
                    IsSunday = isSunday,
                    IsNonWorking = nwd is not null,
                    NonWorkingTitle = nwd?.Title,
                    IsWorkingDay = overrideDay is not null,
                    SubstituteDayOfWeek = overrideDay?.SubstituteDayOfWeek,
                    WorkingDayTitle = overrideDay?.Title,
                    PracticeKinds = coversPractice.Select(p => p.Kind).Distinct().ToList(),
                    PracticeName = coversPractice
                        .FirstOrDefault(p => p.Kind == PracticeKind.Up)
                        ?.Name,
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
        if (!data.IsSuccess)
            return Result<ScheduleSemesterViewResponse>.Fail(
                data.ErrorMessage ?? "Ошибка загрузки данных",
                data.StatusCode
            );

        var weeks = new List<ScheduleSemesterWeekResponse>();
        for (var week = 1; week <= StudyWeek.TotalWeeks; week++)
        {
            var weekStart = SemesterMonday.AddDays((week - 1) * 7);
            var days = new List<ScheduleDayViewResponse>();
            for (var i = 0; i < 7; i++)
            {
                var target = weekStart.AddDays(i);
                if (!ShouldIncludeDay(data.Data!, target, week))
                    continue;
                days.Add(BuildDay(data.Data!, target, week));
            }
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
    private async Task<Result<ScheduleRangeData>> LoadRangeAsync(
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

        var workingDays = await db
            .WorkingDayOverrides.AsNoTracking()
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
        if (!practiceResult.IsSuccess)
            return Result<ScheduleRangeData>.Fail(
                practiceResult.ErrorMessage ?? "Ошибка загрузки данных",
                practiceResult.StatusCode
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

        // Резолв профиля звонков на каждую дату диапазона (кэш сервиса звонков делает это дешёвым).
        var bellByDate = new Dictionary<DateTime, BellDayInfo>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var substitute = workingDays
                .FirstOrDefault(d => d.DateFrom.Date <= date && d.DateTo.Date >= date)
                ?.SubstituteDayOfWeek;
            var resolved = await bells.GetResolvedAsync(date, substitute, ct);
            var profile = resolved.Data;
            bellByDate[date] = new BellDayInfo(
                profile is null
                    ? new()
                    : profile.Slots.ToDictionary(s => s.NumberPair, s => (s.StartTime, s.EndTime)),
                profile?.BigBreak
            );
        }

        int? course = null;
        if (groupId.HasValue)
            course = await db
                .Groups.AsNoTracking()
                .Where(g => g.Id == groupId.Value)
                .Select(g => (int?)g.Course)
                .FirstOrDefaultAsync(ct);
        var insertResult = await inserts.GetAllAsync(null, course, activeOnly: true, ct);
        if (!insertResult.IsSuccess)
            return Result<ScheduleRangeData>.Fail(
                insertResult.ErrorMessage ?? "Ошибка загрузки данных",
                insertResult.StatusCode
            );
        var insertsByDay = (insertResult.Data ?? [])
            .GroupBy(i => (DayOfWeek)i.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Result<ScheduleRangeData>.Ok(
            new ScheduleRangeData(
                nonWorking,
                workingDays,
                practiceList,
                entries,
                changeTags,
                bellByDate,
                insertsByDay
            )
        );
    }

    /// <summary>Показывать ли день недели: Пн–Пт всегда, Сб — при override или контенте, Вс — только при override.</summary>
    private static bool ShouldIncludeDay(ScheduleRangeData data, DateTime target, int week)
    {
        if (target.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
            return true;

        if (HasWorkingOverride(data, target))
            return true;

        if (target.DayOfWeek == DayOfWeek.Sunday)
            return false;

        if (data.Practices.Any(p => p.DateFrom <= target && p.DateTo >= target))
            return true;

        if (data.Entries.Any(e => e.DayOfWeek == target.DayOfWeek && e.Weeks.Contains(week)))
            return true;

        return data.InsertsByDay.TryGetValue(target.DayOfWeek, out var inserts)
            && inserts.Count > 0;
    }

    private static WorkingDayOverride? FindWorkingOverride(
        ScheduleRangeData data,
        DateTime target
    ) => data.WorkingDays.FirstOrDefault(d => d.DateFrom.Date <= target && d.DateTo.Date >= target);

    private static bool HasWorkingOverride(ScheduleRangeData data, DateTime target) =>
        FindWorkingOverride(data, target) is not null;

    /// <summary>Сборка дня из предзагруженных слоёв (без обращений к БД).</summary>
    private static ScheduleDayViewResponse BuildDay(
        ScheduleRangeData data,
        DateTime target,
        int week
    )
    {
        var workingDay = FindWorkingOverride(data, target);
        if (workingDay is not null)
        {
            var effectiveDay = workingDay.SubstituteDayOfWeek is int s and >= 1 and <= 7
                ? (DayOfWeek)s
                : target.DayOfWeek;
            var bell = data.BellByDate.GetValueOrDefault(target.Date);
            var practicesToday = data
                .Practices.Where(p => p.DateFrom <= target && p.DateTo >= target)
                .ToList();

            return new ScheduleDayViewResponse
            {
                Date = target,
                Week = week,
                DayOfWeek = (int)target.DayOfWeek,
                IsWorkingDay = true,
                SubstituteDayOfWeek = workingDay.SubstituteDayOfWeek,
                WorkingDayTitle = workingDay.Title,
                BigBreak = bell?.BigBreak,
                Practices = practicesToday,
                Inserts =
                    practicesToday.Count > 0
                        ? []
                        : data.InsertsByDay.GetValueOrDefault(effectiveDay) ?? [],
                Entries =
                    practicesToday.Count > 0
                        ? PracticeEntries(data, target, week, practicesToday)
                        : BuildEntries(data, target, week, effectiveDay),
            };
        }

        var nonWorking = data.NonWorking.FirstOrDefault(d =>
            d.DateFrom.Date <= target && d.DateTo.Date >= target
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

        if (target.DayOfWeek == DayOfWeek.Sunday)
            return Empty(target, week, isSunday: true);

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
                Entries = PracticeEntries(data, target, week, practiceList),
            };

        var dayBell = data.BellByDate.GetValueOrDefault(target.Date);
        return new ScheduleDayViewResponse
        {
            Date = target,
            Week = week,
            DayOfWeek = (int)target.DayOfWeek,
            Inserts = data.InsertsByDay.GetValueOrDefault(target.DayOfWeek) ?? [],
            Entries = BuildEntries(data, target, week, target.DayOfWeek),
            BigBreak = dayBell?.BigBreak,
        };
    }

    private static List<ScheduleResponse> BuildEntries(
        ScheduleRangeData data,
        DateTime target,
        int week,
        DayOfWeek effectiveDay
    )
    {
        var times = data.BellByDate.GetValueOrDefault(target.Date)?.Times ?? new();

        return data
            .Entries.Where(e => e.DayOfWeek == effectiveDay && e.Weeks.Contains(week))
            .Select(e =>
            {
                var tags = data
                    .ChangeTags.GetValueOrDefault((e.GroupId, e.DayOfWeek, e.NumberPair))
                    ?.Where(t => t.Week == week)
                    .ToList();
                var dto = e.ToDto(tags);
                if (times.TryGetValue(e.NumberPair, out var time))
                {
                    dto.StartTime = time.Start;
                    dto.EndTime = time.End;
                }
                return dto;
            })
            .ToList();
    }

    /// <summary>Пары УП: синтезируются по PracticeDay (номера из PairNumbers, время из звонков дня).</summary>
    private static List<ScheduleResponse> PracticeEntries(
        ScheduleRangeData data,
        DateTime target,
        int week,
        List<PracticeResponse> practicesToday
    )
    {
        var times = data.BellByDate.GetValueOrDefault(target.Date)?.Times ?? new();
        var entries = new List<ScheduleResponse>();

        foreach (var practice in practicesToday.Where(p => p.Kind == PracticeKind.Up))
        {
            var day = practice.Days.FirstOrDefault(d => d.Date.Date == target.Date);
            if (day is null)
                continue;

            var teacherName = string.Join(
                ", ",
                practice.Teachers.Select(t => t.Name).Where(n => n.Length > 0)
            );

            foreach (var number in day.PairNumbers)
            {
                var entry = new ScheduleResponse
                {
                    Id = Guid.NewGuid(),
                    GroupId = practice.GroupId,
                    GroupName = practice.GroupName,
                    TeacherId = practice.TeacherIds.Count > 0 ? practice.TeacherIds[0] : null,
                    TeacherName = teacherName.Length == 0 ? null : teacherName,
                    Subject = practice.Name,
                    Room = string.Empty,
                    DayOfWeek = (int)target.DayOfWeek,
                    NumberPair = number,
                    Weeks = [week],
                    LessonType = nameof(LessonType.Practice),
                    IsPractice = true,
                    PracticeName = practice.Name,
                };

                if (times.TryGetValue(number, out var time))
                {
                    entry.StartTime = time.Start;
                    entry.EndTime = time.End;
                }

                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.NumberPair).ToList();
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
        List<WorkingDayOverride> WorkingDays,
        List<PracticeResponse> Practices,
        List<ScheduleEntry> Entries,
        Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>> ChangeTags,
        Dictionary<DateTime, BellDayInfo> BellByDate,
        Dictionary<DayOfWeek, List<ScheduleInsertResponse>> InsertsByDay
    );

    private sealed record BellDayInfo(
        Dictionary<int, (TimeSpan Start, TimeSpan End)> Times,
        BigBreakResponse? BigBreak
    );
}
