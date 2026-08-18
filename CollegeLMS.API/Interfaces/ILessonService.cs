using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ILessonService
{
    Task<Result<List<LessonResponse>>> GetAllAsync(Guid courseId, CancellationToken ct);
    Task<Result<LessonResponse>> GetByIdAsync(Guid courseId, Guid id, CancellationToken ct);
    Task<Result<LessonResponse>> CreateAsync(
        Guid courseId,
        CreateLessonRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
    Task<Result<LessonResponse>> UpdateAsync(
        Guid courseId,
        Guid id,
        UpdateLessonRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
    Task<Result> DeleteAsync(
        Guid courseId,
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );

    Task<Result> ReorderAsync(
        Guid courseId,
        ReorderLessonsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );

    Task<Result> SetCurrentAsync(
        Guid courseId,
        Guid id,
        UpdateLessonCurrentRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
}
