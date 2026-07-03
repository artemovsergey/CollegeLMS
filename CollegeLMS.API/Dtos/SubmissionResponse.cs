namespace CollegeLMS.API.Dtos;

public class SubmissionResponse
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Score { get; set; }
    public DateTime SubmittedAt { get; set; }
}
