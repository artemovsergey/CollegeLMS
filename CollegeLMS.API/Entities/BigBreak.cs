namespace CollegeLMS.API.Entities;

/// <summary>Большая перемена после выбранной пары (не более одной).</summary>
public class BigBreak : Entity
{
    public int AfterPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
