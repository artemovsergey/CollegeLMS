using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

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

        var token = tokenService.GenerateAccessToken(user);

        Guid? teacherId = null;
        if (user.Role == Entities.Enums.UserRole.Teacher)
        {
            teacherId = (await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id, ct))?.Id;
        }

        return Result<LoginResponse>.Ok(new LoginResponse { Token = token, User = user.ToDto(teacherId) });
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

        if (user.Role == Entities.Enums.UserRole.Teacher)
        {
            var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
            if (teacher is not null)
            {
                if (!string.IsNullOrWhiteSpace(request.CyclicalCommission))
                    teacher.CyclicalCommission = request.CyclicalCommission;
                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    if (Enum.TryParse<Entities.Enums.TeacherCategory>(request.Category, out var category))
                        teacher.Category = category;
                }
                teacher.UpdatedAt = DateTime.UtcNow;
            }
        }

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

    public async Task<Result<ProfileResponse>> UploadAvatarAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<ProfileResponse>.Fail("Пользователь не найден", 404);

        if (user.Role == Entities.Enums.UserRole.Student)
            return Result<ProfileResponse>.Fail("Студенты не могут менять аватар", 403);

        if (file is null || file.Length == 0)
            return Result<ProfileResponse>.Fail("Файл не выбран", 400);

        if (file.Length > 5 * 1024 * 1024)
            return Result<ProfileResponse>.Fail("Файл больше 5 МБ", 400);

        if (file.ContentType is not ("image/jpeg" or "image/png"))
            return Result<ProfileResponse>.Fail("Разрешены только JPEG и PNG", 400);

        var uploadsDir = Path.Combine("uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);
        var outputPath = Path.Combine(uploadsDir, $"{userId}.jpg");

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, ct);
        image.Mutate(x =>
            x.Resize(
                new ResizeOptions
                {
                    Size = new Size(256, 256),
                    Mode = ResizeMode.Crop,
                }
            )
        );
        var encoder = new JpegEncoder { Quality = 85 };
        await image.SaveAsync(outputPath, encoder, ct);

        user.AvatarPath = $"/uploads/avatars/{userId}.jpg";
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
