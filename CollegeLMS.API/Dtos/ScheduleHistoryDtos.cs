using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleHistoryResponse
{
    public Guid Id { get; set; }
    public ScheduleChangeType ChangeType { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Room { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public int Week { get; set; }
    public string? Note { get; set; }
    public string? RemovedSubject { get; set; }
    public string? RemovedRoom { get; set; }
    public int? RemovedNumberPair { get; set; }
}