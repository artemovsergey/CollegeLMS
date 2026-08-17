using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<Result<TeacherDashboardResponse>> GetTeacherDashboardAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher is null)
            return Result<TeacherDashboardResponse>.Fail("Преподаватель не найден", 404);

        var courses = await db
            .Courses.AsNoTracking()
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
            .Include(c => c.CourseAuthors)
            .Where(
                c =>
                    c.IsActive
                    && (c.TeacherId == teacher.Id || c.CourseAuthors.Any(a => a.TeacherId == teacher.Id))
            )
            .Select(c => new CourseBriefDto
            {
                Id = c.Id,
                Title = c.Title,
                GroupNames = string.Join(", ", c.CourseGroups.Select(cg => cg.Group.Name)),
            })
            .ToListAsync(ct);

        return Result<TeacherDashboardResponse>.Ok(
            new TeacherDashboardResponse { Courses = courses }
        );
    }

    public async Task<Result<StudentDashboardResponse>> GetStudentDashboardAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student is null)
            return Result<StudentDashboardResponse>.Fail("Студент не найден", 404);

        var courseIds = await db
            .CourseGroups.AsNoTracking()
            .Where(cg => cg.GroupId == student.GroupId)
            .Select(cg => cg.CourseId)
            .ToListAsync(ct);

        var courses = await db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Assignments)
            .Where(c => courseIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(ct);

        var result = new List<CourseWithProgressDto>();
        foreach (var course in courses)
        {
            var totalAssignments = course.Assignments.Count;
            var completedAssignments = await db.AssignmentSubmissions.CountAsync(
                s =>
                    course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                    && s.StudentId == student.Id
                    && s.Score.HasValue,
                ct
            );

            var totalTests = await db.Tests.CountAsync(t => t.CourseId == course.Id, ct);
            var completedTests = await db.TestAttempts.CountAsync(
                a =>
                    a.StudentId == student.Id
                    && a.Test.CourseId == course.Id
                    && a.Status == Entities.Enums.AttemptStatus.Completed,
                ct
            );

            var total = totalAssignments + totalTests;
            var completed = completedAssignments + completedTests;
            var percent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

            result.Add(
                new CourseWithProgressDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    TeacherName = course.Teacher?.User?.FullName ?? "",
                    CompletionPercent = percent,
                    CompletedItems = completed,
                    TotalItems = total,
                }
            );
        }

        return Result<StudentDashboardResponse>.Ok(
            new StudentDashboardResponse { Courses = result }
        );
    }

    public async Task<Result<AdminDashboardResponse>> GetAdminDashboardAsync(CancellationToken ct)
    {
        var userCount = await db.Users.CountAsync(ct);
        var teacherCount = await db.Teachers.CountAsync(ct);
        var studentCount = await db.Students.CountAsync(ct);
        var courseCount = await db.Courses.CountAsync(ct);
        var groupCount = await db.Groups.CountAsync(ct);
        var newsCount = await db.News.CountAsync(n => !n.IsDeleted, ct);
        var feedbackCount = await db.Feedbacks.CountAsync(ct);

        return Result<AdminDashboardResponse>.Ok(
            new AdminDashboardResponse
            {
                UserCount = userCount,
                TeacherCount = teacherCount,
                StudentCount = studentCount,
                CourseCount = courseCount,
                GroupCount = groupCount,
                NewsCount = newsCount,
                FeedbackCount = feedbackCount,
            }
        );
    }
}
