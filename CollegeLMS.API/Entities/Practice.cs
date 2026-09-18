using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

/// <summary>Период учебной (УП) или производственной (ПП) практики группы.</summary>
public class Practice : Entity
{
    public PracticeKind Kind { get; set; }
    public Guid GroupId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Organization { get; set; }
    public string? Note { get; set; }

    [JsonIgnore]
    public Group? Group { get; set; }

    [JsonIgnore]
    public Teacher? Teacher { get; set; }
}
