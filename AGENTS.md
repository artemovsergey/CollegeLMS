# CollegeLMS — Agent instructions

## Stack
- Backend: .NET 10, ASP.NET Core Web API (single project, no .sln)
- Frontend: Next.js 14, TS, Tailwind CSS 4 (in `frontend/`)
- DB: PostgreSQL 16, 
- Cache: Redis (sessions only)
- Deploy: Docker Compose, GitHub Actions CI/CD
- Files: local FS (MinIO later)

## Architecture
- Monolith, Clean Architecture (folders, not projects)
- REST API, JSON, JWT auth (no refresh tokens in MVP)
- `Result<T>` pattern everywhere — no try-catch in controllers/services
- `ExceptionHandlerMiddleware` catches unexpected exceptions
- Manual mappers (no AutoMapper), FluentValidation, Swashbuckle
- All data in Russian, code/comments in English

## Project structure
```
CollegeLMS.slnx            # Solution file — all projects included
CollegeLMS.API/
  Program.cs             # Minimal bootstrap — everything delegated to extension methods
  Controllers/           # ControllerBase subclasses
  Services/              # {Name}Service implementations
  Interfaces/            # I{Name}Service interfaces
  Mappers/               # Static extension mappers (Entity ↔ DTO)
  Entities/              # Domain classes (inherit from Entity base)
  Entities/Enums/        # Enum types
  Data/                  # AppDbContext
  Data/Configurations/   # IEntityTypeConfiguration<T> (with HasData + raw SQL)
  Dtos/                  # Request/Response DTOs
  Validators/            # FluentValidation validators
  SwaggerExamples/       # Response example classes for Swagger docs
  Middleware/             # ExceptionHandlerMiddleware
  Response/              # Result<T>, ApiResult<T>, ErrorResponse
  Exceptions/            # NotFoundException, ValidationException, ForbiddenException
  Extensions/            # ServiceCollectionExtensions, ApplicationBuilderExtensions, ClaimsPrincipalExtensions
docs/spec/               # Postman collection, task.md, userstories.md
CollegeLMS.Tests/
  Integration/           # WebApplicationFactory tests
  Integration/Controllers/
  Unit/Services/
  Fixtures/              # Bogus fixtures
frontend/                # Next.js 14 + Tailwind CSS 4 + TypeScript
import/                  # WordPress import data
scripts/                 # WP data scraper
```

## Agent Roles

| Role | Type | Skills | Responsibility |
|------|------|--------|---------------|
| **Architect** | Main agent | — | Orchestrates workflow, reads task.md, decomposes into User Stories, creates branches, reviews, merges |
| **BackendAgent** | Subagent | dotnet-entity, dotnet-endpoint, result-pattern, fluent-validation, swagger-docs | Entity → migration → service → controller → DI → Swagger → Postman |
| **TesterAgent** | Subagent | dotnet-test | Unit tests (xUnit + Moq + Bogus), integration tests (WebApplicationFactory), E2E (Playwright), coverage |
| **FrontendAgent** | Subagent | nextjs-page, frontend-scaffold | Next.js pages/components, API integration, Tailwind, loading/error states |
| **AnalystAgent** | Subagent | plantuml-docs | PlantUML diagrams (ER, Class, Sequence, UseCase, Deployment), tech documentation, architecture docs |
| **DevOpsAgent** | Subagent | docker-compose-dev, vps-deploy, cicd-pipeline | Docker, nginx, CI/CD pipelines, deploy to VPS |

## Skills

