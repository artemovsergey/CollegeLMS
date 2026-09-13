using System.Globalization;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class NotificationSettingsService(AppDbContext db) : INotificationSettingsService
{
    private static readonly TimeSpan MinTime = new(7, 30, 0);
    private static readonly TimeSpan MaxTime = new(8, 30, 0);

    public async Task<Result<NotificationSettingsResponse>> GetAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var settings = await EnsureAsync(userId, ct);
        var next = NextNotifyAt(settings.Time, settings.Days);
        return Result<NotificationSettingsResponse>.Ok(
            new NotificationSettingsResponse
            {
                Enabled = settings.Enabled,
                Time = settings.Time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                Days = settings.Days.OrderBy(d => d).ToList(),
                NextNotifyAt = settings.Enabled ? next : null,
            }
        );
    }

    public async Task<Result<NotificationSettingsResponse>> UpdateAsync(
        Guid userId,
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    )
    {
        if (
            !TimeSpan.TryParseExact(
                request.Time,
                @"hh\:mm",
                CultureInfo.InvariantCulture,
                out var time
            )
        )
            return Result<NotificationSettingsResponse>.Fail(
                "Время должно быть в формате ЧЧ:ММ",
                400
            );

        var totalMinutes = (int)time.TotalMinutes;
        if (time < MinTime || time > MaxTime)
            return Result<NotificationSettingsResponse>.Fail(
                "Время дайджеста должно быть от 07:30 до 08:30",
                400
            );
        if (totalMinutes % 5 != 0)
            return Result<NotificationSettingsResponse>.Fail(
                "Время дайджеста выбирается с шагом 5 минут",
                400
            );
        if (request.Days.Count == 0)
            return Result<NotificationSettingsResponse>.Fail(
                "Выберите хотя бы один день недели",
                400
            );
        if (request.Days.Any(d => d is < 1 or > 7))
            return Result<NotificationSettingsResponse>.Fail(
                "Дни недели должны быть от 1 (Пн) до 7 (Вс)",
                400
            );

        var settings = await EnsureAsync(userId, ct);
        settings.Enabled = request.Enabled;
        settings.Time = time;
        settings.Days = request.Days.Distinct().OrderBy(d => d).ToList();
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<NotificationSettingsResponse>.Ok(
            new NotificationSettingsResponse
            {
                Enabled = settings.Enabled,
                Time = settings.Time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                Days = settings.Days,
                NextNotifyAt = settings.Enabled ? NextNotifyAt(settings.Time, settings.Days) : null,
            }
        );
    }

    private async Task<NotificationSettings> EnsureAsync(Guid userId, CancellationToken ct)
    {
        var existing = await db.NotificationSettings.FirstOrDefaultAsync(
            x => x.UserId == userId,
            ct
        );
        if (existing is not null)
            return existing;

        var created = new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Enabled = true,
            Time = new TimeSpan(7, 30, 0),
            Days = new List<int> { 1, 2, 3, 4, 5 },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.NotificationSettings.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    private static DateTime? NextNotifyAt(TimeSpan time, List<int> days)
    {
        if (days.Count == 0)
            return null;

        var now = DateTime.UtcNow;
        for (var offset = 0; offset <= 14; offset++)
        {
            var candidate = now.Date.AddDays(offset).Add(time);
            var dayIndex = ((int)candidate.DayOfWeek == 0) ? 7 : (int)candidate.DayOfWeek;
            if (days.Contains(dayIndex) && candidate > now)
                return candidate;
        }

        return null;
    }
}
