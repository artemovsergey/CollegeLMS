namespace CollegeLMS.API.Dtos;

public class AdminDashboardResponse
{
    public int UserCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int CourseCount { get; set; }
    public int GroupCount { get; set; }
    public int NewsCount { get; set; }
    public int FeedbackCount { get; set; }
}
