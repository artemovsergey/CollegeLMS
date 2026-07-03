using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ICourseService
{
    Task<Result<List<CourseResponse>>> GetAllAsync(
        Guid? teacherId,
        Guid? groupId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default
    );
    Task<Result<CourseResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CourseResponse>> CreateAsync(
        CreateCourseRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default
    );
    Task<Result<CourseResponse>> UpdateAsync(
        Guid id,
        UpdateCourseRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default
    );
    Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default
    );
}
