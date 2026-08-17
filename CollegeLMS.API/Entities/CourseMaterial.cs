using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseMaterial : Entity
{
    public Guid CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;

    [JsonIgnore]
    public Course Course { get; set; } = null!;
}
