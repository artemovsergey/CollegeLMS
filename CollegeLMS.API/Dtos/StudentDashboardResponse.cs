namespace CollegeLMS.API.Dtos;

public class StudentDashboardResponse
{
    public int CoursesCount { get; set; }
    public List<RecentGradeDto> RecentGrades { get; set; } = [];
    public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = [];
}

public class RecentGradeDto
{
    public string CourseName { get; set; } = string.Empty;
    public int? Grade { get; set; }
}

public class UpcomingDeadlineDto
{
    public string AssignmentTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
