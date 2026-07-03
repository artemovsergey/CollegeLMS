using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Teacher : Entity
{
    public Guid UserId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    [JsonIgnore]
    public User User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
