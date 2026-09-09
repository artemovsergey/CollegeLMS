namespace CollegeLMS.MaxBot.Models;

public class UserSettings
{
    public Guid Id { get; set; }
    public long MaxUserId { get; set; }
    public long MaxChatId { get; set; }
    public string Role { get; set; } = "student";
    public Guid? GroupId { get; set; }
    public Guid? TeacherId { get; set; }
    public bool NotifyEnabled { get; set; } = true;
    public int[] NotifyDays { get; set; } = [1, 2, 3, 4, 5];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
