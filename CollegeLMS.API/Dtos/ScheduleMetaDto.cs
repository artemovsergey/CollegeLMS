namespace CollegeLMS.API.Dtos;

public class ScheduleContextResponse
{
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>Календарь учебного семестра — используется мини-аппом и веб-приложением.</summary>
public class ScheduleMetaResponse
{
    public DateTime SemesterStart { get; set; }
    public int TotalWeeks { get; set; }
    public int CurrentWeek { get; set; }
    public DateTime CurrentDate { get; set; }
}