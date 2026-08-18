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
    public async Task<Result<List<LessonResponse>>> GetAllAsync(Guid courseId, CancellationToken ct)
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

        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).ToListAsync(ct);

        int newOrder;
        if (request.AfterLessonId.HasValue)
        {
            var after = lessons.FirstOrDefault(l => l.Id == request.AfterLessonId.Value);
            if (after is null)
                return Result<LessonResponse>.Fail(
                    "Занятие, после которого нужно вставить, не найдено",
                    400
                );

            foreach (var l in lessons.Where(l => l.Order > after.Order))
            {
                l.Order += 1;
                l.UpdatedAt = DateTime.UtcNow;
            }
            newOrder = after.Order + 1;
        }
        else
        {
            foreach (var l in lessons)
            {
                l.Order += 1;
                l.UpdatedAt = DateTime.UtcNow;
            }
            newOrder = 1;
        }

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = newOrder,
            Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk) ? lk : LessonKind.Lecture,
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
        lesson.Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk) ? lk : LessonKind.Lecture;
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

        var remaining = await db
            .Lessons.Where(l => l.CourseId == courseId && l.Order > lesson.Order)
            .ToListAsync(ct);
        foreach (var l in remaining)
        {
            l.Order -= 1;
            l.UpdatedAt = DateTime.UtcNow;
        }

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> ReorderAsync(
        Guid courseId,
        ReorderLessonsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result.Fail("У вас нет прав на изменение порядка занятий", 403);
        }

        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).ToListAsync(ct);
        if (request.LessonIds.Count != lessons.Count)
            return Result.Fail("Список занятий не соответствует курсу", 400);

        foreach (var lessonId in request.LessonIds)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson is null)
                return Result.Fail("Одно из занятий не принадлежит этому курсу", 400);
        }

        for (var i = 0; i < request.LessonIds.Count; i++)
        {
            var lesson = lessons.First(l => l.Id == request.LessonIds[i]);
            lesson.Order = i + 1;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> SetCurrentAsync(
        Guid courseId,
        Guid id,
        UpdateLessonCurrentRequest request,
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
                return Result.Fail("У вас нет прав на изменение текущего занятия", 403);
        }

        if (request.IsCurrent)
        {
            var others = await db
                .Lessons.Where(l => l.CourseId == courseId && l.Id != lesson.Id && l.IsCurrent)
                .ToListAsync(ct);
            foreach (var other in others)
            {
                other.IsCurrent = false;
                other.UpdatedAt = DateTime.UtcNow;
            }
            lesson.IsCurrent = true;
        }
        else
        {
            lesson.IsCurrent = false;
        }
        lesson.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
