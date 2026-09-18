namespace CollegeLMS.API.Entities;

/// <summary>Время одной пары из справочника звонков.</summary>
public class BellSlot : Entity
{
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
