namespace CollegeLMS.API.Dtos;

public class SubmitAssignmentRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class GradeSubmissionRequest
{
    public int Score { get; set; }
}
