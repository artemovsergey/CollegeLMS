---
name: project-bootstrap
description: Full project bootstrap — NuGet packages, DbContext, Program.cs (EF + JWT + Swagger + Serilog + CORS + HealthChecks + Middleware), XML docs, Postman spec, appsettings, first migration
---

# project-bootstrap

Complete one-time setup for CollegeLMS API project. Run steps in order.

## Workflow

### 1. Install NuGet packages

Run from project root:

```powershell
cd CollegeLMS.API

dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package EFCore.NamingConventions
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Swashbuckle.AspNetCore
dotnet add package Swashbuckle.AspNetCore.Annotations
dotnet add package Serilog.AspNetCore
dotnet add package AspNetCore.HealthChecks.NpgSql
```

### 2. Create directory structure

```powershell
New-Item -ItemType Directory -Path Controllers, Services, Interfaces, Mappers, Entities, Entities\Enums, Data, Data\Configurations, Dtos, Validators, SwaggerExamples, Middleware, Response, Exceptions, Extensions -Force
```

Also create root `spec/` for Postman collection:
```powershell
New-Item -ItemType Directory -Path ..\spec -Force
```

### 3. Enable XML documentation (Swagger)

Add to `CollegeLMS.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### 4. Create Result\<T\> pattern

**`Response/Result.cs`** — generic + non-generic `Result`:
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

    public static Result<T> Ok(T data) =>
        new() { IsSuccess = true, Data = data, StatusCode = 200 };

    public static Result<T> Fail(string error, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };

    public static implicit operator Result<T>(T data) => Ok(data);
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    public static Result Ok() => new() { IsSuccess = true, StatusCode = 200 };
    public static Result Fail(string error, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };
}
```

**`Response/ApiResult.cs`** — pagination wrapper:
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

    public static ApiResult<T> Ok(List<T> items, int total, int page, int pageSize) =>
        new()
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            HasNext = page * pageSize < total,
            HasPrevious = page > 1
        };

    public static ApiResult<T> Fail(string error) => throw new NotImplementedException();
}
```

### 5. Create ErrorResponse

**`Response/ErrorResponse.cs`**:
```csharp
namespace CollegeLMS.API.Response;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
```

### 6. Create base Entity class

**`Entities/Entity.cs`**:

```csharp
namespace CollegeLMS.API.Entities;

public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

All domain entities MUST inherit from `Entity`.

### 7. Create exception classes

**`Exceptions/NotFoundException.cs`**:
```csharp
namespace CollegeLMS.API.Exceptions;

public class NotFoundException(string message) : Exception(message) { }
```

**`Exceptions/ValidationException.cs`**:
```csharp
namespace CollegeLMS.API.Exceptions;

public class ValidationException(string message) : Exception(message) { }
```

**`Exceptions/ForbiddenException.cs`**:
```csharp
namespace CollegeLMS.API.Exceptions;

public class ForbiddenException(string message) : Exception(message) { }
```

### 8. Create ExceptionHandlerMiddleware

**`Middleware/ExceptionHandlerMiddleware.cs`**:
```csharp
using System.Net;
using System.Text.Json;
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
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
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Не авторизован"),
            NpgsqlException or DbUpdateException => ((int)HttpStatusCode.InternalServerError, "Ошибка базы данных"),
            _ => ((int)HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера")
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

### 9. Create AppDbContext

**`Data/AppDbContext.cs`**:
```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 10. Create extension methods for DI and middleware pipeline

Extension methods keep `Program.cs` clean. All `builder.Services.Add*` calls go into `Extensions/ServiceCollectionExtensions.cs`, all `app.Use*` calls into `Extensions/ApplicationBuilderExtensions.cs`.

**`Extensions/ServiceCollectionExtensions.cs`**:

```csharp
using System.Text;
using CollegeLMS.API.Data;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace CollegeLMS.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());
        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CollegeLMS API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer"), [] }
            });
        });
        return services;
    }

    public static IServiceCollection AddCorsFrontend(this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(o =>
        {
            o.AddPolicy("AllowFrontend", p =>
                p.WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
                    .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        });
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all application services here
        // services.AddScoped<I{Name}Service, {Name}Service>();
        return services;
    }

    public static IServiceCollection AddHealthChecksWithDb(this IServiceCollection services, IConfiguration config)
    {
        services.AddHealthChecks()
            .AddNpgSql(config.GetConnectionString("DefaultConnection")!);
        return services;
    }
}
```

**`Extensions/ApplicationBuilderExtensions.cs`**:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Middleware;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(db);
        }
    }
}
```

### 11. Create ClaimsPrincipalExtensions

**`Extensions/ClaimsPrincipalExtensions.cs`**:
```csharp
using System.Security.Claims;

