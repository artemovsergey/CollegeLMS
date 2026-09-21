using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Автоматические уведомления о начале и окончании практик (УП и ПП).
/// Рассылка — в пер-пользовательский NotifyTime (окно 15 минут, МСК), как у ежедневного
/// дайджеста ScheduleNotifier. Идемпотентность — таблица practice_notifications
/// (уникальный индекс practice_id + event): повторная попытка просто пропускается.
/// </summary>
public class PracticeNotifier : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly TimeZoneInfo _tz;
    private readonly ILogger<PracticeNotifier> _logger;

    private readonly TimeSpan _window = TimeSpan.FromMinutes(15);

    public PracticeNotifier(IServiceProvider sp, TimeZoneInfo tz, ILogger<PracticeNotifier> logger)
    {
        _sp = sp;
        _tz = tz;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("PracticeNotifier started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = GetNow();
                var today = DateOnly.FromDateTime(now);

                var subscribers = await LoadSubscribersAsync(ct);
                if (subscribers.Count == 0)
                {
                    await DelayTillNextWeekday(today, now, ct);
                    continue;
                }

                var minTime = subscribers.Select(s => s.NotifyTime).Min();
                var target = today.ToDateTime(
                    new TimeOnly(minTime.Hours, minTime.Minutes),
                    DateTimeKind.Unspecified
                );
                if (now < target)
                {
                    _logger.LogDebug("Next practice notification at {Target:O} (МСК)", target);
                    await Task.Delay(target - now, ct);
                    continue;
                }

                await SendDuePracticesAsync(subscribers, now, today, ct);
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PracticeNotifier");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task<List<UserSettings>> LoadSubscribersAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        return await db.UserSettings.AsNoTracking().Where(x => x.NotifyEnabled).ToListAsync(ct);
    }

    /// <summary>
    /// Отправляет уведомления о практиках, у которых сегодня день начала или окончания.
    /// Вызывается из цикла после наступления ближайшего NotifyTime; вынесено отдельно
    /// для тестирования.
    /// </summary>
    private async Task SendDuePracticesAsync(
        List<UserSettings> subscribers,
        DateTime now,
        DateOnly today,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        var api = scope.ServiceProvider.GetRequiredService<CollegeLmsApiClient>();
        var max = scope.ServiceProvider.GetRequiredService<MaxApiClient>();

        // from = to = today: API возвращает практики, чей период пересекает сегодня.
        var practices = await api.GetPracticesAsync(today, today, ct: ct);
        if (practices.Count == 0)
            return;

        foreach (var practice in practices)
        {
            if (practice.Id == Guid.Empty)
            {
                _logger.LogWarning("Практика без идентификатора пропущена");
                continue;
            }

            if (DateOnly.FromDateTime(practice.DateFrom) == today)
                await NotifyPracticeAsync(
                    db,
                    max,
                    subscribers,
                    practice,
                    PracticeEvent.Started,
                    now,
                    today,
                    ct
                );

            if (DateOnly.FromDateTime(practice.DateTo) == today)
                await NotifyPracticeAsync(
                    db,
                    max,
                    subscribers,
                    practice,
                    PracticeEvent.Finished,
                    now,
                    today,
                    ct
                );
        }
    }

    private async Task NotifyPracticeAsync(
        MaxBotDbContext db,
        MaxApiClient max,
        List<UserSettings> subscribers,
        PracticeDto practice,
        PracticeEvent practiceEvent,
        DateTime now,
        DateOnly today,
        CancellationToken ct
    )
    {
        var alreadySent = await db
            .PracticeNotifications.AsNoTracking()
            .AnyAsync(x => x.PracticeId == practice.Id && x.Event == practiceEvent, ct);
        if (alreadySent)
            return;

        // Получатели: студенты группы + преподаватели практики; по одному сообщению на чат.
        var recipients = SelectRecipients(subscribers, practice)
            .Where(u => NotificationWindow.IsDue(now.TimeOfDay, u.NotifyTime, _window))
            .GroupBy(u => u.MaxChatId)
            .Select(g => g.First())
            .ToList();
        if (recipients.Count == 0)
            return;

        var text =
            practiceEvent == PracticeEvent.Started
                ? MessageFormatter.FormatPracticeStarted(practice)
                : MessageFormatter.FormatPracticeFinished(practice);

        var sent = false;
        foreach (var user in recipients)
        {
            try
            {
                await max.SendMessageAsync(user.MaxChatId, text, ct: ct);
                await Task.Delay(500, ct);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Не удалось отправить уведомление о практике {PracticeId} в чат {ChatId}",
                    practice.Id,
                    user.MaxChatId
                );
            }
        }

        if (!sent)
            return;

        try
        {
            db.PracticeNotifications.Add(
                new PracticeNotification
                {
                    Id = Guid.NewGuid(),
                    PracticeId = practice.Id,
                    Event = practiceEvent,
                    SentOn = today,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Practice {PracticeId} {Event} notification sent to {Count} chats",
                practice.Id,
                practiceEvent,
                recipients.Count
            );
        }
        catch (DbUpdateException ex)
        {
            // Уникальный индекс (practice_id, event): отправлено другой репликой — пропускаем.
            _logger.LogWarning(
                ex,
                "Уведомление о практике {PracticeId}/{Event} уже отправлено",
                practice.Id,
                practiceEvent
            );
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Получатели практики: подписчики с включёнными уведомлениями, у которых
    /// GroupId совпадает с группой практики либо TeacherId входит в список преподавателей.
    /// </summary>
    private static List<UserSettings> SelectRecipients(
        List<UserSettings> subscribers,
        PracticeDto practice
    )
    {
        var teacherIds = practice.TeacherIdList();

        return subscribers
            .Where(u => u.NotifyEnabled)
            .Where(u =>
                (u.GroupId.HasValue && u.GroupId.Value == practice.GroupId)
                || (u.TeacherId.HasValue && teacherIds.Contains(u.TeacherId.Value))
            )
            .ToList();
    }

    private async Task DelayTillNextWeekday(DateOnly today, DateTime now, CancellationToken ct)
    {
        var daysTillMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysTillMonday);
        var target = nextDay.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Unspecified);
        _logger.LogDebug("Next practice notification at {Target:O} (МСК)", target);
        await Task.Delay(target - now, ct);
    }

    private DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
}
