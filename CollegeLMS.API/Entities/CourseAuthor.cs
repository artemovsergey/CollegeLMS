using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseAuthor : Entity
{
    public Guid CourseId { get; set; }
    public Guid TeacherId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public Teacher Teacher { get; set; } = null!;
}