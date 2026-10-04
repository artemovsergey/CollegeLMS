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

        // Имена преподавателей нужны для подсказки бейджа: по одному
        // идентификатору подсказка «вместо: Физика» ничего не говорит. Новый
        // преподаватель нужен и для отбора бейджа по паре.
        var teacherIds = history
            .Where(h => h.TeacherId.HasValue || h.RemovedTeacherId.HasValue)
            .SelectMany(h => new[] { h.TeacherId, h.RemovedTeacherId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var teacherNames =
            teacherIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await db
                    .Teachers.AsNoTracking()
                    .Include(t => t.User)
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, t => t.User.FullName, ct);

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
                            Subject = h.Subject,
                            TeacherName = h.TeacherId is { } teacherId
                                ? teacherNames.GetValueOrDefault(teacherId)
                                : null,
                            RemovedNumberPair = h.RemovedNumberPair,
                            RemovedSubject = h.RemovedSubject,
                            RemovedTeacherName = h.RemovedTeacherId is { } removedId
                                ? teacherNames.GetValueOrDefault(removedId)
                                : null,
                            Note = h.Note,
                        })
                        .ToList()
            );
    }

    /// <summary>
    /// Бейджи, которые относятся к самой паре: в одной паре могут стоять две
    /// пары параллельно, и «Добавлено» одной из них не должно достаться другой.
    /// </summary>
    public static List<ChangeTag> ForPair(
        IEnumerable<ChangeTag>? tags,
        string subject,
        string? teacherName
    ) => tags is null ? [] : [.. tags.Where(t => Matches(t, subject, teacherName))];

    private static bool Matches(ChangeTag tag, string subject, string? teacherName) =>
        MatchesLesson(tag.Subject, tag.TeacherName, subject, teacherName)
        || (
            !string.IsNullOrWhiteSpace(tag.RemovedSubject)
            && MatchesLesson(tag.RemovedSubject, tag.RemovedTeacherName, subject, teacherName)
        );

    private static bool MatchesLesson(
        string? tagSubject,
        string? tagTeacher,
        string subject,
        string? teacherName
    )
    {
        // Предмет в журнале не указан — фильтровать не по чему, бейдж остаётся.
        if (string.IsNullOrWhiteSpace(tagSubject))
            return true;

        if (!Same(tagSubject, subject))
            return false;

        // Преподаватель известен с обеих сторон — сверяем. Иначе пара подходит по
        // предмету: в журнале могла не сохраниться ссылка на преподавателя.
        return string.IsNullOrWhiteSpace(tagTeacher)
            || string.IsNullOrWhiteSpace(teacherName)
            || Same(tagTeacher, teacherName);
    }

    private static bool Same(string left, string right) =>
        ScheduleImportService
            .SubjectLookupKey(left)
            .Equals(
                ScheduleImportService.SubjectLookupKey(right),
                StringComparison.OrdinalIgnoreCase
            );
}
