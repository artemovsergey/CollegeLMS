using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IMediaMigrationService
{
    Task<Result<MediaMigrationResult>> MigrateAllAsync(CancellationToken ct = default);
    Task<string?> DownloadImageAsync(Guid newsId, string imageUrl, CancellationToken ct = default);
}

public record MediaMigrationResult(int Processed, int Downloaded, int Failed, List<string> Errors);

