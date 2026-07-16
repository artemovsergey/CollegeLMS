using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Exam : Entity
{
    public string Subject { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public DateTime ExamDate { get; set; }
    public ExamType Type { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SemesterId { get; set; }
    public ExamStatus Status { get; set; }

    [JsonIgnore]
    public Group Group { get; set; } = null!;

    [JsonIgnore]
    public Teacher Teacher { get; set; } = null!;

    [JsonIgnore]
    public Semester Semester { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Retake> Retakes { get; set; } = new List<Retake>();
}
