using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ISemesterService
{
    Task<Result<List<SemesterResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<SemesterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SemesterResponse>> CreateAsync(CreateSemesterRequest request, CancellationToken ct = default);
    Task<Result<SemesterResponse>> UpdateAsync(Guid id, UpdateSemesterRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
