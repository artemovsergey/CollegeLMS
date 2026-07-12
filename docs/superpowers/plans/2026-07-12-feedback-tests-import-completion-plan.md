# Feedback Tests + Import Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add tests for Feedback (UC-5) and complete WordPress Import with progress polling + live REST API + admin UI (UC-6)

**Architecture:** Tests follow existing xUnit + InMemory EF + WebApplicationFactory patterns. Import service gains a `ConcurrentDictionary` for progress tracking, shared parsing methods for JSON and REST API sources, and a polling endpoint for the frontend.

**Tech Stack:** C# 13, xUnit, Moq, Bogus, FluentAssertions, ASP.NET Core, HttpClient, ConcurrentDictionary, Next.js 14, shadcn/ui

## Global Constraints

- All methods use `CancellationToken ct` as last parameter
- `Result<T>` pattern for all service returns (no exceptions)
- Error messages in Russian
- Tests use InMemory EF (not Npgsql)
- Tests follow existing NewsServiceTests / NewsControllerTests patterns
- Frontend uses axios (via `@/lib/api`), shadcn/ui components, Tailwind CSS v4
- All feature branch commits prefixed `feature:`

---

### Task 1: FeedbackService unit tests

**Files:**
- Create: `CollegeLMS.Tests/Unit/Services/FeedbackServiceTests.cs`

**Interfaces:**
- Consumes: `FeedbackService`, `FeedbackRequest`, `FeedbackResponse`, `TestDbContextFactory`, `AppDbContext`
- Produces: 3 passing tests

- [ ] **Step 1: Create FeedbackServiceTests.cs**

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class FeedbackServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly FeedbackService _sut;

    public FeedbackServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new FeedbackService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_ReturnsOk_WithValidRequest()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван Иванов",
            Email = "ivan@test.ru",
            Message = "Отличный сайт!",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Message.Should().Be("Сообщение отправлено");

        var saved = await _db.Set<API.Entities.Feedback>().FirstAsync();
        saved.Name.Should().Be("Иван Иванов");
        saved.Email.Should().Be("ivan@test.ru");
        saved.Message.Should().Be("Отличный сайт!");
    }

    [Fact]
    public async Task CreateAsync_Returns429_WhenDuplicateWithin5Minutes()
    {
        var firstRequest = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Первое сообщение",
        };

        var firstResult = await _sut.CreateAsync(firstRequest, default);
        firstResult.IsSuccess.Should().BeTrue();

        var secondRequest = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Второе сообщение",
        };

        var secondResult = await _sut.CreateAsync(secondRequest, default);

        secondResult.IsSuccess.Should().BeFalse();
        secondResult.StatusCode.Should().Be(429);
        secondResult.ErrorMessage.Should().Be("Вы уже отправляли сообщение. Попробуйте позже.");
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_SameEmailAfter5Minutes()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Сообщение",
        };

        var firstResult = await _sut.CreateAsync(request, default);
        firstResult.IsSuccess.Should().BeTrue();

        // Manually set CreatedAt to 6 minutes ago to bypass rate limit
        var saved = await _db.Set<API.Entities.Feedback>().FirstAsync();
        saved.CreatedAt = DateTime.UtcNow.AddMinutes(-6);
        await _db.SaveChangesAsync();

        var secondResult = await _sut.CreateAsync(request, default);

        secondResult.IsSuccess.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

```powershell
dotnet test --filter "FullyQualifiedName~FeedbackServiceTests"
```
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feature: add FeedbackService unit tests"
```

---

### Task 2: FeedbackController integration tests

**Files:**
- Create: `CollegeLMS.Tests/Integration/Controllers/FeedbackControllerTests.cs`

**Interfaces:**
- Consumes: `BaseIntegrationTest`, `FeedbackRequest`, `HttpClient`, `WebApplicationFactory`
- Produces: 3 passing integration tests

- [ ] **Step 1: Create FeedbackControllerTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using FluentAssertions;

namespace CollegeLMS.Tests.Integration.Controllers;

public class FeedbackControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task Create_Returns200_WithValidRequest()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван Иванов",
            Email = "ivan@test.ru",
            Message = "Отличный сайт!",
        };

        var response = await Client.PostAsJsonAsync("/api/feedback", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FeedbackResponse>>();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Message.Should().Be("Сообщение отправлено");
    }

    [Fact]
    public async Task Create_Returns400_WhenEmailInvalid()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "не-email",
            Message = "Тест",
        };

        var response = await Client.PostAsJsonAsync("/api/feedback", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns429_WhenTooFrequent()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Первое сообщение",
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/feedback", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await Client.PostAsJsonAsync("/api/feedback", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

```powershell
dotnet test --filter "FullyQualifiedName~FeedbackControllerTests"
```
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feature: add FeedbackController integration tests"
```

