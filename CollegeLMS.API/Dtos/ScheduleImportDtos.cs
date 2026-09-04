namespace CollegeLMS.API.Dtos;

public class SchedulePreviewResponse
{
    public int TotalEntries { get; set; }
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
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
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ScheduleValidationError
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ConfirmImportRequest
{
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
}

public class ConfirmResult
{
    public bool IsSuccess { get; set; }
    public int Imported { get; set; }
    public List<ScheduleResponse> Schedule { get; set; } = [];
}
