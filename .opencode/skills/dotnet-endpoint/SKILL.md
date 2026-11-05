---
name: dotnet-endpoint
description: Create a REST API endpoint with Controller, Service Interface/Implementation, DTO, manual mapper, fluent validation, search/filter/sort/pagination, and Swagger
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

Path: `CollegeLMS.API/Mappers/{Name}Mapper.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

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

Mappers are placed in the root `Mappers/` folder (not nested under Services).

### 3. Create service interface

Path: `CollegeLMS.API/Interfaces/I{Name}Service.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface I{Name}Service
{
    Task<Result<List<{Name}Response>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<{Name}Response>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<{Name}Response>> CreateAsync({Action}{Name}Request request, CancellationToken ct = default);
    Task<Result<{Name}Response>> UpdateAsync(Guid id, {Action}{Name}Request request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

Service interfaces are placed in the root `Interfaces/` folder.

### 4. Create service implementation

Path: `CollegeLMS.API/Services/{Name}Service.cs`

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
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

### 5. Register in DI

In `Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<I{Name}Service, {Name}Service>();
```

All DI registrations go into `ServiceCollectionExtensions.cs`, NOT in `Program.cs` directly.

### 6. Create Swagger example classes

Creates example responses for common error cases. Reuse `ErrorResponseExample` for all controllers.

Path: `CollegeLMS.API/SwaggerExamples/ErrorResponseExample.cs`

```csharp
using CollegeLMS.API.Response;

namespace CollegeLMS.API.SwaggerExamples;

/// <summary>Примеры ответов с ошибками.</summary>
public static class ErrorResponseExample
{
    public static ErrorResponse NotFound(string entity = "Запись") => new()
    {
        StatusCode = 404,
        Message = $"{entity} не найдена",
    };

    public static ErrorResponse ValidationError() => new()
    {
        StatusCode = 400,
        Message = "Ошибка валидации",
        Detail = "{\"Поле\": [\"Поле обязательно\"]}",
    };

    public static ErrorResponse Unauthorized() => new()
    {
        StatusCode = 401,
        Message = "Не авторизован",
    };

    public static ErrorResponse Forbidden() => new()
    {
        StatusCode = 403,
        Message = "Доступ запрещён",
    };

    public static ErrorResponse ServerError() => new()
    {
        StatusCode = 500,
        Message = "Внутренняя ошибка сервера",
    };
}
```

Path: `CollegeLMS.API/SwaggerExamples/{Name}Example.cs`

Create a Swagger example for the response DTO:

```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class {Name}ResponseExample
{
    public static {Name}Response Create() => new()
    {
        Id = Guid.NewGuid(),
        Property = "Пример значения",
    };
}
```

### 7. Create controller (fully documented)

Path: `CollegeLMS.API/Controllers/{Name}Controller.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Управление {entity_ru}.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class {Name}Controller(I{Name}Service service) : ControllerBase
{
    /// <summary>Получить список всех записей.</summary>
    /// <remarks>Возвращает полный список записей. Доступно всем авторизованным пользователям.</remarks>
    /// <response code="200">Список успешно получен</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список {entity_ru}")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<{Name}Response>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<{Name}Response>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<{Name}Response>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>Получить запись по ID.</summary>
    /// <param name="id">Идентификатор записи</param>
    /// <response code="200">Запись найдена</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Запись не найдена</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить {entity_ru} по ID")]
    [SwaggerResponse(200, "Запись найдена", typeof(Result<{Name}Response>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<{Name}Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<{Name}Response>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Создать новую запись.</summary>
    /// <param name="request">Данные для создания</param>
    /// <response code="200">Запись создана</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost]
    [SwaggerOperation(Summary = "Создать {entity_ru}")]
    [SwaggerResponse(200, "Запись создана", typeof(Result<{Name}Response>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<{Name}Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<{Name}Response>>> Create({Action}{Name}Request request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Обновить существующую запись.</summary>
    /// <param name="id">Идентификатор записи</param>
    /// <param name="request">Новые данные</param>
    /// <response code="200">Запись обновлена</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Запись не найдена</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Обновить {entity_ru}")]
    [SwaggerResponse(200, "Запись обновлена", typeof(Result<{Name}Response>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<{Name}Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<{Name}Response>>> Update(Guid id, {Action}{Name}Request request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Удалить запись.</summary>
    /// <param name="id">Идентификатор записи</param>
    /// <response code="200">Запись удалена</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Запись не найдена</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить {entity_ru}")]
    [SwaggerResponse(200, "Запись удалена", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

### 9. For list endpoints with search/filter/sort/pagination

Add to `{Name}Service`:

```csharp
public async Task<Result<ApiResult<{Name}Response>>> GetFilteredAsync(
    {Name}FilterRequest filter, CancellationToken ct)
{
    var query = db.Set<{Name}>().AsNoTracking();

    // Search
    if (!string.IsNullOrWhiteSpace(filter.Search))
        query = query.Where(x => x.Property.Contains(filter.Search));

    // Filter
    if (!string.IsNullOrWhiteSpace(filter.FilterProperty))
        query = query.Where(x => x.Property == filter.FilterProperty);

    // Sort
    query = (filter.SortBy?.ToLower()) switch
    {
        "property" => filter.SortDescending
            ? query.OrderByDescending(x => x.Property)
            : query.OrderBy(x => x.Property),
        _ => query.OrderByDescending(x => x.CreatedAt)
    };

    // Pagination
    var total = await query.CountAsync(ct);
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync(ct);

    return Result<ApiResult<{Name}Response>>.Ok(new ApiResult<{Name}Response>
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

### 10. FluentValidation (optional but recommended)

Create validator in `CollegeLMS.API/Validators/{Action}{Name}RequestValidator.cs`:

```csharp
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class Create{Name}RequestValidator : AbstractValidator<Create{Name}Request>
{
    public Create{Name}RequestValidator()
    {
        RuleFor(x => x.Property)
            .NotEmpty().WithMessage("Поле обязательно")
            .MaximumLength({n}).WithMessage("Максимум {n} символов");
    }
}
```

Register in `ServiceCollectionExtensions.cs`:
```csharp
services.AddValidatorsFromAssemblyContaining<Program>();
```

### 11. Postman collection

После создания endpoint-а обновить Postman коллекцию в `spec/CollegeLMS.postman_collection.json`.

Структура файла:
```json
{
  "info": {
    "name": "CollegeLMS API",
    "description": "API для управления учебным заведением"
  },
  "item": [
    {
      "name": "{EntityPlural}",
      "item": [
        {
          "name": "Get all",
          "request": {
            "method": "GET",
            "url": "{{baseUrl}}/api/{route}",
            "auth": { "type": "bearer", "bearer": [{"key": "token", "value": "{{jwt}}", "type": "string"}] }
          }
        }
      ]
    }
  ]
}
```

Каждый endpoint добавляется как отдельный `item` внутри папки сущности.
Коллекция лежит в `spec/` и коммитится вместе с кодом.

## Swagger conventions

- `/// <summary>` — русское описание метода
- `/// <remarks>` — расширенное описание с контекстом использования
- `/// <response code="...">` — описание каждого возможного ответа
- `[ProducesResponseType(typeof(...), StatusCodes.Status...)]` — для всех статусов
- `[SwaggerOperation(Summary = "...")]` — краткое название на русском
- `[SwaggerResponse(code, "...", typeof(...))]` — для всех не-success статусов
- Статусы: 200 (OK), 400 (BadRequest), 401 (Unauthorized), 403 (Forbidden), 404 (NotFound), 500 (InternalServerError)
- `ErrorResponseExample` в `SwaggerExamples/` — для всех error response типов

## Convention rules

- Primary constructor DI everywhere
- All service methods return `Result<T>`, accept `CancellationToken ct = default`
- Controller returns `ActionResult<Result<T>>`
- Manual mapping via static extension methods in root `Mappers/` folder
- Service interfaces go into root `Interfaces/` folder
- DI registrations go into `Extensions/ServiceCollectionExtensions.cs`
- Flat DTOs with default values
- File-scoped namespaces
- `AsNoTracking()` for read-only queries
- `FindAsync()` by PK, `FirstOrDefaultAsync()` for filtered queries

## Verification

- `dotnet build` succeeds
- Swagger UI shows all endpoints with docs
- CRUD operations return correct status codes
- Search/filter/sort/pagination works (if implemented)
