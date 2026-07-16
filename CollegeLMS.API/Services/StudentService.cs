using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
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

    public async Task<Result<StudentImportProgress>> ImportAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Result<StudentImportProgress>.Fail("Файл не загружен", 400);

        var progress = new StudentImportProgress();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        string? headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
            return Result<StudentImportProgress>.Fail("Файл пуст", 400);

        int row = 1;
        while (!reader.EndOfStream)
        {
            row++;
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                progress.Errors.Add(new ImportError { Row = row, Message = "Недостаточно полей" });
                continue;
            }

            var fullName = parts[0].Trim();
            var groupName = parts[1].Trim();
            var recordBook = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                progress.Errors.Add(new ImportError { Row = row, Message = "ФИО обязательно" });
                continue;
            }

            var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == groupName, ct);
            if (group is null)
            {
                progress.Errors.Add(new ImportError { Row = row, Message = $"Группа '{groupName}' не найдена" });
                continue;
            }

            var exists = await db.Students.AnyAsync(s => s.RecordBookNumber == recordBook, ct);
            if (exists)
            {
                progress.Skipped++;
                continue;
            }

            var login = $"student{recordBook.Replace("-", "").Replace("/", "").Substring(0, Math.Min(6, recordBook.Length))}";
            var loginExists = await db.Users.AnyAsync(u => u.Login == login, ct);
            if (loginExists)
                login = $"{login}{Guid.NewGuid().ToString()[..4]}";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                FullName = fullName,
                Role = UserRole.Student,
                IsActive = true,
            };
            db.Users.Add(user);

            db.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GroupId = group.Id,
                RecordBookNumber = recordBook,
            });

            progress.Imported++;
        }

        await db.SaveChangesAsync(ct);
        return Result<StudentImportProgress>.Ok(progress);
    }

    public async Task<Result<StudentResponse>> TransferAsync(Guid id, TransferStudentRequest request, CancellationToken ct)
    {
        var student = await db.Students.Include(s => s.User).Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null)
            return Result<StudentResponse>.Fail("Студент не найден", 404);

        var newGroup = await db.Groups.FindAsync([request.NewGroupId], ct);
        if (newGroup is null)
            return Result<StudentResponse>.Fail("Группа не найдена", 404);

        var oldGroupId = student.GroupId;

        db.TransferRecords.Add(new TransferRecord
        {
            Id = Guid.NewGuid(),
            StudentId = id,
            FromGroupId = oldGroupId,
            ToGroupId = request.NewGroupId,
            Reason = request.Reason,
        });

        student.GroupId = request.NewGroupId;
        student.Group = newGroup;
        student.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<StudentResponse>.Ok(student.ToDto());
    }

    public async Task<Result<List<TransferRecordResponse>>> GetTransfersAsync(Guid id, CancellationToken ct)
    {
        var student = await db.Students.AnyAsync(s => s.Id == id, ct);
        if (!student)
            return Result<List<TransferRecordResponse>>.Fail("Студент не найден", 404);

        var records = await db.TransferRecords.AsNoTracking()
            .Where(r => r.StudentId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var groupIds = records.SelectMany(r => new[] { r.FromGroupId, r.ToGroupId }).Distinct().ToList();
        var groups = await db.Groups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

        var result = records.Select(r => new TransferRecordResponse
        {
            Id = r.Id,
            StudentId = r.StudentId,
            FromGroupId = r.FromGroupId,
            FromGroupName = groups.GetValueOrDefault(r.FromGroupId, string.Empty),
            ToGroupId = r.ToGroupId,
            ToGroupName = groups.GetValueOrDefault(r.ToGroupId, string.Empty),
            Reason = r.Reason,
            CreatedAt = r.CreatedAt,
        }).ToList();

        return Result<List<TransferRecordResponse>>.Ok(result);
    }
}
