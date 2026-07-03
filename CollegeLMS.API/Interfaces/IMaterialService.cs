using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Interfaces;

public interface IMaterialService
{
    Task<Result<MaterialResponse>> UploadAsync(Guid courseId, IFormFile file, Guid? lectureId, Guid? assignmentId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<List<MaterialResponse>>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
}
