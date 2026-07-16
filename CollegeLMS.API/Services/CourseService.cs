using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseService(AppDbContext db) : ICourseService
{
    public async Task<Result<List<CourseResponse>>> GetAllAsync(
        Guid? teacherId,
        Guid? groupId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var query = db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .AsQueryable();

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null)
                return Result<List<CourseResponse>>.Fail("Преподаватель не найден", 404);

            query = query.Where(c => c.TeacherId == teacher.Id);
        }

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (groupId.HasValue)
            query = query.Where(c => c.GroupId == groupId.Value);

        var courses = await query.OrderBy(c => c.Title).ToListAsync(ct);

        return Result<List<CourseResponse>>.Ok(courses.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var course = await db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        return Result<CourseResponse>.Ok(course.ToDto());
    }

    public async Task<Result<CourseResponse>> CreateAsync(
        CreateCourseRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        Guid teacherId;

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null)
                return Result<CourseResponse>.Fail("Преподаватель не найден", 404);

            teacherId = teacher.Id;
        }
        else
        {
            if (!request.TeacherId.HasValue)
                return Result<CourseResponse>.Fail("Администратор должен указать TeacherId", 400);

            teacherId = request.TeacherId.Value;
        }

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<CourseResponse>.Fail("Группа не найдена", 404);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            TeacherId = teacherId,
            GroupId = request.GroupId,
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);

        course = await db
            .Courses.Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstAsync(c => c.Id == course.Id, ct);

        return Result<CourseResponse>.Ok(course.ToDto());
    }

    public async Task<Result<CourseResponse>> UpdateAsync(
        Guid id,
        UpdateCourseRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<CourseResponse>.Fail(
                    "У вас нет прав на редактирование этого курса",
                    403
                );
        }

        var groupExists = await db.Groups.AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return Result<CourseResponse>.Fail("Группа не найдена", 404);

        course.Title = request.Title;
        course.Description = request.Description;
        course.GroupId = request.GroupId;
        course.Status = Enum.Parse<CourseStatus>(request.Status);
        course.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<CourseResponse>.Ok(course.ToDto());
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("У вас нет прав на удаление этого курса", 403);
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> AssignGroupsAsync(
        Guid courseId, AssignGroupsRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([courseId], ct);
        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (!await CanManageCourse(courseId, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на редактирование этого курса", 403);

        foreach (var groupId in request.GroupIds)
        {
            var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId, ct);
            if (!groupExists)
                return Result.Fail($"Группа {groupId} не найдена", 404);

            var alreadyAssigned = await db.CourseGroups.AnyAsync(
                cg => cg.CourseId == courseId && cg.GroupId == groupId, ct);
            if (!alreadyAssigned)
            {
                db.CourseGroups.Add(new CourseGroup
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    GroupId = groupId,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<List<CourseGroupResponse>>> GetCourseGroupsAsync(Guid courseId, CancellationToken ct)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<CourseGroupResponse>>.Fail("Курс не найден", 404);

        var groups = await db.CourseGroups.AsNoTracking()
            .Include(cg => cg.Group)
            .Where(cg => cg.CourseId == courseId)
            .Select(cg => new CourseGroupResponse
            {
                GroupId = cg.GroupId,
                GroupName = cg.Group.Name,
            })
            .ToListAsync(ct);

        return Result<List<CourseGroupResponse>>.Ok(groups);
    }

    public async Task<Result> RemoveGroupAsync(
        Guid courseId, Guid groupId, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([courseId], ct);
        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (!await CanManageCourse(courseId, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на редактирование этого курса", 403);

        var courseGroup = await db.CourseGroups
            .FirstOrDefaultAsync(cg => cg.CourseId == courseId && cg.GroupId == groupId, ct);
        if (courseGroup is null)
            return Result.Fail("Группа не привязана к курсу", 404);

        db.CourseGroups.Remove(courseGroup);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<CourseProgressResponse>> GetProgressAsync(
        Guid courseId, Guid currentUserId, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking()
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<CourseProgressResponse>.Fail("Курс не найден", 404);

        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<CourseProgressResponse>.Fail("Студент не найден", 404);

        var inGroup = await db.CourseGroups.AnyAsync(cg => cg.CourseId == courseId && cg.GroupId == student.GroupId, ct)
            || course.GroupId == student.GroupId;
        if (!inGroup)
            return Result<CourseProgressResponse>.Fail("Вы не привязаны к этому курсу", 403);

        var totalAssignments = course.Assignments.Count;
        var completedAssignments = await db.AssignmentSubmissions
            .CountAsync(s => s.StudentId == student.Id
                && course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                && s.Score.HasValue, ct);

        var totalTests = await db.Tests.CountAsync(t => t.CourseId == courseId, ct);
        var completedTests = await db.TestAttempts
            .CountAsync(a => a.StudentId == student.Id
                && a.Test.CourseId == courseId
                && a.Status == Entities.Enums.AttemptStatus.Completed, ct);

        var scoreSubmissions = await db.AssignmentSubmissions
            .Where(s => s.StudentId == student.Id
                && course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                && s.Score.HasValue)
            .Select(s => s.Score!.Value)
            .ToListAsync(ct);
        var avgScore = scoreSubmissions.Count > 0 ? scoreSubmissions.Average() : 0;

        var total = totalAssignments + totalTests;
        var completed = completedAssignments + completedTests;

        return Result<CourseProgressResponse>.Ok(new CourseProgressResponse
        {
            CourseId = courseId,
            CourseTitle = course.Title,
            TotalAssignments = totalAssignments,
            CompletedAssignments = completedAssignments,
            TotalTests = totalTests,
            CompletedTests = completedTests,
            AverageScore = totalAssignments > 0 ? Math.Round(avgScore, 1) : 0,
            CompletionPercent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
        });
    }

    private async Task<bool> CanManageCourse(
        Guid courseId, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        if (currentUserRole == "Admin") return true;
        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null) return false;
            return await db.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacher.Id, ct);
        }
        return false;
    }
}
