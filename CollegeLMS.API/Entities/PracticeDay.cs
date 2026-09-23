using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

/// <summary>Учебный день учебной практики (УП) с номерами пар.</summary>
public class PracticeDay : Entity
{
    public Guid PracticeId { get; set; }

    /// <summary>Дата учебного дня (без времени).</summary>
    public DateTime Date { get; set; }

    /// <summary>Номера пар в этот день (1–8), например [1,2,3,4].</summary>
    public int[] PairNumbers { get; set; } = [];

    /// <summary>Число пар в этот день (производное от <see cref="PairNumbers"/>).</summary>
    [NotMapped]
    public int PairCount => PairNumbers.Length;

    [JsonIgnore]
    public Practice? Practice { get; set; }
}
