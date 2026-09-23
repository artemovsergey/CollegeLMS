using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class LiveDashboardService(AppDbContext db, IBellScheduleService bells, TimeZoneInfo tz)
    : ILiveDashboardService
{
    public async Task<Result<LiveDashboardResponse>> GetLiveAsync(
        DateTime? date,
        DateTime? at,
        CancellationToken ct
    )
    {
        var now = at ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var target = (date ?? now.Date).Date;
        var week = StudyWeek.ForDate(target);
        var dayOfWeek = target.DayOfWeek;

        var overrideDay = await db
            .WorkingDayOverrides.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom.Date <= target && d.DateTo.Date >= target, ct);
        var nonWorking = await db
            .NonWorkingDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom.Date <= target && d.DateTo.Date >= target, ct);

        var isWorkingDay =
            overrideDay is not null || (dayOfWeek != DayOfWeek.Sunday && nonWorking is null);
        var effectiveDay = overrideDay?.SubstituteDayOfWeek is int s and >= 1 and <= 7
            ? (DayOfWeek)s
            : dayOfWeek;

        var groups = new Dictionary<Guid, EntityAccumulator>();
        var teachers = new Dictionary<Guid, EntityAccumulator>();

        foreach (var group in await db.Groups.AsNoTracking().ToListAsync(ct))
            GetOrCreate(groups, group.Id, group.Name);

        foreach (
            var teacher in await db.Teachers.AsNoTracking().Include(t => t.User).ToListAsync(ct)
        )
            GetOrCreate(teachers, teacher.Id, teacher.User?.FullName);

        if (isWorkingDay)
        {
            var resolved = await bells.GetResolvedAsync(
                target,
                overrideDay?.SubstituteDayOfWeek,
                ct
            );
            var times = BuildTimes(resolved.Data);

            var entries = await db
                .ScheduleEntries.AsNoTracking()
                .Include(s => s.Group)
                .Include(s => s.Teacher!)
                    .ThenInclude(t => t.User)
                .Where(s => s.DayOfWeek == effectiveDay && s.Weeks.Contains(week))
                .ToListAsync(ct);

            foreach (var e in entries)
            {
                var (start, end) = ResolveTime(times, e.NumberPair, e.StartTime, e.EndTime);
                var entry = new LiveEntry
                {
                    GroupId = e.GroupId,
                    GroupName = e.Group?.Name ?? string.Empty,
                    Subject = e.Subject,
                    Room = e.Room,
                    NumberPair = e.NumberPair,
                    StartTime = start,
                    EndTime = end,
                    LessonType = e.LessonType.ToString(),
                    IsPractice = false,
                    PracticeName = null,
                    TeacherId = e.TeacherId,
                    TeacherName = e.Teacher?.User?.FullName,
                };

                GetOrCreate(groups, e.GroupId, e.Group?.Name).Entries.Add(entry);

                if (e.TeacherId is Guid teacherId)
                    GetOrCreate(teachers, teacherId, e.Teacher?.User?.FullName).Entries.Add(entry);
            }

            var practices = await db
                .Practices.AsNoTracking()
                .Include(p => p.Group)
                .Include(p => p.Teachers)
                    .ThenInclude(t => t.Teacher!)
                        .ThenInclude(t => t.User)
                .Include(p => p.Days)
                .Where(p => p.DateFrom <= target && p.DateTo >= target)
                .ToListAsync(ct);

            foreach (var practice in practices)
            {
                var group = GetOrCreate(groups, practice.GroupId, practice.Group?.Name);
                var practiceTeachers = practice
                    .Teachers.Select(t => (Id: t.TeacherId, Name: t.Teacher?.User?.FullName))
                    .ToList();

                foreach (var teacher in practiceTeachers)
                    GetOrCreate(teachers, teacher.Id, teacher.Name);

                var day = practice.Days.FirstOrDefault(d => d.Date.Date == target);
                if (practice.Kind != PracticeKind.Up || day is null)
                    continue;

                var teacherName = string.Join(
                    ", ",
                    practiceTeachers.Select(t => t.Name).Where(n => !string.IsNullOrEmpty(n))
                );
                var firstTeacherId =
                    practiceTeachers.Count > 0 ? practiceTeachers[0].Id : (Guid?)null;
                var firstTeacher = firstTeacherId is Guid id
                    ? teachers.GetValueOrDefault(id)
                    : null;

                foreach (var number in day.PairNumbers)
                {
                    var (start, end) = ResolveTime(times, number, TimeSpan.Zero, TimeSpan.Zero);
                    var entry = new LiveEntry
                    {
                        GroupId = practice.GroupId,
                        GroupName = practice.Group?.Name ?? string.Empty,
                        Subject = practice.Name,
                        Room = string.Empty,
                        NumberPair = number,
                        StartTime = start,
                        EndTime = end,
                        LessonType = nameof(LessonType.Practice),
                        IsPractice = true,
                        PracticeName = practice.Name,
                        TeacherId = firstTeacherId,
                        TeacherName = teacherName.Length == 0 ? null : teacherName,
                    };

                    group.Entries.Add(entry);
                    firstTeacher?.Entries.Add(entry);
                }
            }
        }

        var teacherItems = teachers
            .Values.Select(a => BuildStatus(a, now.TimeOfDay))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var groupItems = groups
            .Values.Select(a => BuildStatus(a, now.TimeOfDay))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var all = teacherItems.Concat(groupItems).ToList();

        return Result<LiveDashboardResponse>.Ok(
            new LiveDashboardResponse
            {
                Now = now.TimeOfDay,
                Date = target,
                Week = week,
                DayOfWeek = (int)dayOfWeek,
                IsWorkingDay = isWorkingDay,
                IsNonWorking = nonWorking is not null,
                WorkingDayTitle = overrideDay?.Title,
                NonWorkingTitle = nonWorking?.Title,
                Counts = new LiveCounts
                {
                    InLesson = all.Count(x => x.Status == LiveLessonStatus.InLesson),
                    Finished = all.Count(x => x.Status == LiveLessonStatus.Finished),
                    NoPairs = all.Count(x => x.Status == LiveLessonStatus.NoPairs),
                    Waiting = all.Count(x => x.Status == LiveLessonStatus.Waiting),
                },
                Teachers = teacherItems,
                Groups = groupItems,
            }
        );
    }

    private static Dictionary<int, (TimeSpan Start, TimeSpan End)> BuildTimes(
        BellProfileResponse? profile
    ) =>
        profile is null
            ? new Dictionary<int, (TimeSpan Start, TimeSpan End)>()
            : profile.Slots.ToDictionary(s => s.NumberPair, s => (s.StartTime, s.EndTime));

    private static (TimeSpan Start, TimeSpan End) ResolveTime(
        Dictionary<int, (TimeSpan Start, TimeSpan End)> times,
        int numberPair,
        TimeSpan fallbackStart,
        TimeSpan fallbackEnd
    ) => times.TryGetValue(numberPair, out var time) ? time : (fallbackStart, fallbackEnd);

    private static EntityAccumulator GetOrCreate(
        Dictionary<Guid, EntityAccumulator> map,
        Guid id,
        string? name
    )
    {
        if (!map.TryGetValue(id, out var accumulator))
        {
            accumulator = new EntityAccumulator { Id = id, Name = name ?? string.Empty };
            map[id] = accumulator;
        }
        else if (string.IsNullOrEmpty(accumulator.Name) && !string.IsNullOrEmpty(name))
        {
            accumulator.Name = name;
        }

        return accumulator;
    }

    private static LiveEntityStatus BuildStatus(EntityAccumulator accumulator, TimeSpan now)
    {
        var entries = accumulator
            .Entries.OrderBy(e => e.NumberPair)
            .ThenBy(e => e.StartTime)
            .ToList();

        var current = entries
            .Where(e => e.StartTime <= now && now <= e.EndTime)
            .OrderBy(e => e.NumberPair)
            .FirstOrDefault();
        var next = entries
            .Where(e => e.StartTime > now)
            .OrderBy(e => e.StartTime)
            .ThenBy(e => e.NumberPair)
            .FirstOrDefault();

        var status =
            entries.Count == 0 ? LiveLessonStatus.NoPairs
            : current is not null ? LiveLessonStatus.InLesson
            : next is not null ? LiveLessonStatus.Waiting
            : LiveLessonStatus.Finished;

        return new LiveEntityStatus
        {
            Id = accumulator.Id,
            Name = accumulator.Name,
            Status = status,
            TotalPairs = entries.Count,
            CurrentPair = current is null ? null : ToPairInfo(current),
            NextPair = next is null ? null : ToPairInfo(next),
            Entries = entries,
        };
    }

    private static LivePairInfo ToPairInfo(LiveEntry entry) =>
        new()
        {
            NumberPair = entry.NumberPair,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
        };

    private sealed class EntityAccumulator
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public List<LiveEntry> Entries { get; } = [];
    }
}
