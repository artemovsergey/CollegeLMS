using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class TestAttempt : Entity
{
    public Guid TestId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AttemptStatus Status { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }

    [JsonIgnore]
    public Test Test { get; set; } = null!;

    [JsonIgnore]
    public Student Student { get; set; } = null!;

    [JsonIgnore]
    public ICollection<TestAnswer> Answers { get; set; } = new List<TestAnswer>();
}
