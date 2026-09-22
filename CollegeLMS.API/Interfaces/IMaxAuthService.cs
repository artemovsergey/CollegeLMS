using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IMaxAuthService
{
    Task<Result<MaxAuthResponse>> LoginAsync(MaxAuthRequest request, CancellationToken ct);
}
