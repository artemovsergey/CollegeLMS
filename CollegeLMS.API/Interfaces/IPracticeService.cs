using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IPracticeService
{
    Task<Result<PagedResponse<PracticeResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        PracticeKind? kind,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    );

    Task<Result<PracticeResponse>> CreateAsync(PracticeRequest request, CancellationToken ct);

    Task<Result<PracticeResponse>> UpdateAsync(
        Guid id,
        PracticeRequest request,
        CancellationToken ct
    );

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}
