namespace CollegeLMS.API.Services;

/// <summary>
/// Разрешение часового пояса приложения для расчёта текущего времени и даты.
/// </summary>
public static class TimeZoneProvider
{
    private const string IanaId = "Europe/Moscow";
    private const string WindowsId = "Russian Standard Time";

    /// <summary>
    /// Возвращает часовой пояс по идентификатору из конфигурации.
    /// При пустом или неизвестном значении гарантированно используется Europe/Moscow,
    /// при недоступности обоих идентификаторов — UTC.
    /// </summary>
    /// <param name="configured">Идентификатор часового пояса из конфигурации (например, Europe/Moscow).</param>
    /// <returns>Разрешённый часовой пояс.</returns>
    public static TimeZoneInfo Resolve(string? configured)
    {
        var preferred = string.IsNullOrWhiteSpace(configured) ? IanaId : configured.Trim();

        if (TimeZoneInfo.TryFindSystemTimeZoneById(preferred, out var tz))
            return tz;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(IanaId, out var moscow))
            return moscow;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(WindowsId, out var windows))
            return windows;

        return TimeZoneInfo.Utc;
    }
}
