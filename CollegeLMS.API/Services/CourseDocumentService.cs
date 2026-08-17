using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseDocumentService(
    AppDbContext db,
    IFileService fileService,
    ICourseAccessService access
) : ICourseDocumentService
{
    public async Task<Result<List<CourseDocumentResponse>>> GetAllAsync(
        Guid courseId,
        CancellationToken ct
    )
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<CourseDocumentResponse>>.Fail("Курс не найден", 404);

        var documents = await db
            .CourseDocuments.AsNoTracking()
            .Where(d => d.CourseId == courseId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return Result<List<CourseDocumentResponse>>.Ok(documents.Select(d => d.ToDto()).ToList());
    }

    public async Task<Result<CourseDocumentResponse>> UploadAsync(
        Guid courseId,
        IFormFile file,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<CourseDocumentResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<CourseDocumentResponse>.Fail(
                    "У вас нет прав на добавление документов в этот курс",
                    403
                );
        }

        var filePath = await fileService.SaveFileAsync("documents", courseId, file, ct);

        var document = new CourseDocument
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
        };
        db.CourseDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return Result<CourseDocumentResponse>.Ok(document.ToDto());
    }

    public async Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(
        Guid id,
        CancellationToken ct
    )
    {
        var document = await db.CourseDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return Result<(Stream, string, string)>.Fail("Документ не найден", 404);

        var fullPath = Path.Combine("uploads", document.FilePath);
        if (!File.Exists(fullPath))
            return Result<(Stream, string, string)>.Fail("Файл не найден на сервере", 404);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Result<(Stream, string, string)>.Ok(
            (stream, document.FileName, document.ContentType)
        );
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var document = await db
            .CourseDocuments.Include(d => d.Course)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return Result.Fail("Документ не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (
                teacher is null
                || !await access.CanManageCourseAsync(document.Course, teacher.Id, ct)
            )
                return Result.Fail("У вас нет прав на удаление этого документа", 403);
        }

        db.CourseDocuments.Remove(document);
        await db.SaveChangesAsync(ct);

        await fileService.DeleteFileAsync(document.FilePath, ct);

        return Result.Ok();
    }
}