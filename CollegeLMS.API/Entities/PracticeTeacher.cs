using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Связь практики с преподавателем (многие-ко-многим).</summary>
public class PracticeTeacher : Entity
{
    public Guid PracticeId { get; set; }
    public Guid TeacherId { get; set; }

    [JsonIgnore]
    public Practice? Practice { get; set; }

    [JsonIgnore]
    public Teacher? Teacher { get; set; }
}
