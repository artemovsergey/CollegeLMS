using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class LectureService(AppDbContext db) : ILectureService
{
    public async Task<Result<List<LectureResponse>>> GetAllAsync(
        Guid courseId,
        CancellationToken ct
    )
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<LectureResponse>>.Fail("Курс не найден", 404);

        var lectures = await db
            .Lectures.AsNoTracking()
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .ToListAsync(ct);

        return Result<List<LectureResponse>>.Ok(lectures.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<LectureResponse>> GetByIdAsync(
        Guid courseId,
        Guid id,
        CancellationToken ct
    )
    {
        var lecture = await db
            .Lectures.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.CourseId == courseId, ct);

        if (lecture is null)
            return Result<LectureResponse>.Fail("Лекция не найдена", 404);

        return Result<LectureResponse>.Ok(lecture.ToDto());
    }

    public async Task<Result<LectureResponse>> CreateAsync(
        Guid courseId,
        CreateLectureRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<LectureResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<LectureResponse>.Fail(
                    "У вас нет прав на добавление лекций в этот курс",
                    403
                );
        }

        var maxOrder =
            await db.Lectures.Where(l => l.CourseId == courseId).MaxAsync(l => (int?)l.Order, ct)
            ?? 0;

        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = maxOrder + 1,
        };
        db.Lectures.Add(lecture);
        await db.SaveChangesAsync(ct);

        return Result<LectureResponse>.Ok(lecture.ToDto());
    }

    public async Task<Result<LectureResponse>> UpdateAsync(
        Guid courseId,
        Guid id,
        UpdateLectureRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var lecture = await db.Lectures.FirstOrDefaultAsync(
            l => l.Id == id && l.CourseId == courseId,
            ct
        );

        if (lecture is null)
            return Result<LectureResponse>.Fail("Лекция не найдена", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<LectureResponse>.Fail(
                    "У вас нет прав на редактирование лекций в этом курсе",
                    403
                );
        }

        lecture.Title = request.Title;
        lecture.Content = request.Content;
        lecture.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<LectureResponse>.Ok(lecture.ToDto());
    }

    public async Task<Result> DeleteAsync(
        Guid courseId,
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var lecture = await db.Lectures.FirstOrDefaultAsync(
            l => l.Id == id && l.CourseId == courseId,
            ct
        );

        if (lecture is null)
            return Result.Fail("Лекция не найдена", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("У вас нет прав на удаление лекций из этого курса", 403);
        }

        db.Lectures.Remove(lecture);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
