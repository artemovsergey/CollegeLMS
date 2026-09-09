using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Services;

public class ScheduleNotifier : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly TimeZoneInfo _tz;
    private readonly MaxBotOptions _options;
    private readonly ILogger<ScheduleNotifier> _logger;

    private DateOnly? _lastSentDay;

    public ScheduleNotifier(
        IServiceProvider sp,
        TimeZoneInfo tz,
        IOptions<MaxBotOptions> options,
        ILogger<ScheduleNotifier> logger
    )
    {
        _sp = sp;
        _tz = tz;
        _options = options.Value;
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
                var target = now
                    .Date.AddHours(_options.NotifyHour)
                    .AddMinutes(_options.NotifyMinute);

                if (_lastSentDay == today)
                {
                    target = target.AddDays(1);
                    await Task.Delay(target - GetNow(), ct);
                    continue;
                }

                if (now < target)
                {
                    _logger.LogDebug("Next notification at {Target:O} (МСК)", target);
                    await Task.Delay(target - now, ct);
                    continue;
                }

                if (now < target.AddMinutes(15))
                {
                    await SendNotificationsAsync(ct);
                    _lastSentDay = today;
                    continue;
                }

                _lastSentDay = today;
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

    private async Task SendNotificationsAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        var api = scope.ServiceProvider.GetRequiredService<CollegeLmsApiClient>();
        var max = scope.ServiceProvider.GetRequiredService<MaxApiClient>();

        var today = GetNow();
        var dayOfWeek = (int)today.DayOfWeek; // 0 = Воскресенье, 6 = Суббота
        if (dayOfWeek is 0 or 6)
        {
            _logger.LogInformation("Weekend — no notifications");
            return;
        }

        var week = StudyWeek.Current(_tz);

        var subscribers = await db
            .UserSettings.Where(x => x.NotifyEnabled && x.NotifyDays.Contains(dayOfWeek))
            .ToListAsync(ct);

        _logger.LogInformation(
            "Sending notifications to {Count} users for day {Day} (week {Week})",
            subscribers.Count,
            dayOfWeek,
            week
        );

        foreach (var user in subscribers)
        {
            try
            {
                var entries = await api.GetScheduleAsync(
                    groupId: user.GroupId,
                    teacherId: user.TeacherId,
                    dayOfWeek: dayOfWeek,
                    week: week,
                    ct: ct
                );

                var entityName = user.GroupId.HasValue ? "Группа" : "Преподаватель";
                var text = MessageFormatter.FormatDaySchedule(entries, dayOfWeek, entityName);

                await max.SendMessageAsync(user.MaxChatId, text, ct: ct);
                await Task.Delay(500, ct);
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

    private DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
}
