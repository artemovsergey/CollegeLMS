using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class SubmissionMapper
{
    public static SubmissionResponse ToDto(this AssignmentSubmission submission) =>
        new()
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            StudentId = submission.StudentId,
            StudentName = submission.Student?.User?.FullName ?? string.Empty,
            FilePath = submission.FilePath,
            Comment = submission.Comment,
            Score = submission.Score,
            SubmittedAt = submission.SubmittedAt,
        };
}
