using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Group : Entity
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }

    [JsonIgnore]
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
