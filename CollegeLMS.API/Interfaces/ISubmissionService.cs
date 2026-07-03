using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ISubmissionService
{
    Task<Result<SubmissionResponse>> SubmitAsync(Guid assignmentId, SubmitAssignmentRequest request, Guid currentUserId, CancellationToken ct = default);
    Task<Result<List<SubmissionResponse>>> GetSubmissionsAsync(Guid assignmentId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<SubmissionResponse>> GradeAsync(Guid submissionId, GradeSubmissionRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<List<SubmissionResponse>>> GetMySubmissionsAsync(Guid currentUserId, CancellationToken ct = default);
}
