namespace CollegeLMS.API.Dtos;

public class ScheduleSearchGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}

public class ScheduleSearchTeacher
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
}

public class ScheduleSearchResponse
{
    public List<ScheduleSearchGroup> Groups { get; set; } = new();
    public List<ScheduleSearchTeacher> Teachers { get; set; } = new();
    public int TotalGroups { get; set; }
    public int TotalTeachers { get; set; }
}