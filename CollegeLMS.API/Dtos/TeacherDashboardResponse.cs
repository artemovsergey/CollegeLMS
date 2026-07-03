namespace CollegeLMS.API.Dtos;

public class TeacherDashboardResponse
{
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public List<RecentSubmissionDto> RecentSubmissions { get; set; } = [];
    public List<string> Courses { get; set; } = [];
}

public class RecentSubmissionDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
