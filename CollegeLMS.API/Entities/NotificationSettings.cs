namespace CollegeLMS.API.Entities;

public class NotificationSettings : Entity
{
    public Guid UserId { get; set; }
    public bool Enabled { get; set; } = true;
    public TimeSpan Time { get; set; } = new(7, 30, 0);
    public List<int> Days { get; set; } = new() { 1, 2, 3, 4, 5 };
}
