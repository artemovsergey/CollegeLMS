using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Рассылка уведомлений об изменениях расписания подписчикам MaxBot.</summary>
public class ChangeNotifier
{
    private readonly MaxBotDbContext _db;
    private readonly MaxApiClient _max;
    private readonly CollegeLmsApiClient _api;
    private readonly ILogger<ChangeNotifier> _logger;

    public ChangeNotifier(
        MaxBotDbContext db,
        MaxApiClient max,
        CollegeLmsApiClient api,
        ILogger<ChangeNotifier> logger
    )
    {
        _db = db;
        _max = max;
        _api = api;
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

        var recipients = SelectRecipients(settings, groupNames, teacherNames, revisions);

        foreach (var (chatId, revision) in recipients)
        {
            try
            {
                await _max.SendMessageAsync(
                    chatId,
                    MessageFormatter.FormatChangeNotification(revision),
                    ct: ct
                );
            }
            catch (Exception ex)
            {
                // Fail-safe: сбой доставки не роняет остальных получателей
                _logger.LogWarning(ex, "Не удалось отправить уведомление в чат {ChatId}", chatId);
            }
        }
    }

    /// <summary>
    /// Выбор получателей: Роль «Группа» — имя группы, Роль «Преподаватель» — ФИО,
    /// плюс день недели в NotifyDays.
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
                    && tName == r.TeacherName;

                if (groupMatches || teacherMatches)
                    result.Add((s.MaxChatId, r));
            }
        }

        return result.DistinctBy(x => (x.ChatId, x.Revision.Id)).ToList();
    }
}
