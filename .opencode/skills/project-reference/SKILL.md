---
name: project-reference
description: CollegeLMS project-wide conventions — packages, JWT auth, Docker, EF Core rules
---

# project-reference

Project-level reference: NuGet packages, JWT auth setup, Docker/deploy config, EF Core conventions, and connection strings.

## NuGet packages (CollegeLMS MVP)

| Purpose | Package |
|---------|---------|
| ORM | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Migrations | `Microsoft.EntityFrameworkCore.Design` |
| Snake case | `EFCore.NamingConventions` |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Validation | `FluentValidation.DependencyInjectionExtensions` |
| Swagger | `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations` |
| Logging | `Serilog.AspNetCore` |
| Healthchecks | `AspNetCore.HealthChecks.NpgSql` |
| Tests | `xunit`, `coverlet.msbuild`, `Bogus` |

## JWT Auth

### TokenService

```csharp
public interface ITokenService
{
    string GenerateAccessToken(string userId, string email, IList<string> roles);
}

public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(string userId, string email, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, email),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Program.cs registration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
```

### Swagger JWT config

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
```

### BCrypt

```csharp
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
bool valid = BCrypt.Net.BCrypt.Verify(input, storedHash);
```

### Claims helpers

```csharp
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)!;
}
```

## EF Core conventions

- Snake_case via `UseSnakeCaseNamingConvention()`
- GUID PKs with `.ValueGeneratedNever()`
- Strings: `.HasMaxLength(n)` required
- Enums: `.HasConversion<string>()` + `.HasMaxLength(n)` required
- Navigation properties: `[JsonIgnore]`
- Timestamps: `CreatedAt`, `UpdatedAt` = `DateTime.UtcNow`
- `[SwaggerOperation(Summary = "...")]` in Russian
- No AutoMapper — manual static extension mappers
- `CancellationToken ct` on all service/endpoint methods

## Docker & Deploy

### docker-compose.yml (dev)

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: collegelms-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: collegelms
    ports: ["5432:5432"]
    volumes: [postgres_data:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: collegelms-redis
    ports: ["6379:6379"]
    volumes: [redis_data:/data]
    command: redis-server --appendonly yes

volumes:
  postgres_data:
  redis_data:
```

### Dockerfile (API)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CollegeLMS.API/CollegeLMS.csproj", "CollegeLMS.API/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/CollegeLMS.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY appsettings.Production.json .
ENTRYPOINT ["dotnet", "CollegeLMS.API.dll"]
```

### Connection strings

| Context | String |
|---------|--------|
| Local dev | `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=postgres` |
| Docker | `Host=db;Port=5432;Database=collegelms;Username=postgres;Password=postgres` |
| Production | Via env vars: `ConnectionStrings__DefaultConnection=...` |

### CI/CD secrets

`VPS_HOST`, `VPS_USERNAME`, `SSH_PRIVATE_KEY`, `GITHUB_TOKEN`
