# CollegeLMS — Agent instructions

## Stack
- Backend: .NET 10, ASP.NET Core Web API (single project, no .sln)
- Frontend: Next.js 14, TS, Tailwind CSS 4 (in `frontend/`)
- DB: PostgreSQL 16
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

## Superpowers Rule
**Invoke relevant or requested skills BEFORE any response or action** — including clarifying questions, exploring the codebase, or checking files. If a skill exists for the task, load it first.

| Situation | Skill | When |
|-----------|-------|------|
| Creative work, new feature | `brainstorming` | Before any code or exploring |
| Bug, test failure, unexpected behavior | `systematic-debugging` | Before proposing any fix |
| Implementation task | `writing-plans` → `executing-plans` | After spec is approved |
| TDD | `test-driven-development` | Write test before code |
| About to claim "done" | `verification-before-completion` | Before commit or success claim |
| Parallel independent work | `dispatching-parallel-agents` | 2+ independent sub-tasks |

## MCP Servers (opencode.json)

| Server | Purpose | Enabled |
|--------|---------|---------|
| `postgres` | Read-only DB queries, data inspection | yes |
| `playwright` | Visual debugging, E2E tests, DOM inspection, screenshots | yes |
| `github` | PRs, issues, checks, branches, repo management | yes |

## Plugin (Superpowers)
Installed: `superpowers@git+https://github.com/obra/superpowers.git`
Location: `~/.cache/opencode/packages/superpowers/.../`
Provides: process skills (brainstorming, systematic-debugging, verification-before-completion, etc.)

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
  components/ui/         # shadcn/ui primitives
  components/            # Project-specific composed components
  lib/utils.ts           # cn() helper
