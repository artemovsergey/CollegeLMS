using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ITeacherService
{
    Task<Result<List<TeacherResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<TeacherResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TeacherResponse>> CreateAsync(CreateTeacherRequest request, CancellationToken ct = default);
    Task<Result<TeacherResponse>> UpdateAsync(Guid id, UpdateTeacherRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
