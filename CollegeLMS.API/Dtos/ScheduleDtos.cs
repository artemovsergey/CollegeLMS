using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<int> Weeks { get; set; } = new();
    public string LessonType { get; set; } = string.Empty;
    public List<ChangeTag> ChangeTags { get; set; } = new();

    /// <summary>Признак пары практики (УП), синтезированной в расписании дня.</summary>
    public bool IsPractice { get; set; }

    /// <summary>Название практики для пар УП (null для обычных пар).</summary>
    public string? PracticeName { get; set; }
}

public class ChangeTag
{
    public ScheduleChangeType ChangeType { get; set; }
    public int Week { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? RemovedSubject { get; set; }
    public string? Note { get; set; }
}

public class CreateScheduleRequest
{
    public Guid GroupId { get; set; }
    public Guid? TeacherId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public List<int> Weeks { get; set; } = new();
    public string LessonType { get; set; } = string.Empty;
}

public class UpdateScheduleRequest
{
    public Guid GroupId { get; set; }
    public Guid? TeacherId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public List<int> Weeks { get; set; } = new();
    public string LessonType { get; set; } = string.Empty;
}
