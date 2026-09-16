using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class CorrectionBatchResponse
{
    public Guid Id { get; set; }
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public CorrectionBatchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PositionCount { get; set; }
    public List<CorrectionPositionResponse> Positions { get; set; } = [];
}

public class CorrectionPositionResponse
{
    public Guid Id { get; set; }
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
    public CorrectionPositionStatus Status { get; set; }
    public Guid? HistoryId { get; set; }
}

public class CreateCorrectionBatchRequest
{
    public DateTime CorrectionDate { get; set; }
}

public class CreateCorrectionPositionRequest
{
    public ScheduleChangeType ChangeType { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
}

public class CorrectionImportResponse
{
    public Guid? BatchId { get; set; }
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public int TotalEntries { get; set; }
    public List<CorrectionPositionResponse> Positions { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class CorrectionApplyResult
{
    public int Applied { get; set; }
    public Guid BatchId { get; set; }
    public List<ScheduleHistoryResponse> History { get; set; } = [];
}
