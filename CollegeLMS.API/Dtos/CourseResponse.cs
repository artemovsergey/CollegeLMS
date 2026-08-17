namespace CollegeLMS.API.Dtos;

public class CourseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string GroupNames { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<Guid> AuthorIds { get; set; } = new();
    public string AuthorNames { get; set; } = string.Empty;
    public int LectureCount { get; set; }
}
