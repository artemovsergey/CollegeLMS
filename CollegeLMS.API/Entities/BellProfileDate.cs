using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Диапазон дат, на который действует профиль звонков (например, сессия или каникулы).</summary>
public class BellProfileDate : Entity
{
    public Guid ProfileId { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    [JsonIgnore]
    public BellProfile? Profile { get; set; }
}
