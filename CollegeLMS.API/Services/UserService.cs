using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);

        return Result<List<UserResponse>>.Ok(users.Select(x => x.ToDto()).ToList());
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Result<UserResponse>.Fail("Пользователь не найден", 404);

        return Result<UserResponse>.Ok(user.ToDto());
    }

    public async Task<Result<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct
    )
    {
        var exists = await db.Users.AnyAsync(u => u.Login == request.Login, ct);
        if (exists)
            return Result<UserResponse>.Fail("Пользователь с таким логином уже существует", 409);

        var user = request.ToEntity();
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Result<UserResponse>.Ok(user.ToDto());
    }

    public async Task<Result<UserResponse>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Result<UserResponse>.Fail("Пользователь не найден", 404);

        var loginExists = await db.Users.AnyAsync(u => u.Login == request.Login && u.Id != id, ct);
        if (loginExists)
            return Result<UserResponse>.Fail("Пользователь с таким логином уже существует", 409);

        user.Login = request.Login;
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<UserResponse>.Ok(user.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Result.Fail("Пользователь не найден", 404);

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<UserResponse>> ChangeRoleAsync(
        Guid id,
        ChangeRoleRequest request,
        CancellationToken ct
    )
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Result<UserResponse>.Fail("Пользователь не найден", 404);

        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<UserResponse>.Ok(user.ToDto());
    }
}
