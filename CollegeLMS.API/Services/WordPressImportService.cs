using System.Collections.Concurrent;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class WordPressImportService(
    IServiceScopeFactory scopeFactory,
    ILogger<WordPressImportService> logger
) : IWordPressImportService
{
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _importCts = new();

    public string StartImport(Func<CancellationToken, Task> importAction)
    {
        var jobId = Guid.NewGuid();
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ImportJobs.Add(new ImportJob { Id = jobId, Status = "running" });
            db.SaveChanges();
        }

        var cts = new CancellationTokenSource();
        _importCts[jobId] = cts;
        var token = cts.Token;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await importAction(token);
                    await UpdateJobAsync(
                        jobId,
                        job =>
                        {
                            job.Status = token.IsCancellationRequested ? "cancelled" : "completed";
                            job.CompletedAt = DateTime.UtcNow;
                        }
                    );
                }
                catch (OperationCanceledException)
                {
                    await UpdateJobAsync(
                        jobId,
                        job =>
                        {
                            job.Status = "cancelled";
                            job.CompletedAt = DateTime.UtcNow;
                        }
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Import {ImportId} failed", jobId);
                    await UpdateJobAsync(
                        jobId,
                        job =>
                        {
                            job.Status = "failed";
                            job.CompletedAt = DateTime.UtcNow;
                            job.ErrorCount = 1;
                            job.ErrorMessages = [ex.Message];
                        }
                    );
                }
                finally
                {
                    _importCts.TryRemove(jobId, out _);
                }
            },
            token
        );

        return jobId.ToString();
    }

    public void StopImport(string importId)
    {
        if (Guid.TryParse(importId, out var id) && _importCts.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public async Task<ImportProgressDto?> GetImportProgressAsync(
        string importId,
        CancellationToken ct
    )
    {
        if (!Guid.TryParse(importId, out var id))
            return null;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);
        return job?.ToDto();
    }

    public async Task<ImportProgressDto?> GetActiveImportAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db
            .ImportJobs.AsNoTracking()
            .Where(j => j.Status == "running")
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return job?.ToDto();
    }

    public async Task<Result<ImportResult>> ImportFromJsonAsync(
        string jsonPath,
        CancellationToken ct,
        Guid? jobId = null
    )
    {
        if (!File.Exists(jsonPath))
            return Result<ImportResult>.Fail($"Файл не найден: {jsonPath}", 404);

        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(jsonPath, ct);
            using var doc = JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;
            return await ProcessImportAsync(root, ct, jobId);
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
        Guid? jobId = null
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

            if (jobId.HasValue)
            {
                await UpdateJobAsync(
                    jobId.Value,
                    job =>
                    {
                        job.Total = totalPosts > 0 ? totalPosts : totalPages * 100;
                        job.Processed = 0;
                    }
                );
            }

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

                var pageResult = await ProcessImportAsync(pageDoc.RootElement, ct, jobId);

                logger.LogInformation(
                    "WP REST: page {Page}/{Total} done, imported={Imported}, skipped={Skipped}",
                    page,
                    totalPages,
                    pageResult.Data?.PostsImported ?? 0,
                    pageResult.Data?.PostsSkipped ?? 0
                );

                if (page < totalPages)
                    await Task.Delay(200, ct);
            }

            return await GetStoredImportResultAsync(jobId, ct);
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

    private async Task<Result<ImportResult>> GetStoredImportResultAsync(
        Guid? jobId,
        CancellationToken ct
    )
    {
        if (!jobId.HasValue)
            return Result<ImportResult>.Fail("Не найден ImportJob", 500);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
            return Result<ImportResult>.Fail("Не найден ImportJob", 500);

        return Result<ImportResult>.Ok(job.ToDto().Result!);
    }

    private async Task<Result<ImportResult>> ProcessImportAsync(
        JsonElement root,
        CancellationToken ct,
        Guid? jobId = null
    )
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        ImportJob? job = null;
        if (jobId.HasValue)
            job = await db.ImportJobs.FirstOrDefaultAsync(j => j.Id == jobId.Value, ct);

        List<string> errors = [];
        int categoriesCreated = 0;
        int postsImported = 0;
        int postsSkipped = 0;

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

        await db.SaveChangesAsync(ct);

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

            if (job != null)
            {
                job.Total = totalPosts;
                job.Processed = 0;
            }

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

                if (job != null)
                {
                    job.Processed = processed;
                    job.ErrorCount = errors.Count;
                    job.ErrorMessages = errors;
                }

                if (postsImported % 50 == 0)
                {
                    await db.SaveChangesAsync(ct);
                    logger.LogInformation("Импортировано {Count} новостей...", postsImported);
                }
            }
        }

        if (job != null)
        {
            job.CategoriesCreated += categoriesCreated;
            job.PostsImported += postsImported;
            job.PostsSkipped += postsSkipped;
            job.ErrorCount = errors.Count;
            job.ErrorMessages = errors;
        }

        await db.SaveChangesAsync(ct);

        var result = new ImportResult(categoriesCreated, postsImported, postsSkipped, errors);
        return Result<ImportResult>.Ok(result);
    }

    private async Task UpdateJobAsync(Guid jobId, Action<ImportJob> update)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.ImportJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null)
            return;
        update(job);
        job.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
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
