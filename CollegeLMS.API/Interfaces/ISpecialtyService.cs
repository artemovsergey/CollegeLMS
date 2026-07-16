using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ISpecialtyService
{
    Task<Result<List<SpecialtyResponse>>> GetAllAsync(string? search, CancellationToken ct = default);
    Task<Result<SpecialtyResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SpecialtyResponse>> CreateAsync(CreateSpecialtyRequest request, CancellationToken ct = default);
    Task<Result<SpecialtyResponse>> UpdateAsync(Guid id, UpdateSpecialtyRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
