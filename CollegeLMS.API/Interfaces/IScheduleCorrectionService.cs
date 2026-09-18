using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleCorrectionService
{
    Task<Result<CorrectionPreviewResponse>> PreviewAsync(Stream fileStream, CancellationToken ct);

    Task<Result<DocumentDownloadResult>> ExportManualAsync(
        ManualCorrectionExportRequest request,
        CancellationToken ct
    );

    Task<Result<CorrectionConfirmResult>> ConfirmAsync(
        CorrectionConfirmRequest request,
        string idempotencyKey,
        Guid appliedByUserId,
        CancellationToken ct
    );

    Task<Result<List<ScheduleChangeDto>>> ApplyEntriesAsync(
        List<CorrectionPreviewEntry> entries,
        Guid appliedByUserId,
        DateTime? correctionDate,
        CancellationToken ct
    );

    /// <summary>Эффективное расписание группы на дату: поправки + неприменённые позиции пакета.</summary>
    Task<Result<CorrectionDayResponse>> GetDayAsync(
        Guid groupId,
        DateTime date,
        Guid? batchId,
        CancellationToken ct
    );

    Task<Result<PagedResponse<ScheduleHistoryResponse>>> GetHistoryAsync(
        Guid? groupId,
        Guid? teacherId,
        int? week,
        DateTime? date,
        DateTime? from,
        DateTime? to,
        ScheduleChangeType? changeType,
        int? page,
        int? pageSize,
        CancellationToken ct
    );
}
