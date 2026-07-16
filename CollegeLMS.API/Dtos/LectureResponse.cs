namespace CollegeLMS.API.Dtos;

public class LectureResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public Guid? TestId { get; set; }
    public string? TestTitle { get; set; }
}