import/                  # WordPress import data
scripts/                 # WP data scraper
```

## Agent Roles

| Role | Type | Skills | Responsibility |
|------|------|--------|---------------|
| **Architect** | Main agent | brainstorming, writing-plans, verification-before-completion, requesting-code-review, yeet | Orchestrates workflow, reads task.md, decomposes into User Stories, creates branches, reviews, merges |
| **BackendAgent** | Subagent | dotnet-entity, dotnet-endpoint, result-pattern, fluent-validation, swagger-docs, aspnet-core | Entity → migration → service → controller → DI → Swagger → Postman |
| **TesterAgent** | Subagent | dotnet-test, playwright, playwright-interactive, test-driven-development, systematic-debugging | Unit tests (xUnit + Moq + Bogus), integration tests (WebApplicationFactory), E2E (Playwright), coverage |
| **FrontendAgent** | Subagent | nextjs-page, design-system, layers-orient, layers-user-needs, layers-conceptual-model, layers-interaction-flow, layers-surface, refactor-ui | Next.js pages/components, API integration, Tailwind, shadcn/ui |
| **AnalystAgent** | Subagent | plantuml-docs, security-threat-model | PlantUML diagrams (ER, Class, Sequence, UseCase, Deployment), tech documentation, threat modeling |
| **DevOpsAgent** | Subagent | docker-compose-dev, vps-deploy, cicd-pipeline, gh-fix-ci, sentry | Docker, nginx, CI/CD pipelines, deploy to VPS, error monitoring |

## Skills

### Project Skills (.opencode/skills/)

| Skill | What it creates |
|-------|----------------|
| `dotnet-entity` | Entity class, EF config, enum, migration |
| `dotnet-endpoint` | Controller, Service, DTO, mapper, DI registration |
| `result-pattern` | Result<T>, ApiResult, ExceptionHandlerMiddleware |
| `jwt-auth` | TokenService, BCrypt, Swagger bearer, Claims helpers |
| `dotnet-test` | Unit tests (xUnit + Moq + Bogus), integration tests (WebApplicationFactory) |
| `fluent-validation` | FluentValidation validators + DI registration |
| `nextjs-page` | Next.js page, loading/error boundaries, types |
| `design-system` | shadcn/ui + Tailwind v4 + Lucide — production UI components |
| `impeccable` | UI review, polish, responsive, accessibility, interaction design |
| `docker-compose-dev` | dev docker-compose.yml (Postgres 16 + Redis 7) |
| `vps-deploy` | Nginx, Dockerfiles, GH Actions CI/CD, deploy scripts |
| `cicd-pipeline` | GitHub Actions: test.yml + quality gates |
| `swagger-docs` | Swagger XML comments, Examples, ProducesResponseType, Postman spec |
| `plantuml-docs` | UML diagrams: ER, Class, Sequence, UseCase, Deployment |
| `cors-security` | CORS + security headers |
| `seed-data` | EF Core HasData + Seed classes |
| `feature-workflow` | Full vertical-slice feature orchestration — branch, entity, API, tests, frontend, docs, merge |
| `project-reference` | Project-wide reference: NuGet packages, JWT auth, Docker, EF Core conventions |

### Agent Skills (.agents/skills/)

| Skill | What it does |
|-------|-------------|
| `layers-orient` | Audits which design layer is the bottleneck |
| `layers-user-needs` | Turns interviews into prioritised job stories |
| `layers-conceptual-model` | Maps objects, relationships, vocabulary for a feature |
| `layers-interaction-flow` | Surfaces edge cases, empty states, flows |
| `layers-surface` | Visual design decisions — layout, states, responsive |
| `refactor-ui` | Full design pass — hierarchy, typography, color, spacing, buttons, clutter, empty states, shadows, contrast, grouping |

### Marketplace Skills (~/.codex/skills/)

| Skill | What it does |
|-------|-------------|
| `aspnet-core` | ASP.NET Core best practices, patterns, security, testing |
| `playwright` | Playwright E2E testing patterns and best practices |
| `playwright-interactive` | Interactive Playwright debugging and exploration |
| `sentry` | Error monitoring and issue tracking |
| `security-best-practices` | Security patterns and vulnerability prevention |
| `security-threat-model` | Threat modeling and attack path analysis |
| `gh-fix-ci` | Fix GitHub Actions CI failures |
| `gh-address-comments` | Address PR review comments |
| `yeet` | Git operations and branch management |

### Superpowers Process Skills (~/.cache/opencode/.../superpowers/)

| Skill | Purpose | When to use |
|-------|---------|-------------|
| `brainstorming` | Design exploration before creative work | New features, UI design, architecture |
| `systematic-debugging` | Root cause investigation before fixes | Any bug, test failure, unexpected behavior |
| `test-driven-development` | Red-Green-Refactor cycle | When writing tests before code |
| `writing-plans` | Generate implementation plans from spec | After spec is approved |
| `executing-plans` | Execute plans with review checkpoints | After writing-plans |
| `subagent-driven-development` | Dispatch subagents per task | Complex multi-step features |
| `finishing-a-development-branch` | Verify merge/PR readiness | Before completing feature branch |
| `using-git-worktrees` | Isolated workspaces | Starting parallel feature branches |
| `verification-before-completion` | Fresh verification before any "done" claim | Before every commit or merge |
| `writing-skills` | Create/edit process documentation | Maintaining skills |
| `requesting-code-review` | Dispatch reviewer subagent | Before merging PR |
| `receiving-code-review` | Handle review feedback with rigor | When PR has comments |
| `dispatching-parallel-agents` | Independent subagents in parallel | 2+ independent sub-tasks |

## Workflow Protocol

### Mandatory Process Skills Per Phase

| Phase | Load Skills | Verification Gate |
|-------|------------|-------------------|
| **0: Planning** | brainstorming → writing-plans | Spec reviewed by user |
| **1: Backend** | dotnet-entity, dotnet-endpoint, fluent-validation, swagger-docs, aspnet-core | G1: dotnet build |
| **2: Tests** | dotnet-test, test-driven-development | G2: dotnet test |
| **3: Frontend** | layers-orient, layers-user-needs, layers-conceptual-model, layers-interaction-flow, layers-surface, design-system, refactor-ui, nextjs-page | G3: npm run dev |
| **4: E2E** | playwright, playwright-interactive | G4: npx playwright test |
| **5: Docs** | plantuml-docs, security-threat-model | Visual review |
| **6: DevOps** | docker-compose-dev, vps-deploy, cicd-pipeline | G5: docker compose build |
| **7: Review** | verification-before-completion, requesting-code-review, yeet | Merge + push |

### Full Feature Cycle (vertical slice)

```
Phase 0: PLANNING (Architect)
  Load: brainstorming
  Read task.md, understand requirements
  Decompose into User Stories (see template below)
  Store task.md and User Stories in docs/spec/
  Propose approaches → user approves design
  Write design spec → docs/spec/{feature}-design.md
  Load: writing-plans
  Generate implementation plan
  git checkout -b feature/{service}-{feature}

