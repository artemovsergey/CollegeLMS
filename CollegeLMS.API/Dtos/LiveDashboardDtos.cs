using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class LiveDashboardResponse
{
    public TimeSpan Now { get; set; }
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsNonWorking { get; set; }
    public string? WorkingDayTitle { get; set; }
    public string? NonWorkingTitle { get; set; }
    public LiveCounts Counts { get; set; } = new();
    public List<LiveEntityStatus> Teachers { get; set; } = [];
    public List<LiveEntityStatus> Groups { get; set; } = [];
}

public class LiveCounts
{
    public int InLesson { get; set; }
    public int Finished { get; set; }
    public int NoPairs { get; set; }
    public int Waiting { get; set; }
}

public class LiveEntityStatus
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LiveLessonStatus Status { get; set; }
    public int TotalPairs { get; set; }
    public LivePairInfo? CurrentPair { get; set; }
    public LivePairInfo? NextPair { get; set; }
    public List<LiveEntry> Entries { get; set; } = [];
}

public class LivePairInfo
{
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class LiveEntry
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public bool IsPractice { get; set; }
    public string? PracticeName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}
