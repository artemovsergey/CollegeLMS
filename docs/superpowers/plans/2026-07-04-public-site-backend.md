# Public Site — Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create News entity + API (CRUD with pagination) for the public college website.

**Architecture:** Monolithic .NET 10 Web API. News and NewsCategory entities with EF Core, `Result<T>` pattern, manual mappers, FluentValidation, Swagger annotations. Public GET endpoints (no auth), Admin CRUD (auth required).

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core, Npgsql, FluentValidation, Swashbuckle, xUnit, Moq, Bogus.

## Global Constraints

1. All entities inherit from `Entity` base class (Guid Id, CreatedAt, UpdatedAt)
2. File-scoped namespaces throughout
3. Primary constructor DI (`class Service(AppDbContext db)`)
4. `AsNoTracking()` on read queries, `FindAsync()` for PK lookups
5. `CancellationToken ct` on all async methods
6. `Result<T>.Ok()` / `Result<T>.Fail()` — never throw in services/controllers
7. EF Configurations in `Data/Configurations/`, `ToTable("snake_case")`, `HasMaxLength()`, `HasConversion<string>()` for enums
8. DTOs in `Dtos/`, mappers in `Mappers/`, services in `Services/`, interfaces in `Interfaces/`
9. Error messages in Russian, Swagger summaries in Russian
10. Soft delete: `IsDeleted = true`, don't actually remove rows (except for categories)
11. Validation: FluentValidation via `AddValidatorsFromAssemblyContaining<Program>()`
12. Formatting: `dotnet csharpier .` before commit

## File Structure

### New files:

| File | Responsibility |
|------|---------------|
| `Entities/NewsCategory.cs` | News category entity (Name, Slug) |
| `Entities/News.cs` | News entity (Title, Content, ImageUrl, CategoryId, IsPublished, PublishedAt, IsDeleted) |
| `Data/Configurations/NewsCategoryConfiguration.cs` | EF config for NewsCategory |
| `Data/Configurations/NewsConfiguration.cs` | EF config for News |
| `Response/PagedResponse.cs` | Generic paginated response wrapper |
| `Dtos/NewsRequest.cs` | Create + Update DTOs |
| `Dtos/NewsResponse.cs` | News response DTO |
| `Dtos/NewsCategoryResponse.cs` | Category response DTO |
| `Mappers/NewsMapper.cs` | Entity → DTO extension methods |
| `Interfaces/INewsService.cs` | Service interface |
| `Services/NewsService.cs` | Service implementation with pagination |
| `Controllers/NewsController.cs` | REST endpoints |
| `Validators/NewsRequestValidator.cs` | FluentValidation rules |
| `SwaggerExamples/NewsResponseExample.cs` | Swagger example |

### Modified files:

| File | Change |
|------|--------|
| `Data/AppDbContext.cs` | Add `DbSet<News>` + `DbSet<NewsCategory>` |
| `Extensions/ServiceCollectionExtensions.cs` | Add `INewsService` DI registration |
| `CollegeLMS.Tests/Unit/Services/` | Add `NewsServiceTests.cs` |
| `CollegeLMS.Tests/Integration/Controllers/` | Add `NewsControllerTests.cs` |

---

### Task 1: Branch + Entity + EF Config + Migration

**Files:**
- Create: `Entities/NewsCategory.cs`
- Create: `Entities/News.cs`
- Create: `Data/Configurations/NewsCategoryConfiguration.cs`
- Create: `Data/Configurations/NewsConfiguration.cs`
- Modify: `Data/AppDbContext.cs` (lines 13-21)

- [ ] **Step 1: Create branch**

```bash
git checkout -b feature/public-site
```

- [ ] **Step 2: Create NewsCategory entity** at `CollegeLMS.API/Entities/NewsCategory.cs`

```csharp
namespace CollegeLMS.API.Entities;

public class NewsCategory : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create News entity** at `CollegeLMS.API/Entities/News.cs`

```csharp
namespace CollegeLMS.API.Entities;

