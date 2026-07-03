namespace CollegeLMS.API.Dtos;

public class AssignmentResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int MaxScore { get; set; }
    public int Order { get; set; }
    public int SubmissionCount { get; set; }
}
