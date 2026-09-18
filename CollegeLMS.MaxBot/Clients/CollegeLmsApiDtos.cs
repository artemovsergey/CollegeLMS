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
    public DateTime? CorrectionDate { get; init; }
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

/// <summary>Нерабочий день (праздник или каникулы) из справочника CollegeLMS.</summary>
public record NonWorkingDayDto
{
    public Guid Id { get; init; }
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
    public string Title { get; init; } = "";
}

/// <summary>Специальная вставка в расписании дня (без номера пары).</summary>
public record ScheduleInsertDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public int DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public int? Course { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>Практика УП/ПП для группы и преподавателя.</summary>
public record PracticeDto
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = "";
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = "";
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
    public string? Organization { get; init; }
    public string? Note { get; init; }
}

/// <summary>Постраничный ответ API CollegeLMS (items + пагинация).</summary>
public record PagedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
