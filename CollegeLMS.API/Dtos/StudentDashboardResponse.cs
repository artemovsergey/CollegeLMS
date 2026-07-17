namespace CollegeLMS.API.Dtos;

public class StudentDashboardResponse
{
    public List<CourseWithProgressDto> Courses { get; set; } = [];
}

public class CourseWithProgressDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public double CompletionPercent { get; set; }
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
}
