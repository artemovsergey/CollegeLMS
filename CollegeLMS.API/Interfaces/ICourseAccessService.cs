using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Interfaces;

public interface ICourseAccessService
{
    Task<bool> CanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken ct);
    Task<bool> CanManageCourseAsync(Course course, Guid teacherId, CancellationToken ct);
    Task<List<Guid>> GetManagedCourseIdsAsync(Guid teacherId, CancellationToken ct);
}