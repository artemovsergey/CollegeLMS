# CollegeLMS — инструкции для агента

## Начало работы

- **Перед любой работой** (ответ, изучение кода, правки) обновить локальную копию:
  ```powershell
  git fetch origin && git pull --rebase origin master
  ```
- Пуш только при заданном `GITHUB_TOKEN`/`GH_TOKEN` (gh CLI v2.101+).

## Стек и архитектура

- Backend: .NET 10, ASP.NET Core Web API, PostgreSQL 16, EF Core Code First (Npgsql + `EFCore.NamingConventions`, snake_case)
- Frontend: Next.js 14 (App Router), TypeScript, Tailwind CSS 4, shadcn/ui — в `CollegeLMS.Next/`
- Bot: `CollegeLMS.MaxBot/` (Max messenger). Cache: Redis (сессии)
- Монолит, Clean Architecture (папки, а не проекты), REST/JSON, JWT без refresh-токенов в MVP
- `Result<T>` везде; никаких try-catch в контроллерах/сервисах — исключения ловит `ExceptionHandlerMiddleware`
- Ручные мапперы (без AutoMapper), FluentValidation, Swashbuckle
- Deploy: Docker Compose + GitHub Actions (`quality.yml` → `deploy.yml` на VPS). Локальный Docker не запускаем — стек собирается в CI/CD.

## Источники правды (не дублировать)

- Агенты, плагины, MCP, доступные модели — конфиг инструмента (`.opencode/agent/*.md`, `opencode.json`)
- ТЗ и User Stories — `docs/spec/task.md`, `docs/spec/userstories.md`
- Структура проекта — `README.md`; структура папок — сам репозиторий
- Дизайн — `DESIGN.md`, `PRODUCT.md`
- Postman — `docs/spec/CollegeLMS.postman_collection.json`

## Карта скиллов (загружай под задачу)

- Новая вертикальная фича → `feature-workflow`
- Backend: сущность → `dotnet-entity`; endpoint + DI → `dotnet-endpoint`; Result → `result-pattern`; валидация → `fluent-validation`; Swagger → `swagger-docs`; ASP.NET → `aspnet-core`
- Тесты → `dotnet-test` (перед кодом — `test-driven-development`)
- Frontend → `nextjs-page`, `design-system`, `impeccable`
- DevOps → `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`, `gh-fix-ci`
- Конвенции проекта (пакеты, JWT, EF Core, Docker, connection strings) → `project-reference`
- Баг/падение теста → `systematic-debugging`; перед «готово» → `verification-before-completion`; параллельные задачи → `dispatching-parallel-agents`

## Ключевые правила

- `CancellationToken ct` на всех async-методах; `AsNoTracking()` на чтении; `FindAsync()` по PK; предпочитать `List<T>` вместо `IEnumerable<T>`
- Данные, комментарии, Swagger-summaries и сообщения об ошибках — на русском
- Primary constructor DI (`class Service(AppDbContext db)`); плоские DTO; file-scoped namespaces
- `Program.cs` минимален: `Add*` → `Extensions/ServiceCollectionExtensions.cs`, `Use*` → `Extensions/ApplicationBuilderExtensions.cs`
- Мапперы → корень `Mappers/`, интерфейсы сервисов → корень `Interfaces/`
- Все entity наследуют базовый `Entities/Entity` (Guid Id, CreatedAt, UpdatedAt = `DateTime.UtcNow`)
- БД: GUID PK `ValueGeneratedNever()`; строки `HasMaxLength()`; enum `HasConversion<string>()` + `HasMaxLength()`; nav `[JsonIgnore]`
- Индексы (UNIQUE, простые) — в EF Configuration (`HasIndex` с `HasDatabaseName`); CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)
- EF Configuration включают `HasData()` для seed-данных
- Git-префиксы: `feat:` / `fix:` / `docs:` / `test:` / `refactor:` / `chore:` / `hotfix:` / `merge:`; `git add -A` для всех изменений
- Фронтенд: DESIGN.md §6 (иконки, touch 44×44px, адаптив 393px ↔ 1920px), mobile-first

## Спецификация

- В ТЗ каждое базовое требование в основных сервисах разбито на User Stories с чёткими критериями приёмки (см. `docs/spec/userstories.md`).

## Команды

| Задача | Команда |
|--------|---------|
| Build backend | `dotnet build CollegeLMS.slnx` |
| Тесты (таргетно) | `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}` |
| Тесты (полный прогон — в CI) | `dotnet test CollegeLMS.slnx` |
| Миграция | `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql` |
| Frontend | `cd CollegeLMS.Next && npm run dev` / `npm run build` |
| E2E (затронутый спек) | `cd CollegeLMS.Next && npx playwright test {spec}` |
| Формат | `dotnet csharpier format .` (проверка — `dotnet csharpier check .`) |

## Завершение задачи

- Обязателен commit + push в `master`; проверить, что CI/CD (quality → deploy) запустился.
- Перед утверждением «готово» — скилл `verification-before-completion` (свежая проверка, а не по памяти).
