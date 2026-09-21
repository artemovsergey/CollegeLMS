using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Профиль звонков: набор пар и большая перемена, привязанный к дням недели и/или датам.</summary>
public class BellProfile : Entity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Базовый профиль — используется как fallback, если день не совпал с другими профилями.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Дни недели по ISO (1=Пн … 7=Вс). У базового профиля пусто.</summary>
    public int[] DaysOfWeek { get; set; } = [];

    [JsonIgnore]
    public ICollection<BellSlot> Slots { get; set; } = [];

    [JsonIgnore]
    public ICollection<BigBreak> BigBreaks { get; set; } = [];

    [JsonIgnore]
    public ICollection<BellProfileDate> Dates { get; set; } = [];
}