---

### Task 3: WordPress import progress DTOs + interface changes

**Files:**
- Create: `CollegeLMS.API/Dtos/ImportProgressDto.cs`
- Modify: `CollegeLMS.API/Interfaces/IWordPressImportService.cs`

**Interfaces:**
- Produces: `ImportProgressDto` class, updated `IWordPressImportService` with `ImportFromRestApiAsync`, `GetImportProgress`, progress records

- [ ] **Step 1: Create ImportProgressDto.cs**

```csharp
namespace CollegeLMS.API.Dtos;

public class ImportProgressDto
{
    public string ImportId { get; set; } = string.Empty;
    public string Status { get; set; } = "running"; // running | completed | failed
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = [];
    public ImportResult? Result { get; set; }
}
```

- [ ] **Step 2: Update IWordPressImportService.cs**

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWordPressImportService
{
    Task<Result<ImportResult>> ImportFromJsonAsync(string jsonPath, CancellationToken ct);

    string StartImport(Func<CancellationToken, Task> importAction);

    ImportProgressDto? GetImportProgress(string importId);

    Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct
    );
}

public record ImportResult(
    int CategoriesCreated,
    int PostsImported,
    int PostsSkipped,
    List<string> Errors
);
```

- [ ] **Step 3: Read existing IWordPressImportService and replace content**

Read the current file first, then write with the updated interface.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feature: add import progress DTOs and interface"
```

---

### Task 4: Refactor WordPressImportService — shared parsers

**Files:**
- Modify: `CollegeLMS.API/Services/WordPressImportService.cs`

- [ ] **Step 1: Read the existing file**

- [ ] **Step 2: Rewrite WordPressImportService to add:**

1. Static `ConcurrentDictionary<string, ImportProgressDto>` for progress tracking
2. `StartImport` method — generates GUID, stores progress entry, starts Task.Run
3. `GetImportProgress` — reads from dictionary
4. `ImportFromRestApiAsync` — fetches from WordPress REST API, calls shared parsers
5. Extract shared `ImportCategoriesAsync` and `ImportPostsAsync` from existing `ImportFromJsonAsync`

Full implementation:

```csharp
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
        var progress = new ImportProgressDto
        {
            ImportId = importId,
            Status = "running",
        };
        _imports[importId] = progress;

        _ = Task.Run(async () =>
        {
            try
            {
                var ct = new CancellationToken();
                var progressRef = _imports[importId];
                progressRef.Status = "running";

                var importCt = CancellationToken.None;
                await importAction(importCt);

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
        CancellationToken ct
    )
    {
        if (!File.Exists(jsonPath))
            return Result<ImportResult>.Fail($"Файл не найден: {jsonPath}", 404);

        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(jsonPath, ct);
            using var doc = JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;

            return await ProcessImportAsync(root, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта WordPress из JSON");
            return Result<ImportResult>.Fail($"Ошибка импорта: {ex.Message}", 500);
        }
    }

    public async Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct
    )
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CollegeLMS/1.0");

            // Fetch categories
            var categoriesJson = await httpClient.GetStringAsync(
                "/wp-json/wp/v2/categories?per_page=100",
                ct
            );
            var categories = JsonSerializer.Deserialize<List<JsonElement>>(categoriesJson) ?? [];

            // Fetch posts
            var postsJson = await httpClient.GetStringAsync(
                "/wp-json/wp/v2/posts?per_page=100&_embed=1",
                ct
            );
            var posts = JsonSerializer.Deserialize<List<JsonElement>>(postsJson) ?? [];

            // Build a combined structure matching JSON import format
            using var combinedDoc = JsonDocument.Parse(
                $"{{\"categories\":{categoriesJson},\"posts\":{postsJson}}}"
            );

            return await ProcessImportAsync(combinedDoc.RootElement, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ошибка连接到 WordPress REST API");
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
        CancellationToken ct
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
            .Where(kvp => kvp.Value.Status != "running")
            .ToList();

        foreach (var kvp in stale)
        {
            _imports.TryRemove(kvp.Key, out _);
        }
    }
}
```

- [ ] **Step 3: Verify build passes**

