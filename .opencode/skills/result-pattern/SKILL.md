---
name: result-pattern
description: Implement Result<T> pattern and ExceptionHandlerMiddleware for unified error handling
---

# result-pattern

Implement `Result<T>` for all API responses and `ExceptionHandlerMiddleware` for centralized error handling. No try-catch in controllers or services — errors flow through the result type or are caught by middleware.

## Workflow

### 1. Create Result\<T\>

Path: `CollegeLMS.API/Response/Result.cs`

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Response;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    [JsonConstructor]
    private Result() { }

    public static Result<T> Ok(T data)
    {
        return new Result<T> { IsSuccess = true, Data = data, StatusCode = 200 };
    }

    public static Result<T> Error(string error, int statusCode = 400)
    {
        return new Result<T> { IsSuccess = false, Error = error, StatusCode = statusCode };
    }

    public static implicit operator Result<T>(T data) => Ok(data);
}
```

Also create a non-generic `Result` for void-returning operations:

```csharp
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    public static Result Ok() => new() { IsSuccess = true, StatusCode = 200 };
    public static Result Error(string error, int statusCode = 400)
        => new() { IsSuccess = false, Error = error, StatusCode = statusCode };
}
```

### 2. Create pagination wrapper

Path: `CollegeLMS.API/Response/ApiResult.cs`

```csharp
namespace CollegeLMS.API.Response;

public class ApiResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
```

### 3. Create ErrorResponse

Path: `CollegeLMS.API/Response/ErrorResponse.cs`

```csharp
namespace CollegeLMS.API.Response;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
```

### 4. Create exception classes

Path: `CollegeLMS.API/Exceptions/NotFoundException.cs`

```csharp
namespace CollegeLMS.API.Exceptions;

public class NotFoundException(string message) : Exception(message) { }
```

Also add:
- `ValidationException` — invalid input
- `ForbiddenException` — access denied

### 5. Create ExceptionHandlerMiddleware

Path: `CollegeLMS.API/Middleware/ExceptionHandlerMiddleware.cs`

```csharp
using System.Net;
using System.Text.Json;
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Response;
using Npgsql;

namespace CollegeLMS.API.Middleware;

public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
            ValidationException => ((int)HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
            NpgsqlException or DbUpdateException => ((int)HttpStatusCode.InternalServerError, "Database error"),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal server error")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Detail = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment() ? ex.ToString() : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 6. Register middleware

In `Program.cs`, before `app.MapControllers()`:
```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

### 7. Password hashing (BCrypt)

```csharp
// Hash
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// Verify
var valid = BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
```

### 8. Usage patterns

**Service layer — expected errors as Result:**
```csharp
return Result<Entity>.Error("Not found", 404);
return Result<List<Entity>>.Ok(entities);

// Unexpected exceptions throw normally — caught by middleware
```

**Service layer — unexpected exceptions:**
```csharp
// OK to throw — middleware catches it
throw new NotFoundException($"User {id} not found");
```

**Controller:**
```csharp
var result = await service.GetByIdAsync(id);
if (!result.IsSuccess)
    return StatusCode(result.StatusCode, result);
return Ok(result);
```

## Convention rules

- Every service method returns `Result<T>` or `Result`
- Every controller action returns `ActionResult<Result<T>>`
- Status codes: 200 (OK), 400 (validation), 401 (auth), 404 (not found), 500 (server error)
- Error messages in Russian (user-facing), code in English
- Never throw in service layer for expected errors — return `Result.Error()`
- `CancellationToken` passed through all layers
- BCrypt workFactor: 12

## Verification

- Missing entity → `404` with `ErrorResponse` JSON
- Invalid input → `400` with error message
- Internal errors → `500` without stack trace in production
- `dotnet build` succeeds
