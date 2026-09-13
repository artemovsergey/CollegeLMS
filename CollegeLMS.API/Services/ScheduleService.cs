using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleService(AppDbContext db, ScheduleExportService exportService)
    : IScheduleService
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

        var changeTagsBySlot = await GetChangeTagsAsync(items, week, ct);

        var dtos = items
            .Select(s =>
                s.ToDto(changeTagsBySlot.GetValueOrDefault((s.GroupId, s.DayOfWeek, s.NumberPair)))
            )
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

        return Result<ScheduleContextResponse>.Ok(
            new ScheduleContextResponse { Role = "Other" }
        );
    }

    public async Task<Result<JournalResponse>> GetJournalAsync(
        Guid teacherId,
        CancellationToken ct
    )
    {
        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null)
            return Result<JournalResponse>.Fail("Преподаватель не найден.", 404);

        var entries = await db
            .ScheduleEntries.AsNoTracking()
            .Where(e => e.TeacherId == teacherId)
            .ToListAsync(ct);

        // Группировка по (предмет, неделя) с набором номеров пар.
        var itemMap = new Dictionary<(string Subject, int Week), List<int>>();
        foreach (var entry in entries)
        {
            foreach (var week in entry.Weeks.Where(w => w >= 1 && w <= StudyWeek.TotalWeeks))
            {
                var key = (entry.Subject, week);
                if (!itemMap.TryGetValue(key, out var pairs))
                {
                    pairs = new List<int>();
                    itemMap[key] = pairs;
                }
                pairs.Add(entry.NumberPair);
            }
        }

        var mondayOfWeek1 = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        var subjects = itemMap
            .OrderBy(x => x.Key.Subject)
            .ThenBy(x => x.Key.Week)
            .GroupBy(x => x.Key.Subject)
            .Select(g =>
            {
                var items = g
                    .Select(x => new JournalEntryItem
                    {
                        Week = x.Key.Week,
                        Date = mondayOfWeek1.AddDays((x.Key.Week - 1) * 7),
                        NumberPairs = x.Value.Distinct().OrderBy(v => v).ToList(),
                    })
                    .ToList();

                return new JournalSubjectGroup
                {
                    Subject = g.Key,
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
            .Take(ps)
            .ToListAsync(ct);

        var teachers = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.User.FullName.ToLower().Contains(q))
            .OrderBy(t => t.User.FullName)
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

    private async Task<
        Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>>
    > GetChangeTagsAsync(List<ScheduleEntry> items, int? week, CancellationToken ct)
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

        return history
            .GroupBy(h => (h.GroupId, h.DayOfWeek, h.NumberPair))
            .ToDictionary(
                g => g.Key,
                g =>
                    g.Select(h => new ChangeTag
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

        return Result<ScheduleResponse>.Ok(entry.ToDto());
    }

    public async Task<Result<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct
    )
    {
        if (request.NumberPair < 1 || request.NumberPair > 8)
            return Result<ScheduleResponse>.Fail("Номер пары должен быть от 1 до 8", 400);

        if (request.StartTime >= request.EndTime)
            return Result<ScheduleResponse>.Fail(
                "Время начала должно быть раньше времени окончания",
                400
            );

        if (request.Weeks.Count == 0)
            return Result<ScheduleResponse>.Fail("Укажите хотя бы одну неделю", 400);

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
            request.StartTime,
            request.EndTime,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        var entry = request.ToEntity();
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

        if (request.StartTime >= request.EndTime)
            return Result<ScheduleResponse>.Fail(
                "Время начала должно быть раньше времени окончания",
                400
            );

        if (request.Weeks.Count == 0)
            return Result<ScheduleResponse>.Fail("Укажите хотя бы одну неделю", 400);

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
            request.StartTime,
            request.EndTime,
            ct
        );
        if (overlap is not null)
            return Result<ScheduleResponse>.Fail(overlap, 409);

        entry.GroupId = request.GroupId;
        entry.TeacherId = request.TeacherId;
        entry.Subject = request.Subject;
        entry.Room = request.Room;
        entry.DayOfWeek = request.DayOfWeek;
        entry.NumberPair = request.NumberPair;
        entry.StartTime = request.StartTime;
        entry.EndTime = request.EndTime;
        entry.Weeks = request.Weeks;
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
        string? period,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        return await exportService.ExportAsync(
            groupId,
            teacherId,
            room,
            period,
            format,
            layout,
            ct
        );
    }

    public async Task<Result<CalendarResponse>> GetCalendarAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        CancellationToken ct
    )
    {
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

        var entries = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(ct);

        var days = new List<CalendarDayResponse>();
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var dayEntries = entries.Where(s => s.DayOfWeek == day).Select(s => s.ToDto()).ToList();
            if (
                dayEntries.Count > 0
                || groupId.HasValue
                || teacherId.HasValue
                || !string.IsNullOrEmpty(room)
            )
            {
                days.Add(
                    new CalendarDayResponse
                    {
                        Day = GetRussianDayName(day),
                        DayOfWeek = (int)day,
                        Entries = dayEntries,
                    }
                );
            }
        }

        var today = DateTime.UtcNow;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        return Result<CalendarResponse>.Ok(
            new CalendarResponse { WeekStart = weekStart, Days = days }
        );
    }

    private static string GetRussianDayName(DayOfWeek day) =>
        day switch
        {
            DayOfWeek.Monday => "Понедельник",
            DayOfWeek.Tuesday => "Вторник",
            DayOfWeek.Wednesday => "Среда",
            DayOfWeek.Thursday => "Четверг",
            DayOfWeek.Friday => "Пятница",
            DayOfWeek.Saturday => "Суббота",
            DayOfWeek.Sunday => "Воскресенье",
            _ => day.ToString(),
        };

    private async Task<string?> CheckOverlapAsync(
        Guid? excludeId,
        Guid groupId,
        Guid? teacherId,
        string room,
        DayOfWeek dayOfWeek,
        int numberPair,
        TimeSpan startTime,
        TimeSpan endTime,
        CancellationToken ct
    )
    {
        var baseQuery = db.ScheduleEntries.Where(s =>
            s.DayOfWeek == dayOfWeek
            && s.NumberPair == numberPair
            && startTime < s.EndTime
            && endTime > s.StartTime
        );

        if (excludeId.HasValue)
            baseQuery = baseQuery.Where(s => s.Id != excludeId.Value);

        if (await baseQuery.AnyAsync(s => s.GroupId == groupId, ct))
            return "У группы уже есть занятие в эту пару";

        if (teacherId.HasValue)
        {
            if (await baseQuery.AnyAsync(s => s.TeacherId == teacherId.Value, ct))
                return "У преподавателя уже есть занятие в эту пару";
        }

        if (await baseQuery.AnyAsync(s => s.Room == room, ct))
            return "Аудитория уже занята в эту пару";

        return null;
    }
}
