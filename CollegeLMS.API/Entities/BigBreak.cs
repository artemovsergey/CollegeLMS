using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Большая перемена после выбранной пары (не более одной на профиль).</summary>
public class BigBreak : Entity
{
    public Guid ProfileId { get; set; }

    public int AfterPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    [JsonIgnore]
    public BellProfile? Profile { get; set; }
}
