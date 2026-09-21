using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

/// <summary>Период учебной (УП) или производственной (ПП) практики группы.</summary>
public class Practice : Entity
{
    public PracticeKind Kind { get; set; }

    /// <summary>Название практики, например «УП 01» или «ПП 09».</summary>
    public string Name { get; set; } = string.Empty;

    public Guid GroupId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Note { get; set; }

    [JsonIgnore]
    public Group? Group { get; set; }

    /// <summary>Преподаватели практики (многие-ко-многим).</summary>
    [JsonIgnore]
    public ICollection<PracticeTeacher> Teachers { get; set; } = new List<PracticeTeacher>();

    /// <summary>Учебные дни УП с числом пар (только для учебной практики).</summary>
    [JsonIgnore]
    public ICollection<PracticeDay> Days { get; set; } = new List<PracticeDay>();
}