| Skill | What it creates |
|-------|----------------|
| `project-bootstrap` | Full project setup: NuGet, Program.cs, DbContext, middleware |
| `dotnet-entity` | Entity class, EF config, enum, migration |
| `dotnet-endpoint` | Controller, Service, DTO, mapper, DI registration |
| `result-pattern` | Result<T>, ApiResult, ExceptionHandlerMiddleware |
| `jwt-auth` | TokenService, BCrypt, Swagger bearer, Claims helpers |
| `dotnet-test` | Unit tests (xUnit + Moq + Bogus), integration tests (WebApplicationFactory) |
| `testing-xunit` | Full test project scaffold: xUnit, WebApplicationFactory, Bogus fixtures, Controller/Service tests |
| `fluent-validation` | FluentValidation validators + DI registration |
| `nextjs-page` | Next.js page, loading/error boundaries, types |
| `frontend-scaffold` | Next.js project scaffold with Tailwind CSS v4 |
| `docker-compose-dev` | dev docker-compose.yml (Postgres 16 + Redis 7) |
| `vps-deploy` | Nginx, Dockerfiles, GH Actions CI/CD, deploy scripts |
| `cicd-pipeline` | GitHub Actions: test.yml + quality gates |
| `swagger-docs` | Swagger XML comments, Examples, ProducesResponseType, Postman spec |
| `plantuml-docs` | UML diagrams: ER, Class, Sequence, UseCase, Deployment |
| `cors-security` | CORS + security headers |
| `seed-data` | EF Core HasData + Seed classes |
| `redis-session` | StackExchangeRedis + IDistributedCache setup |
| `feature-workflow` | Full vertical-slice feature orchestration — branch, entity, API, tests, frontend, docs, merge |
| `project-reference` | Project-wide reference: NuGet packages, JWT auth, Docker, EF Core conventions |

## Workflow Protocol

### Full Feature Cycle (vertical slice)

```
Phase 0: PLANNING (Architect)
  Decompose task into User Stories (see template below)
  Store task.md and User Stories in docs/spec/
  git checkout -b feature/{service}-{feature}
  Load skills: read skill files before generating code

Phase 1: BACKEND (BackendAgent)
  skill("dotnet-entity")    → Entities/{Name}.cs (inherits from Entity), Data/Configurations/{Name}Configuration.cs, migration
    • Entity class extends `Entity` base (Id, CreatedAt, UpdatedAt)
    • Enum (if needed) in Entities/Enums/
    • EF Config: ToTable, HasKey, ValueGeneratedNever, HasMaxLength, HasConversion<string>
    • Indexes: HasIndex with custom names (EF manages these via migrations)
    • HasData for test/seed data
    • DbContext auto-discovers via ApplyConfigurationsFromAssembly
    • CHECK constraints: NOT in EF Config → add to Data/DbConstraints.cs (идемпотентный SQL)
  dotnet ef migrations add  → creates SQL migration
  skill("dotnet-endpoint")  → DTOs, Mapper, Interface, Service, Controller, DI registration
    • Dtos/{Name}Request.cs + {Name}Response.cs
    • Mappers/{Name}Mapper.cs (root Mappers/ folder)
    • Interfaces/I{Name}Service.cs (root Interfaces/ folder)
    • Services/{Name}Service.cs (primary constructor, Result<T>, AsNoTracking, FindAsync)
    • Controllers/{Name}Controller.cs (CRUD, SwaggerOperation, SwaggerResponse)
    • DI: add to Extensions/ServiceCollectionExtensions.cs (NOT Program.cs)
  skill("fluent-validation")
    • Validators/{Name}RequestValidator.cs (NotEmpty, MaxLength, messages in Russian)
    • DI: AddValidatorsFromAssemblyContaining<Program>() in ServiceCollectionExtensions
  skill("swagger-docs")     → SwaggerExamples/{Name}ResponseExample.cs + update Postman
    • XML comments on Controller: <summary>, <remarks>, <response code="...">
    • [ProducesResponseType(typeof(...), StatusCodes.Status...)] для всех статусов
    • [SwaggerResponse(code, "...", typeof(...))] + ErrorResponseExample для ошибок
    • SwaggerExamples/ErrorResponseExample.cs для общих ошибок
    • SwaggerExamples/{Name}ResponseExample.cs для успешного ответа
    • docs/spec/CollegeLMS.postman_collection.json — добавить endpoint в коллекцию
  dotnet build              → MUST PASS (Gate G1)
  If fail → fix and re-run
  git add -A && git commit -m "phase 1: {feature} backend"

Phase 2: TESTS (TesterAgent)
  skill("dotnet-test")      → test class for new endpoint
  dotnet test               → MUST PASS (Gate G2)
  If fail → return to Phase 1
  git add -A && git commit -m "phase 2: {feature} tests"

Phase 3: FRONTEND (FrontendAgent)
  --- Runs in parallel with Phases 1+2 if backend contract is stable ---
  skill("nextjs-page")      → page.tsx + loading.tsx + error.tsx
  API integration           → fetch with types
  npm run dev               → MUST WORK (Gate G3)
  Playwright MCP            → visually verify page renders (screenshots, clicks, form fills)
  If API contract changed  → notify Architect to re-sync
  git add -A && git commit -m "phase 3: {feature} frontend"

Phase 4: E2E (TesterAgent)
  Playwright E2E tests      → user flow tests
  Playwright MCP            → debug failed tests visually, capture screenshots, inspect DOM
  User Story verification   → acceptance criteria check
  npx playwright test       → MUST PASS (Gate G4)
  If fail → determine root cause:
    • Backend bug     → return to Phase 1
    • Frontend bug    → return to Phase 3
    • Test bug        → fix in Phase 4
  git add -A && git commit -m "phase 4: {feature} e2e"

Phase 5: DOCS (AnalystAgent)
  PlantUML diagrams         → ER (entities), Class (services), Sequence (flows)
  skill("plantuml-docs")
  git add -A && git commit -m "phase 5: {feature} docs"

Phase 6: DEVOPS (DevOpsAgent)
  Docker check              → docker compose build
  CI/CD check               → .github/workflows updated
  Local staging test        → Gate G5
  If Docker/CI fails        → fix and re-run Phase 6
  git add -A && git commit -m "phase 6: {feature} devops"

Phase 7: REVIEW & MERGE (Architect)
  Code review               → peer review of all changes
  All Gates G1-G5 verified  → check each gate has passed
  git checkout master && git merge feature/{service}-{feature}
  git push                  → CI/CD runs pipeline, deploys to VPS
  If merge conflict         → resolve in feature branch, re-run gates
```

