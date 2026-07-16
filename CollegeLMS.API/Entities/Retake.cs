using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Retake : Entity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime RetakeDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RetakeStatus Status { get; set; }

    [JsonIgnore]
    public Exam Exam { get; set; } = null!;

    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
