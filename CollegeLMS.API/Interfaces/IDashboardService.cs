using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IDashboardService
{
    Task<Result<TeacherDashboardResponse>> GetTeacherDashboardAsync(Guid userId, CancellationToken ct = default);
    Task<Result<StudentDashboardResponse>> GetStudentDashboardAsync(Guid userId, CancellationToken ct = default);
}
