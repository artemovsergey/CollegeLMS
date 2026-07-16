using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class StipendListItem : Entity
{
    public Guid StipendListId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Amount { get; set; }
    public double AverageScore { get; set; }

    [JsonIgnore]
    public StipendList StipendList { get; set; } = null!;

    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
