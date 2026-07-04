---
name: feature-workflow
description: Orchestrate full vertical-slice feature development — branch, entity, API, tests, frontend, docs, deploy. Use when starting a new feature in CollegeLMS
---

# feature-workflow

Orchestrate complete vertical-slice feature development in CollegeLMS. Each phase has a checklist with exact commands and verification criteria.

## Pipeline Overview

```
Phase 0: Git Branch         git checkout -b feature/{name}
Phase 1: Entity Layer       dotnet-entity -> migration -> dotnet build
Phase 2: API Layer          dotnet-endpoint -> DTOs/Service/Controller -> Swagger
Phase 3: Tests              dotnet-test -> unit + integration -> dotnet test
Phase 4: Frontend           nextjs-page -> pages/components -> npm run dev
Phase 5: Documentation      plantuml-docs -> UML diagrams
Phase 6: Integration        docker compose + manual testing
Phase 7: Merge + Deploy     git merge master -> CI/CD -> VPS
```

## Phase 0: Git Branch

```bash
git checkout master
git pull origin master
git checkout -b feature/{service-name}-{feature-name}
```

**Checklist:**
- [ ] Branch created from latest master
- [ ] Read `task.md` -- find User Story for this feature
- [ ] Identify: Entity, Service, Controller, Frontend pages needed

---

## Phase 1: Entity Layer

Load skill: `skill("dotnet-entity")`

```bash
# Create entity files:
# CollegeLMS.API/Entities/{Name}.cs
# CollegeLMS.API/Entities/Enums/{Name}Type.cs (if needed)
# CollegeLMS.API/Data/Configurations/{Name}Configuration.cs

# Register in DbContext:
# CollegeLMS.API/Data/AppDbContext.cs -> DbSet + ApplyConfiguration

# Create migration
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql

# Verify
dotnet build
```

**Checklist:**
- [ ] Entity has Guid PK, CreatedAt, UpdatedAt
- [ ] Configuration: HasMaxLength, HasConversion for enum
- [ ] Migration generated
- [ ] `dotnet build` passes

---

## Phase 2: API Layer

Load skill: `skill("dotnet-endpoint")`

```bash
# Create files:
# CollegeLMS.API/Dtos/{Action}{Name}Request.cs
# CollegeLMS.API/Dtos/{Name}Response.cs
# CollegeLMS.API/Services/Mappers/{Name}Mapper.cs
# CollegeLMS.API/Services/I{Name}Service.cs
# CollegeLMS.API/Services/{Name}Service.cs
# CollegeLMS.API/Controllers/{Name}Controller.cs

# Register in DI (Program.cs):
# builder.Services.AddScoped<I{Name}Service, {Name}Service>();

# Verify
dotnet build
dotnet run --project CollegeLMS.API
# Open http://localhost:5026/swagger -- check endpoints
```

**Checklist:**
- [ ] DTOs with validation attributes
- [ ] Mapper -- manual extension methods
- [ ] Service returns Result<T>
- [ ] Controller with [SwaggerOperation(Summary = "...")]
- [ ] DI registered
- [ ] `dotnet build` passes
- [ ] Swagger UI shows all endpoints

---

## Phase 3: Tests

Load skill: `skill("dotnet-test")`

```bash
# If test project doesn't exist yet:
dotnet new xunit -n CollegeLMS.Tests -o CollegeLMS.Tests
dotnet add CollegeLMS.Tests reference CollegeLMS.API
dotnet add CollegeLMS.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add CollegeLMS.Tests package Moq
dotnet add CollegeLMS.Tests package Bogus

# Create tests:
# CollegeLMS.Tests/Services/{Name}ServiceTests.cs
# CollegeLMS.Tests/Controllers/{Name}ControllerTests.cs

# Run tests
dotnet test CollegeLMS.Tests --verbosity normal
```

**Checklist:**
- [ ] Unit tests for each service method (happy + error case)
- [ ] Integration tests for each controller endpoint
- [ ] InMemory DB for isolation
- [ ] All tests green
- [ ] Coverage > 70% (optional)

