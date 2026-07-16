using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Student : Entity
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string RecordBookNumber { get; set; } = string.Empty;

    [JsonIgnore]
    public User User { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;

    [JsonIgnore]
    public ICollection<AssignmentSubmission> Submissions { get; set; } =
        new List<AssignmentSubmission>();

    [JsonIgnore]
    public ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();
}
