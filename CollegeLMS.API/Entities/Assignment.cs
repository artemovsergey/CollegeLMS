using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Assignment : Entity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int MaxScore { get; set; }
    public int Order { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public ICollection<AssignmentSubmission> Submissions { get; set; } =
        new List<AssignmentSubmission>();
}
