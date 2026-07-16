using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseGroup : Entity
{
    public Guid CourseId { get; set; }
    public Guid GroupId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
