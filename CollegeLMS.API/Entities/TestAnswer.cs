using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class TestAnswer : Entity
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string GivenAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    [JsonIgnore]
    public TestAttempt Attempt { get; set; } = null!;

    [JsonIgnore]
    public TestQuestion Question { get; set; } = null!;
}
