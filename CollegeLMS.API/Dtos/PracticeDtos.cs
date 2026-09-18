using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class PracticeResponse
{
    public Guid Id { get; set; }
    public PracticeKind Kind { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Organization { get; set; }
    public string? Note { get; set; }
}

public class PracticeRequest
{
    public PracticeKind Kind { get; set; }
    public Guid GroupId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Organization { get; set; }
    public string? Note { get; set; }
}

/// <summary>Строка импорта практик из XLSX (все поля — сырые значения файла).</summary>
public class PracticeImportRow
{
    public int Row { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Note { get; set; }
}

public class PracticeImportPreviewResponse
{
    public int TotalRows { get; set; }
    public List<PracticeImportRow> Rows { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class PracticeImportConfirmRequest
{
    public List<PracticeImportRow> Rows { get; set; } = [];
}

public class PracticeImportConfirmResponse
{
    public int Imported { get; set; }
    public List<PracticeResponse> Practices { get; set; } = [];
}
