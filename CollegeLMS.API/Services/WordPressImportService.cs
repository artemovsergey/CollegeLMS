using System.Collections.Concurrent;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class WordPressImportService(
    IServiceScopeFactory scopeFactory,
    ILogger<WordPressImportService> logger
) : IWordPressImportService
{
    private static readonly ConcurrentDictionary<string, ImportProgressDto> _imports = new();

    private static readonly TimeSpan CleanupAge = TimeSpan.FromMinutes(30);

    public string StartImport(Func<CancellationToken, Task> importAction)
    {
        CleanupOldEntries();

        var importId = Guid.NewGuid().ToString();
        var progress = new ImportProgressDto { ImportId = importId, Status = "running" };
        _imports[importId] = progress;

        _ = Task.Run(async () =>
        {
            try
            {
                var progressRef = _imports[importId];
                progressRef.Status = "running";

                await importAction(CancellationToken.None);

                progressRef.Status = "completed";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import {ImportId} failed", importId);
                if (_imports.TryGetValue(importId, out var p))
                {
                    p.Status = "failed";
                    p.ErrorMessages.Add(ex.Message);
                }
            }
        });

        return importId;
    }

    public ImportProgressDto? GetImportProgress(string importId)
    {
        _imports.TryGetValue(importId, out var progress);
        return progress;
    }

    public async Task<Result<ImportResult>> ImportFromJsonAsync(
        string jsonPath,
        CancellationToken ct,
        string? importId = null
    )
    {
        if (!File.Exists(jsonPath))
            return Result<ImportResult>.Fail($"Файл не найден: {jsonPath}", 404);

        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(jsonPath, ct);
            using var doc = JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;

            var progress = importId != null ? GetImportProgress(importId) : null;
            return await ProcessImportAsync(root, ct, progress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта WordPress из JSON");
            return Result<ImportResult>.Fail($"Ошибка импорта: {ex.Message}", 500);
        }
    }

    public async Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct,
        string? importId = null
    )
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CollegeLMS/1.0");

            var categoriesJson = await httpClient.GetStringAsync(
                "/wp-json/wp/v2/categories?per_page=100",
                ct
            );

            // --- First request: get total pages ---
            var firstUrl = $"/wp-json/wp/v2/posts?per_page=100&page=1&_embed=1";
            var firstResponse = await httpClient.GetAsync(firstUrl, ct);
            firstResponse.EnsureSuccessStatusCode();

            var totalPages = 1;
            if (firstResponse.Headers.Contains("X-WP-TotalPages"))
            {
                var tp = firstResponse.Headers.GetValues("X-WP-TotalPages").FirstOrDefault();
                int.TryParse(tp, out totalPages);
            }

            var totalPosts = 0;
            if (firstResponse.Headers.Contains("X-WP-Total"))
            {
                var tp = firstResponse.Headers.GetValues("X-WP-Total").FirstOrDefault();
                int.TryParse(tp, out totalPosts);
            }

            var firstBody = await firstResponse.Content.ReadAsStringAsync(ct);

            // --- Process page by page ---
            var progress = importId != null ? GetImportProgress(importId) : null;
            if (progress != null)
                progress.Total = totalPosts > 0 ? totalPosts : totalPages * 100;

            int totalCategoriesCreated = 0;
            int totalPostsImported = 0;
            int totalPostsSkipped = 0;
            int totalErrors = 0;
            List<string> allErrors = [];

            for (int page = 1; page <= totalPages; page++)
            {
                var body = page == 1 ? firstBody : "";
                if (page > 1)
                {
                    var url = $"/wp-json/wp/v2/posts?per_page=100&page={page}&_embed=1";
                    var resp = await httpClient.GetAsync(url, ct);
                    resp.EnsureSuccessStatusCode();
                    body = await resp.Content.ReadAsStringAsync(ct);
                }

                using var pageDoc = JsonDocument.Parse(
                    $"{{\"categories\":{categoriesJson},\"posts\":{body}}}"
                );

                var pageResult = await ProcessImportAsync(pageDoc.RootElement, ct, null);

                if (pageResult.IsSuccess && pageResult.Data != null)
                {
                    totalCategoriesCreated += pageResult.Data.CategoriesCreated;
                    totalPostsImported += pageResult.Data.PostsImported;
                    totalPostsSkipped += pageResult.Data.PostsSkipped;
                    allErrors.AddRange(pageResult.Data.Errors);
                    totalErrors += pageResult.Data.Errors.Count;
                }

                if (progress != null)
                {
                    progress.Processed = totalPostsImported + totalPostsSkipped;
                    progress.Errors = totalErrors;
                }

                logger.LogInformation(
                    "WP REST: page {Page}/{Total} done, imported={Imported}, skipped={Skipped}",
                    page,
                    totalPages,
                    totalPostsImported,
                    totalPostsSkipped
                );

                if (page < totalPages)
                    await Task.Delay(200, ct);
            }

            return Result<ImportResult>.Ok(
                new ImportResult(
                    totalCategoriesCreated,
                    totalPostsImported,
                    totalPostsSkipped,
                    allErrors
                )
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ошибка подключения к WordPress REST API");
            return Result<ImportResult>.Fail(
                $"Не удалось подключиться к WordPress REST API: {ex.Message}",
                502
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта WordPress из REST API");
            return Result<ImportResult>.Fail($"Ошибка импорта: {ex.Message}", 500);
        }
    }

    private async Task<Result<ImportResult>> ProcessImportAsync(
        JsonElement root,
        CancellationToken ct,
        ImportProgressDto? progress = null
    )
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<string> errors = [];
        int categoriesCreated = 0;
        int postsImported = 0;
        int postsSkipped = 0;

        var wpCategoryMap = new Dictionary<int, Guid>();

        // --- 1. Import categories ---
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

        await db.SaveChangesAsync(ct);

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

            var totalPosts = 0;
            foreach (var _ in postsEl.EnumerateArray())
                totalPosts++;

            if (progress != null)
                progress.Total = totalPosts;

            var processed = 0;
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
                        processed++;
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
                        processed++;
                        continue;
                    }

                    DateTime publishedAt = DateTime.TryParse(dateStr, out var dt)
                        ? dt
                        : DateTime.UtcNow;

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

                    Guid? categoryId = null;
                    if (post.TryGetProperty("categories", out var catIds))
                    {
                        foreach (var cid in catIds.EnumerateArray())
                        {
                            var wpId = cid.GetInt32();
                            if (wpCategoryMap.TryGetValue(wpId, out var mappedId))
                            {
                                categoryId = mappedId;
                                break;
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
                    processed++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка при импорте поста: {ex.Message}");
                }

                if (progress != null)
                    progress.Processed = processed;

                if (postsImported % 50 == 0)
                {
                    await db.SaveChangesAsync(ct);
                    logger.LogInformation("Импортировано {Count} новостей...", postsImported);
                }
            }
        }

        await db.SaveChangesAsync(ct);

        var result = new ImportResult(categoriesCreated, postsImported, postsSkipped, errors);
        return Result<ImportResult>.Ok(result);
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

    private static void CleanupOldEntries()
    {
        var cutoff = DateTime.UtcNow - CleanupAge;
        var stale = _imports
            .Where(kvp => kvp.Value.Status != "running" && kvp.Value.CreatedAt < cutoff)
            .ToList();

        foreach (var kvp in stale)
        {
            _imports.TryRemove(kvp.Key, out _);
        }
    }
}
