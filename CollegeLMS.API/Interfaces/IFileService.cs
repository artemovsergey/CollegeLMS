using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(string entityType, Guid entityId, IFormFile file, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default);
}
