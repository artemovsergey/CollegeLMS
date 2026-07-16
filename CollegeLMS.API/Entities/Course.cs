using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Course : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public Guid GroupId { get; set; }
    public CourseStatus Status { get; set; }

    [JsonIgnore]
    public Teacher Teacher { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();

    [JsonIgnore]
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [JsonIgnore]
    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();

    [JsonIgnore]
    public ICollection<Test> Tests { get; set; } = new List<Test>();

    [JsonIgnore]
    public ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>();
}
