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

    public async Task<Result<ProfileResponse>> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<ProfileResponse>.Fail("Пользователь не найден", 404);

        object? roleData = null;

        if (user.Role == Entities.Enums.UserRole.Teacher)
        {
            roleData = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        }
        else if (user.Role == Entities.Enums.UserRole.Student)
        {
            roleData = await db
                .Students.AsNoTracking()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        }

        return Result<ProfileResponse>.Ok(user.ToProfileDto(roleData));
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([userId], ct);

        if (user is null)
            return Result<ProfileResponse>.Fail("Пользователь не найден", 404);

        var emailExists = await db.Users.AnyAsync(
            u => u.Email == request.Email && u.Id != userId,
            ct
        );
        if (emailExists)
            return Result<ProfileResponse>.Fail("Пользователь с таким email уже существует", 409);

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        object? roleData = null;

        if (user.Role == Entities.Enums.UserRole.Teacher)
        {
            roleData = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        }
        else if (user.Role == Entities.Enums.UserRole.Student)
        {
            roleData = await db
                .Students.AsNoTracking()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        }

        return Result<ProfileResponse>.Ok(user.ToProfileDto(roleData));
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
