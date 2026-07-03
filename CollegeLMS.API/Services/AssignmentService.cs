using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class AssignmentService(AppDbContext db) : IAssignmentService
{
    public async Task<Result<List<AssignmentResponse>>> GetAllAsync(Guid courseId, CancellationToken ct)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<AssignmentResponse>>.Fail("Курс не найден", 404);

        var assignments = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.Order)
            .ToListAsync(ct);

        return Result<List<AssignmentResponse>>.Ok(assignments.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<AssignmentResponse>> GetByIdAsync(Guid courseId, Guid id, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id && a.CourseId == courseId, ct);

        if (assignment is null)
            return Result<AssignmentResponse>.Fail("Задание не найдено", 404);

        return Result<AssignmentResponse>.Ok(assignment.ToDto());
    }

    public async Task<Result<AssignmentResponse>> CreateAsync(
        Guid courseId, CreateAssignmentRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<AssignmentResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<AssignmentResponse>.Fail("У вас нет прав на добавление заданий в этот курс", 403);
        }

        var maxOrder = await db.Assignments
            .Where(a => a.CourseId == courseId)
            .MaxAsync(a => (int?)a.Order, ct) ?? 0;

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            MaxScore = request.MaxScore,
            Order = maxOrder + 1,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return Result<AssignmentResponse>.Ok(assignment.ToDto());
    }

    public async Task<Result<AssignmentResponse>> UpdateAsync(
        Guid courseId, Guid id, UpdateAssignmentRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id && a.CourseId == courseId, ct);

        if (assignment is null)
            return Result<AssignmentResponse>.Fail("Задание не найдено", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<AssignmentResponse>.Fail("У вас нет прав на редактирование заданий в этом курсе", 403);
        }

        assignment.Title = request.Title;
        assignment.Description = request.Description;
        assignment.DueDate = request.DueDate;
        assignment.MaxScore = request.MaxScore;
        assignment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<AssignmentResponse>.Ok(assignment.ToDto());
    }

    public async Task<Result> DeleteAsync(
        Guid courseId, Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.Id == id && a.CourseId == courseId, ct);

        if (assignment is null)
            return Result.Fail("Задание не найдено", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("У вас нет прав на удаление заданий из этого курса", 403);
        }

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
