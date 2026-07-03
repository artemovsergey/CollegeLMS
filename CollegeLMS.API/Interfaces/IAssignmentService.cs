using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IAssignmentService
{
    Task<Result<List<AssignmentResponse>>> GetAllAsync(Guid courseId, CancellationToken ct = default);
    Task<Result<AssignmentResponse>> GetByIdAsync(Guid courseId, Guid id, CancellationToken ct = default);
    Task<Result<AssignmentResponse>> CreateAsync(Guid courseId, CreateAssignmentRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<AssignmentResponse>> UpdateAsync(Guid courseId, Guid id, UpdateAssignmentRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid courseId, Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
}