```powershell
dotnet build
```
Expected: Build succeeded

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feature: refactor WordPressImportService with shared parsers and progress tracking"
```

---

### Task 5: Update ImportController with new endpoints

**Files:**
- Modify: `CollegeLMS.API/Controllers/ImportController.cs`

- [ ] **Step 1: Read the existing ImportController.cs**

- [ ] **Step 2: Rewrite ImportController with new endpoints**

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/import")]
[Produces("application/json")]
public class ImportController(
    IWordPressImportService importService,
    IWebHostEnvironment env,
    IConfiguration config
) : ControllerBase
{
    [HttpPost("wordpress")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Импортировать данные из WordPress JSON")]
    [SwaggerResponse(200, "Импорт запущен", typeof(Result<string>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public ActionResult<Result<string>> ImportWordPress()
    {
        var jsonPath = Path.Combine(env.ContentRootPath, "..", "import", "wp_data_full.json");
        if (!System.IO.File.Exists(jsonPath))
            jsonPath = "/import/wp_data_full.json";

        var importId = importService.StartImport(async ct =>
        {
            var result = await importService.ImportFromJsonAsync(jsonPath, ct);
            var progress = importService.GetImportProgress(importId);
            if (progress != null && result.IsSuccess)
            {
                progress.Result = result.Data;
            }
        });

        return Ok(Result<string>.Ok(importId));
    }

    [HttpPost("wordpress/rest")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Импортировать данные через WordPress REST API")]
    [SwaggerResponse(200, "Импорт запущен", typeof(Result<string>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(502, "Ошибка подключения к WordPress")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public ActionResult<Result<string>> ImportWordPressRest()
    {
        var baseUrl = config.GetValue<string>("WordPress:BaseUrl") ?? "https://stvcc.ru";

        var importId = importService.StartImport(async ct =>
        {
            var result = await importService.ImportFromRestApiAsync(baseUrl, ct);
            var progress = importService.GetImportProgress(importId);
            if (progress != null && result.IsSuccess)
            {
                progress.Result = result.Data;
            }
        });

        return Ok(Result<string>.Ok(importId));
    }

    [HttpGet("wordpress/status/{importId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить статус импорта")]
    [SwaggerResponse(200, "Статус импорта", typeof(Result<ImportProgressDto>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Импорт не найден")]
    [ProducesResponseType(typeof(Result<ImportProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<Result<ImportProgressDto>> GetImportStatus(string importId)
    {
        var progress = importService.GetImportProgress(importId);
        if (progress == null)
            return NotFound(Result<ImportProgressDto>.Fail("Импорт не найден", 404));

        return Ok(Result<ImportProgressDto>.Ok(progress));
    }
}
```

Note: I need to verify that `Result<T>.Ok(T data)` accepts `string`. Looking at the existing Result.cs:
```csharp
public static Result<T> Ok(T data) => new() { IsSuccess = true, Data = data, StatusCode = 200 };
```
Yes, `Result<string>.Ok(importId)` will work.

- [ ] **Step 3: Add WordPress base URL to appsettings.json**

Read `appsettings.json` and add:
```json
"WordPress": {
    "BaseUrl": "https://stvcc.ru"
}
```

- [ ] **Step 4: Verify build passes**

```powershell
dotnet build
```
Expected: Build succeeded

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feature: add import endpoints with progress polling + REST API import"
```

---

### Task 6: Add frontend types for import

**Files:**
- Modify: `frontend/types/index.ts`

- [ ] **Step 1: Add import types to index.ts**

```typescript
export interface ImportResult {
  categoriesCreated: number
  postsImported: number
  postsSkipped: number
  errors: string[]
}

export interface ImportProgressDto {
  importId: string
  status: "running" | "completed" | "failed"
  total: number
  processed: number
  errors: number
  errorMessages: string[]
  result: ImportResult | null
}
```

- [ ] **Step 2: Commit**

```powershell
git add -A
git commit -m "feature: add import frontend types"
```

---

### Task 7: Create admin import page (frontend)

**Files:**
- Create: `frontend/app/admin/import/page.tsx`

- [ ] **Step 1: Create admin import page**

```tsx
"use client"

