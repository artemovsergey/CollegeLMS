namespace CollegeLMS.API.Dtos;

public class WorkingDayResponse
{
    public Guid Id { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }

    /// <summary>День недели (1=Пн … 5=Пт), по расписанию которого работает день.</summary>
    public int? SubstituteDayOfWeek { get; set; }

    public string Title { get; set; } = string.Empty;
}

public class WorkingDayRequest
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int? SubstituteDayOfWeek { get; set; }
    public string Title { get; set; } = string.Empty;
}
