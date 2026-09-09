using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleCorrectionService
{
    Task<Result<CorrectionPreviewResponse>> PreviewAsync(Stream fileStream, CancellationToken ct);

    Task<Result<CorrectionConfirmResult>> ConfirmAsync(
        CorrectionConfirmRequest request,
        Guid appliedByUserId,
        CancellationToken ct
    );

    Task<Result<PagedResponse<ScheduleHistoryResponse>>> GetHistoryAsync(
        Guid? groupId,
        Guid? teacherId,
        int? week,
        int? page,
        int? pageSize,
        CancellationToken ct
    );
}