public class News : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public NewsCategory? Category { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}
```

- [ ] **Step 4: Create NewsCategory EF config** at `CollegeLMS.API/Data/Configurations/NewsCategoryConfiguration.cs`

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NewsCategoryConfiguration : IEntityTypeConfiguration<NewsCategory>
{
    public void Configure(EntityTypeBuilder<NewsCategory> builder)
    {
        builder.ToTable("news_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).HasDatabaseName("ix_news_categories_slug").IsUnique();
    }
}
```

- [ ] **Step 5: Create News EF config** at `CollegeLMS.API/Data/Configurations/NewsConfiguration.cs`

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.ToTable("news");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder
            .HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.PublishedAt).HasDatabaseName("ix_news_published_at");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("ix_news_is_deleted");
    }
}
```

- [ ] **Step 6: Add DbSets to AppDbContext** at `CollegeLMS.API/Data/AppDbContext.cs` — add after line 21:

```csharp
    public DbSet<News> News => Set<News>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
```

- [ ] **Step 7: Build and add migration**

```bash
dotnet build
dotnet ef migrations add AddNewsEntity --project CollegeLMS.API -- --provider Npgsql
```

Expected: build passes, migration files created in `CollegeLMS.API/Migrations/`

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feature: add News and NewsCategory entities with migration"
```

---

### Task 2: PagedResponse + DTOs + Mapper

**Files:**
- Create: `Response/PagedResponse.cs`
- Create: `Dtos/NewsRequest.cs`
- Create: `Dtos/NewsResponse.cs`
- Create: `Dtos/NewsCategoryResponse.cs`
- Create: `Mappers/NewsMapper.cs`

- [ ] **Step 1: Create PagedResponse** at `CollegeLMS.API/Response/PagedResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Response;

public class PagedResponse<T>
{
    [JsonInclude]
    public List<T> Items { get; private set; }

    [JsonInclude]
    public int TotalCount { get; private set; }

    [JsonInclude]
    public int Page { get; private set; }

    [JsonInclude]
    public int PageSize { get; private set; }

    [JsonInclude]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResponse() { }

    public PagedResponse(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
```

- [ ] **Step 2: Create NewsRequest DTOs** at `CollegeLMS.API/Dtos/NewsRequest.cs`

```csharp
namespace CollegeLMS.API.Dtos;

public class CreateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class UpdateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

- [ ] **Step 3: Create NewsResponse DTO** at `CollegeLMS.API/Dtos/NewsResponse.cs`

```csharp
namespace CollegeLMS.API.Dtos;

public class NewsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Create NewsCategoryResponse DTO** at `CollegeLMS.API/Dtos/NewsCategoryResponse.cs`

```csharp
namespace CollegeLMS.API.Dtos;

public class NewsCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
```

- [ ] **Step 5: Create NewsMapper** at `CollegeLMS.API/Mappers/NewsMapper.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class NewsMapper
{
    public static NewsResponse ToDto(this News news) =>
        new()
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            ImageUrl = news.ImageUrl,
            CategoryId = news.CategoryId,
            CategoryName = news.Category?.Name ?? string.Empty,
            IsPublished = news.IsPublished,
            PublishedAt = news.PublishedAt,
            CreatedAt = news.CreatedAt,
            CreatedByName = news.CreatedBy?.FullName ?? string.Empty,
        };

    public static NewsCategoryResponse ToDto(this NewsCategory category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
        };
}
```

