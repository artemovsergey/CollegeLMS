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

        return BuildResponse(payload.MaxUserId, payload.FullName, botProfile);
    }

    public async Task<Result<MaxAuthResponse>> SelectAsync(
        long maxUserId,
        MaxSelectionRequest request,
        CancellationToken ct
    )
    {
        if ((request.GroupId is null) == (request.TeacherId is null))
            return Result<MaxAuthResponse>.Fail(
                "Нужно выбрать ровно одну цель: группу или преподавателя.",
                400
            );

        var botProfile = await botClient.SetSelectionAsync(
            maxUserId,
            request.GroupId,
            request.TeacherId,
            ct
        );

        // Выбор сохранён только если бот подтвердил запись — иначе мини-апп
        // показывал бы выбор, о котором бот не знает.
        if (botProfile is not { Found: true })
            return Result<MaxAuthResponse>.Fail(
                "Не удалось сохранить выбор. Попробуйте позже.",
                503
            );

        return BuildResponse(maxUserId, null, botProfile);
    }

    /// <summary>Собирает профиль и свежий токен по данным бота.</summary>
    private Result<MaxAuthResponse> BuildResponse(
        long maxUserId,
        string? fullName,
        MaxInternalUserDto? botProfile
    )
    {
        // Нет записи у бота (Found = false) или бот недоступен — гость без роли.
        var hasProfile = botProfile is { Found: true };
        var role = hasProfile ? MapRole(botProfile!.Role) : "Other";
        var profile = new MaxAuthProfile
        {
            MaxUserId = maxUserId,
            FullName = fullName,
            Role = role,
            GroupId = hasProfile ? botProfile!.GroupId : null,
            GroupName = hasProfile ? botProfile!.GroupName : null,
            TeacherId = hasProfile ? botProfile!.TeacherId : null,
            TeacherName = hasProfile ? botProfile!.TeacherName : null,
        };

        var claims = new List<Claim> { new("max_user_id", maxUserId.ToString()) };
        if (profile.GroupId.HasValue)
            claims.Add(new Claim("groupId", profile.GroupId.Value.ToString()));
        if (profile.TeacherId.HasValue)
            claims.Add(new Claim("teacherId", profile.TeacherId.Value.ToString()));

        var roles = role == "Other" ? Array.Empty<string>() : new[] { role };
        var token = tokenService.GenerateCustomToken(
            roles,
            TokenLifetimeMinutes,
            SyntheticUserId(maxUserId).ToString(),
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
