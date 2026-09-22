using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class MaxAuthService(
    MaxInitDataValidator validator,
    MaxBotHttpClient botClient,
    ITokenService tokenService
) : IMaxAuthService
{
    private const int TokenLifetimeMinutes = 1440;

    public async Task<Result<MaxAuthResponse>> LoginAsync(
        MaxAuthRequest request,
        CancellationToken ct
    )
    {
        var validation = validator.Validate(request.InitData);
        if (!validation.IsSuccess)
            return Result<MaxAuthResponse>.Fail(
                validation.ErrorMessage ?? "Невалидный initData.",
                401
            );

        var payload = validation.Data!;
        var botProfile = await botClient.GetInternalUserAsync(payload.MaxUserId, ct);

        var role = MapRole(botProfile?.Role);
        var profile = new MaxAuthProfile
        {
            MaxUserId = payload.MaxUserId,
            FullName = payload.FullName,
            Role = role,
            GroupId = botProfile?.GroupId,
            GroupName = botProfile?.GroupName,
            TeacherId = botProfile?.TeacherId,
            TeacherName = botProfile?.TeacherName,
        };

        var claims = new List<Claim> { new("max_user_id", payload.MaxUserId.ToString()) };
        if (profile.GroupId.HasValue)
            claims.Add(new Claim("groupId", profile.GroupId.Value.ToString()));
        if (profile.TeacherId.HasValue)
            claims.Add(new Claim("teacherId", profile.TeacherId.Value.ToString()));

        var roles = role == "Other" ? Array.Empty<string>() : new[] { role };
        var token = tokenService.GenerateCustomToken(
            roles,
            TokenLifetimeMinutes,
            SyntheticUserId(payload.MaxUserId).ToString(),
            claims
        );

        return Result<MaxAuthResponse>.Ok(new MaxAuthResponse { Token = token, Profile = profile });
    }

    private static string MapRole(string? botRole) =>
        botRole?.ToLowerInvariant() switch
        {
            "student" => "Student",
            "teacher" => "Teacher",
            _ => "Other",
        };

    private static Guid SyntheticUserId(long maxUserId)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"max:{maxUserId}"));
        return new Guid(bytes);
    }
}
