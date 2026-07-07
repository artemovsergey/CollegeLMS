using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class ScheduleEntry : Entity
{
    public Guid GroupId { get; set; }
    public Guid? TeacherId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public LessonType LessonType { get; set; }

    [JsonIgnore]
    public Group? Group { get; set; }

    [JsonIgnore]
    public Teacher? Teacher { get; set; }
}
