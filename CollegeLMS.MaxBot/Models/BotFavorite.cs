namespace CollegeLMS.MaxBot.Models;

public class BotFavorite
{
    public Guid Id { get; set; }
    public long MaxUserId { get; set; }
    public string TargetType { get; set; } = "Group";
    public Guid TargetId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
