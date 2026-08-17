using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class LessonService(AppDbContext db, ICourseAccessService access) : ILessonService
{
    public async Task<Result<List<LessonResponse>>> GetAllAsync(
        Guid courseId,
        CancellationToken ct
    )
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<LessonResponse>>.Fail("Курс не найден", 404);

        var lessons = await db
            .Lessons.Include(l => l.Test)
            .AsNoTracking()
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .ToListAsync(ct);

        return Result<List<LessonResponse>>.Ok(lessons.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<LessonResponse>> GetByIdAsync(
        Guid courseId,
        Guid id,
        CancellationToken ct
    )
    {
        var lesson = await db
            .Lessons.Include(l => l.Test)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.CourseId == courseId, ct);

        if (lesson is null)
            return Result<LessonResponse>.Fail("Занятие не найдено", 404);

        return Result<LessonResponse>.Ok(lesson.ToDto());
    }

    public async Task<Result<LessonResponse>> CreateAsync(
        Guid courseId,
        CreateLessonRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<LessonResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<LessonResponse>.Fail(
                    "У вас нет прав на добавление занятий в этот курс",
                    403
                );
        }

        var maxOrder =
            await db.Lessons.Where(l => l.CourseId == courseId).MaxAsync(l => (int?)l.Order, ct)
            ?? 0;

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = maxOrder + 1,
            Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk)
                ? lk
                : LessonKind.Lecture,
            TestId = request.TestId,
        };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(ct);

        return Result<LessonResponse>.Ok(lesson.ToDto());
    }

    public async Task<Result<LessonResponse>> UpdateAsync(
        Guid courseId,
        Guid id,
        UpdateLessonRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(
            l => l.Id == id && l.CourseId == courseId,
            ct
        );

        if (lesson is null)
            return Result<LessonResponse>.Fail("Занятие не найдено", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<LessonResponse>.Fail(
                    "У вас нет прав на редактирование занятий в этом курсе",
                    403
                );
        }

        lesson.Title = request.Title;
        lesson.Content = request.Content;
        lesson.Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk)
            ? lk
            : LessonKind.Lecture;
        lesson.TestId = request.TestId;
        lesson.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<LessonResponse>.Ok(lesson.ToDto());
    }

    public async Task<Result> DeleteAsync(
        Guid courseId,
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(
            l => l.Id == id && l.CourseId == courseId,
            ct
        );

        if (lesson is null)
            return Result.Fail("Занятие не найдено", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result.Fail("У вас нет прав на удаление занятий из этого курса", 403);
        }

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}