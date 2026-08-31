namespace CollegeLMS.API.Dtos;

public class SchedulePreviewResponse
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int WarningsCount { get; set; }
    public List<SchedulePreviewWarning> Warnings { get; set; } = [];
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
}

public class SchedulePreviewEntry
{
    public string GroupName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public int Pair { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public List<int> Weeks { get; set; } = [];
    public string Status { get; set; } = "ok";
    public string? StatusMessage { get; set; }
}

public class SchedulePreviewWarning
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ConfirmImportRequest
{
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
    public bool CreateMissingGroups { get; set; }
    public bool CreateMissingTeachers { get; set; }
}

public class ConfirmImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = [];
}

public class ImportError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
