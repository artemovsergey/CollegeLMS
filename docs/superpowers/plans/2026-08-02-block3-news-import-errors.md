# Блок 3: Новости, импорт, ошибки — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Удалить статус IsPublished у новостей, перевести импорт WordPress на персистентные ImportJob в БД, добавить тосты для 500/504, улучшить модалку новостей (скролл, HTML-предпросмотр, превью картинки, валидация форм) и галерею.

**Architecture:** Бэкенд — монолит Clean Architecture: удаляем `IsPublished` из цепочки News (entity → DTO → маппер → сервис → миграция DropColumn), WordPressImportService переходит с in-memory `ConcurrentDictionary` на таблицу `import_jobs` (уже существует, миграция `AddImportJob`) с добавлением полей результата. Фронтенд — Next.js: правки типов, страницы `/admin/news`, `/admin/import`, `lib/api.ts` (тосты), `components/ui/dialog.tsx` (скролл), публичная страница новости (галерея).

**Tech Stack:** .NET 10 + EF Core Npgsql, Next.js 14 + TS + Tailwind 4 + sonner, FluentValidation, xUnit + InMemory.

## Global Constraints

- `Result<T>` везде, никаких try-catch в контроллерах/сервисах; `ExceptionHandlerMiddleware` ловит неожиданные
- Ручные мапперы в `Mappers/`, интерфейсы в `Interfaces/`, primary constructor DI, `AsNoTracking()` на чтении
- Сообщения об ошибках и Swagger summaries на русском
- String props: `HasMaxLength()`; enum: `HasConversion<string>()`; Guid PK `ValueGeneratedNever()`
- Миграции: `dotnet ef migrations add {Name} --project CollegeLMS.API -- --provider Npgsql`
- Проверки: `dotnet build` + `dotnet test` (корень решения), frontend `npx tsc --noEmit` + `npm run build` (в `CollegeLMS.Next`)
- Коммиты: `git add -A`, формат CSharpier `dotnet csharpier format .`
- Фронтенд: компоненты `components/FormField.tsx` (Label+id/error/hint/required), `components/FormErrorBanner.tsx`, `lib/errors.ts` (parseErrors) — уже созданы в Блоке 2
- InMemory-провайдер: `ExecuteUpdateAsync` не работает; тесты строятся через `TestDbContextFactory.Create()` (unit) и `CreateDbContext()` (integration)

---

### Task 1: Удаление IsPublished из News (backend)

**Files:**
- Modify: `CollegeLMS.API/Entities/News.cs`
- Modify: `CollegeLMS.API/Dtos/NewsRequest.cs`
- Modify: `CollegeLMS.API/Dtos/NewsResponse.cs`
- Modify: `CollegeLMS.API/Mappers/NewsMapper.cs`
- Modify: `CollegeLMS.API/Services/NewsService.cs`
- Modify: `CollegeLMS.API/Services/WordPressImportService.cs:384`
- Modify: `CollegeLMS.API/Data/DataSeeder.cs` (11 строк: 5991, 6007, 6022, 6038, 6054, 6069, 6084, 6099, 6114, 6129, 6588)
- Modify: `CollegeLMS.API/SwaggerExamples/NewsResponseExample.cs:14`
- Modify: `CollegeLMS.Tests/Fixtures/NewsFixture.cs:15`
- Modify: `CollegeLMS.Tests/Unit/Services/NewsServiceTests.cs` (CreateAsync_CreatesNews, UpdateAsync_UpdatesExistingNews)
- Modify: `CollegeLMS.Tests/Integration/Controllers/NewsControllerTests.cs:123`
- Modify: `CollegeLMS.Tests/Integration/Controllers/SearchControllerTests.cs:42`
- Migration: `AddNewsDropIsPublished` (DropColumn `is_published` из `news`)

**Interfaces:**
- Consumes: текущие `CreateNewsRequest`/`UpdateNewsRequest`/`NewsResponse`/`News`
- Produces: `CreateNewsRequest` без `IsPublished` и без `PublishedAt`; `UpdateNewsRequest` без `IsPublished`; `NewsResponse` без `IsPublished`; `News` без `IsPublished`; `CreateAsync` ставит `PublishedAt = DateTime.UtcNow`

- [ ] **Step 1: Написать падающие тесты (red)**

В `CollegeLMS.Tests/Unit/Services/NewsServiceTests.cs` заменить тело `CreateAsync_CreatesNews`:

