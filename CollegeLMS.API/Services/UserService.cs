using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
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

    public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Result<UserProfileResponse>.Fail("Пользователь не найден", 404);

        var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == id, ct);
        var courses = new List<UserCourseItem>();
        if (teacher is not null)
        {
            courses = await db
                .Courses.AsNoTracking()
                .Where(c => c.TeacherId == teacher.Id)
                .OrderBy(c => c.Title)
                .Select(c => new UserCourseItem { Id = c.Id, Title = c.Title })
                .ToListAsync(ct);
        }

        return Result<UserProfileResponse>.Ok(
            new UserProfileResponse { User = user.ToDto(), Courses = courses }
        );
    }

    public async Task<Result<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct
    )
    {
        var exists = await db.Users.AnyAsync(u => u.Login == request.Login, ct);
        if (exists)
            return Result<UserResponse>.Fail("Пользователь с таким логином уже существует", 409);

        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            return Result<UserResponse>.Fail("Пользователь с таким email уже существует", 409);

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

        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id, ct);
        if (emailExists)
            return Result<UserResponse>.Fail("Пользователь с таким email уже существует", 409);

        if (
            user.Role == UserRole.Admin
            && request.Role != UserRole.Admin
            && !await db.Users.AnyAsync(u => u.Role == UserRole.Admin && u.Id != id, ct)
        )
            return Result<UserResponse>.Fail("Нельзя изменить роль последнего администратора", 409);

        user.Email = request.Email;
        user.Login = request.Login;
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        var profileResult = await EnsureProfileAsync(user, ct);
        if (!profileResult.IsSuccess)
            return Result<UserResponse>.Fail(
                profileResult.ErrorMessage ?? "Не удалось создать профиль",
                profileResult.StatusCode
            );

        await db.SaveChangesAsync(ct);
        return Result<UserResponse>.Ok(user.ToDto());
    }

    private async Task<Result> EnsureProfileAsync(User user, CancellationToken ct)
    {
        if (user.Role == UserRole.Teacher)
        {
            var exists = await db.Teachers.AnyAsync(t => t.UserId == user.Id, ct);
            if (!exists)
            {
                db.Teachers.Add(
                    new Teacher
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CyclicalCommission = "Не назначена",
                        Position = "Преподаватель",
                        Category = Entities.Enums.TeacherCategory.None,
                    }
                );
            }
        }
        else if (user.Role == UserRole.Student)
        {
            var exists = await db.Students.AnyAsync(s => s.UserId == user.Id, ct);
            if (!exists)
            {
                var group = await db.Groups.OrderBy(g => g.Name).FirstOrDefaultAsync(ct);
                if (group is null)
                    return Result.Fail(
                        "Невозможно назначить роль «Студент»: в системе нет ни одной группы",
                        400
                    );

                db.Students.Add(
                    new Student
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        GroupId = group.Id,
                        RecordBookNumber =
                            $"ЗН-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    }
                );
            }
        }

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Result.Fail("Пользователь не найден", 404);

        var admin = await db
            .Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Admin && u.Id != id)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (admin is null)
        {
            if (user.Role == UserRole.Admin)
                return Result.Fail("Нельзя удалить последнего администратора", 409);

            return Result.Fail("Не найден системный администратор", 500);
        }

        var news = await db.News.Where(n => n.CreatedById == id).ToListAsync(ct);
        foreach (var item in news)
            item.CreatedById = admin.Id;

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == id, ct);
        if (teacher is not null)
        {
            var adminTeacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == admin.Id, ct);
            if (adminTeacher is null)
            {
                adminTeacher = new Teacher
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    CyclicalCommission = "Администрация",
                    Position = "Преподаватель",
                };
                db.Teachers.Add(adminTeacher);
            }

            var courses = await db.Courses.Where(c => c.TeacherId == teacher.Id).ToListAsync(ct);
            foreach (var course in courses)
                course.TeacherId = adminTeacher.Id;

            db.Teachers.Remove(teacher);
        }

        db.Users.Remove(user);
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

        if (user.Role == UserRole.Admin && request.Role != UserRole.Admin)
        {
            var otherAdminExists = await db.Users.AnyAsync(
                u => u.Role == UserRole.Admin && u.Id != id,
                ct
            );
            if (!otherAdminExists)
                return Result<UserResponse>.Fail(
                    "Нельзя изменить роль последнего администратора",
                    409
                );
        }

        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        var profileResult = await EnsureProfileAsync(user, ct);
        if (!profileResult.IsSuccess)
            return Result<UserResponse>.Fail(
                profileResult.ErrorMessage ?? "Не удалось создать профиль",
                profileResult.StatusCode
            );

        await db.SaveChangesAsync(ct);

        return Result<UserResponse>.Ok(user.ToDto());
    }
}
