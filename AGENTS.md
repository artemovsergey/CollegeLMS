# CollegeLMS — Agent instructions

## Stack (verified)
- Backend: .NET 10, ASP.NET Core Web API (single project, no .sln)
- Frontend: Next.js 14, TS, Tailwind CSS 4 (not yet scaffolded in repo)
- DB: PostgreSQL 16, Cache: Redis (sessions only)
- Deploy: Docker Compose, GitHub Actions CI/CD (not yet set up)
- Files: local FS (MinIO later)

## Architecture
- Monolith, Clean Architecture (folders not separate projects)
- REST API, JSON, JWT auth (no refresh tokens in MVP)
- No message brokers, gRPC, WebSocket in MVP
- `Result<T>` pattern everywhere — no try-catch in controllers/services
- `ExceptionHandlerMiddleware` catches unexpected exceptions
- Manual mappers (no AutoMapper), FluentValidation, Swashbuckle
- All data in Russian, code/comments in English

## Project structure
```
CollegeLMS.API/          # .NET project (net10.0)
  Program.cs             # Minimal scaffold — needs EF/JWT/Swagger/Middleware setup
  Controllers/           # ControllerBase subclasses
  Services/              # I{Name}Service + {Name}Service
  Services/Mappers/      # Static extension mappers
  Entities/              # Domain classes
  Entities/Enums/        # Enum types
  Data/                  # AppDbContext
  Data/Configurations/   # IEntityTypeConfiguration<T>
  Dtos/                  # Request/Response DTOs
  Middleware/             # ExceptionHandlerMiddleware
  Response/              # Result<T>, ApiResult<T>, ErrorResponse
  Exceptions/            # NotFoundException, etc.
import/                  # WordPress import data (wp_structure_raw.json)
scripts/                 # parsing_stvcc.py (WP data scraper)
task.md                  # Full product plan (read this for feature scope)
```

## Key skills (in `.mimocode/skills/`)
Use these for code generation — they encode all conventions:
| Skill | What it creates |
|-------|----------------|
| `dotnet-entity` | Entity class, EF config, enum, migration |
| `dotnet-endpoint` | Controller, Service, DTO, mapper, DI registration |
| `result-pattern` | Result<T>, ApiResult, ExceptionHandlerMiddleware |
| `jwt-auth` | TokenService, BCrypt, Swagger bearer, Claims helpers |
| `nextjs-page` | Next.js page, loading/error boundaries, types |
| `docker-compose-dev` | dev docker-compose.yml (Postgres 16 + Redis 7) |
| `vps-deploy` | Nginx, Dockerfiles, GH Actions CI/CD, deploy scripts |
| `project-reference` | NuGet packages, JWT auth, Docker/deploy, EF conventions, connection strings |

## DB conventions
- EF Core Code First, Npgsql, `EFCore.NamingConventions` (snake_case)
- GUID PKs with `ValueGeneratedNever()`
- String props: `HasMaxLength()` required
- Enum props: `HasConversion<string>()` required
- Migration cmd: `dotnet ef migrations add Add{Name}Entity -- --provider Npgsql`
- Connection strings: `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=postgres`

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

## Development commands
```powershell
# Build
dotnet build

# Run API (HTTP:5026, HTTPS:7214)
dotnet run --project CollegeLMS.API

# Add EF migration (note --provider Npgsql)
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql

# Apply migration
dotnet ef database update --project CollegeLMS.API

# Docker infra (dev)
docker compose up -d
```

No frontend project exists in this repo yet — scaffold in `frontend/` or root as needed. No `.sln` file — use the `.csproj` directly.

## Code conventions
- Primary constructor DI (`class Service(AppDbContext db)`)
- `CancellationToken ct` on all async methods
- `AsNoTracking()` on read queries, `FindAsync()` for PK lookups
- Error messages in Russian, Swagger summaries in Russian
- Git prefixes: `feature:` / `fix:`
- Prefer `List<T>` over `IEnumerable<T>`
- Flat DTOs with default values, file-scoped namespaces

## Agent workflow
1. Read `task.md` for feature requirements first
2. Load the relevant skill before generating code: `skill("{name}")` (e.g., `skill("dotnet-entity")`, `skill("project-reference")`)
3. Always create/generate EF Core migrations after entity changes
4. Verify with `dotnet build` and lint if available
5. Work in feature branches, merge to master after passing tests

## What's NOT in the repo yet (scaffold stage)
- EF Core DbContext, migrations, models
- Controllers, services, DTOs
- Authentication (JWT)
- Frontend (Next.js project)
- Docker files, Nginx config
- CI/CD workflows
- Tests project