### Handoff Format (Architect → Subagent)

```json
{
  "task": "Create Schedule entity with CRUD endpoint",
  "userStory": "UC-2: Преподаватель просматривает расписание группы",
  "branch": "feature/schedule-view",
  "assignedTo": "BackendAgent",
  "acceptanceCriteria": [
    "GET /api/schedule?groupId={id} returns List<ScheduleResponse>",
    "POST /api/schedule creates new entry (диспетчер only)",
    "DTOs have [Required] + [MaxLength] validation",
    "Swagger annotated with Russian summaries"
  ],
  "skills": ["dotnet-entity", "dotnet-endpoint"],
  "dependsOn": ["AuthService", "GroupService"]
}
```

### User Story Template

```markdown
## UC-N: {Role} может {действие}

**Acceptance Criteria:**
- [ ] {критерий 1}
- [ ] {критерий 2}

**API:**
- `{method} /api/{route}` — {описание}

**UI (если есть):**
- Страница `/{route}` с таблицей/формой
- Ошибки показаны пользователю (тост/сообщение)

**Dependencies:** {сервисы, от которых зависит}
```

### Acceptance Gates

| Gate | Check | Who | Phase |
|------|-------|-----|-------|
| **G1** | `dotnet build` | BackendAgent | Phase 1 |
| **G2** | `dotnet test` — all green | TesterAgent | Phase 2 |
| **G3** | `npm run dev` — page renders | FrontendAgent | Phase 3 |
| **G4** | Playwright — all User Stories pass | TesterAgent | Phase 4 |
| **G5** | `docker compose` — all services start | DevOpsAgent | Phase 6 |

### Definition of Done

- [ ] dotnet build passes
- [ ] dotnet test passes (coverage ≥ 50%)
- [ ] npm run dev works (frontend project)
- [ ] Playwright E2E tests pass
- [ ] Swagger UI shows endpoint with Russian docs
- [ ] SwaggerExamples created for all error/success responses
- [ ] Postman collection updated in docs/spec/
- [ ] PlantUML diagrams generated — ER, Class, Sequence for the feature
- [ ] CI/CD pipeline passes
- [ ] Feature branch merged to master
- [ ] VPS deploy verified

