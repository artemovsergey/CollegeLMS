using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Models.Max;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Рассылка уведомлений об изменениях расписания подписчикам MaxBot.</summary>
public class ChangeNotifier
{
    private readonly MaxBotDbContext _db;
    private readonly MaxApiClient _max;
    private readonly CollegeLmsApiClient _api;
    private readonly ILogger<ChangeNotifier> _logger;
    private readonly MaxBotOptions _options;

    public ChangeNotifier(
        MaxBotDbContext db,
        MaxApiClient max,
        CollegeLmsApiClient api,
        IOptions<MaxBotOptions> options,
        ILogger<ChangeNotifier> logger
    )
    {
        _db = db;
        _max = max;
        _api = api;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(List<ScheduleRevision> revisions, CancellationToken ct)
    {
        if (revisions.Count == 0)
            return;

        var groupNames = (await _api.GetGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name);
        var teacherNames = (await _api.GetTeachersAsync(ct)).ToDictionary(
            t => t.Id,
            t => t.FullName
        );
        var settings = await _db.UserSettings.Where(u => u.NotifyEnabled).ToListAsync(ct);

        var recipients = SelectRecipientsGrouped(settings, groupNames, teacherNames, revisions);

        foreach (var (chatId, groupId, teacherId, recipientRevisions) in recipients)
        {
            try
            {
                var text = MessageFormatter.FormatCorrectionDigest(recipientRevisions);
                var buttons = BuildDayButtons(groupId, teacherId, recipientRevisions);
                await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
            }
            catch (Exception ex)
            {
                // Fail-safe: сбой доставки не роняет остальных получателей
                _logger.LogWarning(ex, "Не удалось отправить уведомление в чат {ChatId}", chatId);
            }
        }
    }

    /// <summary>Кнопки дней изменения: до шести дат, по три в ряд.</summary>
    private List<List<MaxButton>> BuildDayButtons(
        Guid? groupId,
        Guid? teacherId,
        List<ScheduleRevision> revisions
    )
    {
        var dates = revisions
            .Select(MessageFormatter.DateForRevision)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .OrderBy(d => d)
            .Take(6)
            .ToList();

        var rows = new List<List<MaxButton>>();
        for (var i = 0; i < dates.Count; i += 3)
            rows.Add(
                dates
                    .Skip(i)
                    .Take(3)
                    .Select(d => MiniAppButtons.OpenDay(_options, d, groupId, teacherId))
                    .ToList()
            );
        return rows;
    }

    /// <summary>
    /// Группирует уведомления по чату: подписчик получает одно сообщение
    /// со всеми позициями корректировки, затрагивающими его выбор.
    /// </summary>
    public static List<(
        long ChatId,
        Guid? GroupId,
        Guid? TeacherId,
        List<ScheduleRevision> Revisions
    )> SelectRecipientsGrouped(
        List<UserSettings> settings,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, string> teacherNames,
        List<ScheduleRevision> revisions
    )
    {
        var flat = SelectRecipients(settings, groupNames, teacherNames, revisions);

        return flat.GroupBy(x => x.ChatId)
            .Select(g =>
            {
                var chatRevisions = g.Select(x => x.Revision).ToList();
                var (groupId, teacherId) = ResolveChatTarget(
                    chatRevisions,
                    groupNames,
                    teacherNames
                );
                return (g.Key, groupId, teacherId, chatRevisions);
            })
            .ToList();
    }

    /// <summary>
    /// Цель кнопки дня для набора позиций чата: определяется по именам самих позиций,
    /// а не по настройкам получателя (в чате может быть несколько настроек).
    /// </summary>
    private static (Guid? GroupId, Guid? TeacherId) ResolveChatTarget(
        List<ScheduleRevision> revisions,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, string> teacherNames
    )
    {
        foreach (var revision in revisions)
        {
            var target = ResolveTarget(revision, groupNames, teacherNames);
            if (target.GroupId.HasValue || target.TeacherId.HasValue)
                return target;
        }

        return (null, null);
    }

    /// <summary>
    /// Сущность позиции по её собственным именам: сначала группа с именем
    /// <see cref="ScheduleRevision.GroupName"/>, иначе преподаватель по
    /// <see cref="ScheduleRevision.TeacherName"/> или <see cref="ScheduleRevision.RemovedTeacherName"/>.
    /// </summary>
    public static (Guid? GroupId, Guid? TeacherId) ResolveTarget(
        ScheduleRevision revision,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, string> teacherNames
    )
    {
        var groupId = groupNames
            .Where(kv => kv.Value == revision.GroupName)
            .Select(kv => (Guid?)kv.Key)
            .FirstOrDefault();
        if (groupId.HasValue)
            return (groupId, null);

        var teacherId = teacherNames
            .Where(kv =>
                kv.Value == revision.TeacherName || kv.Value == revision.RemovedTeacherName
            )
            .Select(kv => (Guid?)kv.Key)
            .FirstOrDefault();

        return (null, teacherId);
    }

    /// <summary>
    /// Выбор получателей: Роль «Группа» — имя группы, Роль «Преподаватель» — ФИО
    /// (по текущему или снятому преподавателю позиции), плюс день недели в NotifyDays.
    /// </summary>
    /// <returns>Пары (chatId, ревизия); дубликаты отбрасываются.</returns>
    public static List<(long ChatId, ScheduleRevision Revision)> SelectRecipients(
        List<UserSettings> settings,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, string> teacherNames,
        List<ScheduleRevision> revisions
    )
    {
        var result = new List<(long ChatId, ScheduleRevision Revision)>();
        foreach (var r in revisions)
        {
            foreach (var s in settings)
            {
                if (!s.NotifyEnabled)
                    continue;

                var groupMatches =
                    s.GroupId.HasValue
                    && groupNames.TryGetValue(s.GroupId.Value, out var gName)
                    && gName == r.GroupName;
                var teacherMatches =
                    s.TeacherId.HasValue
                    && teacherNames.TryGetValue(s.TeacherId.Value, out var tName)
                    && (tName == r.TeacherName || tName == r.RemovedTeacherName);

                if (groupMatches || teacherMatches)
                    result.Add((s.MaxChatId, r));
            }
        }

        return result.DistinctBy(x => (x.ChatId, x.Revision.Id)).ToList();
    }
}
