using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleService
{
    Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DayOfWeek? dayOfWeek,
        string? period,
        int? week,
        DateTime? date,
        string? view,
        int? page,
        int? pageSize,
        CancellationToken ct = default
    );
    Task<Result<ScheduleMetaResponse>> GetMetaAsync(CancellationToken ct = default);
    Task<Result<ScheduleContextResponse>> GetContextAsync(
        Guid userId,
        CancellationToken ct = default
    );
    Task<Result<ScheduleSearchResponse>> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task<Result<SubjectsResponse>> GetSubjectsAsync(
        string? q,
        Guid? teacherId,
        CancellationToken ct = default
    );
    Task<Result<JournalResponse>> GetJournalAsync(
        Guid teacherId,
        string? subject,
        CancellationToken ct = default
    );
    Task<Result<ScheduleResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct = default
    );

    Task<Result<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken ct = default
    );

    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<ExportResult>> ExportScheduleAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? period,
        string? scope,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct = default
    );
}
