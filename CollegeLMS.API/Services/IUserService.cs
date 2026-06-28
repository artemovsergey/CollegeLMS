using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public interface IUserService
{
    Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct = default);
}
