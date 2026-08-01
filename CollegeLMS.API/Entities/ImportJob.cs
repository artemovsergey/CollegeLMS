using System.Text.Json;

namespace CollegeLMS.API.Entities;

public class ImportJob : Entity
{
    public string Status { get; set; } = "running";
    public int Total { get; set; }
    public int Processed { get; set; }
    public int ErrorCount { get; set; }
    public int CategoriesCreated { get; set; }
    public int PostsImported { get; set; }
    public int PostsSkipped { get; set; }
    public string? ErrorMessagesJson { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<string>? ErrorMessages
    {
        get =>
            ErrorMessagesJson == null
                ? null
                : JsonSerializer.Deserialize<List<string>>(ErrorMessagesJson);
        set => ErrorMessagesJson = value == null ? null : JsonSerializer.Serialize(value);
    }
}
