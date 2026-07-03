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
        Guid? teacherId, Guid? groupId, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var query = db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .AsQueryable();

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null)
                return Result<List<CourseResponse>>.Fail("Преподаватель не найден", 404);

            query = query.Where(c => c.TeacherId == teacher.Id);
        }

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (groupId.HasValue)
            query = query.Where(c => c.GroupId == groupId.Value);

        var courses = await query
            .OrderBy(c => c.Title)
            .ToListAsync(ct);

        return Result<List<CourseResponse>>.Ok(courses.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        return Result<CourseResponse>.Ok(course.ToDto());
    }

    public async Task<Result<CourseResponse>> CreateAsync(
        CreateCourseRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        Guid teacherId;

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

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

        course = await db.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstAsync(c => c.Id == course.Id, ct);

        return Result<CourseResponse>.Ok(course.ToDto());
    }

    public async Task<Result<CourseResponse>> UpdateAsync(
        Guid id, UpdateCourseRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<CourseResponse>.Fail("У вас нет прав на редактирование этого курса", 403);
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
        Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("У вас нет прав на удаление этого курса", 403);
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
