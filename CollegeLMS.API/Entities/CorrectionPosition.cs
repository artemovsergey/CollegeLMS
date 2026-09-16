using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

/// <summary>Одна позиция корректировки: добавление, снятие, замена или перенос.</summary>
public class CorrectionPosition : Entity
{
    public Guid BatchId { get; set; }
    public int Row { get; set; }
    public ScheduleChangeType ChangeType { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public int Week { get; set; }
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
    public CorrectionPositionStatus Status { get; set; } = CorrectionPositionStatus.Draft;
    public Guid? HistoryId { get; set; }

    [JsonIgnore]
    public CorrectionBatch? Batch { get; set; }
}
