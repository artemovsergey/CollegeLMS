using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class SubmissionService(AppDbContext db) : ISubmissionService
{
    public async Task<Result<SubmissionResponse>> SubmitAsync(Guid assignmentId, SubmitAssignmentRequest request, Guid currentUserId, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment is null)
            return Result<SubmissionResponse>.Fail("Задание не найдено", 404);

        var student = await db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);

        if (student is null)
            return Result<SubmissionResponse>.Fail("Студент не найден", 404);

        if (student.GroupId != assignment.Course.GroupId)
            return Result<SubmissionResponse>.Fail("Вы не зачислены на этот курс", 403);

        var existing = await db.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == student.Id, ct);

        if (existing is not null)
        {
            existing.FilePath = request.FilePath;
            existing.Comment = request.Comment;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = student.Id,
                FilePath = request.FilePath,
                Comment = request.Comment,
                SubmittedAt = DateTime.UtcNow,
            };
            db.AssignmentSubmissions.Add(existing);
        }

        await db.SaveChangesAsync(ct);

        existing.Student = student;
        return Result<SubmissionResponse>.Ok(existing.ToDto());
    }

    public async Task<Result<List<SubmissionResponse>>> GetSubmissionsAsync(Guid assignmentId, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment is null)
            return Result<List<SubmissionResponse>>.Fail("Задание не найдено", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || assignment.Course.TeacherId != teacher.Id)
                return Result<List<SubmissionResponse>>.Fail("У вас нет прав на просмотр ответов этого задания", 403);
        }

        var submissions = await db.AssignmentSubmissions
            .AsNoTracking()
            .Include(s => s.Student).ThenInclude(s => s.User)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);

        return Result<List<SubmissionResponse>>.Ok(submissions.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<SubmissionResponse>> GradeAsync(Guid submissionId, GradeSubmissionRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var submission = await db.AssignmentSubmissions
            .Include(s => s.Assignment)
            .Include(s => s.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
            return Result<SubmissionResponse>.Fail("Ответ не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var course = await db.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == submission.Assignment.CourseId, ct);

            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course is null || course.TeacherId != teacher.Id)
                return Result<SubmissionResponse>.Fail("У вас нет прав на оценку этого ответа", 403);
        }

        if (request.Score > submission.Assignment.MaxScore)
            return Result<SubmissionResponse>.Fail($"Оценка не может превышать максимальный балл ({submission.Assignment.MaxScore})", 400);

        submission.Score = request.Score;
        submission.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<SubmissionResponse>.Ok(submission.ToDto());
    }

    public async Task<Result<List<SubmissionResponse>>> GetMySubmissionsAsync(Guid currentUserId, CancellationToken ct)
    {
        var student = await db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);

        if (student is null)
            return Result<List<SubmissionResponse>>.Fail("Студент не найден", 404);

        var submissions = await db.AssignmentSubmissions
            .AsNoTracking()
            .Include(s => s.Student).ThenInclude(s => s.User)
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);

        return Result<List<SubmissionResponse>>.Ok(submissions.Select(s => s.ToDto()).ToList());
    }
}
