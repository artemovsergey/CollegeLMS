# CollegeLMS — Agent instructions

## Stack
- Backend: .NET 10, ASP.NET Core Web API (single project, no .sln)
- Frontend: Next.js 14, TS, Tailwind CSS 4 (in `frontend/`)
- DB: PostgreSQL 16, Cache: Redis (sessions only)
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
CollegeLMS.API/
  Program.cs             # Full bootstrap: EF, JWT, Swagger, Serilog, CORS, HealthChecks, Middleware
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
  Exceptions/            # NotFoundException, ValidationException, ForbiddenException
  Extensions/            # ClaimsPrincipalExtensions
CollegeLMS.Tests/
  Integration/           # WebApplicationFactory tests
  Integration/Controllers/
  Unit/Services/
  Fixtures/              # Bogus fixtures
frontend/                # Next.js project (TBD)
import/                  # WordPress import data
scripts/                 # WP data scraper
```

## Agent Roles

| Role | Type | Skills | Responsibility |
|------|------|--------|---------------|
| **Architect** | Main agent | — | Orchestrates workflow, reads task.md, decomposes into User Stories, creates branches, reviews, merges |
| **BackendAgent** | Subagent | dotnet-entity, dotnet-endpoint, result-pattern, fluent-validation | Entity → migration → service → controller → DI → Swagger |
| **TesterAgent** | Subagent | testing-xunit | Unit tests (xUnit + Moq + Bogus), integration tests (WebApplicationFactory), E2E (Playwright), coverage |
| **FrontendAgent** | Subagent | nextjs-page, frontend-scaffold | Next.js pages/components, API integration, Tailwind, loading/error states |
| **DevOpsAgent** | Subagent | docker-compose-dev, vps-deploy, cicd-pipeline | Docker, nginx, CI/CD pipelines, deploy to VPS |

## Skills

| Skill | What it creates |
|-------|----------------|
| `project-bootstrap` | Full project setup: NuGet, Program.cs, DbContext, middleware |
| `dotnet-entity` | Entity class, EF config, enum, migration |
| `dotnet-endpoint` | Controller, Service, DTO, mapper, DI registration |
| `result-pattern` | Result<T>, ApiResult, ExceptionHandlerMiddleware |
| `jwt-auth` | TokenService, BCrypt, Swagger bearer, Claims helpers |
| `testing-xunit` | Test project, WebApplicationFactory, Bogus fixtures |
| `fluent-validation` | FluentValidation validators + DI registration |
| `nextjs-page` | Next.js page, loading/error boundaries, types |
| `frontend-scaffold` | Next.js project scaffold with Tailwind CSS v4 |
| `docker-compose-dev` | dev docker-compose.yml (Postgres 16 + Redis 7) |
| `vps-deploy` | Nginx, Dockerfiles, GH Actions CI/CD, deploy scripts |
| `cicd-pipeline` | GitHub Actions: test.yml + deploy.yml + quality gates |
| `plantuml-docs` | UML diagrams: ER, Class, Sequence, UseCase, Deployment |
| `cors-security` | CORS + security headers |
| `seed-data` | EF Core HasData + Seed classes |
| `redis-session` | StackExchangeRedis + IDistributedCache setup |

## Workflow Protocol

### Full Feature Cycle (vertical slice)

```
Phase 0: BRANCH
  Architect: git checkout -b feature/{service}-{feature}
  Load skills: read skill files before generating code

Phase 1: BACKEND (BackendAgent)
  skill("dotnet-entity")    → Entities/{Name}.cs, Data/Configurations/{Name}Configuration.cs, migration
  dotnet ef migrations add  → creates SQL migration
  skill("dotnet-endpoint")  → DTOs, Mapper, Service, Controller, DI registration
  dotnet build              → MUST PASS (Gate G1)

Phase 2: TESTS (TesterAgent)
  skill("testing-xunit")    → test class for new endpoint
  dotnet test               → MUST PASS (Gate G2)
  If fail → return to Phase 1

Phase 3: FRONTEND (FrontendAgent)
  skill("nextjs-page")      → page.tsx + loading.tsx + error.tsx
  API integration           → fetch with types
  npm run dev               → MUST WORK (Gate G3)

Phase 4: E2E (TesterAgent)
  Playwright E2E tests      → user flow tests
  User Story verification   → acceptance criteria check
  npx playwright test       → MUST PASS (Gate G4)

Phase 5: DOCS (Architect)
  PlantUML diagrams         → ER (entities), Class (services), Sequence (flows)
  skill("plantuml-docs")

Phase 6: DEVOPS (DevOpsAgent)
  Docker check              → docker compose build
  CI/CD check               → .github/workflows updated
  Local staging test        → Gate G5

Phase 7: MERGE (Architect)
  Final review              → all Gates G1-G5 passed
  git merge master
  git push                  → CI/CD deploys to VPS
```

### Handoff Format (Architect → Subagent)

```json
{
  "task": "Create Schedule entity with CRUD endpoint",
  "userStory": "UC-2: Преподаватель просматривает расписание группы",
  "branch": "feature/schedule-view",
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
| **G5** | `docker compose build` | DevOpsAgent | Phase 6 |

### Definition of Done

- [ ] dotnet build passes
- [ ] dotnet test passes (coverage ≥ 60%)
- [ ] npm run dev works (frontend project)
- [ ] Playwright E2E tests pass
- [ ] Swagger UI shows endpoint with Russian docs
- [ ] PlantUML diagrams generated (ER + Sequence for the feature)
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

## NuGet packages

| Purpose | Package |
|---------|---------|
| ORM | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Migrations | `Microsoft.EntityFrameworkCore.Design` |
| Snake case | `EFCore.NamingConventions` |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Validation | `FluentValidation.DependencyInjectionExtensions` |
| Swagger | `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`, `Microsoft.OpenApi` |
| Logging | `Serilog.AspNetCore` |
| Healthchecks | `AspNetCore.HealthChecks.NpgSql` |
| Tests | `xunit`, `coverlet.msbuild`, `Bogus`, `Moq`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` |

## Development commands
```powershell
dotnet build
dotnet run --project CollegeLMS.API
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql
dotnet ef database update --project CollegeLMS.API
docker compose up -d
dotnet test CollegeLMS.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutput=TestResults/
```

## Code conventions
- Primary constructor DI (`class Service(AppDbContext db)`)
- `CancellationToken ct` on all async methods
- `AsNoTracking()` on read queries, `FindAsync()` for PK lookups
- Error messages in Russian, Swagger summaries in Russian
- Git prefixes: `feature:` / `fix:` / `docs:` / `test:`
- Prefer `List<T>` over `IEnumerable<T>`
- Flat DTOs with default values, file-scoped namespaces
- `Result<T>.Ok()` for success, `Result<T>.Fail()` for errors
- `OpenApi v3` namespace: `using Microsoft.OpenApi;` (NOT `Microsoft.OpenApi.Models`)

## PlantUML conventions
- ER diagrams: `/docs/diagrams/er/{entity}.puml`
- Sequence diagrams: `/docs/diagrams/sequence/{flow}.puml`
- Class diagrams: `/docs/diagrams/class/{service}.puml`
- Use PlantUML online: `https://www.plantuml.com/plantuml/uml/{encoded}`
- Or local: `java -jar plantuml.jar docs/diagrams/**/*.puml`