```csharp
    [Fact]
    public async Task CreateAsync_CreatesNews()
    {
        var request = new CreateNewsRequest
        {
            Title = "Тестовая новость",
            Content = "<p>Содержание</p>",
        };

        var result = await _sut.CreateAsync(request, _adminId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Тестовая новость");
        result.Data.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        var saved = await _db.News.FirstAsync();
        saved.Title.Should().Be("Тестовая новость");
        saved.CreatedById.Should().Be(_adminId);
        saved.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
```

В том же файле заменить тело `UpdateAsync_UpdatesExistingNews`:

```csharp
    [Fact]
    public async Task UpdateAsync_UpdatesExistingNews()
    {
        var news = NewsFixture.CreateFaker().Generate();
        news.CreatedById = _adminId;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var request = new UpdateNewsRequest
        {
            Title = "Обновлённый заголовок",
            Content = "<p>Обновлённое содержание</p>",
        };

        var result = await _sut.UpdateAsync(news.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Обновлённый заголовок");
        result.Data.PublishedAt.Should().Be(news.PublishedAt);
    }
```

- [ ] **Step 2: Запустить тесты — зафиксировать baseline**

Run: `dotnet test --filter "FullyQualifiedName~NewsServiceTests"` — с текущим кодом тесты ПРОХОДЯТ (green baseline). Red-состояние фиксируется после правок DTO в Step 5 (тесты перестанут компилироваться, т.к. тестовые request-объекты ещё содержат `IsPublished`, а DTO его потеряют).

- [ ] **Step 3: Удалить IsPublished из entity и DTO**

`CollegeLMS.API/Entities/News.cs` — удалить строку `public bool IsPublished { get; set; }`.

`CollegeLMS.API/Dtos/NewsRequest.cs` — полностью заменить содержимое:

```csharp
namespace CollegeLMS.API.Dtos;

public class CreateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
}

public class UpdateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
}
```

`CollegeLMS.API/Dtos/NewsResponse.cs` — удалить строку `public bool IsPublished { get; set; }`.

`CollegeLMS.API/Mappers/NewsMapper.cs` — удалить строку `IsPublished = news.IsPublished,`.

- [ ] **Step 4: Обновить NewsService (создание → сразу опубликовано)**

`CollegeLMS.API/Services/NewsService.cs` — в `CreateAsync` заменить блок инициализации `News`:

```csharp
        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            PublishedAt = DateTime.UtcNow,
            CreatedById = currentUserId,
        };
```

В `UpdateAsync` удалить строку `news.IsPublished = request.IsPublished;` (строку `news.PublishedAt = request.PublishedAt ?? news.PublishedAt;` удалить тоже — UpdateNewsRequest больше не содержит PublishedAt, дата публикации не переключается).

- [ ] **Step 5: Убрать IsPublished из импорта и сидера**

`CollegeLMS.API/Services/WordPressImportService.cs:384` — удалить строку `IsPublished = status == "publish",`.

`CollegeLMS.API/Data/DataSeeder.cs` — удалить 10 строк `IsPublished = true,` (после `ImageUrl = ...` в каждой из 10 seed-новостей) и строку `IsPublished = status == "publish",` в методе WP-импорта сидера (строка 6588).

`CollegeLMS.API/SwaggerExamples/NewsResponseExample.cs` — удалить строку `isPublished = true,`.

- [ ] **Step 6: Запустить тесты — зафиксировать red (после правок кода)**

Run: `dotnet test --filter "FullyQualifiedName~NewsServiceTests"` — ожидаем FAIL (CS1061: `IsPublished` отсутствует в DTO/entity, тестовые request-объекты и фикстура ещё ссылаются на поле).

- [ ] **Step 7: Обновить тестовые фикстуры и прочие тесты**

`CollegeLMS.Tests/Fixtures/NewsFixture.cs` — удалить строку `.RuleFor(n => n.IsPublished, true)`.

`CollegeLMS.Tests/Integration/Controllers/NewsControllerTests.cs:123` — удалить `IsPublished = true,` из request в `Create_Returns201_WhenAdmin`.

`CollegeLMS.Tests/Integration/Controllers/SearchControllerTests.cs:42` — удалить `IsPublished = true,` из инициализатора `News`.

- [ ] **Step 7: Проверить Postman**

