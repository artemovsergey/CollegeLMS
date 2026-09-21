using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWorkingDayService
{
    Task<Result<PagedResponse<WorkingDayResponse>>> GetAllAsync(
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    );

    Task<Result<WorkingDayResponse>> CreateAsync(WorkingDayRequest request, CancellationToken ct);

    Task<Result<WorkingDayResponse>> UpdateAsync(
        Guid id,
        WorkingDayRequest request,
        CancellationToken ct
    );

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}
