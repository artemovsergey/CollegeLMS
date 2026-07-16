using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class TransferRecord : Entity
{
    public Guid StudentId { get; set; }
    public Guid FromGroupId { get; set; }
    public Guid ToGroupId { get; set; }
    public string Reason { get; set; } = string.Empty;

    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
