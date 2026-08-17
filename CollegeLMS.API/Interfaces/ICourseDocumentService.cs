using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Interfaces;

public interface ICourseDocumentService
{
    Task<Result<List<CourseDocumentResponse>>> GetAllAsync(Guid courseId, CancellationToken ct);
    Task<Result<CourseDocumentResponse>> UploadAsync(
        Guid courseId,
        IFormFile file,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
    Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(
        Guid id,
        CancellationToken ct
    );
    Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
}