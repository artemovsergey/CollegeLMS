namespace CollegeLMS.MaxBot.Models;

public class ScheduleRevision
{
    public long Id { get; set; }

    public Guid ForeignId { get; set; }

    public string ChangeType { get; set; } = "";

    public string GroupName { get; set; } = "";

    public string? TeacherName { get; set; }

    public string Subject { get; set; } = "";

    public string Room { get; set; } = "";

    public string DayOfWeek { get; set; } = "";

    public int Week { get; set; }

    public int NumberPair { get; set; }

    public string? Note { get; set; }

    public string? RemovedSubject { get; set; }

    public string? RemovedTeacherName { get; set; }

    public int? RemovedNumberPair { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
