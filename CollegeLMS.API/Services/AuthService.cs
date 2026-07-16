using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == request.Login, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Fail("Неверный логин или пароль", 401);

        if (!user.IsActive)
            return Result<LoginResponse>.Fail("Пользователь деактивирован", 403);

        var token = tokenService.GenerateAccessToken(user);

        return Result<LoginResponse>.Ok(new LoginResponse { Token = token, User = user.ToDto() });
    }

    public async Task<Result<UserResponse>> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<UserResponse>.Fail("Пользователь не найден", 404);

        return Result<UserResponse>.Ok(user.ToDto());
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result.Fail("Пользователь не найден", 404);

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return Result.Fail("Неверный старый пароль", 400);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
