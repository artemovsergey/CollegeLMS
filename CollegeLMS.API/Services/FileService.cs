using CollegeLMS.API.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Services;

public class FileService : IFileService
{
    public async Task<string> SaveFileAsync(
        string entityType,
        Guid entityId,
        IFormFile file,
        CancellationToken ct
    )
    {
        var uploadsDir = Path.Combine("uploads", entityType, entityId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return Path.Combine(entityType, entityId.ToString(), fileName).Replace('\\', '/');
    }

    public Task<bool> DeleteFileAsync(string filePath, CancellationToken ct)
    {
        var fullPath = Path.Combine("uploads", filePath);
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }
}
