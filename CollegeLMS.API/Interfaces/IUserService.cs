using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IUserService
{
    Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct = default
    );
    Task<Result<UserResponse>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken ct = default
    );
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserResponse>> ToggleActiveAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserResponse>> ChangeRoleAsync(
        Guid id,
        ChangeRoleRequest request,
        CancellationToken ct = default
    );
}