---

## Phase 4: Frontend

Load skill: `skill("nextjs-page")`

```bash
cd frontend

# Create pages and components:
# app/{route}/page.tsx
# app/{route}/loading.tsx
# app/{route}/error.tsx
# components/{Name}*.tsx
# types/{name}.ts

# Verify
npm run dev
# Open http://localhost:3000 -- check UI
```

**Checklist:**
- [ ] Pages created (server/client components)
- [ ] Loading and error states
- [ ] TypeScript types for API responses
- [ ] API calls via NEXT_PUBLIC_API_URL
- [ ] Responsive design (Tailwind)
- [ ] `npm run dev` works
- [ ] User Story from task.md verified visually

---

## Phase 5: Documentation

Load skill: `skill("plantuml-docs")`

```bash
# Create diagrams:
# docs/diagrams/er/{entity}.puml
# docs/diagrams/class/{service}.puml
# docs/diagrams/sequence/{flow}.puml

# Verify Swagger
# http://localhost:5026/swagger -- all methods documented
```

**Checklist:**
- [ ] ER diagram for entity relationships (`docs/diagrams/er/`)
- [ ] Class diagram for service architecture (`docs/diagrams/class/`)
- [ ] Sequence diagram for main API flows (`docs/diagrams/sequence/`)
- [ ] Swagger summary in Russian

---

## Phase 6: Integration Test

```bash
# Start infrastructure (always rebuild)
docker compose up --build -d

# Apply migrations
dotnet ef database update --project CollegeLMS.API

# Start API
dotnet run --project CollegeLMS.API

# Start frontend
cd frontend && npm run dev

# Manual testing:
# 1. Open Swagger -> authorize -> call endpoints
# 2. Open Next.js -> check UI
# 3. Verify scenarios from task.md
```

**Checklist:**
- [ ] Docker Compose starts Postgres + Redis
- [ ] Migrations apply
- [ ] API works via Docker
- [ ] Frontend communicates with API
- [ ] Key User Stories work end-to-end

---

## Phase 7: Merge + Deploy

```bash
# Final check
dotnet build
dotnet test CollegeLMS.Tests

# Git
git add -A
git commit -m "feature: {service-name} -- {description}"

git checkout master
git merge feature/{service-name}-{feature-name}
git push origin master
```

**CI/CD (automatic):**
- Push to master -> GitHub Actions: `test.yml` -> `dotnet test`
- If tests pass -> deploy workflow available

**Manual deploy (if needed):**
- Deploy workflow was removed — not needed for MVP
- To deploy manually later: `docker compose up -d --build` on VPS

**Checklist:**
- [ ] `git add -A` stages all changes
- [ ] Commit with `feature:` prefix
- [ ] Merge to master
- [ ] CI tests pass
- [ ] Deploy to VPS (if applicable)

## Phase Gate Rules

- Phase N+1 starts **ONLY** after `dotnet build` / `dotnet test` passes in phase N
- `dotnet build` must pass before any tests
- `dotnet test` must pass before frontend
- Frontend `npm run dev` must work before merge
- Merge to master only after all checks pass
- Commit prefixes: `feature:`, `fix:`, `docs:`, `test:`

## Agent Roles

| Role | Type | Phases | Skills |
|------|------|--------|--------|
| **Backend Agent** | `general` | 0-2 | `dotnet-entity`, `dotnet-endpoint`, `result-pattern`, `fluent-validation` |
| **Test Agent** | `general` | 3 | `dotnet-test` |
| **Frontend Agent** | `general` | 4 | `nextjs-page` |
| **Docs Agent** | `general` | 5 | `plantuml-docs` |
| **DevOps Agent** | `general` | 6-7 | `cicd-pipeline` |

## Git Branch Strategy

- `master` -- stable code, always working
- `feature/{service}-{feature}` -- each feature branch
- Commit prefixes: `feature:`, `fix:`, `docs:`, `test:`
- Merge via fast-forward or merge commit (no squash)