`docs/spec/CollegeLMS.postman_collection.json` — если body новостей содержит `isPublished` или `publishedAt` — удалить эти поля (grep `isPublished|publishedAt` по файлу).

- [ ] **Step 8: Создать миграцию**

Run: `dotnet ef migrations add RemoveNewsIsPublished --project CollegeLMS.API -- --provider Npgsql`
Expected: создаётся `CollegeLMS.API/Migrations/{timestamp}_RemoveNewsIsPublished.cs` с `migrationBuilder.DropColumn(name: "is_published", table: "news")`.

- [ ] **Step 9: Запустить тесты (green)**

Run: `dotnet build` затем `dotnet test`
Expected: 323 теста пройдено (0 failed).

- [ ] **Step 10: CSharpier + коммит**

Run: `dotnet csharpier format .` затем:
```bash
git add -A
git commit -m "feat: удалён статус IsPublished у новостей — создание публикует сразу"
```

---

### Task 2: Удаление IsPublished из фронтенда

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts` (строки 135, 146, 155)
- Modify: `CollegeLMS.Next/app/admin/news/page.tsx` (formPublished state, чекбокс, handleTogglePublish, колонка «Статус»)

**Interfaces:**
- Consumes: `NewsResponse`/`CreateNewsRequest`/`UpdateNewsRequest` из Task 1 (без isPublished)
- Produces: страница `/admin/news` без чекбокса «Опубликовано», без тумблера статуса в таблице, без `handleTogglePublish`

- [ ] **Step 1: Обновить типы**

`CollegeLMS.Next/types/index.ts`:
- `NewsResponse` (строка 135): удалить `isPublished: boolean`
- `CreateNewsRequest` (строка 146): удалить `isPublished?: boolean` и `publishedAt?: string`
- `UpdateNewsRequest` (строка 155): удалить `isPublished?: boolean` и `publishedAt?: string`

- [ ] **Step 2: Убрать форму статуса**

`CollegeLMS.Next/app/admin/news/page.tsx`:
- Удалить `const [formPublished, setFormPublished] = useState(true)` (строка 77)
- В `resetForm` удалить `setFormPublished(true)` (строка 186)
- В `fillForm` удалить `setFormPublished(item.isPublished)` (строка 198)
- В `handleCreate` удалить `isPublished: formPublished,` (строка 212)
- В `handleUpdate` удалить `isPublished: formPublished,` (строка 240)
- Удалить весь метод `handleTogglePublish` (строки 269-282)
- Удалить чекбокс-блок из `formDialog` (строки 361-372: `<div className="flex items-center gap-2">` с чекбоксом «Опубликовано»)

- [ ] **Step 3: Убрать колонку «Статус» из таблицы**

`CollegeLMS.Next/app/admin/news/page.tsx`:
- Удалить `<TableHead>Статус</TableHead>` (строка 483)
- Удалить `<TableCell>` со статус-кнопкой (строки 504-516)

- [ ] **Step 4: Проверить типы и сборку**

Run (в `CollegeLMS.Next`): `npx tsc --noEmit` затем `npm run build`
Expected: 0 ошибок.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: убран IsPublished из типов и страницы новостей админа"
```

---

### Task 3: Toast для 500 и 504 в lib/api.ts

**Files:**
- Modify: `CollegeLMS.Next/lib/api.ts`

**Interfaces:**
- Produces: глобальный response-interceptor с тостами на 500/504 (debounce 5 сек на код статуса), тосты не дублируются при polling импорта

- [ ] **Step 1: Реализовать тосты с debounce**

`CollegeLMS.Next/lib/api.ts` — полностью заменить содержимое:

```ts
import axios from "axios"
import { toast } from "sonner"

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "",
  headers: { "Content-Type": "application/json" },
})

api.interceptors.request.use(config => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("token")
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
  }
  return config
})

const TOAST_DEBOUNCE_MS = 5000
const lastShownAt: Record<number, number> = {}

function showDebouncedToast(status: number, message: string) {
  const now = Date.now()
  if (now - (lastShownAt[status] ?? 0) < TOAST_DEBOUNCE_MS) return
  lastShownAt[status] = now
  toast.error(message)
}

api.interceptors.response.use(
  response => response,
  error => {
    const status = error.response?.status as number | undefined
    if (typeof window !== "undefined" && status) {
      if (status === 401) {
        localStorage.removeItem("token")
        localStorage.removeItem("user")
        if (!window.location.pathname.startsWith("/login")) {
          window.location.href = "/login"
        }
      } else if (status === 500) {
        showDebouncedToast(500, "Ошибка сервера. Попробуйте позже")
      } else if (status === 504) {
        showDebouncedToast(504, "Сервер недоступен. Проверьте соединение")
      }
    }
    return Promise.reject(error)
  },
)

export default api
```