- [ ] **Step 6: Build**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feature: add PagedResponse, DTOs, and mappers for news"
```

---

### Task 3: Service Interface + Implementation

**Files:**
- Create: `Interfaces/INewsService.cs`
- Create: `Services/NewsService.cs`

- [ ] **Step 1: Create INewsService** at `CollegeLMS.API/Interfaces/INewsService.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface INewsService
{
    Task<Result<PagedResponse<NewsResponse>>> GetAllAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken ct = default
    );
    Task<Result<NewsResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<NewsResponse>> CreateAsync(
        CreateNewsRequest request,
        Guid currentUserId,
        CancellationToken ct = default
    );
    Task<Result<NewsResponse>> UpdateAsync(
        Guid id,
        UpdateNewsRequest request,
        CancellationToken ct = default
    );
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<List<NewsCategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create NewsService** at `CollegeLMS.API/Services/NewsService.cs`

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class NewsService(AppDbContext db) : INewsService
{
    public async Task<Result<PagedResponse<NewsResponse>>> GetAllAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken ct
    )
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db
            .News.AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(n => n.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) || n.Content.ToLower().Contains(term)
            );
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => n.ToDto())
            .ToListAsync(ct);

        return Result<PagedResponse<NewsResponse>>.Ok(
            new PagedResponse<NewsResponse>(items, totalCount, page, pageSize)
        );
    }

    public async Task<Result<NewsResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var news = await db
            .News.AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result<NewsResponse>.Fail("Новость не найдена", 404);

        return Result<NewsResponse>.Ok(news.ToDto());
    }

    public async Task<Result<NewsResponse>> CreateAsync(
        CreateNewsRequest request,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.NewsCategories.AnyAsync(
                c => c.Id == request.CategoryId,
                ct
            );
            if (!categoryExists)
                return Result<NewsResponse>.Fail("Категория не найдена", 404);
        }

        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            IsPublished = request.IsPublished,
            PublishedAt = request.PublishedAt ?? DateTime.UtcNow,
            CreatedById = currentUserId,
        };

        db.News.Add(news);
        await db.SaveChangesAsync(ct);

        news = await db
            .News.Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstAsync(n => n.Id == news.Id, ct);

        return Result<NewsResponse>.Ok(news.ToDto());
    }

    public async Task<Result<NewsResponse>> UpdateAsync(
        Guid id,
        UpdateNewsRequest request,
        CancellationToken ct
    )
    {
        var news = await db
            .News.Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result<NewsResponse>.Fail("Новость не найдена", 404);

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.NewsCategories.AnyAsync(
                c => c.Id == request.CategoryId,
                ct
            );
            if (!categoryExists)
                return Result<NewsResponse>.Fail("Категория не найдена", 404);
        }

        news.Title = request.Title;
        news.Content = request.Content;
        news.ImageUrl = request.ImageUrl;
        news.CategoryId = request.CategoryId;
        news.IsPublished = request.IsPublished;
        news.PublishedAt = request.PublishedAt ?? news.PublishedAt;
        news.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<NewsResponse>.Ok(news.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var news = await db.News.FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result.Fail("Новость не найдена", 404);

        news.IsDeleted = true;
        news.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<List<NewsCategoryResponse>>> GetCategoriesAsync(CancellationToken ct)
    {
        var categories = await db
            .NewsCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(ct);

        return Result<List<NewsCategoryResponse>>.Ok(categories);
    }
}
```

- [ ] **Step 3: Build**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feature: add INewsService and NewsService"
```

---

### Task 4: Controller + Validators + Swagger + DI Registration

**Files:**
- Create: `Controllers/NewsController.cs`
- Create: `Validators/NewsRequestValidator.cs`
- Create: `SwaggerExamples/NewsResponseExample.cs`
- Modify: `Extensions/ServiceCollectionExtensions.cs` (line 174)

- [ ] **Step 1: Create NewsRequestValidator** at `CollegeLMS.API/Validators/NewsRequestValidator.cs`

```csharp
using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateNewsRequestValidator : AbstractValidator<CreateNewsRequest>
{
    public CreateNewsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок новости обязателен")
            .MaximumLength(255)
            .WithMessage("Заголовок не должен превышать 255 символов");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Содержание новости обязательно");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .WithMessage("URL изображения не должен превышать 2048 символов");
    }
}

public class UpdateNewsRequestValidator : AbstractValidator<UpdateNewsRequest>
{
    public UpdateNewsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок новости обязателен")
            .MaximumLength(255)
            .WithMessage("Заголовок не должен превышать 255 символов");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Содержание новости обязательно");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .WithMessage("URL изображения не должен превышать 2048 символов");
    }
}
```

