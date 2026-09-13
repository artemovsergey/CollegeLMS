using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IDispatcherAuthService
{
    Task<Result<DispatcherLoginResponse>> LoginAsync(
        string password,
        string clientIp,
        CancellationToken ct
    );
}
