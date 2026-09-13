namespace CollegeLMS.API.Dtos;

/// <summary>Календарь учебного семестра — используется мини-аппом и веб-приложением.</summary>
public class ScheduleMetaResponse
{
    public DateTime SemesterStart { get; set; }
    public int TotalWeeks { get; set; }
    public int CurrentWeek { get; set; }
    public DateTime CurrentDate { get; set; }
}