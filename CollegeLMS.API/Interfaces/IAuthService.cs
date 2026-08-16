using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<ProfileResponse>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default
    );
    Task<Result<ProfileResponse>> UploadAvatarAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct = default
    );
    Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken ct = default
    );
}
