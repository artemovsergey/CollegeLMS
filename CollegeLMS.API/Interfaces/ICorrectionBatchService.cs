using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ICorrectionBatchService
{
    Task<Result<CorrectionBatchResponse>> CreateBatchAsync(
        CreateCorrectionBatchRequest request,
        Guid createdByUserId,
        CancellationToken ct
    );

    /// <summary>Смена даты пакета: пересчитывает неделю и день недели для всех позиций.</summary>
    Task<Result<CorrectionBatchResponse>> UpdateBatchAsync(
        Guid id,
        CreateCorrectionBatchRequest request,
        CancellationToken ct
    );

    Task<Result<PagedResponse<CorrectionBatchResponse>>> GetBatchesAsync(
        CorrectionBatchStatus? status,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    );

    Task<Result<CorrectionBatchResponse>> GetBatchAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Удаляет пакет. Применённый пакет перед удалением откатывается: расписание
    /// возвращается к состоянию до корректировки, записи журнала исчезают.
    /// </summary>
    Task<Result<CorrectionBatchDeleteResult>> DeleteBatchAsync(Guid id, CancellationToken ct);

    /// <summary>Удаляет все применённые пакеты разом (например, в начале нового семестра).</summary>
    Task<Result<CorrectionBatchDeleteResult>> ClearAppliedBatchesAsync(CancellationToken ct);

    Task<Result<CorrectionPositionResponse>> AddPositionAsync(
        Guid batchId,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    );

    Task<Result<CorrectionPositionResponse>> UpdatePositionAsync(
        Guid batchId,
        Guid positionId,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    );

    Task<Result> DeletePositionAsync(Guid batchId, Guid positionId, CancellationToken ct);

    Task<Result<CorrectionImportResponse>> ImportAsync(
        Stream fileStream,
        Guid createdByUserId,
        CancellationToken ct
    );

    Task<Result<DocumentDownloadResult>> ExportAsync(Guid batchId, CancellationToken ct);

    Task<Result<CorrectionApplyResult>> ApplyAsync(
        Guid batchId,
        string idempotencyKey,
        Guid appliedByUserId,
        CancellationToken ct
    );
}
