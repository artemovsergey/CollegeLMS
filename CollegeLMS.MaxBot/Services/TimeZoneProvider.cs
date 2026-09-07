namespace CollegeLMS.MaxBot.Services;

public static class TimeZoneProvider
{
    private const string IanaId = "Europe/Moscow";
    private const string WindowsId = "Russian Standard Time";

    public static TimeZoneInfo Resolve(string? configured)
    {
        var preferred = string.IsNullOrWhiteSpace(configured) ? IanaId : configured.Trim();

        if (TimeZoneInfo.TryFindSystemTimeZoneById(preferred, out var tz))
            return tz;

        if (
            preferred != WindowsId
            && TimeZoneInfo.TryFindSystemTimeZoneById(WindowsId, out var windows)
        )
            return windows;

        throw new TimeZoneNotFoundException($"Не удалось найти часовой пояс '{preferred}'");
    }
}
