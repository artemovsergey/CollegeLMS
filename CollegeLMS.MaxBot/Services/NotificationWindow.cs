namespace CollegeLMS.MaxBot.Services;

public static class NotificationWindow
{
    public static bool IsDue(TimeSpan now, TimeSpan notifyTime, TimeSpan window) =>
        now >= notifyTime && now <= notifyTime.Add(window);
}