Phase 1: BACKEND (BackendAgent)
  Load: dotnet-entity, dotnet-endpoint, fluent-validation, swagger-docs, aspnet-core
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
  ⚠️ verification-before-completion: dotnet build MUST PASS (Gate G1)
  If fail → fix and re-run
  git add -A && git commit -m "phase 1: {feature} backend"

Phase 2: TESTS (TesterAgent)
  Load: dotnet-test, test-driven-development
  skill("dotnet-test")      → test class for new endpoint
  ⚠️ verification-before-completion: dotnet test MUST PASS (Gate G2)
  If fail → return to Phase 1
  git add -A && git commit -m "phase 2: {feature} tests"

Phase 3: FRONTEND + UX (FrontendAgent)
  --- Runs in parallel with Phases 1+2 if backend contract is stable ---
  Load: layers-orient, layers-user-needs, layers-conceptual-model, layers-interaction-flow, layers-surface, design-system, refactor-ui, nextjs-page

  Step 1 — UX: problem space
    skill("layers-orient")            → аудит: на каком слое узкое место
    skill("layers-user-needs")        → job stories из требований
    skill("layers-conceptual-model")  → объекты, отношения, терминология фичи

  Step 2 — UX: interaction flows
    skill("layers-interaction-flow")  → edge cases, empty states, состояния
    • Какие UI состояния нужны: loading, empty, error, success
    • Нарисовать user flow (ключевой сценарий для User Story):
      Шаги: входные данные → действия → состояния

  Step 3 — UX: information architecture & wireframes
    • Структура страницы: навигация, секции, хлебные крошки
    • Расположение элементов: где форма, где таблица, где действия
    • Опционально: быстрый ASCII wireframe

  Step 4 — UI: implement
    skill("layers-surface")   → visual design decisions
    skill("design-system")    → shadcn/ui компоненты, токены, паттерны
    skill("nextjs-page")      → page.tsx + loading.tsx + error.tsx
    API integration           → fetch with types

  Step 5 — UI: polish
    skill("refactor-ui")      → полный design pass: hierarchy, typography, color, spacing, buttons, clutter, empty states, shadows, contrast, grouping

  npm run dev               → MUST WORK (Gate G3)
  Playwright MCP            → visually verify page renders
  If API contract changed   → notify Architect to re-sync
  ⚠️ verification-before-completion: npm run dev, page renders
  git add -A && git commit -m "phase 3: {feature} frontend"

Phase 4: E2E (TesterAgent)
  Load: playwright, playwright-interactive
  Playwright E2E tests      → user flow tests
  Playwright MCP            → debug failed tests visually, capture screenshots, inspect DOM
  User Story verification   → acceptance criteria check
  ⚠️ verification-before-completion: npx playwright test MUST PASS (Gate G4)
  If fail → determine root cause:
    • Backend bug     → return to Phase 1
    • Frontend bug    → return to Phase 3
    • Test bug        → fix in Phase 4
  git add -A && git commit -m "phase 4: {feature} e2e"

Phase 5: DOCS (AnalystAgent)
  Load: plantuml-docs, security-threat-model
  PlantUML diagrams         → ER (entities), Class (services), Sequence (flows)
  Security threat model     → trust boundaries, attack paths (если нужно)
  skill("plantuml-docs")
  git add -A && git commit -m "phase 5: {feature} docs"

Phase 6: DEVOPS (DevOpsAgent)
  Load: docker-compose-dev, vps-deploy, cicd-pipeline
  Docker check              → docker compose build
  CI/CD check               → .github/workflows updated
  Local staging test        → Gate G5
  If Docker/CI fails        → fix and re-run Phase 6
  ⚠️ verification-before-completion: docker compose build passes
  git add -A && git commit -m "phase 6: {feature} devops"

Phase 7: REVIEW & MERGE (Architect)
  Load: verification-before-completion, requesting-code-review, yeet
  ⚠️ verification-before-completion: verify ALL gates G1-G5 passed
  Load: requesting-code-review
  Code review               → peer review of all changes
  If review comments        → fix, re-run gates
  Load: yeet
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
- [ ] Security threat model reviewed (если нужно)
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
- Connection: `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root`
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

## Development workflow

### Infrastructure (всегда запущено)
PostgreSQL + Redis работают в Docker на время любой разработки:
```powershell
docker compose up -d db redis     # стартовать один раз в начале сессии
docker compose down               # остановить
```

