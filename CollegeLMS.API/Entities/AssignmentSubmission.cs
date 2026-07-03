using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class AssignmentSubmission : Entity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Score { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public Assignment Assignment { get; set; } = null!;

    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
