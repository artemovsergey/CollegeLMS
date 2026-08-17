using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseService(AppDbContext db, ICourseAccessService access) : ICourseService
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
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .Include(c => c.CourseAuthors)
                .ThenInclude(a => a.Teacher)
                    .ThenInclude(t => t.User)
            .AsQueryable();

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null)
                return Result<List<CourseResponse>>.Fail("Преподаватель не найден", 404);

            var managedIds = await access.GetManagedCourseIdsAsync(teacher.Id, ct);
            query = query.Where(c => managedIds.Contains(c.Id));
        }
        else if (currentUserRole == "Student")
        {
            query = query.Where(c => c.IsActive);
        }

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (groupId.HasValue)
            query = query.Where(c => c.CourseGroups.Any(cg => cg.GroupId == groupId.Value));

        var courses = await query.OrderBy(c => c.Title).ToListAsync(ct);

        return Result<List<CourseResponse>>.Ok(courses.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .Include(c => c.CourseAuthors)
                .ThenInclude(a => a.Teacher)
                    .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<CourseResponse>.Fail(
                    "У вас нет прав на просмотр этого курса",
                    403
                );
        }

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

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            TeacherId = teacherId,
            Status = CourseStatus.Draft,
        };
        db.Courses.Add(course);

        foreach (var authorId in request.AuthorIds.Distinct())
        {
            if (authorId == teacherId)
                continue;

            var teacherExists = await db.Teachers.AnyAsync(t => t.Id == authorId, ct);
            if (!teacherExists)
                return Result<CourseResponse>.Fail($"Преподаватель не найден: {authorId}", 400);

            db.CourseAuthors.Add(
                new CourseAuthor
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    TeacherId = authorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

        await db.SaveChangesAsync(ct);

        course = await db
            .Courses.Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
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
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
            .Include(c => c.Lectures)
            .Include(c => c.Assignments)
            .Include(c => c.CourseAuthors)
                .ThenInclude(a => a.Teacher)
                    .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<CourseResponse>.Fail(
                    "У вас нет прав на редактирование этого курса",
                    403
                );
        }

        course.Title = request.Title;
        course.Description = request.Description;
        course.Status = Enum.Parse<CourseStatus>(request.Status);
        course.UpdatedAt = DateTime.UtcNow;

        var existingAuthorIds = course.CourseAuthors.Select(a => a.TeacherId).ToList();
        foreach (var removed in existingAuthorIds.Where(id => !request.AuthorIds.Contains(id)))
        {
            var author = course.CourseAuthors.First(a => a.TeacherId == removed);
            db.CourseAuthors.Remove(author);
        }

        var newAuthorIds = request
            .AuthorIds.Distinct()
            .Where(id => id != course.TeacherId && !existingAuthorIds.Contains(id));
        foreach (var authorId in newAuthorIds)
        {
            var teacherExists = await db.Teachers.AnyAsync(t => t.Id == authorId, ct);
            if (!teacherExists)
                return Result<CourseResponse>.Fail($"Преподаватель не найден: {authorId}", 400);

            db.CourseAuthors.Add(
                new CourseAuthor
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    TeacherId = authorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

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
        Guid courseId,
        AssignGroupsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
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
                cg => cg.CourseId == courseId && cg.GroupId == groupId,
                ct
            );
            if (!alreadyAssigned)
            {
                db.CourseGroups.Add(
                    new CourseGroup
                    {
                        Id = Guid.NewGuid(),
                        CourseId = courseId,
                        GroupId = groupId,
                    }
                );
            }
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<List<CourseGroupResponse>>> GetCourseGroupsAsync(
        Guid courseId,
        CancellationToken ct
    )
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<CourseGroupResponse>>.Fail("Курс не найден", 404);

        var groups = await db
            .CourseGroups.AsNoTracking()
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
        Guid courseId,
        Guid groupId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FindAsync([courseId], ct);
        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (!await CanManageCourse(courseId, currentUserId, currentUserRole, ct))
            return Result.Fail("У вас нет прав на редактирование этого курса", 403);

        var courseGroup = await db.CourseGroups.FirstOrDefaultAsync(
            cg => cg.CourseId == courseId && cg.GroupId == groupId,
            ct
        );
        if (courseGroup is null)
            return Result.Fail("Группа не привязана к курсу", 404);

        db.CourseGroups.Remove(courseGroup);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<CourseProgressResponse>> GetProgressAsync(
        Guid courseId,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.AsNoTracking()
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<CourseProgressResponse>.Fail("Курс не найден", 404);

        var student = await db
            .Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<CourseProgressResponse>.Fail("Студент не найден", 404);

        var inGroup = await db.CourseGroups.AnyAsync(
            cg => cg.CourseId == courseId && cg.GroupId == student.GroupId,
            ct
        );
        if (!inGroup)
            return Result<CourseProgressResponse>.Fail("Вы не привязаны к этому курсу", 403);

        var totalAssignments = course.Assignments.Count;
        var completedAssignments = await db.AssignmentSubmissions.CountAsync(
            s =>
                s.StudentId == student.Id
                && course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                && s.Score.HasValue,
            ct
        );

        var totalTests = await db.Tests.CountAsync(t => t.CourseId == courseId, ct);
        var completedTests = await db.TestAttempts.CountAsync(
            a =>
                a.StudentId == student.Id
                && a.Test.CourseId == courseId
                && a.Status == Entities.Enums.AttemptStatus.Completed,
            ct
        );

        var scoreSubmissions = await db
            .AssignmentSubmissions.Where(s =>
                s.StudentId == student.Id
                && course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                && s.Score.HasValue
            )
            .Select(s => s.Score!.Value)
            .ToListAsync(ct);
        var avgScore = scoreSubmissions.Count > 0 ? scoreSubmissions.Average() : 0;

        var total = totalAssignments + totalTests;
        var completed = completedAssignments + completedTests;

        return Result<CourseProgressResponse>.Ok(
            new CourseProgressResponse
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                TotalAssignments = totalAssignments,
                CompletedAssignments = completedAssignments,
                TotalTests = totalTests,
                CompletedTests = completedTests,
                AverageScore = totalAssignments > 0 ? Math.Round(avgScore, 1) : 0,
                CompletionPercent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            }
        );
    }

    public async Task<Result<CourseResponse>> DuplicateAsync(
        Guid courseId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var source = await db
            .Courses
            .Include(c => c.Lectures)
            .Include(c => c.Materials)
            .Include(c => c.CourseAuthors)
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (source is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        Guid authorTeacherId;
        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(
                t => t.UserId == currentUserId,
                ct
            );
            if (teacher is null)
                return Result<CourseResponse>.Fail("Преподаватель не найден", 404);
            authorTeacherId = teacher.Id;
            if (!await access.CanManageCourseAsync(source, teacher.Id, ct))
                return Result<CourseResponse>.Fail("У вас нет прав на дублирование этого курса", 403);
        }
        else
        {
            authorTeacherId = source.TeacherId;
        }

        var copy = new Course
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (копия)",
            Description = source.Description,
            TeacherId = authorTeacherId,
            Status = CourseStatus.Draft,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Courses.Add(copy);

        foreach (var lecture in source.Lectures)
        {
            db.Lectures.Add(
                new Lecture
                {
                    Id = Guid.NewGuid(),
                    CourseId = copy.Id,
                    Title = lecture.Title,
                    Content = lecture.Content,
                    Order = lecture.Order,
                    LectureType = lecture.LectureType,
                    TestId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

        foreach (var material in source.Materials)
        {
            var newPath = await CopyMaterialFileAsync(material.FilePath, copy.Id, ct);
            db.CourseMaterials.Add(
                new CourseMaterial
                {
                    Id = Guid.NewGuid(),
                    CourseId = copy.Id,
                    LectureId = null,
                    AssignmentId = null,
                    FileName = material.FileName,
                    FilePath = newPath,
                    FileSize = material.FileSize,
                    MimeType = material.MimeType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

        foreach (var author in source.CourseAuthors)
        {
            db.CourseAuthors.Add(
                new CourseAuthor
                {
                    Id = Guid.NewGuid(),
                    CourseId = copy.Id,
                    TeacherId = author.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

        await db.SaveChangesAsync(ct);
        copy.CourseAuthors = source.CourseAuthors;
        copy.Teacher = source.Teacher;
        return Result<CourseResponse>.Ok(copy.ToDto());
    }

    public async Task<Result> SetActiveAsync(
        Guid courseId,
        bool isActive,
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
            var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(
                t => t.UserId == currentUserId,
                ct
            );
            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("Только владелец курса может менять активность", 403);
        }

        course.IsActive = isActive;
        course.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<bool> CanManageCourse(
        Guid courseId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        if (currentUserRole == "Admin")
            return true;
        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null)
                return false;
            return await db.Courses.AnyAsync(
                c => c.Id == courseId && c.TeacherId == teacher.Id,
                ct
            );
        }
        return false;
    }

    private static async Task<string> CopyMaterialFileAsync(
        string relativePath,
        Guid newCourseId,
        CancellationToken ct
    )
    {
        var sourcePath = Path.Combine("uploads", relativePath);
        var fileName = Path.GetFileName(relativePath);
        var destDir = Path.Combine("uploads", "materials", newCourseId.ToString());
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, fileName);
        await using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        await using var dst = new FileStream(destPath, FileMode.Create);
        await src.CopyToAsync(dst, ct);
        return Path.Combine("materials", newCourseId.ToString(), fileName).Replace('\\', '/');
    }
}