namespace CollegeLMS.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)!;
}
```

### 12. Write Program.cs (FULL setup)

**Important API changes in this version** (OpenApi v3, .NET 10):
- `using Microsoft.OpenApi;` — NOT `using Microsoft.OpenApi;` (removed in v3)
- `new OpenApiSecuritySchemeReference("Bearer")` — instead of `new OpenApiSecurityScheme { Reference = new OpenApiReference { ... } }`
- `AddSecurityRequirement(_ => ...)` — takes `Func<OpenApiDocument, ...>` now
- `Result<T>.Fail(...)` and `Result.Fail(...)` — `Error()` renamed to avoid naming conflict with `ErrorMessage` property

**`Program.cs`** — minimal, all setup delegated to extension methods:
```csharp
using CollegeLMS.API.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services
    .AddDatabase(builder.Configuration)
    .AddJwt(builder.Configuration)
    .AddSwaggerWithBearer()
    .AddCorsFrontend(builder.Configuration)
    .AddApplicationServices()
    .AddHealthChecksWithDb(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionMiddleware();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync();
}

await app.RunAsync();
```

### 13. Update appsettings.json

**`appsettings.json`**:
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/collegelms-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "Jwt": {
    "Key": "collegelms-super-secret-key-minimum-32-characters"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=postgres"
  },
  "Cors": {
    "Origins": ["http://localhost:3000"]
  },
  "AllowedHosts": "*"
}
```

### 14. Verify build

```powershell
dotnet build
```

### 16. First EF migration (after at least one entity exists)

```powershell
dotnet ef migrations add InitialCreate -- --provider Npgsql
dotnet ef database update
```

### 15. Create initial Postman collection

Path: `spec/CollegeLMS.postman_collection.json`:

```json
{
  "info": {
    "name": "CollegeLMS API",
    "description": "API для управления учебным заведением.",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "auth": {
    "type": "bearer",
    "bearer": [{ "key": "token", "value": "{{jwt}}", "type": "string" }]
  },
  "variable": [
    { "key": "baseUrl", "value": "http://localhost:5000" },
    { "key": "jwt", "value": "" }
  ],
  "item": []
}
```

Каждый новый endpoint добавляется в соответствующую папку коллекции. Порядок:
1. Название папки = русское название сущности
2. Внутри — запросы для каждого CRUD метода
3. Использовать `{{baseUrl}}` и `{{jwt}}` переменные
4. JWT токен получать через отдельный запрос (POST /api/auth/login)

## Convention rules

- Middleware order: `ExceptionHandler` → `Cors` → `Swagger` (dev) → `Auth` → `Authorization` → `Controllers`
- `builder.Services.*` calls in `Extensions/ServiceCollectionExtensions.cs` as chainable extension methods
- `app.Use*` and `app.Map*` calls in `Extensions/ApplicationBuilderExtensions.cs`
- `Program.cs` remains minimal — only Serilog bootstrap, chained `Add*()`, and pipeline
- All domain entities inherit from `Entities/Entity` (Guid PK, CreatedAt, UpdatedAt)
- Service interfaces go into `Interfaces/` folder
- Mappers go into root `Mappers/` folder
- All service DI registered with comments grouped by feature
- JWT key min 32 chars, stored in `appsettings.json` (overridden by env var in prod)
- CORS origins configurable via `Cors:Origins` array
- HealthChecks at `/healthz`
- Migrations auto-applied in Development only
- Serilog writes to Console + rolling File
- Connection string uses `DefaultConnection` key

## Verification checklist

- [ ] `dotnet build` — succeeds with no errors
- [ ] Directory structure matches AGENTS.md spec (`Mappers/`, `Interfaces/`, `Validators/`)
- [ ] Swagger UI available at `/swagger` in Development
- [ ] Swagger shows all endpoints with XML doc comments (<summary>, <remarks>, <response>)
- [ ] Swagger shows ErrorResponse type for 400/401/404/500 responses
- [ ] `/healthz` returns 200 OK when DB is up
- [ ] `ExceptionHandlerMiddleware` registered before any other middleware
- [ ] JWT auth configured with Bearer scheme in Swagger
- [ ] CORS allows frontend origin
- [ ] `Program.cs` is minimal — all setup in extension methods
- [ ] `ServiceCollectionExtensions.cs` has chainable `.Add{Feature}()` methods
- [ ] `ApplicationBuilderExtensions.cs` has `.Use{Feature}()` methods
- [ ] `Entity.cs` base class exists with `Id`, `CreatedAt`, `UpdatedAt`
- [ ] `CollegeLMS.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- [ ] `spec/CollegeLMS.postman_collection.json` exists with current endpoints
