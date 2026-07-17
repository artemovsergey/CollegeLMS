using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ExamService(AppDbContext db) : IExamService
{
    public async Task<Result<List<ExamResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? semesterId,
        string? type,
        CancellationToken ct
    )
    {
        var query = db
            .Exams.AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Teacher)
                .ThenInclude(t => t.User)
            .Include(e => e.Semester)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(e => e.GroupId == groupId.Value);
        if (semesterId.HasValue)
            query = query.Where(e => e.SemesterId == semesterId.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type.ToString() == type);

        var exams = await query.OrderBy(e => e.ExamDate).ToListAsync(ct);
        return Result<List<ExamResponse>>.Ok(exams.Select(e => e.ToDto()).ToList());
    }

    public async Task<Result<ExamResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var exam = await db
            .Exams.AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Teacher)
                .ThenInclude(t => t.User)
            .Include(e => e.Semester)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exam is null)
            return Result<ExamResponse>.Fail("Экзамен не найден", 404);
        return Result<ExamResponse>.Ok(exam.ToDto());
    }

    public async Task<Result<ExamResponse>> CreateAsync(
        CreateExamRequest request,
        CancellationToken ct
    )
    {
        if (!Enum.TryParse<ExamType>(request.Type, out var examType))
            return Result<ExamResponse>.Fail("Некорректный тип экзамена", 400);

        if (!await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct))
            return Result<ExamResponse>.Fail("Группа не найдена", 404);
        if (!await db.Teachers.AnyAsync(t => t.Id == request.TeacherId, ct))
            return Result<ExamResponse>.Fail("Преподаватель не найден", 404);
        if (!await db.Semesters.AnyAsync(s => s.Id == request.SemesterId, ct))
            return Result<ExamResponse>.Fail("Семестр не найден", 404);

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            GroupId = request.GroupId,
            ExamDate = request.ExamDate,
            Type = examType,
            TeacherId = request.TeacherId,
            SemesterId = request.SemesterId,
            Status = ExamStatus.Scheduled,
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(exam.Id, ct);
    }

    public async Task<Result<ExamResponse>> UpdateAsync(
        Guid id,
        UpdateExamRequest request,
        CancellationToken ct
    )
    {
        var exam = await db.Exams.FindAsync([id], ct);
        if (exam is null)
            return Result<ExamResponse>.Fail("Экзамен не найден", 404);

        if (!Enum.TryParse<ExamType>(request.Type, out var examType))
            return Result<ExamResponse>.Fail("Некорректный тип экзамена", 400);
        if (!Enum.TryParse<ExamStatus>(request.Status, out var status))
            return Result<ExamResponse>.Fail("Некорректный статус", 400);

        exam.Subject = request.Subject;
        exam.GroupId = request.GroupId;
        exam.ExamDate = request.ExamDate;
        exam.Type = examType;
        exam.TeacherId = request.TeacherId;
        exam.SemesterId = request.SemesterId;
        exam.Status = status;
        exam.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(exam.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var exam = await db.Exams.FindAsync([id], ct);
        if (exam is null)
            return Result.Fail("Экзамен не найден", 404);

        db.Exams.Remove(exam);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
