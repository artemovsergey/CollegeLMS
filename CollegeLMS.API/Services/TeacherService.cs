using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class TeacherService(AppDbContext db) : ITeacherService
{
    public async Task<Result<List<TeacherResponse>>> GetAllAsync(CancellationToken ct)
    {
        var teachers = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .OrderBy(t => t.User.FullName)
            .ToListAsync(ct);

        return Result<List<TeacherResponse>>.Ok(teachers.Select(t => t.ToDto()).ToList());
    }

    public async Task<Result<TeacherResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (teacher is null)
            return Result<TeacherResponse>.Fail("Преподаватель не найден", 404);

        return Result<TeacherResponse>.Ok(teacher.ToDto());
    }

    public async Task<Result<TeacherResponse>> CreateAsync(
        CreateTeacherRequest request,
        CancellationToken ct
    )
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            return Result<TeacherResponse>.Fail("Пользователь с таким email уже существует", 409);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Teacher,
            IsActive = true,
        };
        db.Users.Add(user);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = request.CyclicalCommission,
            Position = request.Position,
        };
        db.Teachers.Add(teacher);

        await db.SaveChangesAsync(ct);

        teacher.User = user;
        return Result<TeacherResponse>.Ok(teacher.ToDto());
    }

    public async Task<Result<TeacherResponse>> UpdateAsync(
        Guid id,
        UpdateTeacherRequest request,
        CancellationToken ct
    )
    {
        var teacher = await db
            .Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (teacher is null)
            return Result<TeacherResponse>.Fail("Преподаватель не найден", 404);

        var emailExists = await db.Users.AnyAsync(
            u => u.Email == request.Email && u.Id != teacher.UserId,
            ct
        );
        if (emailExists)
            return Result<TeacherResponse>.Fail("Пользователь с таким email уже существует", 409);

        teacher.User.Email = request.Email;
        teacher.User.FullName = request.FullName;
        teacher.User.UpdatedAt = DateTime.UtcNow;
        teacher.CyclicalCommission = request.CyclicalCommission;
        teacher.Position = request.Position;
        teacher.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<TeacherResponse>.Ok(teacher.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var teacher = await db
            .Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (teacher is null)
            return Result.Fail("Преподаватель не найден", 404);

        teacher.User.IsActive = false;
        teacher.User.UpdatedAt = DateTime.UtcNow;
        teacher.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
