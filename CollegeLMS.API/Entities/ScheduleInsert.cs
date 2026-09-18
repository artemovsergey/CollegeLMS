namespace CollegeLMS.API.Entities;

/// <summary>Специальное мероприятие в расписании (не занимает номер пары).</summary>
public class ScheduleInsert : Entity
{
    public string Title { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    /// <summary>Курс 1–4; null — для всех курсов.</summary>
    public int? Course { get; set; }

    public bool IsActive { get; set; } = true;
}