- [ ] **Step 2: Create Swagger example** at `CollegeLMS.API/SwaggerExamples/NewsResponseExample.cs`

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class NewsResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            title = "День открытых дверей в колледже связи",
            content = "<p>Приглашаем всех желающих на день открытых дверей...</p>",
            imageUrl = "https://example.com/images/event.jpg",
            categoryId = Guid.NewGuid(),
            categoryName = "Мероприятия",
            isPublished = true,
            publishedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            createdByName = "Иванов Иван Иванович",
        };
}
```

- [ ] **Step 3: Create NewsController** at `CollegeLMS.API/Controllers/NewsController.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/news")]
[Produces("application/json")]
public class NewsController(INewsService service) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить список новостей с пагинацией")]
    [SwaggerResponse(200, "Список новостей получен", typeof(Result<PagedResponse<NewsResponse>>))]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<PagedResponse<NewsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<PagedResponse<NewsResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default
    )
    {
        var result = await service.GetAllAsync(page, pageSize, categoryId, search, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить новость по ID")]
    [SwaggerResponse(200, "Новость найдена", typeof(Result<NewsResponse>))]
    [SwaggerResponse(404, "Новость не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<NewsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<NewsResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать новость (только Admin)")]
    [SwaggerResponse(200, "Новость создана", typeof(Result<NewsResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Категория не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<NewsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<NewsResponse>>> Create(
        CreateNewsRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.CreateAsync(request, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Обновить новость (только Admin)")]
    [SwaggerResponse(200, "Новость обновлена", typeof(Result<NewsResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Новость не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<NewsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<NewsResponse>>> Update(
        Guid id,
        UpdateNewsRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Удалить новость (мягкое удаление, только Admin)")]
    [SwaggerResponse(200, "Новость удалена", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Новость не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить список рубрик новостей")]
    [SwaggerResponse(200, "Список рубрик получен", typeof(Result<List<NewsCategoryResponse>>))]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<NewsCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<NewsCategoryResponse>>>> GetCategories(
        CancellationToken ct
    )
    {
        var result = await service.GetCategoriesAsync(ct);
        return Ok(result);
    }
}
```

- [ ] **Step 4: Register INewsService in DI** at `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — add after line 174 (`services.AddValidatorsFromAssemblyContaining<Program>();`):

Insert BEFORE the line `services.AddValidatorsFromAssemblyContaining<Program>();`:

```csharp
        services.AddScoped<INewsService, NewsService>();
```

- [ ] **Step 5: Build**

```bash
dotnet build
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feature: add NewsController, validators, swagger, DI"
```

---

### Task 5: Tests

**Files:**
- Create: `CollegeLMS.Tests/Unit/Services/NewsServiceTests.cs`
- Create: `CollegeLMS.Tests/Integration/Controllers/NewsControllerTests.cs`

We use the existing test infrastructure: xUnit + Moq + Bogus + WebApplicationFactory + InMemory EF.

- [ ] **Step 1: Read existing test fixture to understand patterns**

```bash
Get-ChildItem -Path "CollegeLMS.Tests/Fixtures/" -Name
```

Expected: fixture files exist with Bogus generators for existing entities. Create a fixture for News.

- [ ] **Step 2: Create News Fixture** at `CollegeLMS.Tests/Fixtures/NewsFixture.cs`

```csharp
using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class NewsFixture
{
    public static Faker<News> Create() =>
        new Faker<News>()
            .RuleFor(n => n.Id, f => Guid.NewGuid())
            .RuleFor(n => n.Title, f => f.Lorem.Sentence(5))
            .RuleFor(n => n.Content, f => f.Lorem.Paragraphs(3))
            .RuleFor(n => n.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(n => n.IsPublished, true)
            .RuleFor(n => n.PublishedAt, f => f.Date.Past(30))
            .RuleFor(n => n.CreatedAt, f => f.Date.Past(30))
            .RuleFor(n => n.UpdatedAt, f => f.Date.Past(30));
}
```

- [ ] **Step 3: Create unit test** at `CollegeLMS.Tests/Unit/Services/NewsServiceTests.cs`

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class NewsServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.ru",
            FullName = "Администратор Тестов",
            PasswordHash = "hash",
            Role = Entities.Enums.UserRole.Admin,
        };
        db.Users.Add(user);
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedNews()
    {
        using var db = CreateDbContext();
        var newsList = NewsFixture.Create().Generate(5);
        newsList.ForEach(n => n.CreatedById = db.Users.First().Id);
        db.News.AddRange(newsList);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.GetAllAsync(1, 10, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);
        result.Data.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_RespectsPagination()
    {
        using var db = CreateDbContext();
        var newsList = NewsFixture.Create().Generate(10);
        newsList.ForEach(n => n.CreatedById = db.Users.First().Id);
        db.News.AddRange(newsList);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.GetAllAsync(1, 3, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(10);
        result.Data.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNews_WhenExists()
    {
        using var db = CreateDbContext();
        var news = NewsFixture.Create().Generate();
        news.CreatedById = db.Users.First().Id;
        db.News.Add(news);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.GetByIdAsync(news.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(news.Id);
        result.Data.Title.Should().Be(news.Title);
    }

    [Fact]
    public async Task GetByIdAsync_Returns404_WhenNotFound()
    {
        using var db = CreateDbContext();
        var service = new NewsService(db);
        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesNews()
    {
        using var db = CreateDbContext();
        var userId = db.Users.First().Id;

        var service = new NewsService(db);
        var request = new CreateNewsRequest
        {
            Title = "Тестовая новость",
            Content = "<p>Содержание</p>",
            IsPublished = true,
        };

        var result = await service.CreateAsync(request, userId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Тестовая новость");

        var savedNews = await db.News.FirstAsync();
        savedNews.Title.Should().Be("Тестовая новость");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        using var db = CreateDbContext();
        var news = NewsFixture.Create().Generate();
        news.CreatedById = db.Users.First().Id;
        db.News.Add(news);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.DeleteAsync(news.Id);

        result.IsSuccess.Should().BeTrue();

        var deletedNews = await db.News
            .IgnoreQueryFilters()
            .FirstAsync(n => n.Id == news.Id);
        deletedNews.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        using var db = CreateDbContext();

        var cat1 = new NewsCategory { Id = Guid.NewGuid(), Name = "Новости", Slug = "news" };
        var cat2 = new NewsCategory { Id = Guid.NewGuid(), Name = "Мероприятия", Slug = "events" };
        db.NewsCategories.AddRange(cat1, cat2);

        var news1 = NewsFixture.Create().Generate();
        news1.CategoryId = cat1.Id;
        news1.CreatedById = db.Users.First().Id;
        var news2 = NewsFixture.Create().Generate();
        news2.CategoryId = cat2.Id;
        news2.CreatedById = db.Users.First().Id;
        db.News.AddRange(news1, news2);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.GetAllAsync(1, 10, cat1.Id, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().AllSatisfy(n => n.CategoryId.Should().Be(cat1.Id));
    }

    [Fact]
    public async Task GetAllAsync_SearchesByTitle()
    {
        using var db = CreateDbContext();
        var userId = db.Users.First().Id;

        var matching = NewsFixture.Create().Generate();
        matching.Title = "День открытых дверей";
        matching.CreatedById = userId;
        var notMatching = NewsFixture.Create().Generate();
        notMatching.Title = "Расписание экзаменов";
        notMatching.CreatedById = userId;
        db.News.AddRange(matching, notMatching);
        await db.SaveChangesAsync();

        var service = new NewsService(db);
        var result = await service.GetAllAsync(1, 10, null, "дверей");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Title.Should().Be("День открытых дверей");
    }
}
```

- [ ] **Step 4: Create integration test** at `CollegeLMS.Tests/Integration/Controllers/NewsControllerTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Integration.Controllers;

public class NewsControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedNews()
    {
        var newsList = NewsFixture.Create().Generate(3);
        newsList.ForEach(n =>
        {
            n.CreatedById = TestUser.Id;
            n.CreatedBy = TestUser;
        });
        Db.News.AddRange(newsList);
        await Db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/news?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResponse<NewsResponse>>>();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var response = await Client.GetAsync($"/api/news/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthenticated()
    {
        var request = new CreateNewsRequest
        {
            Title = "Новость",
            Content = "Контент",
        };

        var response = await Client.PostAsJsonAsync("/api/news", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 5: Read BaseIntegrationTest to understand test setup**

```bash
Get-Content -Path "CollegeLMS.Tests/Integration/BaseIntegrationTest.cs"
```

- [ ] **Step 6: Run tests**

```bash
dotnet test
```

Expected: all tests pass (142 existing + ~12 new).

- [ ] **Step 7: Format code**

```bash
dotnet csharpier .
```

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feature: add news service and controller tests"
```
