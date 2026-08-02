using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IStvccHealthService
{
    Task<Result<StvccHealthDto>> CheckAsync(CancellationToken ct);
}
