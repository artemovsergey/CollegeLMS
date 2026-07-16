using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IStipendService
{
    Task<Result<StipendListDetailResponse>> GenerateAsync(
        Guid semesterId,
        CancellationToken ct = default
    );
    Task<Result<List<StipendListResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<StipendListDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
