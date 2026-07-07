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
        int? page,
        int? pageSize,
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
}
