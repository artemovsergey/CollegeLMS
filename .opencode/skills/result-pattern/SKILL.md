---
name: result-pattern
description: Implement Result<T> pattern and ExceptionHandlerMiddleware for unified error handling
---

# result-pattern

Create the `Result<T>` pattern for consistent API responses and the global exception handler middleware.

## Files

### `CollegeLMS.API/Response/Result.cs`

```csharp
namespace CollegeLMS.API.Response;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }

    public static Result<T> Ok(T data) =>
        new() { IsSuccess = true, Data = data, StatusCode = 200 };

    public static Result<T> Fail(string message, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode };
}

public class Result
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }

    public static Result Ok() =>
        new() { IsSuccess = true, StatusCode = 200 };

    public static Result Fail(string message, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode };
}
```

### `CollegeLMS.API/Response/ApiResult.cs`

```csharp
namespace CollegeLMS.API.Response;

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static ApiResult<T> FromResult(Result<T> result) =>
        new() { IsSuccess = result.IsSuccess, Data = result.Data, ErrorMessage = result.ErrorMessage };
}
```

### `CollegeLMS.API/Response/ErrorResponse.cs`

```csharp
namespace CollegeLMS.API.Response;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
```

### `CollegeLMS.API/Middleware/ExceptionHandlerMiddleware.cs`

```csharp
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Middleware;

public class ExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = ex.Message, StatusCode = 404 });
        }
        catch (ForbiddenException ex)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = ex.Message, StatusCode = 403 });
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = ex.Message, StatusCode = 400 });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = "Внутренняя ошибка сервера", StatusCode = 500 });
        }
    }
}
```

### `CollegeLMS.API/Exceptions/` — custom exceptions

```csharp
// NotFoundException.cs
namespace CollegeLMS.API.Exceptions;
public class NotFoundException(string message) : Exception(message) { }

// ForbiddenException.cs
namespace CollegeLMS.API.Exceptions;
public class ForbiddenException(string message) : Exception(message) { }

// ValidationException.cs
namespace CollegeLMS.API.Exceptions;
public class ValidationException(string message) : Exception(message) { }
```

## Registration in Program.cs

```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

## Convention rules

- Services return `Result<T>.Ok(data)` or `Result<T>.Fail(message, statusCode)`
- Controllers convert to `ApiResult<T>` via `ApiResult<T>.FromResult(result)`
- Unexpected exceptions → 500 via middleware
- No try-catch in controllers/services (middleware handles it)
