using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseDocument : Entity
{
    public Guid CourseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;
}