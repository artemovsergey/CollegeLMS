using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class MaterialService(AppDbContext db, IFileService fileService) : IMaterialService
{
    public async Task<Result<MaterialResponse>> UploadAsync(Guid courseId, IFormFile file, Guid? lectureId, Guid? assignmentId, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<MaterialResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || course.TeacherId != teacher.Id)
                return Result<MaterialResponse>.Fail("У вас нет прав на добавление материалов в этот курс", 403);
        }

        var filePath = await fileService.SaveFileAsync("materials", courseId, file, ct);

        var material = new CourseMaterial
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            LectureId = lectureId,
            AssignmentId = assignmentId,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            MimeType = file.ContentType ?? "application/octet-stream",
        };
        db.CourseMaterials.Add(material);
        await db.SaveChangesAsync(ct);

        return Result<MaterialResponse>.Ok(material.ToDto());
    }

    public async Task<Result<List<MaterialResponse>>> GetByCourseAsync(Guid courseId, CancellationToken ct)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<MaterialResponse>>.Fail("Курс не найден", 404);

        var materials = await db.CourseMaterials
            .AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return Result<List<MaterialResponse>>.Ok(materials.Select(m => m.ToDto()).ToList());
    }

    public async Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(Guid id, CancellationToken ct)
    {
        var material = await db.CourseMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (material is null)
            return Result<(Stream, string, string)>.Fail("Материал не найден", 404);

        var fullPath = Path.Combine("uploads", material.FilePath);
        if (!File.Exists(fullPath))
            return Result<(Stream, string, string)>.Fail("Файл не найден на сервере", 404);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Result<(Stream, string, string)>.Ok((stream, material.FileName, material.MimeType));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct)
    {
        var material = await db.CourseMaterials
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (material is null)
            return Result.Fail("Материал не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || material.Course.TeacherId != teacher.Id)
                return Result.Fail("У вас нет прав на удаление этого материала", 403);
        }

        db.CourseMaterials.Remove(material);
        await db.SaveChangesAsync(ct);

        await fileService.DeleteFileAsync(material.FilePath, ct);

        return Result.Ok();
    }
}