import { useState } from "react"
import type { Result, ImportProgressDto } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function AdminImportPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [progress, setProgress] = useState<ImportProgressDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [polling, setPolling] = useState(false)

  const startImport = async (type: "json" | "rest") => {
    setError(null)
    setProgress(null)
    setLoading(true)

    try {
      const url = type === "json" ? "/api/import/wordpress" : "/api/import/wordpress/rest"
      const res = await api.post<Result<string>>(url)
      const body = res.data

      if (!body.isSuccess || !body.data) {
        setError(body.errorMessage ?? "Ошибка запуска импорта")
        setLoading(false)
        return
      }

      const importId = body.data
      setLoading(false)
      setPolling(true)
      pollStatus(importId)
    } catch {
      setError("Ошибка запуска импорта")
      setLoading(false)
    }
  }

  const pollStatus = async (importId: string) => {
    const interval = setInterval(async () => {
      try {
        const res = await api.get<Result<ImportProgressDto>>(
          `/api/import/wordpress/status/${importId}`
        )
        const body = res.data

        if (body.isSuccess && body.data) {
          setProgress(body.data)
          if (body.data.status === "completed" || body.data.status === "failed") {
            clearInterval(interval)
            setPolling(false)
          }
        } else {
          clearInterval(interval)
          setPolling(false)
          setError(body.errorMessage ?? "Ошибка получения статуса")
        }
      } catch {
        clearInterval(interval)
        setPolling(false)
        setError("Ошибка получения статуса импорта")
      }
    }, 2000)
  }

  const progressPercent = progress
    ? Math.round((progress.processed / (progress.total || 1)) * 100)
    : 0

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-3xl">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Импорт данных</h2>
      </div>

      {error && <ErrorBanner message={error} />}

      <div className="flex gap-4">
        <Button
          onClick={() => startImport("json")}
          disabled={loading || polling || !isAdmin}
        >
          {loading ? "Запуск..." : "Импорт из JSON"}
        </Button>
        <Button
          onClick={() => startImport("rest")}
          disabled={loading || polling || !isAdmin}
          variant="outline"
        >
          {loading ? "Запуск..." : "Импорт с WordPress (live)"}
        </Button>
      </div>

      {loading && <LoadingSpinner className="py-10" />}

      {polling && !progress && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <div className="h-4 w-4 animate-spin rounded-full border-2 border-muted border-t-primary" />
          Запуск импорта...
        </div>
      )}

      {progress && (
        <div className="flex flex-col gap-4 rounded-lg border bg-card p-6">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">
              Статус:{" "}
              {progress.status === "running"
                ? "Выполняется..."
                : progress.status === "completed"
                  ? "Завершён"
                  : "Ошибка"}
            </span>
            {progress.status === "running" && (
              <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted border-t-primary" />
            )}
          </div>

          {progress.total > 0 && (
            <div className="flex flex-col gap-2">
              <div className="flex justify-between text-sm text-muted-foreground">
                <span>
                  Обработано: {progress.processed} из {progress.total}
                </span>
                <span>{progressPercent}%</span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all duration-500"
                  style={{ width: `${progressPercent}%` }}
                />
              </div>
            </div>
          )}

          {progress.errors > 0 && (
            <div className="text-sm text-destructive">
              Ошибок: {progress.errors}
            </div>
          )}

          {progress.result && (
            <div className="mt-2 flex flex-col gap-1 text-sm">
              <p>Категорий создано: {progress.result.categoriesCreated}</p>
              <p>Новостей импортировано: {progress.result.postsImported}</p>
              <p>Новостей пропущено: {progress.result.postsSkipped}</p>
              {progress.result.errors.length > 0 && (
                <div className="mt-2 flex flex-col gap-1">
                  <p className="font-medium text-destructive">Ошибки:</p>
                  <ul className="list-inside list-disc text-destructive/80">
                    {progress.result.errors.map((err, i) => (
                      <li key={i}>{err}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Add nav link in admin layout**

Read `frontend/app/admin/layout.tsx`, modify `navItems`:
```typescript
const navItems = [
  { href: "/admin", label: "Пользователи", roles: ["Admin"] },
  { href: "/admin/news", label: "Новости", roles: ["Admin", "Dispatcher"] },
  { href: "/admin/import", label: "Импорт", roles: ["Admin"] },
]
```

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feature: add admin import page with progress UI"
```

---

### Task 8: Verify everything works

- [ ] **Step 1: Run all unit tests**

```powershell
dotnet test
```
Expected: All tests pass (including existing ones + new Feedback tests)

- [ ] **Step 2: Verify build**

```powershell
dotnet build
```
Expected: Build succeeded

- [ ] **Step 3: Start frontend and verify page renders**

```powershell
cd frontend && npm run dev
```
Expected: Dev server starts, navigate to `/admin/import` renders the page
