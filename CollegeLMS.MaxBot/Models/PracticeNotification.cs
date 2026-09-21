namespace CollegeLMS.MaxBot.Models;

/// <summary>Событие практики: начало или окончание.</summary>
public enum PracticeEvent
{
    /// <summary>Практика началась (день начала).</summary>
    Started,

    /// <summary>Практика закончилась (день окончания).</summary>
    Finished,
}

/// <summary>Журнал отправленных уведомлений о практиках — идемпотентность рассылки.</summary>
public class PracticeNotification
{
    public Guid Id { get; set; }

    /// <summary>Идентификатор практики в API CollegeLMS.</summary>
    public Guid PracticeId { get; set; }

    /// <summary>Событие практики (начало/окончание).</summary>
    public PracticeEvent Event { get; set; }

    /// <summary>Дата (МСК) отправки уведомления.</summary>
    public DateOnly SentOn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
