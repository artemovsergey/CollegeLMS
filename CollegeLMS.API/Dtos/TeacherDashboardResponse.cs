namespace CollegeLMS.API.Dtos;

public class TeacherDashboardResponse
{
    public List<CourseBriefDto> Courses { get; set; } = [];
}

public class CourseBriefDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string GroupNames { get; set; } = string.Empty;
}
