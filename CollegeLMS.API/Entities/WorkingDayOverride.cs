namespace CollegeLMS.API.Entities;

/// <summary>Рабочий день (например, рабочая суббота): зеркало нерабочего дня с подменой дня недели.</summary>
public class WorkingDayOverride : Entity
{
    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    /// <summary>День недели (1=Пн … 5=Пт), по расписанию которого работает день. Пусто — по своему дню.</summary>
    public int? SubstituteDayOfWeek { get; set; }

    public string Title { get; set; } = string.Empty;
}
