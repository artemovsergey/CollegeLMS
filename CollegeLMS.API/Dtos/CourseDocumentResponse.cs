namespace CollegeLMS.API.Dtos;

public class CourseDocumentResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