### Локальная разработка (Phases 1-5)
Бекенд и фронтенд запускаются локально для скорости и hot-reload:
```powershell
dotnet run --project CollegeLMS.API          # API на localhost:5026
npm run dev                                   # frontend на localhost:3000
```
API подключается к PostgreSQL через `localhost:5432` (проброшен Docker-контейнером).
База данных и Redis уже работают в Docker — не нужно запускать их заново.

### Контейнерный запуск (Phase 6 DevOps)
Только для проверки, что всё работает в контейнерах:
```powershell
docker compose up --build -d     # полный стек: db + redis + api + frontend + nginx
```

### Прочие команды
```powershell
dotnet build                 # builds all projects in CollegeLMS.slnx
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql
dotnet ef database update --project CollegeLMS.API
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

## Frontend conventions (shadcn/ui + Tailwind CSS v4)

- **Design tokens**: CSS custom properties in HSL (colors.css, typography.css, spacing.css)
- **Components**: `components/ui/` — shadcn primitives (не редактировать), `components/` — проектные
- **Icons**: Lucide React. See DESIGN.md §6 for feature→icon map, sizes, color rules (`currentColor` by default, College Blue on hover, semantic colors for status), and a11y (aria-label for icon-only buttons, aria-hidden when adjacent text exists)
- **Responsive**: mobile-first, breakpoints `sm:`, `md:`, `lg:`
- **Touch targets**: минимум 44×44px для interactive elements
- **Accessibility**: семантический HTML, ARIA, keyboard nav, `focus-visible:ring-2`, `sr-only`
- **Forms**: labels всегда видны, ошибки под полем, loading state на submit
- **Container**: `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8`

## Agent Timeline Logging

For tracking how much time is spent per task, the agent writes structured events to `agent-timeline.jsonl` at the project root.

### Protocol

| Event | Args | When |
|-------|------|------|
| `session_start` | — | At the very start of work |
| `task_start` | `id name` | Before starting work on a todo item |
| `tool_call` | `tool_name` | Before making any tool call (bash/read/write etc.) |
| `tool_result` | `tool_name` | After receiving the tool's result |
| `task_end` | `id` | After completing a todo item |
| `session_end` | — | Before the user runs `/time` |

### Usage

```powershell
scripts/timeline.ps1 task_start 1 "Create endpoint"
scripts/timeline.ps1 tool_call dotnet
scripts/timeline.ps1 tool_result dotnet
scripts/timeline.ps1 task_end 1
```

At the end of a session, run one of these to see the report:
```powershell
!scripts/time-report.ps1    # in TUI (works always)
```
Or if custom commands are supported:
```
/time                        # custom command (may need restart)
```

```
Task Timeline Report
====================
 # | Task             | Wall    | Think   | Waiting | Tools
 1 | Create endpoint  | 10.0s   | 3.0s    | 7.0s    | dotnet(3s), bash(4s)
---------------------------------------------------------------
   | Total            | 22.0s   | 8.0s    | 14.0s   |
```

- **Wall** = real time from task start to end
- **Think** = time the model spent generating (wall minus tool execution)
- **Waiting** = cumulative time spent waiting for tool results
- **Tools** = per-tool breakdown

### Auto-report after each task

После завершения каждой задачи (`task_end`), агент **обязан** выполнить:

```powershell
!scripts/time-report.ps1
```

и показать таблицу отчёта в своём ответе пользователю. Это делается для того, чтобы пользователь видел актуальную статистику времени после каждого шага.

### Reset for new session

Before starting a new session, remove the old log:

```powershell
Remove-Item -Force agent-timeline.jsonl -ErrorAction SilentlyContinue
```

## Reference repository

- База заметок по .NET — https://github.com/artemovsergey/.NET/wiki
- Репозиторий по организации работы с брокером сообщений — https://github.com/artemovsergey/ModuleBankApp
- Заметки по микросервисной архитектуре — https://github.com/artemovsergey/SampleApp/wiki
- Репозиторий для примера кода MinimalAPI — https://github.com/artemovsergey/TicTacToe
- Материалы по UML — https://github.com/artemovsergey/UML
- Репозитории с примерами кода — https://github.com/artemovsergey/VendingAppFinal, https://github.com/artemovsergey/ProfApp
- Конфигурации для развертывания — https://github.com/artemovsergey/VPS
- Технический журнал React Native — https://github.com/artemovsergey/ReactNative

**Замечание**: при нахождении лучшего решения, чем в примерах обязательно спросить
