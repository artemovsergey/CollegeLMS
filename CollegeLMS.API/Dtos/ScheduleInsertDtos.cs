namespace CollegeLMS.API.Dtos;

public class ScheduleInsertResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? Course { get; set; }
    public bool IsActive { get; set; }
}

public class ScheduleInsertRequest
{
    public string Title { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? Course { get; set; }
    public bool IsActive { get; set; } = true;
}
