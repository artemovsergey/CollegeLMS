using CollegeLMS.API.Data;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Services;

public class MediaMigrationService(
    AppDbContext db,
    IWebHostEnvironment env,
    ILogger<MediaMigrationService> logger,
    HttpClient httpClient
) : IMediaMigrationService
{
    public async Task<Result<MediaMigrationResult>> MigrateAllAsync(CancellationToken ct)
    {
        var newsWithRemoteImages = await db
            .News.AsNoTracking()
            .Where(n =>
                n.ImageUrl != null
                && n.ImageUrl.StartsWith("http")
            )
            .ToListAsync(ct);

        var result = new MediaMigrationResult(0, 0, 0, []);
        result = result with { Processed = newsWithRemoteImages.Count };

        foreach (var news in newsWithRemoteImages)
        {
            try
            {
                var localPath = await DownloadImageAsync(news.Id, news.ImageUrl!, ct);
                if (localPath != null)
                {
                    news.ImageUrl = localPath;
                    db.News.Update(news);
                    result = result with { Downloaded = result.Downloaded + 1 };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to migrate image for news {Id}", news.Id);
                result = result with { Failed = result.Failed + 1 };
                result = result with
                {
                    Errors = [..result.Errors, $"News {news.Id}: {ex.Message}"]
                };
            }
        }

        await db.SaveChangesAsync(ct);
        return Result<MediaMigrationResult>.Ok(result);
    }

    public async Task<string?> DownloadImageAsync(Guid newsId, string imageUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return null;

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"{Guid.NewGuid()}.jpg";

        var relativeDir = Path.Combine("uploads", "news", newsId.ToString());
        var absoluteDir = Path.Combine(env.ContentRootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var filePath = Path.Combine(absoluteDir, fileName);
        if (File.Exists(filePath))
            return $"/{relativeDir}/{fileName}".Replace('\\', '/');

        var response = await httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream, ct);

        return $"/{relativeDir}/{fileName}".Replace('\\', '/');
    }
}
