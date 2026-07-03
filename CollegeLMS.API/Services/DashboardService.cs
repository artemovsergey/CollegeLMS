using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<Result<TeacherDashboardResponse>> GetTeacherDashboardAsync(Guid userId, CancellationToken ct)
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher is null)
            return Result<TeacherDashboardResponse>.Fail("Преподаватель не найден", 404);

        var courses = await db.Courses
            .Where(c => c.TeacherId == teacher.Id)
            .ToListAsync(ct);

        var courseIds = courses.Select(c => c.Id).ToList();

        var submissions = await db.AssignmentSubmissions
            .Where(s => courseIds.Contains(s.Assignment.CourseId))
            .Include(s => s.Assignment)
            .Include(s => s.Student).ThenInclude(s => s.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToListAsync(ct);

        var groupIds = courses.Select(c => c.GroupId).ToList();
        var studentsCount = await db.Students
            .Where(s => groupIds.Contains(s.GroupId))
            .CountAsync(ct);

        var response = new TeacherDashboardResponse
        {
            CoursesCount = courses.Count,
            StudentsCount = studentsCount,
            Courses = courses.Select(c => c.Title).ToList(),
            RecentSubmissions = submissions.Select(s => new RecentSubmissionDto
            {
                Id = s.Id,
                StudentName = s.Student.User.FullName,
                AssignmentTitle = s.Assignment.Title,
                SubmittedAt = s.SubmittedAt,
            }).ToList(),
        };

        return Result<TeacherDashboardResponse>.Ok(response);
    }

    public async Task<Result<StudentDashboardResponse>> GetStudentDashboardAsync(Guid userId, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student is null)
            return Result<StudentDashboardResponse>.Fail("Студент не найден", 404);

        var courses = await db.Courses
            .Where(c => c.GroupId == student.GroupId)
            .ToListAsync(ct);

        var courseIds = courses.Select(c => c.Id).ToList();

        var submissions = await db.AssignmentSubmissions
            .Where(s => s.StudentId == student.Id && s.Score != null)
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToListAsync(ct);

        var upcomingDeadlines = await db.Assignments
            .Where(a => courseIds.Contains(a.CourseId) && a.DueDate > DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .Take(5)
            .ToListAsync(ct);

        var response = new StudentDashboardResponse
        {
            CoursesCount = courses.Count,
            RecentGrades = submissions.Select(s => new RecentGradeDto
            {
                CourseName = s.Assignment.Course.Title,
                Grade = s.Score,
            }).ToList(),
            UpcomingDeadlines = upcomingDeadlines.Select(a => new UpcomingDeadlineDto
            {
                AssignmentTitle = a.Title,
                DueDate = a.DueDate!.Value,
            }).ToList(),
        };

        return Result<StudentDashboardResponse>.Ok(response);
    }
}
