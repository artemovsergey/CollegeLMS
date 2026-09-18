using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface INonWorkingDayService
{
    Task<Result<PagedResponse<NonWorkingDayResponse>>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    );

    Task<Result<NonWorkingDayResponse>> CreateAsync(
        NonWorkingDayRequest request,
        CancellationToken ct
    );

    Task<Result<NonWorkingDayResponse>> UpdateAsync(
        Guid id,
        NonWorkingDayRequest request,
        CancellationToken ct
    );

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}
