using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Учебный день учебной практики (УП) с числом пар.</summary>
public class PracticeDay : Entity
{
    public Guid PracticeId { get; set; }

    /// <summary>Дата учебного дня (без времени).</summary>
    public DateTime Date { get; set; }

    /// <summary>Число пар в этот день (1–8).</summary>
    public int PairCount { get; set; }

    [JsonIgnore]
    public Practice? Practice { get; set; }
}
