using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleDayViewResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsSunday { get; set; }
    public bool IsNonWorking { get; set; }
    public string? NonWorkingTitle { get; set; }
    public List<PracticeResponse> Practices { get; set; } = [];
    public List<ScheduleInsertResponse> Inserts { get; set; } = [];
    public List<ScheduleResponse> Entries { get; set; } = [];
}

public class ScheduleWeekViewResponse
{
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public List<ScheduleDayViewResponse> Days { get; set; } = [];
}

public class ScheduleSemesterWeekResponse
{
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public List<ScheduleDayViewResponse> Days { get; set; } = [];
}

public class ScheduleSemesterViewResponse
{
    public int TotalWeeks { get; set; }
    public List<ScheduleSemesterWeekResponse> Weeks { get; set; } = [];
}

public class ScheduleMonthDayResponse
{
    public DateTime Date { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsSunday { get; set; }
    public bool IsNonWorking { get; set; }
    public string? NonWorkingTitle { get; set; }
    public List<PracticeKind> PracticeKinds { get; set; } = [];
    public bool IsOutOfSemester { get; set; }
    public int PairCount { get; set; }
}

public class ScheduleMonthViewResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<ScheduleMonthDayResponse> Days { get; set; } = [];
}
