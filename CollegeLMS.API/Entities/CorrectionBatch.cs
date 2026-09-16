using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

/// <summary>Пакет корректировки расписания — заголовок (дата, статус, список позиций).</summary>
public class CorrectionBatch : Entity
{
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public CorrectionBatchStatus Status { get; set; } = CorrectionBatchStatus.Draft;
    public Guid? CreatedByUserId { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public DateTime? AppliedAt { get; set; }

    [JsonIgnore]
    public List<CorrectionPosition> Positions { get; set; } = new();
}
