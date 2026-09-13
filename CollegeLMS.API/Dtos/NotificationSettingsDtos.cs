namespace CollegeLMS.API.Dtos;

public class NotificationSettingsResponse
{
    public bool Enabled { get; set; }
    public string Time { get; set; } = "07:30";
    public List<int> Days { get; set; } = new();
    public DateTime? NextNotifyAt { get; set; }
}

public class UpdateNotificationSettingsRequest
{
    public bool Enabled { get; set; }
    public string Time { get; set; } = "07:30";
    public List<int> Days { get; set; } = new();
}