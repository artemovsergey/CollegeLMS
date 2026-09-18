using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleInsertService
{
    Task<Result<List<ScheduleInsertResponse>>> GetAllAsync(
        DayOfWeek? dayOfWeek,
        int? course,
        bool activeOnly,
        CancellationToken ct
    );

    Task<Result<ScheduleInsertResponse>> CreateAsync(
        ScheduleInsertRequest request,
        CancellationToken ct
    );

    Task<Result<ScheduleInsertResponse>> UpdateAsync(
        Guid id,
        ScheduleInsertRequest request,
        CancellationToken ct
    );

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}
