using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class CorrectionPreviewEntry
{
    public int Row { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public ScheduleChangeType ChangeType { get; set; }
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
}

public class CorrectionPreviewResponse
{
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public int TotalEntries { get; set; }
    public List<CorrectionPreviewEntry> Entries { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class CorrectionConfirmRequest
{
    public List<CorrectionPreviewEntry> Entries { get; set; } = [];
}

public class CorrectionConfirmResult
{
    public int Applied { get; set; }
    public List<ScheduleHistoryResponse> History { get; set; } = [];
}

public class ManualCorrectionExportRequest
{
    public DateTime CorrectionDate { get; set; }
    public List<ManualCorrectionRow> Rows { get; set; } = [];
}

public class ManualCorrectionRow
{
    public string GroupName { get; set; } = string.Empty;
    public string? RemovedSubject { get; set; }
    public string? RemovedTeacherName { get; set; }
    public string? AddedSubject { get; set; }
    public string? AddedTeacherName { get; set; }
    public int NumberPair { get; set; }
    public string? Note { get; set; }
}

/// <summary>Полезная нагрузка POST /notify в MaxBot.</summary>
public class ScheduleChangeDto
{
    public Guid Id { get; init; }
    public string ChangeType { get; init; } = "";
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public int DayOfWeek { get; init; }
    public int Week { get; init; }
    public int NumberPair { get; init; }
    public string Subject { get; init; } = "";
    public string? Note { get; init; }
    public string? RemovedSubject { get; init; }
    public string? RemovedTeacherName { get; init; }
    public int? RemovedNumberPair { get; init; }
}
