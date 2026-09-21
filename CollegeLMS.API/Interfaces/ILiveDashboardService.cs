using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ILiveDashboardService
{
    Task<Result<LiveDashboardResponse>> GetLiveAsync(
        DateTime? date,
        DateTime? at,
        CancellationToken ct = default
    );
}
