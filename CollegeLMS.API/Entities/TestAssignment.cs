using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class TestAssignment : Entity
{
    public Guid TestId { get; set; }
    public Guid GroupId { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public int MaxAttempts { get; set; } = 1;

    [JsonIgnore]
    public Test Test { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
