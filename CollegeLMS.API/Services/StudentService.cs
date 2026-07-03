using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class StudentService(AppDbContext db) : IStudentService
{
    public async Task<Result<List<StudentResponse>>> GetAllAsync(
        Guid? groupId,
        CancellationToken ct
    )
    {
        IQueryable<Student> query = db
            .Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Group);

        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);

        var students = await query.OrderBy(s => s.User.FullName).ToListAsync(ct);

        return Result<List<StudentResponse>>.Ok(students.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<StudentResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var student = await db
            .Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (student is null)
            return Result<StudentResponse>.Fail("Студент не найден", 404);

        return Result<StudentResponse>.Ok(student.ToDto());
    }

    public async Task<Result<StudentResponse>> CreateAsync(
        CreateStudentRequest request,
        CancellationToken ct
    )
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            return Result<StudentResponse>.Fail("Пользователь с таким email уже существует", 409);

        var bookExists = await db.Students.AnyAsync(
            s => s.RecordBookNumber == request.RecordBookNumber,
            ct
        );
        if (bookExists)
            return Result<StudentResponse>.Fail(
                "Студент с таким номером зачётной книжки уже существует",
                409
            );

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<StudentResponse>.Fail("Группа не найдена", 404);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(user);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            GroupId = request.GroupId,
            RecordBookNumber = request.RecordBookNumber,
        };
        db.Students.Add(student);

        await db.SaveChangesAsync(ct);

        student.User = user;
        student.Group = (await db.Groups.FindAsync([student.GroupId], ct))!;
        return Result<StudentResponse>.Ok(student.ToDto());
    }

    public async Task<Result<StudentResponse>> UpdateAsync(
        Guid id,
        UpdateStudentRequest request,
        CancellationToken ct
    )
    {
        var student = await db
            .Students.Include(s => s.User)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (student is null)
            return Result<StudentResponse>.Fail("Студент не найден", 404);

        var emailExists = await db.Users.AnyAsync(
            u => u.Email == request.Email && u.Id != student.UserId,
            ct
        );
        if (emailExists)
            return Result<StudentResponse>.Fail("Пользователь с таким email уже существует", 409);

        var bookExists = await db.Students.AnyAsync(
            s => s.RecordBookNumber == request.RecordBookNumber && s.Id != id,
            ct
        );
        if (bookExists)
            return Result<StudentResponse>.Fail(
                "Студент с таким номером зачётной книжки уже существует",
                409
            );

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<StudentResponse>.Fail("Группа не найдена", 404);

        if (student.GroupId != request.GroupId)
            student.Group = (await db.Groups.FindAsync([request.GroupId], ct))!;

        student.User.Email = request.Email;
        student.User.FullName = request.FullName;
        student.User.UpdatedAt = DateTime.UtcNow;
        student.GroupId = request.GroupId;
        student.RecordBookNumber = request.RecordBookNumber;
        student.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<StudentResponse>.Ok(student.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var student = await db
            .Students.Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (student is null)
            return Result.Fail("Студент не найден", 404);

        student.User.IsActive = false;
        student.User.UpdatedAt = DateTime.UtcNow;
        student.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
