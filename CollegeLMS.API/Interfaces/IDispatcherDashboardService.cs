using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IDispatcherDashboardService
{
    Task<Result<DispatcherDashboardResponse>> GetDailyAsync(DateTime date, CancellationToken ct);
}
