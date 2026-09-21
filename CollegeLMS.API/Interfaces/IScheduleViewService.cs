using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleViewService
{
    Task<Result<ScheduleDayViewResponse>> GetDayAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime? date,
        CancellationToken ct
    );

    Task<Result<ScheduleWeekViewResponse>> GetWeekAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        int? week,
        DateTime? date,
        CancellationToken ct
    );

    Task<Result<ScheduleSemesterViewResponse>> GetSemesterAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    );

    Task<Result<ScheduleMonthViewResponse>> GetMonthAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? month,
        CancellationToken ct
    );
}
