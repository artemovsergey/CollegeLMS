namespace CollegeLMS.MaxBot.Services;

public static class NotificationTimeRules
{
    public static readonly TimeSpan Min = TimeSpan.FromHours(7) + TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Max = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(30);
    public static readonly int StepMinutes = 5;

    public static bool IsValid(TimeSpan time) =>
        time >= Min && time <= Max && (int)time.TotalMinutes % StepMinutes == 0;
}
