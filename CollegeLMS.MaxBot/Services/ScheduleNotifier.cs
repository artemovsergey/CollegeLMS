using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

public class ScheduleNotifier : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly TimeZoneInfo _tz;
    private readonly ILogger<ScheduleNotifier> _logger;

    private readonly TimeSpan _window = TimeSpan.FromMinutes(15);

    public ScheduleNotifier(IServiceProvider sp, TimeZoneInfo tz, ILogger<ScheduleNotifier> logger)
    {
        _sp = sp;
        _tz = tz;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ScheduleNotifier started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = GetNow();
                var today = DateOnly.FromDateTime(now);

                if (IsWeekend(now.DayOfWeek))
                {
                    await DelayTillNextWeekday(today, now, ct);
                    continue;
                }

                var subscribers = await LoadTodaySubscribersAsync(ct);
                var minTime = subscribers.Select(s => s.NotifyTime).DefaultIfEmpty().Min();

                if (minTime == default && !subscribers.Any())
                {
                    await DelayTillNextWeekday(today, now, ct);
                    continue;
                }

                var target = today.ToDateTime(
                    new TimeOnly(minTime.Hours, minTime.Minutes),
                    DateTimeKind.Unspecified
                );
                if (now < target)
                {
                    _logger.LogDebug("Next notification at {Target:O} (МСК)", target);
                    await Task.Delay(target - now, ct);
                    continue;
                }

                await SendDueNotificationsAsync(subscribers, now, today, ct);
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduleNotifier");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task<List<UserSettings>> LoadTodaySubscribersAsync(CancellationToken ct)
    {
        var now = GetNow();
        var dayOfWeek = (int)now.DayOfWeek;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        return await db
            .UserSettings.Where(x => x.NotifyEnabled && x.NotifyDays.Contains(dayOfWeek))
            .ToListAsync(ct);
    }

    private async Task SendDueNotificationsAsync(
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

        var nonWorking = await api.GetNonWorkingDaysAsync(today, today, ct);
        if (nonWorking.Count > 0)
        {
            _logger.LogInformation(
                "Сегодня нерабочий день ({Title}) — рассылка пропущена",
                nonWorking[0].Title
            );
            return;
        }

        var date = today.ToDateTime(TimeOnly.MinValue);

        foreach (var user in subscribers)
        {
            if (user.GroupId is null && user.TeacherId is null)
            {
                _logger.LogDebug(
                    "У пользователя {UserId} не выбраны группа и преподаватель — рассылка пропущена",
                    user.MaxUserId
                );
                continue;
            }

            if (user.LastNotifiedOn == today)
                continue;

            if (!NotificationWindow.IsDue(now.TimeOfDay, user.NotifyTime, _window))
                continue;

            if (!NotificationTimeRules.IsValid(user.NotifyTime))
            {
                _logger.LogWarning(
                    "User {UserId} has invalid NotifyTime {Time}",
                    user.MaxUserId,
                    user.NotifyTime
                );
            }

            try
            {
                var day = await api.GetDayViewAsync(date, user.GroupId, user.TeacherId, ct);
                if (day is null)
                {
                    _logger.LogWarning(
                        "Не удалось загрузить расписание дня для пользователя {UserId} — повтор в следующем цикле",
                        user.MaxUserId
                    );
                    continue;
                }

                if (day.IsNonWorking)
                {
                    _logger.LogInformation(
                        "У пользователя {UserId} сегодня нерабочий день — рассылка пропущена",
                        user.MaxUserId
                    );
                    continue;
                }

                var entityName = user.GroupId.HasValue ? "Группа" : "Преподаватель";
                var text = MessageFormatter.FormatDaySchedule(
                    day,
                    entityName,
                    showGroup: user.Role == "teacher"
                );

                await max.SendMessageAsync(user.MaxChatId, text, ct: ct);
                await Task.Delay(500, ct);

                user.LastNotifiedOn = today;
                db.UserSettings.Attach(user);
                db.Entry(user).Property(x => x.LastNotifiedOn).IsModified = true;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Digest sent to user {UserId} at {Time}",
                    user.MaxUserId,
                    now
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send notification to user {UserId}",
                    user.MaxUserId
                );
            }
        }
    }

    private bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private async Task DelayTillNextWeekday(DateOnly today, DateTime now, CancellationToken ct)
    {
        var daysTillMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysTillMonday);
        var target = nextDay.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Unspecified);
        _logger.LogDebug("Next notification at {Target:O} (МСК)", target);
        await Task.Delay(target - now, ct);
    }

    private DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
}
