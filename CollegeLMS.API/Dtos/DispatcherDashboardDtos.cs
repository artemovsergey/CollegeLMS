using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class DispatcherDashboardResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public List<DispatcherPairSlot> Slots { get; set; } = [];
    public List<DispatcherTeacherStatus> Teachers { get; set; } = [];
}

public class DispatcherPairSlot
{
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<DispatcherEntry> Entries { get; set; } = [];
}

public class DispatcherTeacherStatus
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int TotalPairs { get; set; }
    public List<DispatcherEntry> Entries { get; set; } = [];
}

public class DispatcherEntry
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public ScheduleChangeType? ChangeType { get; set; }
}