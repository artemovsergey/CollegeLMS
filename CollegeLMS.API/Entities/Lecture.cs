using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Lecture : Entity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }

    public Guid? TestId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public Test? Test { get; set; }
}
