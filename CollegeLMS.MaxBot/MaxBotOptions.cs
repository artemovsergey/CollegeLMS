namespace CollegeLMS.MaxBot;

public class MaxBotOptions
{
    public string AccessToken { get; set; } = "";

    public int NotifyHour { get; set; } = 7;

    public int NotifyMinute { get; set; } = 30;

    public string TimeZone { get; set; } = "Europe/Moscow";

    public string MiniAppUrl { get; set; } = "https://stvcc.tech/max/schedule";
}
