namespace CollegeLMS.MaxBot.Clients;

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
