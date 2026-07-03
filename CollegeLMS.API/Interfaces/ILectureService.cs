using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ILectureService
{
    Task<Result<List<LectureResponse>>> GetAllAsync(Guid courseId, CancellationToken ct = default);
    Task<Result<LectureResponse>> GetByIdAsync(Guid courseId, Guid id, CancellationToken ct = default);
    Task<Result<LectureResponse>> CreateAsync(Guid courseId, CreateLectureRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<LectureResponse>> UpdateAsync(Guid courseId, Guid id, UpdateLectureRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid courseId, Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
}
