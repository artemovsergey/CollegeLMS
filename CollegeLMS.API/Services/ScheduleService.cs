using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleService(
    AppDbContext db,
    ScheduleExportService exportService,
    IBellScheduleService bells
) : IScheduleService
{
    public async Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DayOfWeek? dayOfWeek,
        string? period,
        int? week,
        DateTime? date,
        string? view,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        if (date.HasValue)
        {
            week = StudyWeek.WeekOf(date.Value);
            dayOfWeek = date.Value.DayOfWeek;

            // Нерабочий день: пары не выводятся (отображается сообщением на клиенте).
            var nonWorking = await db
                .NonWorkingDays.AsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.DateFrom <= date.Value.Date && d.DateTo >= date.Value.Date,
                    ct
                );
            if (nonWorking is not null)
                return Result<PagedResponse<ScheduleResponse>>.Ok(
                    new PagedResponse<ScheduleResponse>([], 0, 1, 20)
                );
        }

        var query = db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);

        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);

        if (!string.IsNullOrEmpty(room))
            query = query.Where(s => s.Room == room);

        if (dayOfWeek.HasValue)
            query = query.Where(s => s.DayOfWeek == dayOfWeek.Value);

        if (week.HasValue)
            query = query.Where(s => s.Weeks.Contains(week.Value));

        var today = DateTime.UtcNow;
        if (period == "day")
            query = query.Where(s => s.DayOfWeek == today.DayOfWeek);

        query = query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.NumberPair).ThenBy(s => s.StartTime);

        var totalCount = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 2000);
        var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync(ct);

        var changeTagsBySlot = await ScheduleChangeTags.BuildAsync(db, items, week, ct);
        var bellByDay =
            new Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>();
        foreach (var day in items.Select(s => s.DayOfWeek).Distinct())
            bellByDay[day] = await bells.GetTimeMapAsync(day, ct);

        var dtos = items
            .Select(s =>
            {
                var dto = s.ToDto(
                    changeTagsBySlot.GetValueOrDefault((s.GroupId, s.DayOfWeek, s.NumberPair))
                );
                if (bellByDay[s.DayOfWeek].TryGetValue(s.NumberPair, out var time))
                {
                    dto.StartTime = time.Start;
                    dto.EndTime = time.End;
                }
                return dto;
            })
            .ToList();

        return Result<PagedResponse<ScheduleResponse>>.Ok(
            new PagedResponse<ScheduleResponse>(dtos, totalCount, p, ps)
        );
    }

    public async Task<Result<ScheduleMetaResponse>> GetMetaAsync(CancellationToken ct)
    {
        return Result<ScheduleMetaResponse>.Ok(
            new ScheduleMetaResponse
            {
                SemesterStart = StudyWeek.SemesterStart,
                TotalWeeks = StudyWeek.TotalWeeks,
                CurrentWeek = StudyWeek.WeekOf(DateTime.UtcNow),
                CurrentDate = DateTime.UtcNow.Date,
            }
        );
    }

    public async Task<Result<ScheduleContextResponse>> GetContextAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var student = await db
            .Students.AsNoTracking()
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student is not null)
        {
            return Result<ScheduleContextResponse>.Ok(
                new ScheduleContextResponse
                {
                    GroupId = student.GroupId,
                    GroupName = student.Group?.Name ?? string.Empty,
                    Role = "Student",
                }
            );
        }

        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher is not null)
        {
            return Result<ScheduleContextResponse>.Ok(
                new ScheduleContextResponse
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.User.FullName,
                    Role = "Teacher",
                }
            );
        }

        return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse { Role = "Other" });
    }

    public async Task<Result<ScheduleContextResponse>> GetContextByTargetAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        if (teacherId.HasValue)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == teacherId.Value, ct);
            if (teacher is not null)
                return Result<ScheduleContextResponse>.Ok(
                    new ScheduleContextResponse
                    {
                        TeacherId = teacher.Id,
                        TeacherName = teacher.User.FullName,
                        Role = "Teacher",
                    }
                );
        }

        if (groupId.HasValue)
        {
            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId.Value, ct);
            if (group is not null)
                return Result<ScheduleContextResponse>.Ok(
                    new ScheduleContextResponse
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        Role = "Student",
                    }
                );
        }

        return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse { Role = "Other" });
    }

    public async Task<Result<JournalResponse>> GetJournalAsync(
        Guid teacherId,
        string? subject,
        Guid? groupId,
        CancellationToken ct
    )
    {
        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null)
            return Result<JournalResponse>.Fail("Преподаватель не найден.", 404);

        var entriesQuery = db.ScheduleEntries.AsNoTracking().Where(e => e.TeacherId == teacherId);
        var subjectFilter = subject?.Trim();
        if (!string.IsNullOrEmpty(subjectFilter))
            entriesQuery = entriesQuery.Where(e => e.Subject == subjectFilter);
        if (groupId.HasValue)
            entriesQuery = entriesQuery.Where(e => e.GroupId == groupId.Value);

        var entries = await entriesQuery.ToListAsync(ct);

        // Названия групп: пара «группа + предмет» выводится отдельной карточкой.
        var groupNames = await db
            .Groups.AsNoTracking()
            .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

        // Бейджи корректировок: изменения, где преподаватель — новый или снятый.
        var history = await db
            .ScheduleHistory.AsNoTracking()
            .Where(h => h.TeacherId == teacherId || h.RemovedTeacherId == teacherId)
            .ToListAsync(ct);

        // Группировка по (группа, предмет, неделя, день недели) с фактической датой дня.
        // Показываем только проведённые занятия — дата не позже сегодняшнего дня (UTC).
        var utcToday = DateTime.UtcNow.Date;
        var mondayOfWeek1 = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        var itemMap =
            new Dictionary<
                (Guid GroupId, string Subject, int Week, DayOfWeek DayOfWeek),
                List<int>
            >();
        foreach (var entry in entries)
        {
            foreach (var week in entry.Weeks.Where(w => w >= 1 && w <= StudyWeek.TotalWeeks))
            {
                var dayIndex = ((int)entry.DayOfWeek + 6) % 7;
                var date = mondayOfWeek1.AddDays((week - 1) * 7 + dayIndex);
                if (date > utcToday)
                    continue;

                var key = (entry.GroupId, entry.Subject, week, entry.DayOfWeek);
                if (!itemMap.TryGetValue(key, out var pairs))
                {
                    pairs = new List<int>();
                    itemMap[key] = pairs;
                }
                pairs.Add(entry.NumberPair);
            }
        }

        var subjects = itemMap
            .OrderBy(x => groupNames.GetValueOrDefault(x.Key.GroupId, string.Empty))
            .ThenBy(x => x.Key.Subject)
            .ThenBy(x => x.Key.Week)
            .ThenBy(x => x.Key.DayOfWeek)
            .GroupBy(x => (x.Key.GroupId, x.Key.Subject))
            .Select(g =>
            {
                var items = g.Select(x => new JournalEntryItem
                    {
                        Week = x.Key.Week,
                        DayOfWeek = (int)x.Key.DayOfWeek,
                        Date = mondayOfWeek1.AddDays(
                            (x.Key.Week - 1) * 7 + ((int)x.Key.DayOfWeek + 6) % 7
                        ),
                        NumberPairs = x.Value.Distinct().OrderBy(v => v).ToList(),
                        ChangeTypes = BuildChangeTypes(
                            history,
                            x.Key.Subject,
                            x.Key.Week,
                            x.Key.DayOfWeek,
                            teacherId
                        ),
                    })
                    .ToList();

                return new JournalSubjectGroup
                {
                    GroupId = g.Key.GroupId,
                    GroupName = groupNames.GetValueOrDefault(g.Key.GroupId, string.Empty),
                    Subject = g.Key.Subject,
                    Items = items,
                    PairCount = items.Sum(i => i.NumberPairs.Count),
                };
            })
            .ToList();

        return Result<JournalResponse>.Ok(
            new JournalResponse
            {
                TeacherId = teacherId,
                TeacherName = teacher.User.FullName,
                Subjects = subjects,
                TotalPairCount = subjects.Sum(s => s.PairCount),
            }
        );
    }

    /// <summary>Бейджи корректировок для даты журнала: Add/Replace/Move/Remove/SelfStudy.</summary>
    private static List<string> BuildChangeTypes(
        List<Entities.ScheduleHistory> history,
        string subject,
        int week,
        DayOfWeek day,
        Guid teacherId
    )
    {
        var types = new List<string>();

        foreach (var h in history)
        {
            if (h.Week != week || h.DayOfWeek != day)
                continue;
            if (!string.Equals(h.Subject, subject, StringComparison.OrdinalIgnoreCase))
                continue;
            if (h.TeacherId != teacherId)
                continue;

            types.Add(h.ChangeType.ToString());
            if (
                h.ChangeType == Entities.Enums.ScheduleChangeType.Remove
                && string.Equals(h.Note?.Trim(), "сам.р.", StringComparison.OrdinalIgnoreCase)
            )
                types.Add("SelfStudy");
        }

        return types.Distinct().ToList();
    }

    public async Task<Result<ScheduleSearchResponse>> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var q = (query ?? string.Empty).Trim().ToLower();
        var p = Math.Max(page, 1);
        var ps = Math.Clamp(pageSize, 1, 50);

        if (q.Length == 0)
            return Result<ScheduleSearchResponse>.Ok(new ScheduleSearchResponse());

        var groups = await db
            .Groups.AsNoTracking()
            .Where(g => g.Name.ToLower().Contains(q))
            .OrderBy(g => g.Name)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        var teachers = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.User.FullName.ToLower().Contains(q))
            .OrderBy(t => t.User.FullName)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        var totalGroups = await db.Groups.CountAsync(g => g.Name.ToLower().Contains(q), ct);
        var totalTeachers = await db.Teachers.CountAsync(
            t => t.User.FullName.ToLower().Contains(q),
            ct
        );

        return Result<ScheduleSearchResponse>.Ok(
            new ScheduleSearchResponse
            {
                Groups = groups
                    .Select(g => new ScheduleSearchGroup
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Course = g.Course,
                    })
                    .ToList(),
                Teachers = teachers
                    .Select(t => new ScheduleSearchTeacher
                    {
                        Id = t.Id,
                        FullName = t.User.FullName,
                        Position = t.Position,
                    })
                    .ToList(),
                TotalGroups = totalGroups,
                TotalTeachers = totalTeachers,
            }
        );
    }

    public async Task<Result<SubjectsResponse>> GetSubjectsAsync(
        string? q,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        var qTrim = (q ?? string.Empty).Trim();

        // Дедуп по ключу сопоставления предметов: варианты «МДК.01.03» и «МДК.01.03.»
        // схлопываются в один предмет, сохраняя первое встреченное (каноническое) написание.
        var subjects = new Dictionary<string, string>(StringComparer.Ordinal);

        void AddSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return;
            subjects.TryAdd(ScheduleImportService.SubjectLookupKey(subject), subject);
        }

        if (teacherId.HasValue)
        {
            var entrySubjects = await db
                .ScheduleEntries.AsNoTracking()
                .Where(e => e.TeacherId == teacherId.Value && !string.IsNullOrWhiteSpace(e.Subject))
                .Select(e => e.Subject)
                .ToListAsync(ct);
            foreach (var subject in entrySubjects)
                AddSubject(subject);

            var historySubjects = await db
                .ScheduleHistory.AsNoTracking()
                .Where(h => h.TeacherId == teacherId.Value && !string.IsNullOrWhiteSpace(h.Subject))
                .Select(h => h.Subject)
                .ToListAsync(ct);
            foreach (var subject in historySubjects)
                AddSubject(subject);

            var removedSubjects = await db
                .ScheduleHistory.AsNoTracking()
                .Where(h =>
                    h.RemovedTeacherId == teacherId.Value
                    && h.RemovedSubject != null
                    && h.RemovedSubject.Trim().Length > 0
                )
                .Select(h => h.RemovedSubject!)
                .ToListAsync(ct);
            foreach (var subject in removedSubjects)
                AddSubject(subject);
        }
        else
        {
            var entrySubjects = await db
                .ScheduleEntries.AsNoTracking()
                .Where(e => !string.IsNullOrWhiteSpace(e.Subject))
                .Select(e => e.Subject)
                .ToListAsync(ct);
            foreach (var subject in entrySubjects)
                AddSubject(subject);

            var historySubjects = await db
                .ScheduleHistory.AsNoTracking()
                .Where(h => !string.IsNullOrWhiteSpace(h.Subject))
                .Select(h => h.Subject)
                .ToListAsync(ct);
            foreach (var subject in historySubjects)
                AddSubject(subject);

            var removedSubjects = await db
                .ScheduleHistory.AsNoTracking()
                .Where(h => h.RemovedSubject != null && h.RemovedSubject.Trim().Length > 0)
                .Select(h => h.RemovedSubject!)
                .ToListAsync(ct);
            foreach (var subject in removedSubjects)
                AddSubject(subject);
        }

        var result = subjects
            .Values.Where(s =>
                qTrim.Length == 0 || s.Contains(qTrim, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();

        return Result<SubjectsResponse>.Ok(new SubjectsResponse { Subjects = result });
    }

    public async Task<Result<ScheduleResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entry = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entry is null)
            return Result<ScheduleResponse>.Fail("Запись расписания не найдена", 404);

        var dto = entry.ToDto();
        var bellTimes = await bells.GetTimeMapAsync(entry.DayOfWeek, ct);
        if (bellTimes.TryGetValue(entry.NumberPair, out var time))
        {
            dto.StartTime = time.Start;
            dto.EndTime = time.End;
        }

        return Result<ScheduleResponse>.Ok(dto);
    }

    public async Task<Result<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct
    )
    {
        if (request.NumberPair < 1 || request.NumberPair > 8)
            return Result<ScheduleResponse>.Fail("Номер пары должен быть от 1 до 8", 400);

        var weeks = (request.Weeks ?? []).Distinct().OrderBy(w => w).ToList();
        if (weeks.Count == 0 || weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
            return Result<ScheduleResponse>.Fail(
                $"Недели должны быть в диапазоне 1–{StudyWeek.TotalWeeks}",
                400
            );

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<ScheduleResponse>.Fail("Группа не найдена", 404);

        if (request.TeacherId.HasValue)
        {
            var teacherExists = await db.Teachers.AnyAsync(
                t => t.Id == request.TeacherId.Value,
                ct
            );
            if (!teacherExists)
                return Result<ScheduleResponse>.Fail("Преподаватель не найден", 404);
        }

        var overlap = await CheckOverlapAsync(
            null,
            request.GroupId,
            request.TeacherId,
            request.Room,
            request.DayOfWeek,
            request.NumberPair,
            weeks,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        var bellTimes = await bells.GetTimeMapAsync(request.DayOfWeek, ct);
        var (start, end) = bellTimes.TryGetValue(request.NumberPair, out var time)
            ? time
            : ScheduleImportService.GetPairTime(request.DayOfWeek, request.NumberPair);

        var entry = request.ToEntity();
        // Маппер переносит предмет как есть, поэтому нормализуем его здесь,
        // чтобы ручное создание не порождало дубли вида «МДК.01.03.».
        entry.Subject = ScheduleImportService.NormalizeSubject(request.Subject);
        entry.Weeks = weeks;
        entry.StartTime = start;
        entry.EndTime = end;
        db.ScheduleEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var saved = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstAsync(s => s.Id == entry.Id, ct);

        return Result<ScheduleResponse>.Ok(saved.ToDto());
    }

    public async Task<Result<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken ct
    )
    {
        if (request.NumberPair < 1 || request.NumberPair > 8)
            return Result<ScheduleResponse>.Fail("Номер пары должен быть от 1 до 8", 400);

        var weeks = (request.Weeks ?? []).Distinct().OrderBy(w => w).ToList();
        if (weeks.Count == 0 || weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
            return Result<ScheduleResponse>.Fail(
                $"Недели должны быть в диапазоне 1–{StudyWeek.TotalWeeks}",
                400
            );

        var entry = await db.ScheduleEntries.FindAsync([id], ct);
        if (entry is null)
            return Result<ScheduleResponse>.Fail("Запись расписания не найдена", 404);

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<ScheduleResponse>.Fail("Группа не найдена", 404);

        if (request.TeacherId.HasValue)
        {
            var teacherExists = await db.Teachers.AnyAsync(
                t => t.Id == request.TeacherId.Value,
                ct
            );
            if (!teacherExists)
                return Result<ScheduleResponse>.Fail("Преподаватель не найден", 404);
        }

        var overlap = await CheckOverlapAsync(
            id,
            request.GroupId,
            request.TeacherId,
            request.Room,
            request.DayOfWeek,
            request.NumberPair,
            weeks,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        var bellTimes = await bells.GetTimeMapAsync(request.DayOfWeek, ct);
        var (start, end) = bellTimes.TryGetValue(request.NumberPair, out var time)
            ? time
            : ScheduleImportService.GetPairTime(request.DayOfWeek, request.NumberPair);

        entry.GroupId = request.GroupId;
        entry.TeacherId = request.TeacherId;
        entry.Subject = ScheduleImportService.NormalizeSubject(request.Subject);
        entry.Room = request.Room;
        entry.DayOfWeek = request.DayOfWeek;
        entry.NumberPair = request.NumberPair;
        entry.StartTime = start;
        entry.EndTime = end;
        entry.Weeks = weeks;
        entry.LessonType = Enum.Parse<LessonType>(request.LessonType);
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var saved = await db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .FirstAsync(s => s.Id == entry.Id, ct);

        return Result<ScheduleResponse>.Ok(saved.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.ScheduleEntries.FindAsync([id], ct);
        if (entry is null)
            return Result.Fail("Запись расписания не найдена", 404);

        db.ScheduleEntries.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<ExportResult>> ExportScheduleAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? scope,
        DateTime? date,
        int? week,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        return await exportService.ExportAsync(
            groupId,
            teacherId,
            room,
            scope,
            date,
            week,
            format,
            layout,
            ct
        );
    }

    private async Task<string?> CheckOverlapAsync(
        Guid? excludeId,
        Guid groupId,
        Guid? teacherId,
        string room,
        DayOfWeek dayOfWeek,
        int numberPair,
        List<int> weeks,
        CancellationToken ct
    )
    {
        var candidates = await db
            .ScheduleEntries.AsNoTracking()
            .Where(s =>
                s.Id != (excludeId ?? Guid.Empty)
                && s.DayOfWeek == dayOfWeek
                && s.NumberPair == numberPair
                && (
                    s.GroupId == groupId
                    || (teacherId.HasValue && s.TeacherId == teacherId)
                    || s.Room == room
                )
            )
            .Select(s => new
            {
                s.GroupId,
                s.TeacherId,
                s.Room,
                s.Weeks,
            })
            .ToListAsync(ct);

        var overlapping = candidates.Where(c => c.Weeks.Intersect(weeks).Any()).ToList();

        if (overlapping.Any(c => c.GroupId == groupId))
            return "У группы уже есть занятие в эту пару";

        if (teacherId.HasValue && overlapping.Any(c => c.TeacherId == teacherId))
            return "У преподавателя уже есть занятие в эту пару";

        if (overlapping.Any(c => c.Room == room))
            return "Аудитория уже занята в эту пару";

        return null;
    }
}