- [ ] **Step 2: Проверить сборку**

Run (в `CollegeLMS.Next`): `npx tsc --noEmit` затем `npm run build`
Expected: 0 ошибок.

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "feat: тосты для 500 и 504 с debounce в api-интерсепторе"
```

---

### Task 4: ImportJob — персистентный прогресс импорта в БД

**Files:**
- Modify: `CollegeLMS.API/Entities/ImportJob.cs` (добавить CategoriesCreated/PostsImported/PostsSkipped)
- Create: `CollegeLMS.API/Mappers/ImportJobMapper.cs`
- Modify: `CollegeLMS.API/Interfaces/IWordPressImportService.cs`
- Modify: `CollegeLMS.API/Services/WordPressImportService.cs`
- Modify: `CollegeLMS.API/Controllers/ImportController.cs`
- Create: `CollegeLMS.Tests/Integration/Controllers/ImportControllerTests.cs`
- Migration: `AddImportJobResult` (3 AddColumn)

**Interfaces:**
- Consumes: существующая таблица `import_jobs` (миграция `AddImportJob`), `ImportJobConfiguration`, `AppDbContext.ImportJobs` (AppDbContext.cs:39)
- Produces:
  - `ImportJob` + `CategoriesCreated`/`PostsImported`/`PostsSkipped` (int)
  - `IWordPressImportService.StartImport(Func<CancellationToken, Task>)` → `string` (id = job.Id, job создаётся в БД синхронно до запуска таска)
  - `Task<ImportProgressDto?> GetImportProgressAsync(string importId, CancellationToken ct)` (замена `GetImportProgress`)
  - `Task<ImportProgressDto?> GetActiveImportAsync(CancellationToken ct)` (замена `GetActiveImport`)
  - `ImportFromJsonAsync(string jsonPath, CancellationToken ct, Guid? jobId = null)` — обновляет job (Total/Processed/ErrorCount/ErrorMessages/результат)
  - `ImportFromRestApiAsync(string baseUrl, CancellationToken ct, Guid? jobId = null)` — то же
  - Контроллер: методы GetActiveImport/GetImportStatus становятся async, остальные маршруты без изменений

- [ ] **Step 1: Расширить ImportJob**

`CollegeLMS.API/Entities/ImportJob.cs` — добавить поля после `ErrorCount`:

```csharp
    public int ErrorCount { get; set; }
    public int CategoriesCreated { get; set; }
    public int PostsImported { get; set; }
    public int PostsSkipped { get; set; }
```

- [ ] **Step 2: Создать маппер ImportJob → ImportProgressDto**

`CollegeLMS.API/Mappers/ImportJobMapper.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;

namespace CollegeLMS.API.Mappers;

public static class ImportJobMapper
{
    public static ImportProgressDto ToDto(this ImportJob job) =>
        new()
        {
            ImportId = job.Id.ToString(),
            Status = job.Status,
            Total = job.Total,
            Processed = job.Processed,
            Errors = job.ErrorCount,
            ErrorMessages = job.ErrorMessages ?? [],
            Result = new ImportResult(
                job.CategoriesCreated,
                job.PostsImported,
                job.PostsSkipped,
                job.ErrorMessages ?? []
            ),
            CreatedAt = job.CreatedAt,
        };
}
```

- [ ] **Step 3: Обновить интерфейс**

`CollegeLMS.API/Interfaces/IWordPressImportService.cs` — заменить содержимое:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWordPressImportService
{
    Task<Result<ImportResult>> ImportFromJsonAsync(
        string jsonPath,
        CancellationToken ct,
        Guid? jobId = null
    );

    string StartImport(Func<CancellationToken, Task> importAction);

    void StopImport(string importId);

    Task<ImportProgressDto?> GetImportProgressAsync(string importId, CancellationToken ct);
    Task<ImportProgressDto?> GetActiveImportAsync(CancellationToken ct);

    Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct,
        Guid? jobId = null
    );
}

public record ImportResult(
    int CategoriesCreated,
    int PostsImported,
    int PostsSkipped,
    List<string> Errors
);
```

