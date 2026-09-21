using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Время одной пары из справочника звонков.</summary>
public class BellSlot : Entity
{
    public Guid ProfileId { get; set; }

    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    [JsonIgnore]
    public BellProfile? Profile { get; set; }
}
