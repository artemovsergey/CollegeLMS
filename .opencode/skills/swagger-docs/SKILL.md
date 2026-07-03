---
name: swagger-docs
description: Add full Swagger documentation — XML comments, ProducesResponseType, SwaggerExamples, Postman spec
---

# swagger-docs

Add comprehensive Swagger documentation for a newly created endpoint.

## Workflow

### 1. Add XML comments to controller

Every action method MUST have:

```csharp
/// <summary>Краткое описание на русском.</summary>
/// <remarks>Расширенное описание с деталями использования.</remarks>
/// <param name="paramName">Описание параметра</param>
/// <response code="200">Успешный ответ</response>
/// <response code="400">Ошибка валидации</response>
/// <response code="401">Не авторизован</response>
/// <response code="403">Доступ запрещён</response>
/// <response code="404">Не найдено</response>
/// <response code="500">Внутренняя ошибка сервера</response>
```

### 2. Add ProducesResponseType and SwaggerResponse attributes

```csharp
[SwaggerOperation(Summary = "Получить список групп")]
[SwaggerResponse(200, "Список получен", typeof(Result<List<GroupResponse>>))]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(500, "Ошибка сервера")]
[ProducesResponseType(typeof(Result<List<GroupResponse>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
```

Минимальный набор:
- `[ProducesResponseType(typeof(Result<T>), 200)]` — успех
- `[ProducesResponseType(typeof(ErrorResponse), 400)]` — validation (POST/PUT)
- `[ProducesResponseType(typeof(ErrorResponse), 401)]` — unauthorized (всегда)
- `[ProducesResponseType(typeof(ErrorResponse), 403)]` — forbidden (если есть роли)
- `[ProducesResponseType(typeof(ErrorResponse), 404)]` — not found (GET by id, PUT, DELETE)
- `[ProducesResponseType(typeof(ErrorResponse), 500)]` — server error (всегда)

### 3. Create ErrorResponse example class

Path: `CollegeLMS.API/SwaggerExamples/ErrorResponseExample.cs`

```csharp
using CollegeLMS.API.Response;

namespace CollegeLMS.API.SwaggerExamples;

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

### 4. Create response example for the entity

Path: `CollegeLMS.API/SwaggerExamples/{Name}ResponseExample.cs`

```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class {Name}ResponseExample
{
    public static {Name}Response Create() => new()
    {
        Id = Guid.NewGuid(),
        // заполнить поля примерами
    };
}
```

### 5. Update Postman collection

Add endpoint to `spec/CollegeLMS.postman_collection.json`:

```json
{
  "name": "{Entity Russian}",
  "item": [
    {
      "name": "Get all",
      "request": {
        "method": "GET",
        "url": {
          "raw": "{{baseUrl}}/api/{route}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "{route}"]
        }
      }
    },
    {
      "name": "Get by ID",
      "request": {
        "method": "GET",
        "url": {
          "raw": "{{baseUrl}}/api/{route}/{{id}}",
          "host": ["{{baseUrl}}"],
          "path": ["api", "{route}", "{{id}}"]
        }
      }
    }
  ]
}
```

## Conventions

- Summary/Description on Russian
- Every action method has XML `<summary>` and `<response>` for all status codes
- `[ProducesResponseType]` всегда указывает тип (Result<T> или ErrorResponse)
- `[SwaggerResponse]` с кратким описанием
- ErrorResponseExample переиспользуется для всех контроллеров
- Postman коллекция лежит в `spec/` и коммитится с кодом

## Verification

- `dotnet build` succeeds
- Swagger UI показывает все endpoint-ы с полным описанием
- Swagger UI показывает типы ответов (200/400/401/403/404/500)
- Postman коллекция содержит новые endpoint-ы
