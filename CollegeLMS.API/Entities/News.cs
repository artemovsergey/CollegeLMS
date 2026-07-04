using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class News : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public NewsCategory? Category { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public Guid CreatedById { get; set; }

    [JsonIgnore]
    public User CreatedBy { get; set; } = null!;
}
