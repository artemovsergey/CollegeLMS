using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Test : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public TestType Type { get; set; }
    public int PassingScore { get; set; }
    public bool AutoCheck { get; set; } = true;
    public bool ShowCorrectAnswers { get; set; } = true;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public Guid CourseId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public ICollection<TestQuestion> Questions { get; set; } = new List<TestQuestion>();

    [JsonIgnore]
    public ICollection<TestAssignment> Assignments { get; set; } = new List<TestAssignment>();

    [JsonIgnore]
    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}
