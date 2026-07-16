using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class TestQuestion : Entity
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
    public Guid TestId { get; set; }

    [JsonIgnore]
    public Test Test { get; set; } = null!;
}