- [ ] **Step 4: Переписать WordPressImportService**

`CollegeLMS.API/Services/WordPressImportService.cs` — полная замена:

```csharp
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
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _importCts =
        new();

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
```

Примечание: `ImportFromRestApiAsync` не возвращает аккумулированный результат напрямую — результат читается из job после цикла страниц через `GetStoredImportResultAsync`. Аккумулирование в `ProcessImportAsync` происходит потому, что job загружается из БД на каждой странице (предыдущая страница уже сохранена в `SaveChangesAsync`).

- [ ] **Step 5: Обновить ImportController**

`CollegeLMS.API/Controllers/ImportController.cs` — заменить содержимое:

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

        string importId = null!;
        importId = importService.StartImport(async ct =>
        {
            await importService.ImportFromJsonAsync(jsonPath, ct, Guid.Parse(importId));
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

        string importId = null!;
        importId = importService.StartImport(async ct =>
        {
            await importService.ImportFromRestApiAsync(baseUrl, ct, Guid.Parse(importId));
        });

        return Ok(Result<string>.Ok(importId));
    }

    [HttpPost("wordpress/stop/{importId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Остановить импорт")]
    [SwaggerResponse(200, "Импорт остановлен", typeof(Result<bool>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Импорт не найден")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<bool>>> StopImport(string importId, CancellationToken ct)
    {
        var progress = await importService.GetImportProgressAsync(importId, ct);
        if (progress == null)
            return NotFound(Result<bool>.Fail("Импорт не найден", 404));

        importService.StopImport(importId);
        return Ok(Result<bool>.Ok(true));
    }

    [HttpGet("wordpress/active")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить активный импорт")]
    [SwaggerResponse(200, "Активный импорт", typeof(Result<ImportProgressDto>))]
    [SwaggerResponse(404, "Нет активного импорта")]
    [ProducesResponseType(typeof(Result<ImportProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<ImportProgressDto>>> GetActiveImport(
        CancellationToken ct
    )
    {
        var progress = await importService.GetActiveImportAsync(ct);
        if (progress == null)
            return NotFound(Result<ImportProgressDto>.Fail("Нет активного импорта", 404));

        return Ok(Result<ImportProgressDto>.Ok(progress));
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
    public async Task<ActionResult<Result<ImportProgressDto>>> GetImportStatus(
        string importId,
        CancellationToken ct
    )
    {
        var progress = await importService.GetImportProgressAsync(importId, ct);
        if (progress == null)
            return NotFound(Result<ImportProgressDto>.Fail("Импорт не найден", 404));

        return Ok(Result<ImportProgressDto>.Ok(progress));
    }
}
```

- [ ] **Step 6: Написать интеграционные тесты ImportController**

`CollegeLMS.Tests/Integration/Controllers/ImportControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ImportControllerTests : BaseIntegrationTest
{
    private string GetAdminToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.ru",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
        };
        return tokenService.GenerateAccessToken(admin);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetActiveImport_Returns404_WhenNoRunningJobs()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync("/api/import/wordpress/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveImport_ReturnsRunningJob()
    {
        SetAuthHeader(GetAdminToken());

        var job = new ImportJob { Id = Guid.NewGuid(), Status = "running", Total = 10, Processed = 3 };
        using (var db = CreateDbContext())
        {
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/import/wordpress/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<Result<ImportProgressDto>>(response);
        result!.IsSuccess.Should().BeTrue();
        result.Data!.ImportId.Should().Be(job.Id.ToString());
        result.Data.Status.Should().Be("running");
        result.Data.Processed.Should().Be(3);
        result.Data.Total.Should().Be(10);
    }

    [Fact]
    public async Task GetImportStatus_Returns404_WhenNotFound()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/import/wordpress/status/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetImportStatus_ReturnsJobWithResult()
    {
        SetAuthHeader(GetAdminToken());

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            Status = "completed",
            Total = 5,
            Processed = 5,
            CategoriesCreated = 2,
            PostsImported = 3,
            PostsSkipped = 2,
            CompletedAt = DateTime.UtcNow,
        };
        using (var db = CreateDbContext())
        {
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/import/wordpress/status/{job.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<Result<ImportProgressDto>>(response);
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("completed");
        result.Data.Processed.Should().Be(5);
        result.Data.Result.Should().NotBeNull();
        result.Data.Result!.CategoriesCreated.Should().Be(2);
        result.Data.Result.PostsImported.Should().Be(3);
        result.Data.Result.PostsSkipped.Should().Be(2);
    }

    [Fact]
    public async Task ImportWordPress_CreatesJobAndExposesStatus()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.PostAsync("/api/import/wordpress", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var start = await DeserializeAsync<Result<string>>(response);
        start!.IsSuccess.Should().BeTrue();
        start.Data.Should().NotBeNullOrEmpty();

        var statusResponse = await Client.GetAsync($"/api/import/wordpress/status/{start.Data}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await DeserializeAsync<Result<ImportProgressDto>>(statusResponse);
        status!.IsSuccess.Should().BeTrue();
        status.Data.Should().NotBeNull();
    }
}
```

Примечание: `ImportWordPress_CreatesJobAndExposesStatus` стартует импорт JSON-файла — файл в тестовом окружении отсутствует, поэтому фоновая задача быстро завершится со статусом failed; тест проверяет только факт создания job и доступность статуса (job существует независимо от исхода импорта).

- [ ] **Step 7: Создать миграцию AddImportJobResult**

Run: `dotnet ef migrations add AddImportJobResult --project CollegeLMS.API -- --provider Npgsql`
Expected: `AddColumn` для `categories_created`, `posts_imported`, `posts_skipped` (int, not null, default 0) в таблице `import_jobs`.

- [ ] **Step 8: Проверить сборку и тесты (green)**

Run: `dotnet build` затем `dotnet test`
Expected: 328 тестов пройдено (0 failed).

- [ ] **Step 9: CSharpier + коммит**

Run: `dotnet csharpier format .` затем:
```bash
git add -A
git commit -m "feat: персистентный прогресс импорта через ImportJob в БД"
```

---

### Task 5: Подсказка импорта и отказ от localStorage

**Files:**
- Modify: `CollegeLMS.Next/app/admin/import/page.tsx`
- Modify: `CollegeLMS.Next/app/admin/news/page.tsx`

**Interfaces:**
- Consumes: `GET /api/import/wordpress/active` (Task 4)
- Produces: подсказка «Импорт новостей и категорий из WordPress (stvcc.ru). Импорт асинхронный — можно покинуть страницу и вернуться позже» на обеих страницах; страница `/admin/import` восстанавливает состояние только из `/active` (без localStorage)

- [ ] **Step 1: Убрать localStorage из /admin/import**

`CollegeLMS.Next/app/admin/import/page.tsx` — заменить `useEffect` (строки 20-39):

```tsx
  useEffect(() => {
    api
      .get<Result<ImportProgressDto>>("/api/import/wordpress/active")
      .then(res => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setProgress(body.data)
          setPolling(true)
          pollStatus(body.data.importId)
        }
      })
      .catch(() => {})
  }, [])
```

- Удалить `localStorage.setItem("importId", importId)` в `startImport` (строка 57)
- Удалить `localStorage.removeItem("importId")` в `pollStatus` (строка 81)

- [ ] **Step 2: Добавить подсказку в /admin/import**

`CollegeLMS.Next/app/admin/import/page.tsx` — после блока заголовка `<div className="flex items-center justify-between">` (строка 105) вставить:

```tsx
      <p className="text-sm text-muted-foreground">
        Импорт новостей и категорий из WordPress (stvcc.ru). Импорт асинхронный — можно покинуть
        страницу и вернуться позже.
      </p>
```

- [ ] **Step 3: Добавить подсказку в /admin/news**

`CollegeLMS.Next/app/admin/news/page.tsx` — после закрытия блока заголовка `</div>` (конец строки 431) вставить:

```tsx

      <p className="text-xs text-muted-foreground">
        Импорт новостей и категорий из WordPress (stvcc.ru). Импорт асинхронный — можно покинуть
        страницу и вернуться позже.
      </p>
```

- [ ] **Step 4: Проверить сборку**

Run (в `CollegeLMS.Next`): `npx tsc --noEmit` затем `npm run build`
Expected: 0 ошибок.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: подсказка импорта и восстановление из /active без localStorage"
```

---

### Task 6: Модалка новости — скролл, HTML-предпросмотр, превью картинки, валидация форм

**Files:**
- Modify: `CollegeLMS.Next/components/ui/dialog.tsx` (DialogContent: `max-h-[85vh] overflow-y-auto`)
- Modify: `CollegeLMS.Next/app/admin/news/page.tsx` (Textarea font-mono, кнопка «Предпросмотр», превью картинки object-contain, FormField/FormErrorBanner/parseErrors)

**Interfaces:**
- Consumes: `components/ContentRenderer.tsx` (рендер HTML), `components/FormField.tsx`, `components/FormErrorBanner.tsx`, `lib/errors.ts` (parseErrors)
- Produces: модалка создания/редактирования новости: скроллится при длинном HTML, textarea monospace, предпросмотр через ContentRenderer, превью картинки без обрезки, валидация полей с серверными ошибками

- [ ] **Step 1: Добавить скролл в DialogContent**

`CollegeLMS.Next/components/ui/dialog.tsx` — в className компонента `DialogContent` (строка 64) добавить `max-h-[85vh] overflow-y-auto`:

```tsx
        className={cn(
          "fixed top-[50%] left-[50%] z-50 grid w-full max-h-[85vh] overflow-y-auto max-w-[calc(100%-2rem)] translate-x-[-50%] translate-y-[-50%] gap-4 rounded-lg border bg-background p-6 shadow-lg duration-200 outline-none data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95 data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95 sm:max-w-lg",
          className
        )}
```

- [ ] **Step 2: Импорты и состояние формы новости**

`CollegeLMS.Next/app/admin/news/page.tsx`:
- Добавить импорты: `ContentRenderer`, `FormField`, `FormErrorBanner`, `parseErrors`:

```tsx
import ContentRenderer from "@/components/ContentRenderer"
import FormField from "@/components/FormField"
import FormErrorBanner from "@/components/FormErrorBanner"
import { parseErrors } from "@/lib/errors"
```

- Заменить `const [formError, setFormError] = useState<string | null>(null)` на:

```tsx
  const [formError, setFormError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [showPreview, setShowPreview] = useState(false)
```

- В `resetForm` добавить `setFieldErrors({})` и `setShowPreview(false)`
- В `fillForm` добавить `setFieldErrors({})` и `setShowPreview(false)`

- [ ] **Step 3: Обработка серверных ошибок через parseErrors**

`CollegeLMS.Next/app/admin/news/page.tsx` — заменить в `handleCreate` (строка 220) и `handleUpdate` (строка 248):

```tsx
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
        setFieldErrors(res.data.errors ?? {})
      }
```

и

```tsx
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
        setFieldErrors(res.data.errors ?? {})
      }
```

И заменить блоки `catch`:

```tsx
    } catch (err) {
      const parsed = parseErrors(err)
      setFormError(parsed.message)
      setFieldErrors(parsed.fieldErrors)
    } finally {
```

- [ ] **Step 4: Переписать formDialog**

`CollegeLMS.Next/app/admin/news/page.tsx` — заменить весь `formDialog` (строки 284-382):

```tsx
  const formDialog = (
    <form onSubmit={editingId ? handleUpdate : handleCreate} className="flex flex-col gap-4">
      {formError && <FormErrorBanner message={formError} />}

      <FormField id="news-title" label="Заголовок" required error={fieldErrors.Title?.[0]}>
        <Input
          id="news-title"
          required
          value={formTitle}
          onChange={e => setFormTitle(e.target.value)}
        />
      </FormField>

      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <FormField id="news-content" label="Текст (HTML)" required error={fieldErrors.Content?.[0]}>
            <Textarea
              id="news-content"
              required
              value={formContent}
              onChange={e => setFormContent(e.target.value)}
              rows={8}
              className="font-mono text-sm"
            />
          </FormField>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="self-end"
            onClick={() => setShowPreview(v => !v)}
          >
            {showPreview ? "Редактирование" : "Предпросмотр"}
          </Button>
        </div>
        {showPreview && (
          <div className="max-h-64 overflow-y-auto rounded-md border bg-muted/40 p-3">
            <ContentRenderer content={formContent || "<p>Введите текст для предпросмотра</p>"} />
          </div>
        )}
      </div>

      <FormField id="news-image" label="Изображение (постер)" hint="JPG или PNG">
        <div className="flex items-center gap-2">
          <Input
            id="news-image"
            type="file"
            accept="image/jpeg,image/png"
            disabled={uploading}
            onChange={async e => {
              const file = e.target.files?.[0]
              if (!file) return
              setUploading(true)
              setFormError(null)
              try {
                const formData = new FormData()
                formData.append("file", file)
                const res = await api.post<Result<UploadResponse>>("/api/upload", formData, {
                  headers: { "Content-Type": undefined },
                })
                if (res.data.isSuccess && res.data.data) {
                  setFormImageUrl(res.data.data.url)
                } else {
                  setFormError(res.data.errorMessage ?? "Ошибка загрузки")
                }
              } catch {
                setFormError("Ошибка загрузки файла")
              } finally {
                setUploading(false)
              }
            }}
          />
          {uploading && (
            <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted border-t-primary shrink-0" />
          )}
        </div>
        {formImageUrl && (
          <div className="mt-1 flex justify-center rounded-md border bg-muted/40 p-2">
            <Image
              src={formImageUrl}
              alt="Превью"
              width={0}
              height={0}
              sizes="100vw"
              className="h-auto max-h-64 w-auto object-contain"
              style={{ width: "auto", height: "auto", maxHeight: "16rem" }}
              unoptimized
            />
          </div>
        )}
      </FormField>

      <FormField id="news-category" label="Категория">
        <Select value={formCategoryId} onValueChange={setFormCategoryId}>
          <SelectTrigger id="news-category">
            <SelectValue placeholder="Без категории" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none">Без категории</SelectItem>
            {categories.map(cat => (
              <SelectItem key={cat.id} value={cat.id}>
                {cat.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </FormField>

      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetForm}>
          Отмена
        </Button>
        <Button type="submit" disabled={formSubmitting}>
          {formSubmitting ? "Сохранение..." : editingId ? "Сохранить" : "Создать"}
        </Button>
      </div>
    </form>
  )
```

Примечание: `fieldErrors.Title`/`fieldErrors.Content` — ключи валидатора FluentValidation — это имена свойств C# (`Title`, `Content`), которые приходят в `errors` от API. Для поля «Категория» отдельный контроллер: если `formCategoryId === "none"` — при submit передаём `undefined` (обработка уже есть в handleCreate/handleUpdate: `categoryId: formCategoryId || undefined` — строка 211/239 остаётся без изменений).

- [ ] **Step 5: Убрать неиспользуемый импорт Label**

`CollegeLMS.Next/app/admin/news/page.tsx` — если `Label` больше не используется (проверить grep `Label`), удалить `import { Label } from "@/components/ui/label"`.

- [ ] **Step 6: Проверить сборку**

Run (в `CollegeLMS.Next`): `npx tsc --noEmit` затем `npm run build`
Expected: 0 ошибок.

- [ ] **Step 7: Коммит**

```bash
git add -A
git commit -m "feat: модалка новости — скролл, HTML-предпросмотр, превью картинки, валидация форм"
```

---

### Task 7: Галерея новости — отступы лайтбокса

**Files:**
- Modify: `CollegeLMS.Next/app/(public)/news/[id]/page.tsx` (лайтбокс: `p-4`)

**Interfaces:**
- Consumes: существующий лайтбокс (строки 208-257)
- Produces: лайтбокс галереи с отступом `p-4` от краёв экрана

- [ ] **Step 1: Добавить отступ лайтбоксу**

`CollegeLMS.Next/app/(public)/news/[id]/page.tsx` — в контейнере лайтбокса (строка 210) заменить className:

```tsx
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4"
          onClick={() => setGalleryOpen(false)}
        >
```

- [ ] **Step 2: Проверить сборку**

Run (в `CollegeLMS.Next`): `npx tsc --noEmit` затем `npm run build`
Expected: 0 ошибок.

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "feat: отступы лайтбокса галереи новости"
```

---

## Итоговая проверка (после Task 7)

- [ ] Run: `dotnet build` + `dotnet test` — 0 failed
- [ ] Run (в `CollegeLMS.Next`): `npx tsc --noEmit` + `npm run build` — 0 ошибок
- [ ] Run: `dotnet csharpier format .` и проверить `git status` чистый после коммитов
- [ ] Обновить `docs/spec/CollegeLMS.postman_collection.json` при необходимости (Task 1 Step 7)
- [ ] Записать прогресс в `.superpowers/sdd/progress.md` (git-ignored)

