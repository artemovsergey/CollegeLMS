using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Teacher : Entity
{
    public Guid UserId { get; set; }
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public TeacherCategory Category { get; set; }

    [JsonIgnore]
    public User User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
