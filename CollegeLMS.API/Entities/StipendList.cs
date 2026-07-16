using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class StipendList : Entity
{
    public Guid SemesterId { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public Semester Semester { get; set; } = null!;

    [JsonIgnore]
    public ICollection<StipendListItem> Items { get; set; } = new List<StipendListItem>();
}
