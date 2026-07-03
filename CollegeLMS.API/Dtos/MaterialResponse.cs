namespace CollegeLMS.API.Dtos;

public class MaterialResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? LectureId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
