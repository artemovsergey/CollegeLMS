---
name: dotnet-endpoint
description: Create a REST API endpoint with Controller, Service, DTO, manual mapper, search/filter/sort/pagination, and Swagger
---

# dotnet-endpoint

Create a full REST API endpoint following CollegeLMS conventions: Controller → Service → DTO with manual mapping, search/filter/sort/pagination support where needed.

## Workflow

### 1. Create DTOs

Path: `CollegeLMS.API/Dtos/{Action}{Name}Request.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CollegeLMS.API.Dtos;

public class {Action}{Name}Request
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Property { get; set; } = string.Empty;
}
```

Path: `CollegeLMS.API/Dtos/{Name}Response.cs`

```csharp
namespace CollegeLMS.API.Dtos;

public class {Name}Response
{
    public Guid Id { get; set; }
    public string Property { get; set; } = string.Empty;
}
```

For list endpoints with pagination/search/filter/sort, create an options DTO:

```csharp
public class {Name}FilterRequest
{
    public string? Search { get; set; }          // Full-text search
    public string? FilterProperty { get; set; }  // Column filter
    public string? SortBy { get; set; }          // Column name
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

### 2. Create mapper

Path: `CollegeLMS.API/Services/Mappers/{Name}Mapper.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Services.Mappers;

public static class {Name}Mapper
{
    public static {Name}Response ToDto(this {Name} entity)
    {
        return new {Name}Response
        {
            Id = entity.Id,
            // map each field manually
        };
    }

    public static {Name} ToEntity(this {Action}{Name}Request dto)
    {
        return new {Name}
        {
            // map each field manually
        };
    }
}
```

### 3. Create service interface and implementation

Path: `CollegeLMS.API/Services/I{Name}Service.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public interface I{Name}Service
{
    Task<Result<List<{Name}Response>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<{Name}Response>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<{Name}Response>> CreateAsync({Action}{Name}Request request, CancellationToken ct = default);
    Task<Result<{Name}Response>> UpdateAsync(Guid id, {Action}{Name}Request request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

Path: `CollegeLMS.API/Services/{Name}Service.cs`

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class {Name}Service(AppDbContext db) : I{Name}Service
{
    public async Task<Result<List<{Name}Response>>> GetAllAsync(CancellationToken ct)
    {
        var items = await db.Set<{Name}>()
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<List<{Name}Response>>.Ok(items.Select(x => x.ToDto()).ToList());
    }

    public async Task<Result<{Name}Response>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var item = await db.Set<{Name}>().FindAsync([id], ct);
        if (item is null)
            return Result<{Name}Response>.Error("Not found", 404);

        return Result<{Name}Response>.Ok(item.ToDto());
    }

    public async Task<Result<{Name}Response>> CreateAsync({Action}{Name}Request request, CancellationToken ct)
    {
        var entity = request.ToEntity();
        entity.Id = Guid.NewGuid();

        db.Set<{Name}>().Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<{Name}Response>.Ok(entity.ToDto());
    }

    public async Task<Result<{Name}Response>> UpdateAsync(Guid id, {Action}{Name}Request request, CancellationToken ct)
    {
        var entity = await db.Set<{Name}>().FindAsync([id], ct);
        if (entity is null)
            return Result<{Name}Response>.Error("Not found", 404);

        // update fields from request
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<{Name}Response>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Set<{Name}>().FindAsync([id], ct);
        if (entity is null)
            return Result.Error("Not found", 404);

        db.Set<{Name}>().Remove(entity);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
```

### 4. Register in DI

In `Program.cs`:
```csharp
builder.Services.AddScoped<I{Name}Service, {Name}Service>();
```

### 5. Create controller

Path: `CollegeLMS.API/Controllers/{Name}Controller.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class {Name}Controller(I{Name}Service service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список")]
    [SwaggerResponse(200, "Список получен")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result<List<{Name}Response>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить запись по ID")]
    [SwaggerResponse(200, "Запись найдена")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result<{Name}Response>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Создать запись")]
    [SwaggerResponse(200, "Запись создана")]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result<{Name}Response>>> Create({Action}{Name}Request request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Обновить запись")]
    [SwaggerResponse(200, "Запись обновлена")]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result<{Name}Response>>> Update(Guid id, {Action}{Name}Request request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить запись")]
    [SwaggerResponse(200, "Запись удалена")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

### 6. For list endpoints with search/filter/sort/pagination

Add to `{Name}Service`:

```csharp
public async Task<Result<ApiResult<{Name}Response>>> GetFilteredAsync(
    {Name}FilterRequest filter, CancellationToken ct)
{
    var query = db.Set<{Name}>().AsNoTracking();

    // Search
    if (!string.IsNullOrWhiteSpace(filter.Search))
        query = query.Where(x => x.{Property}.Contains(filter.Search));

    // Filter
    if (!string.IsNullOrWhiteSpace(filter.FilterProperty))
        query = query.Where(x => x.{Property} == filter.FilterProperty);

    // Sort
    query = (filter.SortBy?.ToLower()) switch
    {
        "property" => filter.SortDescending
            ? query.OrderByDescending(x => x.{Property})
            : query.OrderBy(x => x.{Property}),
        _ => query.OrderByDescending(x => x.CreatedAt)
    };

    // Pagination
    var total = await query.CountAsync(ct);
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync(ct);

    return Result<ApiResult<{Name}Response>>>.Ok(new ApiResult<{Name}Response>
    {
        Items = items.Select(x => x.ToDto()).ToList(),
        Total = total,
        Page = filter.Page,
        PageSize = filter.PageSize,
        HasNext = filter.Page * filter.PageSize < total,
        HasPrevious = filter.Page > 1
    });
}
```

### 7. Swagger annotation guidelines

- `Summary` in Russian — describes what endpoint does
- `[SwaggerResponse(200)]` — always present
- `[SwaggerResponse(400)]` — validation errors
- `[SwaggerResponse(401)]` — auth errors
- `[SwaggerResponse(404)]` — not found
- `[SwaggerResponse(500)]` — always present

## Convention rules

- Primary constructor DI everywhere
- All service methods return `Result<T>`, accept `CancellationToken ct = default`
- Controller returns `ActionResult<Result<T>>`
- Manual mapping via static extension methods
- Flat DTOs with default values
- File-scoped namespaces
- `AsNoTracking()` for read-only queries
- `FindAsync()` by PK, `FirstOrDefaultAsync()` for filtered queries

## Verification

- `dotnet build` succeeds
- Swagger UI shows all endpoints with docs
- CRUD operations return correct status codes
- Search/filter/sort/pagination works (if implemented)