## DB conventions
- EF Core Code First, Npgsql, `EFCore.NamingConventions` (snake_case)
- GUID PKs with `ValueGeneratedNever()`
- String props: `HasMaxLength()` required
- Enum props: `HasConversion<string>()` + `HasMaxLength()` required
- Navigation properties: `[JsonIgnore]`
- Timestamps: `CreatedAt`, `UpdatedAt` = `DateTime.UtcNow`
- Connection: `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=postgres`
- Indexes (UNIQUE, plain) — в EF Configuration (`HasIndex` c `HasDatabaseName`)
- CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)

## NuGet packages

Packages in `.csproj` grouped into separate `ItemGroup` sections with comments by category:

| Category | Packages |
|----------|----------|
| EF Core & Database | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `EFCore.NamingConventions`, `AspNetCore.HealthChecks.NpgSql` |
| Auth & Security | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Validation | `FluentValidation.DependencyInjectionExtensions` |
| Swagger | `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`, `Microsoft.OpenApi` |
| Logging | `Serilog.AspNetCore` |
| Tests | `xunit`, `coverlet.msbuild`, `Bogus`, `Moq`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` |

## Development commands
```powershell
dotnet build                 # builds all projects in CollegeLMS.slnx
dotnet run --project CollegeLMS.API
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql
dotnet ef database update --project CollegeLMS.API
docker compose up -d
dotnet test                  # runs all test projects in solution
dotnet test /p:CollectCoverage=true /p:CoverletOutput=TestResults/
dotnet csharpier format .    # format all C# files
dotnet csharpier check .     # check formatting (CI)
```

## Code conventions
- Primary constructor DI (`class Service(AppDbContext db)`)
- `CancellationToken ct` on all async methods
- `AsNoTracking()` on read queries, `FindAsync()` for PK lookups
- Error messages in Russian, Swagger summaries in Russian
- Git prefixes: `feature:` / `fix:` / `docs:` / `test:`
- `git add -A` to stage all changes (never list files individually)
- Prefer `List<T>` over `IEnumerable<T>`
- Flat DTOs with default values, file-scoped namespaces
- `Result<T>.Ok()` for success, `Result<T>.Fail()` for errors
- OpenApi namespace: `using Microsoft.OpenApi;`
- Formatting: CSharpier (`dotnet csharpier .` to format, `dotnet csharpier . --check` in CI)
- All entities inherit from `Entities/Entity` base class (Guid Id, CreatedAt, UpdatedAt)
- Mappers in root `Mappers/` folder, service interfaces in root `Interfaces/` folder
- `Program.cs` is minimal — all `builder.Services.Add*` in `Extensions/ServiceCollectionExtensions.cs`, all `app.Use*` in `Extensions/ApplicationBuilderExtensions.cs`
- EF Configurations include `HasData()` for seed data and `HasIndex` with custom names
- CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)

## PlantUML conventions
- ER diagrams: `/docs/diagrams/er/{entity}.puml`
- Sequence diagrams: `/docs/diagrams/sequence/{flow}.puml`
- Class diagrams: `/docs/diagrams/class/{service}.puml`
- Use PlantUML online: `https://www.plantuml.com/plantuml/uml/{encoded}`
- Or local: `java -jar plantuml.jar docs/diagrams/**/*.puml`

## Reference repository

- База заметок по .NET - https://github.com/artemovsergey/.NET/wiki
- Репозиторий по огранизации работы с брокером сообщений - https://github.com/artemovsergey/ModuleBankApp
- Заметки по микросервисной архитектуре - https://github.com/artemovsergey/SampleApp/wiki
- Репозиторий для примера кода MinimalAPI - https://github.com/artemovsergey/TicTacToe
- Материалы по UML - https://github.com/artemovsergey/UML
- Репозитории с примерами кода - https://github.com/artemovsergey/VendingAppFinal, https://github.com/artemovsergey/ProfApp
- Конфигурации для развертывания - https://github.com/artemovsergey/VPS
- Технический журнал React Native - https://github.com/artemovsergey/ReactNative

**Замечание**: при нахождении лучшего решения, чем в примерах обязательно спросить