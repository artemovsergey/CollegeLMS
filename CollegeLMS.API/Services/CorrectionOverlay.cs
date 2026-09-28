using CollegeLMS.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Занятие в наложении разбора файла корректировки.</summary>
internal sealed class CorrectionOverlaySlot
{
    public int NumberPair { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }

    /// <summary>Занятие снято более ранней строкой того же файла.</summary>
    public bool Removed { get; set; }
}

/// <summary>
/// Виртуальное расписание на время разбора одного файла корректировки.
///
/// Файл описывает изменения на одну дату, и строки идут по порядку: если первая
/// строка вводит занятие, вторая может снять или заменить именно его. Пока пакет
/// не применён, в базе этого занятия нет, поэтому обращение только к
/// `schedule_entries` давало ложную ошибку «занятие не найдено». Здесь состояние
/// накапливается: в начале разбора дней подтягиваются из базы один раз, дальше
/// меняются по мере разбора строк — так же, как это делает движок применения
/// (`CorrectionApplyEngine`), поэтому разбор и проверка видят одно и то же.
/// </summary>
internal sealed class CorrectionOverlay
{
    private readonly AppDbContext db;
    private readonly Dictionary<
        (Guid GroupId, DayOfWeek Day, int Week),
        List<CorrectionOverlaySlot>
    > map = new();

    public CorrectionOverlay(AppDbContext db) => this.db = db;

    public async Task<List<CorrectionOverlaySlot>> GetAsync(
        Guid groupId,
        DayOfWeek day,
        int week,
        CancellationToken ct
    )
    {
        var key = (groupId, day, week);
        if (map.TryGetValue(key, out var cached))
            return cached;

        var loaded = await db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .Where(e => e.GroupId == groupId && e.DayOfWeek == day && e.Weeks.Contains(week))
            .Select(e => new CorrectionOverlaySlot
            {
                NumberPair = e.NumberPair,
                Subject = e.Subject,
                TeacherId = e.TeacherId,
                TeacherName = e.Teacher!.User.FullName,
            })
            .ToListAsync(ct);

        map[key] = loaded;
        return loaded;
    }

    /// <summary>
    /// Ищет занятие на паре среди ещё не снятых. Предмет и преподаватель сверяются,
    /// только если указаны: иначе берётся первое занятие пары.
    /// </summary>
    public static CorrectionOverlaySlot? Find(
        IEnumerable<CorrectionOverlaySlot> slots,
        int numberPair,
        string? subject,
        Guid? teacherId,
        string? teacherName
    )
    {
        var wantedSubject = string.IsNullOrWhiteSpace(subject)
            ? null
            : ScheduleImportService.NormalizeSubject(subject);
        var wantedTeacherKey = string.IsNullOrWhiteSpace(teacherName)
            ? null
            : ScheduleImportService.NormalizeTeacherName(teacherName);

        return slots.FirstOrDefault(slot =>
            !slot.Removed
            && slot.NumberPair == numberPair
            && (wantedSubject is null || slot.Subject == wantedSubject)
            && (
                teacherId.HasValue
                    ? slot.TeacherId == teacherId
                    : wantedTeacherKey is null
                        || (
                            slot.TeacherName is not null
                            && ScheduleImportService.NormalizeTeacherName(slot.TeacherName)
                                == wantedTeacherKey
                        )
            )
        );
    }

    public static void Add(
        List<CorrectionOverlaySlot> slots,
        int numberPair,
        string subject,
        Guid? teacherId,
        string? teacherName
    ) =>
        slots.Add(
            new CorrectionOverlaySlot
            {
                NumberPair = numberPair,
                Subject = ScheduleImportService.NormalizeSubject(subject),
                TeacherId = teacherId,
                TeacherName = teacherName,
            }
        );
}
