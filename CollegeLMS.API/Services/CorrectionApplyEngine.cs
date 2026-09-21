using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Эффективная запись расписания с учётом неприменённых позиций пакета.</summary>
public sealed class SimulatedEntry
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public int Week { get; set; }
    public int NumberPair { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Note { get; set; }
    public bool IsSelfStudy { get; set; }
    public bool Removed { get; set; }
    public string? PendingChangeType { get; set; }
    public ScheduleEntry? Entity { get; set; }
}

/// <summary>Результат выполнения пакета: записи истории и данные для уведомлений.</summary>
public sealed class CorrectionApplyOutcome
{
    public List<ScheduleHistory> History { get; init; } = [];
    public List<ScheduleChangeDto> Changes { get; init; } = [];
}

/// <summary>
/// Симуляция позиций корректировки: проверка с учётом ранее созданных
/// неприменённых позиций (порядок строк) и выполнение изменений расписания.
/// </summary>
public sealed class CorrectionApplyEngine(AppDbContext db, IBellScheduleService bells)
{
    private static bool IsSelfStudyNote(string? note) =>
        string.Equals(note?.Trim(), "сам.р.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ошибки пакета: «Строка N: сообщение» (в БД не хранятся).</summary>
    public async Task<List<ScheduleValidationError>> ValidateBatchAsync(
        CorrectionBatch batch,
        CancellationToken ct
    )
    {
        var positions = batch
            .Positions.Where(p => p.Status == CorrectionPositionStatus.Draft)
            .OrderBy(p => p.Row)
            .ToList();
        if (positions.Count == 0)
            return [];

        var simulation = await SimulateAsync(
            positions,
            execute: false,
            Guid.Empty,
            batch.CorrectionDate,
            ct
        );
        return simulation.Errors;
    }

    /// <summary>Выполняет позиции пакета в порядке номеров строк (в транзакции вызывающего).</summary>
    public async Task<CorrectionApplyOutcome> ExecuteBatchAsync(
        CorrectionBatch batch,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        var positions = batch
            .Positions.Where(p => p.Status == CorrectionPositionStatus.Draft)
            .OrderBy(p => p.Row)
            .ToList();
        if (positions.Count == 0)
            return new CorrectionApplyOutcome();

        var simulation = await SimulateAsync(
            positions,
            execute: true,
            appliedByUserId,
            batch.CorrectionDate,
            ct
        );
        return new CorrectionApplyOutcome
        {
            History = simulation.History,
            Changes = simulation.Changes,
        };
    }

    /// <summary>Эффективное расписание группы на дату с pending-позициями пакета.</summary>
    public async Task<List<SimulatedEntry>> BuildEffectiveEntriesAsync(
        Guid groupId,
        DayOfWeek day,
        int week,
        Guid? batchId,
        CancellationToken ct
    )
    {
        var positions = new List<CorrectionPosition>();
        if (batchId.HasValue)
        {
            positions = await db
                .CorrectionPositions.AsNoTracking()
                .Where(p =>
                    p.BatchId == batchId.Value
                    && p.Status == CorrectionPositionStatus.Draft
                    && p.GroupId == groupId
                    && p.DayOfWeek == (int)day
                    && p.Week == week
                )
                .OrderBy(p => p.Row)
                .ToListAsync(ct);
        }

        var simulation = await SimulateAsync(
            positions,
            execute: false,
            Guid.Empty,
            null,
            ct,
            [(groupId, day, week)]
        );

        return simulation
            .Entries.Where(e =>
                e.GroupId == groupId && e.DayOfWeek == day && e.Week == week && !e.Removed
            )
            .ToList();
    }

    private sealed class SimulationResult
    {
        public List<ScheduleValidationError> Errors { get; } = [];
        public List<SimulatedEntry> Entries { get; } = [];
        public List<ScheduleHistory> History { get; } = [];
        public List<ScheduleChangeDto> Changes { get; } = [];
    }

    private async Task<SimulationResult> SimulateAsync(
        IReadOnlyList<CorrectionPosition> positions,
        bool execute,
        Guid appliedByUserId,
        DateTime? correctionDate,
        CancellationToken ct,
        IReadOnlyList<(Guid GroupId, DayOfWeek Day, int Week)>? forcedTargets = null
    )
    {
        var result = new SimulationResult();

        var targets =
            forcedTargets?.ToList()
            ?? positions
                .Where(p => p.GroupId != Guid.Empty)
                .Select(p => (p.GroupId, (DayOfWeek)p.DayOfWeek, p.Week))
                .Distinct()
                .ToList();

        var groupIds = targets.Select(t => t.GroupId).Distinct().ToList();
        var groups = await db
            .Groups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync(ct);
        var groupsById = groups.ToDictionary(g => g.Id);

        var teachers = await db.Teachers.AsNoTracking().Include(t => t.User).ToListAsync(ct);
        var teachersById = teachers.ToDictionary(t => t.Id);
        var teachersByName = teachers
            .GroupBy(t => t.User.FullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var entries = execute
            ? await db
                .ScheduleEntries.Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                .Where(e => groupIds.Contains(e.GroupId))
                .ToListAsync(ct)
            : await db
                .ScheduleEntries.AsNoTracking()
                .Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                .Where(e => groupIds.Contains(e.GroupId))
                .ToListAsync(ct);

        var bellByDay =
            new Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            bellByDay[day] = await bells.GetTimeMapAsync(day, ct);

        var virtualMap =
            new Dictionary<(Guid GroupId, DayOfWeek Day, int Week), List<SimulatedEntry>>();
        foreach (var target in targets)
        {
            virtualMap[target] = entries
                .Where(e =>
                    e.GroupId == target.GroupId
                    && e.DayOfWeek == target.Day
                    && e.Weeks.Contains(target.Week)
                )
                .Select(e => new SimulatedEntry
                {
                    GroupId = e.GroupId,
                    GroupName = groupsById.TryGetValue(e.GroupId, out var g)
                        ? g.Name
                        : string.Empty,
                    DayOfWeek = e.DayOfWeek,
                    Week = target.Week,
                    NumberPair = e.NumberPair,
                    Subject = e.Subject,
                    Room = e.Room,
                    TeacherId = e.TeacherId,
                    TeacherName = e.Teacher?.User?.FullName,
                    Entity = e,
                })
                .ToList();
        }

        var utcNow = DateTime.UtcNow;

        foreach (var position in positions)
        {
            var day = (DayOfWeek)position.DayOfWeek;
            var key = (position.GroupId, day, position.Week);

            if (
                position.GroupId == Guid.Empty
                || !groupsById.TryGetValue(position.GroupId, out var group)
            )
            {
                result.Errors.Add(
                    Error(position.Row, 1, $"группа «{position.GroupName}» не найдена в системе.")
                );
                continue;
            }

            if (position.NumberPair < 1 || position.NumberPair > 8)
            {
                result.Errors.Add(
                    Error(position.Row, 6, "некорректный № пары: ожидается число 1–8.")
                );
                continue;
            }

            if (IsSelfStudyNote(position.Note) && position.ChangeType != ScheduleChangeType.Remove)
            {
                result.Errors.Add(
                    Error(position.Row, 7, "примечание «сам.р.» допустимо только для снятия.")
                );
                continue;
            }

            var hasSubject = !string.IsNullOrWhiteSpace(position.Subject);
            var hasTeacher =
                position.TeacherId.HasValue || !string.IsNullOrWhiteSpace(position.TeacherName);
            if (hasSubject != hasTeacher)
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        4,
                        "заполнен предмет, но не указан преподаватель (или наоборот)."
                    )
                );
                continue;
            }

            if (position.ChangeType != ScheduleChangeType.Remove && !hasSubject)
            {
                result.Errors.Add(
                    Error(position.Row, 4, "не заполнены предмет и преподаватель (вводится).")
                );
                continue;
            }

            if (
                position.ChangeType == ScheduleChangeType.Move
                && position.RemovedNumberPair is null
            )
            {
                result.Errors.Add(
                    Error(position.Row, 6, "не указана старая пара для переноса («вм.X»).")
                );
                continue;
            }

            var teacherId = position.TeacherId;
            if (teacherId.HasValue && !teachersById.ContainsKey(teacherId.Value))
            {
                result.Errors.Add(
                    Error(position.Row, 5, $"преподаватель «{position.TeacherName}» не найден.")
                );
                continue;
            }
            if (!teacherId.HasValue && !string.IsNullOrWhiteSpace(position.TeacherName))
            {
                var normalized = ScheduleImportService.NormalizeTeacherName(position.TeacherName);
                if (!teachersByName.TryGetValue(normalized, out var resolvedTeacherId))
                {
                    result.Errors.Add(
                        Error(position.Row, 5, $"преподаватель «{position.TeacherName}» не найден.")
                    );
                    continue;
                }
                teacherId = resolvedTeacherId;
            }

            var removedTeacherId = position.RemovedTeacherId;
            if (removedTeacherId.HasValue && !teachersById.ContainsKey(removedTeacherId.Value))
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        3,
                        $"преподаватель «{position.RemovedTeacherName}» не найден."
                    )
                );
                continue;
            }
            if (
                !removedTeacherId.HasValue
                && !string.IsNullOrWhiteSpace(position.RemovedTeacherName)
            )
            {
                var normalized = ScheduleImportService.NormalizeTeacherName(
                    position.RemovedTeacherName
                );
                removedTeacherId = teachersByName.TryGetValue(normalized, out var removedId)
                    ? removedId
                    : null;
            }

            if (!virtualMap.TryGetValue(key, out var list))
            {
                list = [];
                virtualMap[key] = list;
            }

            var teacherName = ResolveTeacherName(teacherId, position.TeacherName, teachersById);
            var removedTeacherName = ResolveTeacherName(
                removedTeacherId,
                position.RemovedTeacherName,
                teachersById
            );

            switch (position.ChangeType)
            {
                case ScheduleChangeType.Add:
                {
                    var entry = new SimulatedEntry
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        DayOfWeek = day,
                        Week = position.Week,
                        NumberPair = position.NumberPair,
                        Subject = ScheduleImportService.NormalizeSubject(
                            position.Subject ?? string.Empty
                        ),
                        Room = string.Empty,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        Note = position.Note,
                        PendingChangeType = nameof(ScheduleChangeType.Add),
                    };
                    list.Add(entry);

                    if (execute)
                    {
                        var entity = CreateEntity(entry, utcNow, bellByDay);
                        entry.Entity = entity;
                        db.ScheduleEntries.Add(entity);

                        var history = BuildHistory(
                            position,
                            ScheduleChangeType.Add,
                            entry.Subject,
                            teacherId,
                            entity.Room,
                            entry.NumberPair,
                            null,
                            null,
                            null,
                            null,
                            utcNow,
                            appliedByUserId
                        );
                        ApplyHistory(position, history, result);

                        result.Changes.Add(
                            new ScheduleChangeDto
                            {
                                Id = history.Id,
                                ChangeType = nameof(ScheduleChangeType.Add),
                                GroupId = group.Id,
                                GroupName = group.Name,
                                TeacherId = teacherId,
                                TeacherName = teacherName,
                                DayOfWeek = position.DayOfWeek,
                                Week = position.Week,
                                NumberPair = entry.NumberPair,
                                Subject = entry.Subject,
                                Note = position.Note,
                                CorrectionDate = correctionDate,
                            }
                        );
                    }
                    break;
                }

                case ScheduleChangeType.Remove:
                {
                    var target = FindEntry(
                        list,
                        position.NumberPair,
                        position.RemovedSubject,
                        removedTeacherId,
                        position.RemovedTeacherName
                    );
                    if (target is null)
                    {
                        result.Errors.Add(
                            Error(
                                position.Row,
                                2,
                                $"пара {position.NumberPair} не найдена в расписании на эту дату."
                            )
                        );
                        continue;
                    }

                    if (IsSelfStudyNote(position.Note))
                    {
                        target.IsSelfStudy = true;
                        target.Note = position.Note;
                        target.PendingChangeType = nameof(ScheduleChangeType.Remove);
                        if (execute && target.Entity is not null)
                            target.Entity.UpdatedAt = utcNow;
                    }
                    else
                    {
                        target.Removed = true;
                        target.PendingChangeType = nameof(ScheduleChangeType.Remove);
                        if (execute && target.Entity is not null)
                            RemoveWeekOrDelete(target.Entity, position.Week, utcNow);
                    }

                    if (execute)
                    {
                        var history = BuildHistory(
                            position,
                            ScheduleChangeType.Remove,
                            target.Subject,
                            target.TeacherId,
                            target.Room,
                            target.NumberPair,
                            null,
                            null,
                            null,
                            null,
                            utcNow,
                            appliedByUserId
                        );
                        ApplyHistory(position, history, result);

                        result.Changes.Add(
                            new ScheduleChangeDto
                            {
                                Id = history.Id,
                                ChangeType = nameof(ScheduleChangeType.Remove),
                                GroupId = group.Id,
                                GroupName = group.Name,
                                TeacherId = target.TeacherId,
                                TeacherName = target.TeacherName,
                                DayOfWeek = position.DayOfWeek,
                                Week = position.Week,
                                NumberPair = target.NumberPair,
                                Subject = target.Subject,
                                Note = position.Note,
                                CorrectionDate = correctionDate,
                            }
                        );
                    }
                    break;
                }

                case ScheduleChangeType.Replace:
                case ScheduleChangeType.Move:
                {
                    var sourcePair =
                        position.ChangeType == ScheduleChangeType.Move
                            ? position.RemovedNumberPair!.Value
                            : position.RemovedNumberPair ?? position.NumberPair;

                    var source = FindEntry(
                        list,
                        sourcePair,
                        position.RemovedSubject,
                        removedTeacherId,
                        position.RemovedTeacherName
                    );
                    if (source is null)
                    {
                        result.Errors.Add(
                            Error(
                                position.Row,
                                2,
                                $"пара {sourcePair} не найдена в расписании на эту дату."
                            )
                        );
                        continue;
                    }

                    source.Removed = true;
                    source.PendingChangeType = position.ChangeType.ToString();
                    if (execute && source.Entity is not null)
                        RemoveWeekOrDelete(source.Entity, position.Week, utcNow);

                    var entry = new SimulatedEntry
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        DayOfWeek = day,
                        Week = position.Week,
                        NumberPair = position.NumberPair,
                        Subject = ScheduleImportService.NormalizeSubject(
                            position.Subject ?? string.Empty
                        ),
                        Room = string.Empty,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        Note = position.Note,
                        PendingChangeType = position.ChangeType.ToString(),
                    };
                    list.Add(entry);

                    if (execute)
                    {
                        var entity = CreateEntity(entry, utcNow, bellByDay);
                        entry.Entity = entity;
                        db.ScheduleEntries.Add(entity);

                        var history = BuildHistory(
                            position,
                            position.ChangeType,
                            entry.Subject,
                            teacherId,
                            entity.Room,
                            entry.NumberPair,
                            source.Subject,
                            source.TeacherId,
                            source.Room,
                            source.NumberPair,
                            utcNow,
                            appliedByUserId
                        );
                        ApplyHistory(position, history, result);

                        result.Changes.Add(
                            new ScheduleChangeDto
                            {
                                Id = history.Id,
                                ChangeType = position.ChangeType.ToString(),
                                GroupId = group.Id,
                                GroupName = group.Name,
                                TeacherId = teacherId,
                                TeacherName = teacherName,
                                DayOfWeek = position.DayOfWeek,
                                Week = position.Week,
                                NumberPair = entry.NumberPair,
                                Subject = entry.Subject,
                                Note = position.Note,
                                RemovedSubject = source.Subject,
                                RemovedTeacherName = source.TeacherName,
                                RemovedNumberPair = source.NumberPair,
                                CorrectionDate = correctionDate,
                            }
                        );
                    }
                    break;
                }

                default:
                    result.Errors.Add(Error(position.Row, 2, "неизвестный тип изменения."));
                    continue;
            }
        }

        result.Entries.AddRange(virtualMap.Values.SelectMany(v => v));
        return result;
    }

    private void ApplyHistory(
        CorrectionPosition position,
        ScheduleHistory history,
        SimulationResult result
    )
    {
        db.ScheduleHistory.Add(history);
        result.History.Add(history);
        position.Status = CorrectionPositionStatus.Applied;
        position.HistoryId = history.Id;
        position.UpdatedAt = DateTime.UtcNow;
    }

    private static ScheduleHistory BuildHistory(
        CorrectionPosition position,
        ScheduleChangeType changeType,
        string subject,
        Guid? teacherId,
        string room,
        int numberPair,
        string? removedSubject,
        Guid? removedTeacherId,
        string? removedRoom,
        int? removedNumberPair,
        DateTime utcNow,
        Guid appliedByUserId
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ChangeType = changeType,
            AppliedAt = utcNow,
            AppliedByUserId = appliedByUserId,
            GroupId = position.GroupId,
            TeacherId = teacherId,
            Subject = subject,
            Room = room,
            DayOfWeek = (DayOfWeek)position.DayOfWeek,
            NumberPair = numberPair,
            Week = position.Week,
            Note = position.Note,
            RemovedSubject = removedSubject,
            RemovedTeacherId = removedTeacherId,
            RemovedRoom = removedRoom,
            RemovedNumberPair = removedNumberPair,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

    private static SimulatedEntry? FindEntry(
        List<SimulatedEntry> list,
        int numberPair,
        string? subject,
        Guid? teacherId,
        string? teacherName
    )
    {
        IEnumerable<SimulatedEntry> candidates = list.Where(e =>
            !e.Removed && e.NumberPair == numberPair
        );

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var normalized = ScheduleImportService.NormalizeSubject(subject);
            candidates = candidates.Where(e =>
                string.Equals(e.Subject, normalized, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (teacherId.HasValue)
            candidates = candidates.Where(e => e.TeacherId == teacherId.Value);
        else if (!string.IsNullOrWhiteSpace(teacherName))
        {
            var normalized = ScheduleImportService.NormalizeTeacherName(teacherName);
            candidates = candidates.Where(e =>
                string.Equals(e.TeacherName, normalized, StringComparison.OrdinalIgnoreCase)
            );
        }

        return candidates.FirstOrDefault();
    }

    private static string? ResolveTeacherName(
        Guid? teacherId,
        string? fallbackName,
        Dictionary<Guid, Teacher> teachersById
    )
    {
        if (teacherId.HasValue && teachersById.TryGetValue(teacherId.Value, out var teacher))
            return teacher.User.FullName;

        return string.IsNullOrWhiteSpace(fallbackName)
            ? null
            : ScheduleImportService.NormalizeTeacherName(fallbackName);
    }

    private static ScheduleEntry CreateEntity(
        SimulatedEntry entry,
        DateTime utcNow,
        Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>> bellByDay
    )
    {
        var (start, end) =
            bellByDay.GetValueOrDefault(entry.DayOfWeek) is { } dayMap
            && dayMap.TryGetValue(entry.NumberPair, out var time)
                ? time
                : ScheduleImportService.GetPairTime(entry.DayOfWeek, entry.NumberPair);
        return new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = entry.GroupId,
            TeacherId = entry.TeacherId,
            Subject = entry.Subject,
            Room = entry.Room,
            DayOfWeek = entry.DayOfWeek,
            NumberPair = entry.NumberPair,
            StartTime = start,
            EndTime = end,
            Weeks = [entry.Week],
            LessonType = LessonType.None,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private void RemoveWeekOrDelete(ScheduleEntry entity, int week, DateTime utcNow)
    {
        entity.Weeks = entity.Weeks.Where(w => w != week).ToList();
        if (entity.Weeks.Count == 0)
            db.ScheduleEntries.Remove(entity);
        else
            entity.UpdatedAt = utcNow;
    }

    private static ScheduleValidationError Error(int row, int column, string text) =>
        new()
        {
            Row = row,
            Column = column,
            Level = "logic",
            Message = $"Строка {row}: {text}",
        };
}
