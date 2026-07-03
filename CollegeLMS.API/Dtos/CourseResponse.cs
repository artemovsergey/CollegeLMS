namespace CollegeLMS.API.Dtos;

public class CourseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LectureCount { get; set; }
    public int AssignmentCount { get; set; }
}
