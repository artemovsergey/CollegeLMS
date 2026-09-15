namespace CollegeLMS.MaxBot.Clients;

public record DispatcherLoginResponse
{
    public string Token { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
}

public record ScheduleResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public string Subject { get; init; } = "";
    public string Room { get; init; } = "";
    public int DayOfWeek { get; init; }
    public int NumberPair { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public List<int> Weeks { get; init; } = [];
    public string LessonType { get; init; } = "";
    public List<ChangeTag> ChangeTags { get; init; } = [];
}

public record ChangeTag
{
    public string ChangeType { get; init; } = "";
    public int Week { get; init; }
    public int? RemovedNumberPair { get; init; }
    public string? RemovedSubject { get; init; }
    public string? Note { get; init; }
}

public record GroupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public int Course { get; init; }
}

public record TeacherResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = "";
    public string? Position { get; init; }
}

public record SchedulePageResponse
{
    public List<ScheduleResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>Полезная нагрузка POST /notify — изменения расписания из API CollegeLMS.</summary>
public record NotifyChangeDto
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

/// <summary>Запись корректировки для POST /api/schedule/correction/confirm.</summary>
public record CorrectionEntryDto
{
    public int Row { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public string ChangeType { get; set; } = "Add";
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

public record CorrectionConfirmResponse
{
    public int Applied { get; init; }
}

public record ScheduleMetaDto
{
    public string? SemesterStart { get; init; }
    public int TotalWeeks { get; init; }
    public int CurrentWeek { get; init; }
}
