using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class WordPressImportService(AppDbContext db, ILogger<WordPressImportService> logger)
    : IWordPressImportService
{
    public async Task<Result<ImportResult>> ImportFromJsonAsync(
        string jsonPath,
        CancellationToken ct
    )
    {
        if (!File.Exists(jsonPath))
            return Result<ImportResult>.Fail($"Файл не найден: {jsonPath}", 404);

        List<string> errors = [];
        int categoriesCreated = 0;
        int postsImported = 0;
        int postsSkipped = 0;

        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(jsonPath, ct);
            using var doc = JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;

            // --- 1. Import categories ---
            var wpCategoryMap = new Dictionary<int, Guid>();

            if (root.TryGetProperty("categories", out var categoriesEl))
            {
                foreach (var cat in categoriesEl.EnumerateArray())
                {
                    var wpId = cat.GetProperty("id").GetInt32();
                    var name = cat.GetProperty("name").GetString();
                    var slug = cat.GetProperty("slug").GetString();

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var existing = await db
                        .NewsCategories.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Slug == slug, ct);

                    if (existing != null)
                    {
                        wpCategoryMap[wpId] = existing.Id;
                        continue;
                    }

                    var entity = new NewsCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = name.Trim(),
                        Slug = slug ?? "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.NewsCategories.Add(entity);
                    wpCategoryMap[wpId] = entity.Id;
                    categoriesCreated++;
                }
            }

            // --- 2. Import posts ---
            if (root.TryGetProperty("posts", out var postsEl))
            {
                var adminUser = await db
                    .Users.AsNoTracking()
                    .Where(u => u.Role == UserRole.Admin)
                    .OrderBy(u => u.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (adminUser == null)
                    errors.Add(
                        "Не найден пользователь с ролью Admin — новости будут созданы без автора"
                    );

                foreach (var post in postsEl.EnumerateArray())
                {
                    try
                    {
                        var slug = post.GetProperty("slug").GetString() ?? "";

                        var existingNews = await db
                            .News.AsNoTracking()
                            .FirstOrDefaultAsync(n => n.Slug == slug, ct);

                        if (existingNews != null)
                        {
                            postsSkipped++;
                            continue;
                        }

                        var title = post.GetProperty("title").GetProperty("rendered").GetString();
                        var contentHtml = post.GetProperty("content")
                            .GetProperty("rendered")
                            .GetString();
                        var dateStr = post.GetProperty("date").GetString();
                        var status = post.GetProperty("status").GetString();

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            postsSkipped++;
                            continue;
                        }

                        DateTime publishedAt = DateTime.TryParse(dateStr, out var dt)
                            ? dt
                            : DateTime.UtcNow;

                        // Extract image from _embedded
                        string? imageUrl = null;
                        if (
                            post.TryGetProperty("_embedded", out var embedded)
                            && embedded.TryGetProperty("wp:featuredmedia", out var media)
                            && media.GetArrayLength() > 0
                        )
                        {
                            var mediaObj = media[0];
                            if (
                                mediaObj.TryGetProperty("source_url", out var src)
                                && src.ValueKind == JsonValueKind.String
                            )
                            {
                                imageUrl = src.GetString();
                            }
                        }

                        // Map categories
                        Guid? categoryId = null;
                        if (post.TryGetProperty("categories", out var catIds))
                        {
                            foreach (var cid in catIds.EnumerateArray())
                            {
                                var wpId = cid.GetInt32();
                                if (wpCategoryMap.TryGetValue(wpId, out var mappedId))
                                {
                                    categoryId = mappedId;
                                    break; // take first matching category
                                }
                            }
                        }

                        var news = new News
                        {
                            Id = Guid.NewGuid(),
                            Title = SanitizeHtml(title ?? ""),
                            Content = contentHtml ?? "",
                            Slug = slug,
                            ImageUrl = imageUrl,
                            CategoryId = categoryId,
                            IsPublished = status == "publish",
                            PublishedAt = publishedAt,
                            IsDeleted = false,
                            CreatedById = adminUser?.Id ?? Guid.Empty,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };
                        db.News.Add(news);
                        postsImported++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Ошибка при импорте поста: {ex.Message}");
                    }

                    // Save in batches of 100 to avoid excessive memory
                    if (postsImported % 100 == 0)
                    {
                        await db.SaveChangesAsync(ct);
                        logger.LogInformation("Импортировано {Count} новостей...", postsImported);
                    }
                }
            }

            // Final save
            await db.SaveChangesAsync(ct);

            var result = new ImportResult(categoriesCreated, postsImported, postsSkipped, errors);
            return Result<ImportResult>.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта WordPress");
            return Result<ImportResult>.Fail($"Ошибка импорта: {ex.Message}", 500);
        }
    }

    public string StartImport(Func<CancellationToken, Task> importAction)
    {
        var importId = Guid.NewGuid().ToString();
        logger.LogInformation("Запущен импорт {ImportId}", importId);
        _ = Task.Run(async () =>
        {
            try
            {
                await importAction(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в фоновом импорте {ImportId}", importId);
            }
        });
        return importId;
    }

    public ImportProgressDto? GetImportProgress(string importId)
    {
        return null;
    }

    public Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct
    )
    {
        logger.LogInformation("REST API импорт из {BaseUrl} пока не реализован", baseUrl);
        return Task.FromResult(
            Result<ImportResult>.Fail("REST API импорт ещё не реализован", 501)
        );
    }

    private static string SanitizeHtml(string input)
    {
        return input
            .Replace("&#8212;", "—")
            .Replace("&#8211;", "–")
            .Replace("&#8220;", "\"")
            .Replace("&#8221;", "\"")
            .Replace("&#8216;", "'")
            .Replace("&#8217;", "'")
            .Replace("&#8243;", "\"")
            .Replace("&hellip;", "…")
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&laquo;", "«")
            .Replace("&raquo;", "»")
            .Trim();
    }
}
