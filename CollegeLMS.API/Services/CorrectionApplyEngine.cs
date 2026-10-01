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

    /// <summary>
    /// Пара только для информирования («сам.р.» без «+»): в расписании её нет,
    /// она показана, чтобы диспетчер видел уже существующую пометку.
    /// </summary>
    public bool Informational { get; set; }

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
    /// <summary>Последняя пара учебного дня — в расписании их семь.</summary>
    internal const int MaxPair = 7;

    private static bool IsSelfStudyNote(string? note) => CorrectionNotes.IsSelfStudy(note);

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
    public Task<List<SimulatedEntry>> BuildEffectiveEntriesAsync(
        Guid groupId,
        DayOfWeek day,
        int week,
        Guid? batchId,
        CancellationToken ct
    ) => BuildEffectiveEntriesAsync(groupId, day, week, batchId, null, ct);

    /// <summary>
    /// Эффективное расписание группы на дату с pending-позициями пакета.
    /// <paramref name="excludePositionId"/> исключает редактируемую позицию: иначе её
    /// собственный слот возвращается занятым и выбрать другой уже нельзя.
    /// </summary>
    public async Task<List<SimulatedEntry>> BuildEffectiveEntriesAsync(
        Guid groupId,
        DayOfWeek day,
        int week,
        Guid? batchId,
        Guid? excludePositionId,
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

            if (excludePositionId.HasValue)
                positions.RemoveAll(p => p.Id == excludePositionId.Value);
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
            .Entries.Concat(simulation.InformationalEntries)
            .Where(e => e.GroupId == groupId && e.DayOfWeek == day && e.Week == week && !e.Removed)
            .ToList();
    }

    private sealed class SimulationResult
    {
        public List<ScheduleValidationError> Errors { get; } = [];
        public List<SimulatedEntry> Entries { get; } = [];
        public List<ScheduleHistory> History { get; } = [];
        public List<ScheduleChangeDto> Changes { get; } = [];

        /// <summary>Пары «только для информирования» — в слот не встают.</summary>
        public List<SimulatedEntry> InformationalEntries { get; } = [];
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
        var teachersBySurname = teachers
            .GroupBy(
                t => ScheduleImportService.SurnameKey(t.User.FullName),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t.Id).Distinct().ToList(),
                StringComparer.OrdinalIgnoreCase
            );

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

            if (position.NumberPair < 1 || position.NumberPair > MaxPair)
            {
                result.Errors.Add(
                    Error(position.Row, 6, $"некорректный № пары: ожидается число 1–{MaxPair}.")
                );
                continue;
            }

            if (position.RemovedNumberPair is { } removedPair && removedPair is < 1 or > MaxPair)
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        6,
                        $"некорректный № пары в примечании: ожидается число 1–{MaxPair}."
                    )
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
                teacherId = ScheduleImportService.ResolveTeacherId(
                    position.TeacherName,
                    teachersByName,
                    teachersBySurname
                );
                if (!teacherId.HasValue)
                {
                    result.Errors.Add(
                        Error(position.Row, 5, $"преподаватель «{position.TeacherName}» не найден.")
                    );
                    continue;
                }
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
                removedTeacherId = ScheduleImportService.ResolveTeacherId(
                    position.RemovedTeacherName,
                    teachersByName,
                    teachersBySurname
                );
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
                    // Одна операция «Добавить» покрывает три исхода: новое
                    // занятие в свободный слот, второе занятие в занятом слоте и
                    // замена. Различают их заполненные поля снимаемого занятия,
                    // поэтому отдельных типов в форме не нужно.

                    // «вм.X»: освобождается занятие преподавателя с предметом замены, и
                    // только оно — иначе перенос унёс бы чужую пару.
                    var movePair =
                        position.RemovedNumberPair is { } from && from != position.NumberPair
                            ? from
                            : (int?)null;

                    // В слоте снимается занятие, только если в Removed* лежит не
                    // вводимое. При переносе Removed* — это само вводимое занятие
                    // (сейчас оно стоит в паре X), и слот Y не трогается.
                    var freesSlot =
                        !string.IsNullOrWhiteSpace(position.RemovedSubject)
                        && !(movePair.HasValue && IsIncomingLesson(position, teacherId));

                    // «сам.р» без «+»: сама пара в базу не встаёт — она нужна
                    // только как пометка. С ней можно совмещать замену и
                    // перенос: они снимают занятия, а пара остаётся пометкой.
                    var informational = CorrectionNotes.IsInformational(position.Note);

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
                        IsSelfStudy = IsSelfStudyNote(position.Note),
                        Informational = informational,
                        PendingChangeType = nameof(ScheduleChangeType.Add),
                    };

                    if (freesSlot || movePair.HasValue)
                    {
                        ApplyAddWithFrees(
                            position,
                            group,
                            day,
                            teacherId,
                            removedTeacherId,
                            teacherName,
                            removedTeacherName,
                            list,
                            movePair,
                            freesSlot,
                            strictMove: true,
                            addEntry: !informational,
                            execute,
                            utcNow,
                            correctionDate,
                            appliedByUserId,
                            bellByDay,
                            result,
                            entry
                        );
                        break;
                    }

                    if (informational)
                    {
                        result.InformationalEntries.Add(entry);

                        if (execute)
                            AddInformationalChange(
                                position,
                                group,
                                entry,
                                teacherId,
                                utcNow,
                                correctionDate,
                                appliedByUserId,
                                result
                            );

                        break;
                    }

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
                    // Позиции, созданные до объединения операций. Форма их уже не
                    // создаёт, но они остаются в базе и в файлах, импортированных
                    // ранее, поэтому движок их по-прежнему понимает.
                    var isMove = position.ChangeType == ScheduleChangeType.Move;
                    var legacyMovePair =
                        isMove ? position.RemovedNumberPair
                        : position.RemovedNumberPair is { } oldPair
                        && oldPair != position.NumberPair
                            ? oldPair
                        : (int?)null;

                    ApplyAddWithFrees(
                        position,
                        group,
                        day,
                        teacherId,
                        removedTeacherId,
                        teacherName,
                        removedTeacherName,
                        list,
                        legacyMovePair,
                        freesSlot: !isMove,
                        strictMove: false,
                        addEntry: true,
                        execute,
                        utcNow,
                        correctionDate,
                        appliedByUserId,
                        bellByDay,
                        result,
                        entry: null
                    );
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

    /// <summary>
    /// Описано ли в Removed* именно вводимое занятие. Так выглядит перенос:
    /// в паре «откуда» стоит та же пара, что вводится в выбранную.
    /// </summary>
    private static bool IsIncomingLesson(CorrectionPosition position, Guid? teacherId) =>
        string.Equals(
            ScheduleImportService.SubjectLookupKey(position.RemovedSubject ?? string.Empty),
            ScheduleImportService.SubjectLookupKey(position.Subject ?? string.Empty),
            StringComparison.OrdinalIgnoreCase
        )
        && (
            position.RemovedTeacherId == teacherId
            || (
                position.RemovedTeacherId is null
                && MatchesTeacherName(
                    position.RemovedTeacherName,
                    position.TeacherName ?? string.Empty
                )
            )
        );

    /// <summary>
    /// Добавление, которое заодно освобождает занятия: заменяет то, что стоит в
    /// выбранной паре (<paramref name="freesSlot"/>), и/или переносит вводимое
    /// занятие из пары «откуда» (<paramref name="movePair"/>, это и есть «вм.X»).
    /// </summary>
    private void ApplyAddWithFrees(
        CorrectionPosition position,
        Group group,
        DayOfWeek day,
        Guid? teacherId,
        Guid? removedTeacherId,
        string? teacherName,
        string? removedTeacherName,
        List<SimulatedEntry> list,
        int? movePair,
        bool freesSlot,
        bool strictMove,
        bool addEntry,
        bool execute,
        DateTime utcNow,
        DateTime? correctionDate,
        Guid appliedByUserId,
        Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>> bellByDay,
        SimulationResult result,
        SimulatedEntry? entry
    )
    {
        SimulatedEntry? slotSource = null;
        if (freesSlot)
        {
            slotSource = FindEntry(
                list,
                position.NumberPair,
                position.RemovedSubject,
                removedTeacherId,
                position.RemovedTeacherName
            );
            if (slotSource is null)
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        2,
                        $"пара {position.NumberPair} не найдена в расписании на эту дату."
                    )
                );
                return;
            }

            // Замена на то же самое занятие ничего не меняет, но пара была бы
            // удалена и создана заново — вместе с аудиторией и другими неделями.
            var sameLesson =
                MatchesSubject(slotSource, position.Subject)
                && (
                    teacherId.HasValue
                        ? slotSource.TeacherId == teacherId.Value
                        : MatchesTeacherName(slotSource.TeacherName, teacherName ?? string.Empty)
                );

            if (strictMove && sameLesson)
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        4,
                        $"в паре {position.NumberPair} уже стоит это занятие — заменять не на что."
                    )
                );
                return;
            }
        }

        // «вм.X»: освобождается занятие преподавателя с предметом замены, и
        // только оно — иначе перенос унёс бы чужую пару.
        SimulatedEntry? movedSource = null;
        if (movePair is { } from)
        {
            movedSource = strictMove
                ? FindStrictEntry(
                    list,
                    from,
                    position.Subject,
                    teacherId,
                    position.TeacherName ?? string.Empty
                )
                : FindEntry(list, from, position.Subject, teacherId, position.TeacherName);

            if (movedSource is null)
            {
                result.Errors.Add(
                    Error(
                        position.Row,
                        6,
                        strictMove
                            ? $"в паре {from} нет занятия «{LessonLine(position.Subject, teacherName ?? position.TeacherName)}» — перенос невозможен."
                            : $"занятие для переноса не найдено: пара {from} не найдена в расписании на эту дату."
                    )
                );
                return;
            }
        }

        if (slotSource is not null)
        {
            slotSource.Removed = true;
            slotSource.PendingChangeType = nameof(ScheduleChangeType.Add);
            if (execute && slotSource.Entity is not null)
                RemoveWeekOrDelete(slotSource.Entity, position.Week, utcNow);
        }

        if (movedSource is not null)
        {
            movedSource.Removed = true;
            movedSource.PendingChangeType = nameof(ScheduleChangeType.Remove);
            if (execute && movedSource.Entity is not null)
                RemoveWeekOrDelete(movedSource.Entity, position.Week, utcNow);
        }

        entry ??= new SimulatedEntry
        {
            GroupId = group.Id,
            GroupName = group.Name,
            DayOfWeek = day,
            Week = position.Week,
            NumberPair = position.NumberPair,
            Subject = ScheduleImportService.NormalizeSubject(position.Subject ?? string.Empty),
            Room = string.Empty,
            TeacherId = teacherId,
            TeacherName = teacherName,
            Note = position.Note,
            IsSelfStudy = IsSelfStudyNote(position.Note),
            PendingChangeType = nameof(ScheduleChangeType.Add),
        };

        // Пара «только для информирования» в расписание не встаёт: показываем
        // её пометкой, но снимаемые занятия (замена, перенос) при этом
        // освобождаются по-настоящему.
        if (addEntry)
            list.Add(entry);
        else
            result.InformationalEntries.Add(entry);

        if (!execute)
            return;

        ScheduleEntry? entity = null;
        if (addEntry)
        {
            entity = CreateEntity(entry, utcNow, bellByDay);
            entry.Entity = entity;
            db.ScheduleEntries.Add(entity);
        }

        // В журнал идёт одна запись «добавлено»: что снято в паре назначения и
        // откуда пришёл перенос — её поля Removed*.
        var freed = slotSource ?? movedSource;
        var history = BuildHistory(
            position,
            ScheduleChangeType.Add,
            entry.Subject,
            teacherId,
            entity?.Room ?? string.Empty,
            entry.NumberPair,
            freed?.Subject,
            freed?.TeacherId,
            freed?.Room,
            freed?.NumberPair,
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
                RemovedSubject = freed?.Subject,
                RemovedTeacherName = freed?.TeacherName,
                RemovedNumberPair = freed?.NumberPair,
                CorrectionDate = correctionDate,
            }
        );

        // Замена с переносом даёт вторую запись: занятие уходит из пары
        // «откуда». При простом переносе хватает первой — в ней уже записано,
        // что освободилось.
        if (movedSource is not null && slotSource is not null)
        {
            var removeHistory = BuildHistory(
                position,
                ScheduleChangeType.Remove,
                movedSource.Subject,
                movedSource.TeacherId,
                movedSource.Room,
                movedSource.NumberPair,
                null,
                null,
                null,
                null,
                utcNow,
                appliedByUserId
            );
            db.ScheduleHistory.Add(removeHistory);
            result.History.Add(removeHistory);

            result.Changes.Add(
                new ScheduleChangeDto
                {
                    Id = removeHistory.Id,
                    ChangeType = nameof(ScheduleChangeType.Remove),
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = movedSource.TeacherId,
                    TeacherName = movedSource.TeacherName,
                    DayOfWeek = position.DayOfWeek,
                    Week = position.Week,
                    NumberPair = movedSource.NumberPair,
                    Subject = movedSource.Subject,
                    Note = position.Note,
                    CorrectionDate = correctionDate,
                }
            );
        }
    }

    /// <summary>Запись журнала и уведомление для пары «только для информирования».</summary>
    private void AddInformationalChange(
        CorrectionPosition position,
        Group group,
        SimulatedEntry entry,
        Guid? teacherId,
        DateTime utcNow,
        DateTime? correctionDate,
        Guid appliedByUserId,
        SimulationResult result
    )
    {
        var history = BuildHistory(
            position,
            ScheduleChangeType.Add,
            entry.Subject,
            teacherId,
            entry.Room,
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
                TeacherName = entry.TeacherName,
                DayOfWeek = position.DayOfWeek,
                Week = position.Week,
                NumberPair = entry.NumberPair,
                Subject = entry.Subject,
                Note = position.Note,
                CorrectionDate = correctionDate,
            }
        );
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
            BatchId = position.BatchId,
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
        var candidates = list.Where(e => !e.Removed && e.NumberPair == numberPair).ToList();
        if (candidates.Count == 0)
            return null;

        var strict = candidates
            .Where(e => MatchesSubject(e, subject) && MatchesTeacher(e, teacherId, teacherName))
            .ToList();
        if (strict.Count > 0)
            return strict[0];

        // Предмет в файле мог быть записан иначе (сокращение, пунктуация) — если
        // преподаватель однозначно указывает на пару, считаем её искомой.
        var byTeacher = candidates.Where(e => MatchesTeacher(e, teacherId, teacherName)).ToList();
        return byTeacher.Count == 1 ? byTeacher[0] : null;
    }

    /// <summary>
    /// Строгий поиск занятия пары: предмет и преподаватель должны совпасть. Им
    /// пользуется перенос — освобождать пару «откуда» можно только у того же
    /// преподавателя с тем же предметом, иначе уедет чужая пара.
    /// </summary>
    private static SimulatedEntry? FindStrictEntry(
        List<SimulatedEntry> list,
        int numberPair,
        string? subject,
        Guid? teacherId,
        string teacherName
    ) =>
        list.FirstOrDefault(e =>
            !e.Removed
            && e.NumberPair == numberPair
            && MatchesSubject(e, subject)
            && MatchesTeacher(e, teacherId, teacherName)
        );

    private static bool MatchesSubject(SimulatedEntry entry, string? subject) =>
        string.IsNullOrWhiteSpace(subject)
        || string.Equals(
            ScheduleImportService.SubjectLookupKey(entry.Subject),
            ScheduleImportService.SubjectLookupKey(subject),
            StringComparison.OrdinalIgnoreCase
        );

    /// <summary>«Предмет Преподаватель» одной строкой — так же, как в файле.</summary>
    private static string LessonLine(string? subject, string? teacher) =>
        string.Join(
            " ",
            new[] { subject, teacher }.Where(part => !string.IsNullOrWhiteSpace(part))
        );

    private static bool MatchesTeacher(SimulatedEntry entry, Guid? teacherId, string? teacherName)
    {
        if (teacherId.HasValue)
            return entry.TeacherId == teacherId.Value;

        if (string.IsNullOrWhiteSpace(teacherName))
            return true;

        return MatchesTeacherName(entry.TeacherName, teacherName);
    }

    private static bool MatchesTeacherName(string? entryName, string teacherName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return false;

        foreach (var variant in ScheduleImportService.TeacherNameVariants(teacherName))
        {
            if (string.Equals(entryName, variant, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entryName.StartsWith(variant + " ", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
