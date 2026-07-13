# CollegeLMS — Инструкции для агента

## Спецификация

- В техническом задании каждое базовое требование в основных сервисах должно быть разбито на конкретные пользовательские истории (User Stories) с четкими критериями приемки.

## Стек

- Backend: .NET 10, ASP.NET Core Web API
- Frontend: Next.js 14, TS, Tailwind CSS 4 (в `frontend/`)
- DB: PostgreSQL 16
- Cache: Redis (только сессии)
- Deploy: Docker Compose, GitHub Actions CD (только деплой, тесты локально)
- Files: локальная ФС (позже MinIO)

## Архитектура

- Монолит, Clean Architecture (папки, а не проекты)
- REST API, JSON, JWT auth (без refresh токенов в MVP)
- `Result<T>` — везде, никаких try-catch в контроллерах/сервисах
- `ExceptionHandlerMiddleware` ловит неожиданные исключения
- Ручные мапперы (без AutoMapper), FluentValidation, Swashbuckle
- Все данные и комментарии в коде на русском

## Правило Superpowers

**Загружай нужные skills ДО любого ответа или действия** — включая уточняющие вопросы, изучение кодовой базы и проверку файлов. Если skill существует для задачи — загрузи его первым.

| Ситуация | Skill | Когда |
|----------|-------|-------|
| Творческая работа, новая фича | `brainstorming` | До любого кода или изучения |
| Баг, падение теста, неожиданное поведение | `systematic-debugging` | До предложения фикса |
| Задача на реализацию | `writing-plans` → `executing-plans` | После утверждения спецификации |
| TDD | `test-driven-development` | Сначала тест, потом код |
| Готовишься сказать «готово» | `verification-before-completion` | До коммита или утверждения об успехе |
| Параллельные независимые задачи | `dispatching-parallel-agents` | 2+ независимых подзадачи |

## MCP Серверы (opencode.json)

| Сервер | Назначение | Включён |
|--------|------------|---------|
| `playwright` | Визуальная отладка, E2E-тесты, инспекция DOM, скриншоты | да |
| `github` | PR, issues, checks, ветки, управление репозиторием | да |

## Плагин (Superpowers)

Установлен: `superpowers@git+https://github.com/obra/superpowers.git`
Расположение: `~/.cache/opencode/packages/superpowers/.../`
Предоставляет: process-скиллы (brainstorming, systematic-debugging, verification-before-completion и т.д.)

## Структура проекта

```
CollegeLMS.slnx            # Файл решения — все проекты включены
CollegeLMS.API/
  Program.cs             # Минимальный bootstrap — всё делегировано методам расширения
  Controllers/           # Наследники ControllerBase
  Services/              # Реализации {Name}Service
  Interfaces/            # Интерфейсы I{Name}Service
  Mappers/               # Статические мапперы-расширения (Entity ↔ DTO)
  Entities/              # Доменные классы (наследуют Entity)
  Entities/Enums/        # Типы-перечисления
  Data/                  # AppDbContext
  Data/Configurations/   # IEntityTypeConfiguration<T> (с HasData + raw SQL)
  Dtos/                  # DTO запросов/ответов
  Validators/            # Валидаторы FluentValidation
  SwaggerExamples/       # Классы примеров ответов для Swagger-документации
  Middleware/             # ExceptionHandlerMiddleware
  Response/              # Result<T>, ApiResult<T>, ErrorResponse
  Exceptions/            # NotFoundException, ValidationException, ForbiddenException
  Extensions/            # ServiceCollectionExtensions, ApplicationBuilderExtensions, ClaimsPrincipalExtensions
docs/spec/               # Postman-коллекция, task.md, userstories.md
CollegeLMS.Tests/
  Integration/           # Тесты WebApplicationFactory
  Integration/Controllers/
  Unit/Services/
  Fixtures/              # Фикстуры Bogus
frontend/                # Next.js 14 + Tailwind CSS 4 + TypeScript
  components/ui/         # Примитивы shadcn/ui
  components/            # Проектные компоненты
  lib/utils.ts           # Хелпер cn()
import/                  # Данные импорта
scripts/                 # Скрипты парсинга WP
```

## Роли агентов

| Роль | Тип | Skills | Ответственность |
|------|-----|--------|-----------------|
| **Architect** | Главный агент | brainstorming, writing-plans, verification-before-completion, requesting-code-review, yeet | Оркестрирует workflow, читает task.md, декомпозирует на User Stories, создаёт ветки, ревьюит, сливает |
| **BackendAgent** | Сабагент | dotnet-entity, dotnet-endpoint, result-pattern, fluent-validation, swagger-docs, aspnet-core | Entity → миграция → сервис → контроллер → DI → Swagger → Postman |
| **TesterAgent** | Сабагент | dotnet-test, playwright, playwright-interactive, test-driven-development, systematic-debugging | Модульные тесты (xUnit + Moq + Bogus), интеграционные (WebApplicationFactory), E2E (Playwright), покрытие |
| **FrontendAgent** | Сабагент | impeccable, design-system, nextjs-page | Страницы/компоненты Next.js, интеграция API, Tailwind, shadcn/ui |
| **AnalystAgent** | Сабагент | plantuml-docs, security-threat-model | Диаграммы PlantUML (ER, Class, Sequence, UseCase, Deployment), техдокументация, threat modeling |
| **DevOpsAgent** | Сабагент | docker-compose-dev, vps-deploy, cicd-pipeline, gh-fix-ci, sentry | Docker, nginx, CI/CD пайплайны, деплой на VPS, мониторинг ошибок |

### Поддержка dispatch по платформам

| Платформа | Dispatch сабагента | Как |
|-----------|-------------------|-----|
| **OpenCode** | ⚠️ Ограниченно | Только ручной вызов `task` tool (`subagent_type: "subagent"` — доступен только пользователю) |
| **Codex (OpenAI)** | ✅ Автоматический | `@codex имя-агента` в промпте |
| **Claude Code** | ✅ Автоматический | `/agent` команда в чате |


> **Примечание:** Роли (Architect, BackendAgent, TesterAgent и т.д.) — **концептуальные**. Они задают, какой контекст и фокус нужен на каждой фазе. Физический dispatch зависит от платформы.

## Skills

### Проектные Skills (.opencode/skills/)

| Skill | Что создаёт |
|-------|-------------|
| `dotnet-entity` | Класс Entity, EF-конфигурация, enum, миграция |
| `dotnet-endpoint` | Контроллер, сервис, DTO, маппер, регистрация DI |
| `result-pattern` | Result<T>, ApiResult, ExceptionHandlerMiddleware |
| `jwt-auth` | TokenService, BCrypt, Swagger bearer, хелперы Claims |
| `dotnet-test` | Модульные тесты (xUnit + Moq + Bogus), интеграционные тесты (WebApplicationFactory) |
| `fluent-validation` | Валидаторы FluentValidation + регистрация DI |
| `nextjs-page` | Страница Next.js, loading/error boundaries, типы |
| `design-system` | shadcn/ui + Tailwind v4 + Lucide — production UI-компоненты |
| `docker-compose-dev` | dev docker-compose.yml (Postgres 16 + Redis 7) |
| `vps-deploy` | Nginx, Dockerfile, GH Actions CI/CD, скрипты деплоя |
| `cicd-pipeline` | GitHub Actions: test.yml + quality gates |
| `swagger-docs` | Swagger XML-комментарии, Examples, ProducesResponseType, Postman-спецификация |
| `plantuml-docs` | UML-диаграммы: ER, Class, Sequence, UseCase, Deployment |
| `cors-security` | CORS + заголовки безопасности |
| `seed-data` | EF Core HasData + классы Seed |
| `feature-workflow` | Полная вертикальная фича — ветка, entity, API, тесты, фронтенд, документация, слияние |
| `project-reference` | Общие соглашения проекта: NuGet-пакеты, JWT auth, Docker, EF Core |

### Superpowers Process Skills (~/.cache/opencode/.../superpowers/)

| Skill | Назначение | Когда использовать |
|-------|------------|-------------------|
| `brainstorming` | Исследование дизайна до творческой работы | Новые фичи, UI-дизайн, архитектура |
| `systematic-debugging` | Поиск первопричины до исправлений | Любой баг, падение теста, неожиданное поведение |
| `test-driven-development` | Цикл Red-Green-Refactor | Когда тесты пишутся до кода |
| `writing-plans` | Генерация планов реализации из спецификации | После утверждения спецификации |
| `executing-plans` | Выполнение планов с контрольными точками | После writing-plans |
| `subagent-driven-development` | Распределение подзадач по сабагентам | Сложные многошаговые фичи |
| `finishing-a-development-branch` | Проверка готовности к слиянию/PR | Перед завершением ветки |
| `using-git-worktrees` | Изолированные рабочие пространства | Старт параллельных веток |
| `verification-before-completion` | Свежая проверка перед утверждением «готово» | Перед каждым коммитом или слиянием |
| `writing-skills` | Создание/редактирование процессной документации | Поддержка skills |
| `requesting-code-review` | Запуск сабагента-ревьюера | Перед слиянием PR |
| `receiving-code-review` | Обработка замечаний ревью с rigorous-подходом | Когда есть комментарии в PR |
| `dispatching-parallel-agents` | Независимые сабагенты параллельно | 2+ независимых подзадачи |

> **Примечание:** Skills в `.opencode/skills/` и `.agents/skills/` специфичны для OpenCode.
> При работе в Codex или Claude Code используются их нативные аналоги
> (custom instructions, CLAUDE.md, project files).

## Workflow Protocol

### Обязательные Process Skills по фазам

| Фаза | Загрузить Skills | Гейт проверки |
|------|------------------|---------------|
| **0: Planning** | brainstorming → writing-plans | Спецификация утверждена пользователем |
| **1: Backend** | dotnet-entity, dotnet-endpoint, fluent-validation, swagger-docs, aspnet-core | dotnet build |
| **2: Tests** | dotnet-test, test-driven-development | dotnet test |
| **3: Frontend** | impeccable, design-system, nextjs-page | npm run dev |
| **4: E2E** | playwright, playwright-interactive | npx playwright test |
| **5: Docs** | plantuml-docs, security-threat-model | Визуальная проверка |
| **6: DevOps** | docker-compose-dev, vps-deploy | docker compose up --build |
| **7: Review** | verification-before-completion, requesting-code-review, yeet | Слияние + push |

### Полный цикл фичи (вертикальный срез)

```
Phase 0: PLANNING (Architect)
  Load: brainstorming
  Прочитать task.md, понять требования
  Декомпозировать на User Stories (см. шаблон ниже)
  Сохранить task.md и User Stories в docs/spec/
  Предложить подходы → пользователь утверждает дизайн
  Написать спецификацию дизайна → docs/spec/{feature}-design.md
  Load: writing-plans
  Сгенерировать план реализации
  ⚠️ Синхронизация: git fetch origin && git pull --rebase origin master
  git checkout -b feature/{service}-{feature}

Phase 1: BACKEND (BackendAgent)
  Load: dotnet-entity, dotnet-endpoint, fluent-validation, swagger-docs, aspnet-core
  skill("dotnet-entity")    → Entities/{Name}.cs (наследует Entity), Data/Configurations/{Name}Configuration.cs, миграция
    • Класс Entity расширяет базовый `Entity` (Id, CreatedAt, UpdatedAt)
    • Enum (если нужен) в Entities/Enums/
    • EF Config: ToTable, HasKey, ValueGeneratedNever, HasMaxLength, HasConversion<string>
    • Индексы: HasIndex с кастомными именами (EF управляет через миграции)
    • HasData для тестовых/сидовых данных
    • DbContext авто-обнаруживает через ApplyConfigurationsFromAssembly
    • CHECK constraints: НЕ в EF Config → добавить в Data/DbConstraints.cs (идемпотентный SQL)
  dotnet ef migrations add  → создаёт SQL-миграцию
  skill("dotnet-endpoint")  → DTO, маппер, интерфейс, сервис, контроллер, регистрация DI
    • Dtos/{Name}Request.cs + {Name}Response.cs
    • Mappers/{Name}Mapper.cs (корневая папка Mappers/)
    • Interfaces/I{Name}Service.cs (корневая папка Interfaces/)
    • Services/{Name}Service.cs (primary constructor, Result<T>, AsNoTracking, FindAsync)
    • Controllers/{Name}Controller.cs (CRUD, SwaggerOperation, SwaggerResponse)
    • DI: добавить в Extensions/ServiceCollectionExtensions.cs (НЕ в Program.cs)
  skill("fluent-validation")
    • Validators/{Name}RequestValidator.cs (NotEmpty, MaxLength, сообщения на русском)
    • DI: AddValidatorsFromAssemblyContaining<Program>() в ServiceCollectionExtensions
  skill("swagger-docs")     → SwaggerExamples/{Name}ResponseExample.cs + обновить Postman
    • XML-комментарии на Controller: <summary>, <remarks>, <response code="...">
    • [ProducesResponseType(typeof(...), StatusCodes.Status...)] для всех статусов
    • [SwaggerResponse(code, "...", typeof(...))] + ErrorResponseExample для ошибок
    • SwaggerExamples/ErrorResponseExample.cs для общих ошибок
    • SwaggerExamples/{Name}ResponseExample.cs для успешного ответа
    • docs/spec/CollegeLMS.postman_collection.json — добавить endpoint в коллекцию
  ⚠️ Локальная проверка: dotnet build
  git add -A && git commit -m "phase 1: {feature} backend"

Phase 2: FRONTEND (FrontendAgent)
  Load: impeccable, design-system, nextjs-page

  Step 1 — Design brief (shape)
    skill("impeccable") → shape
    • Определение scope, user flow, edge cases, UI states
    • Результат: design brief с visual direction и composition budget

  Step 2 — UI: implement (craft)
    skill("impeccable") → craft
    skill("design-system")    → shadcn/ui компоненты, токены, паттерны
    skill("nextjs-page")      → page.tsx + loading.tsx + error.tsx
    API integration           → fetch with types

  Step 3 — UI: polish
    skill("impeccable") → polish
    • Design pass: иерархия, типографика, цвет, отступы, пустые состояния, тени

  Step 4 — UI: audit
    skill("impeccable") → audit
    • Проверка против Web Interface Guidelines

  ⚠️ Локальная проверка: npm run dev
  git add -A && git commit -m "phase 2: {feature} frontend"

Phase 3: DOCS (AnalystAgent)
  Load: plantuml-docs, security-threat-model
  PlantUML diagrams         → ER (entities), Class (services), Sequence (flows)
  Security threat model     → trust boundaries, attack paths (если нужно)
  skill("plantuml-docs")
  git add -A && git commit -m "phase 3: {feature} docs"

Phase 4: LOCAL VERIFICATION (DevOpsAgent)
  Load: docker-compose-dev, vps-deploy
  Поднять полный compose:     docker compose up --build -d --profile agentbridge
  Проверить что всё работает: docker compose ps
  Если Docker падает         → исправить
  ⚠️ verification-before-completion: docker compose up --build проходит
  git add -A && git commit -m "phase 4: {feature} devops"

Phase 5: MERGE & DEPLOY (Architect)
  Load: verification-before-completion, requesting-code-review, yeet
  ⚠️ verification-before-completion: проверить compose поднимается
  Load: requesting-code-review
  Code review               → peer review всех изменений
  Если есть замечания       → исправить, перезапустить фазы
  Load: yeet
  git checkout master && git merge feature/{service}-{feature}
  git push                  → GitHub Actions запускает CD
  CD на VPS:
    1. git pull
    2. запись .env из GitHub Secrets
    3. docker compose up --build -d --profile agentbridge
    4. миграции
    5. health check
```


### Шаблон User Story

```markdown
## UC-N: {Роль} может {действие}

**Критерии приёмки:**
- [ ] {критерий 1}
- [ ] {критерий 2}

**API:**
- `{method} /api/{route}` — {описание}

**UI (если есть):**
- Страница `/{route}` с таблицей/формой
- Ошибки показаны пользователю (тост/сообщение)

**Зависимости:** {сервисы, от которых зависит}
```

### Гейты приёмки

| Гейт | Проверка | Кто | Фаза |
|------|----------|-----|------|
| **G1** | `dotnet build` | BackendAgent | Phase 1 |
| **G2** | `npm run dev` — страница рендерится | FrontendAgent | Phase 2 |
| **G3** | `docker compose up --build` — все сервисы стартуют | DevOpsAgent | Phase 4 |

### Definition of Done

- [ ] dotnet build проходит
- [ ] npm run dev работает (frontend проект)
- [ ] Swagger UI показывает endpoint с русской документацией
- [ ] SwaggerExamples созданы для всех ответов (ошибки/успех)
- [ ] Postman-коллекция обновлена в docs/spec/
- [ ] PlantUML диаграммы сгенерированы — ER, Class, Sequence для фичи
- [ ] Security threat model проверен (если нужно)
- [ ] `docker compose up --build -d --profile agentbridge` работает локально
- [ ] Feature-ветка слита в master
- [ ] Push в master → CD развернул на VPS

## Соглашения по БД

- EF Core Code First, Npgsql, `EFCore.NamingConventions` (snake_case)
- GUID PK с `ValueGeneratedNever()`
- String props: `HasMaxLength()` обязательно
- Enum props: `HasConversion<string>()` + `HasMaxLength()` обязательно
- Navigation properties: `[JsonIgnore]`
- Timestamps: `CreatedAt`, `UpdatedAt` = `DateTime.UtcNow`
- Подключение: `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root`
- Индексы (UNIQUE, простые) — в EF Configuration (`HasIndex` с `HasDatabaseName`)
- CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)

## NuGet пакеты

Пакеты в `.csproj` сгруппированы в отдельные `ItemGroup` с комментариями по категориям:

| Категория | Пакеты |
|-----------|--------|
| EF Core и БД | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `EFCore.NamingConventions`, `AspNetCore.HealthChecks.NpgSql` |
| Auth и безопасность | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Валидация | `FluentValidation.DependencyInjectionExtensions` |
| Swagger | `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`, `Microsoft.OpenApi` |
| Логирование | `Serilog.AspNetCore` |
| Тесты | `xunit`, `coverlet.msbuild`, `Bogus`, `Moq`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` |

## Разработка

### Docker-first подход

Всё окружение работает в Docker — никаких локальных SDK, кроме Docker Desktop.

```powershell
# Старт полного стека (db + redis + api + frontend + nginx + agentbridge)
docker compose up --build -d --profile agentbridge

# API доступен через nginx: http://localhost/api/...
# Swagger: http://localhost/swagger/
# Frontend: http://localhost/
```

NuGet пакеты кэшируются в named volume `nuget_packages` — не теряются при пересборке.

### Команды по фазам

| Фаза | Команда | Описание |
|------|---------|----------|
| **Phase 1** | `dotnet build` | Локальная проверка backend |
| **Phase 1** | `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql` | Миграция |
| **Phase 2** | Открыть `http://localhost/` в браузере | Проверка frontend |
| **Phase 4** | `docker compose up --build -d --profile agentbridge` | Локальная проверка compose |
| **Format** | `dotnet csharpier format .` | CSharpier |
| **Stop** | `docker compose down` | Остановить всё |

## Соглашения по коду

- Primary constructor DI (`class Service(AppDbContext db)`)
- `CancellationToken ct` на всех асинхронных методах
- `AsNoTracking()` на чтении, `FindAsync()` для поиска по PK
- Сообщения об ошибках на русском, Swagger summaries на русском
- Git-префиксы: `feature:` / `fix:` / `docs:` / `test:`
- `git add -A` для добавления всех изменений (никогда не перечислять файлы по одному)
- `gh` CLI должен использовать `GITHUB_TOKEN` env var для аутентификации (`GH_TOKEN` тоже работает) — перед любым `git push` или `gh pr create` убедиться, что `$env:GITHUB_TOKEN` или `$env:GH_TOKEN` установлен. Это предотвращает интерактивный выбор аккаунта при пуше.
- Предпочитать `List<T>` вместо `IEnumerable<T>`
- Плоские DTO со значениями по умолчанию, file-scoped namespaces
- `Result<T>.Ok()` для успеха, `Result<T>.Fail()` для ошибок
- OpenApi namespace: `using Microsoft.OpenApi;`
- Форматирование: CSharpier (`dotnet csharpier .` для форматирования, `dotnet csharpier . --check` в CI)
- Все entity наследуют базовый `Entities/Entity` (Guid Id, CreatedAt, UpdatedAt)
- Мапперы в корневой папке `Mappers/`, интерфейсы сервисов в корневой папке `Interfaces/`
- `Program.cs` минимален — все `builder.Services.Add*` в `Extensions/ServiceCollectionExtensions.cs`, все `app.Use*` в `Extensions/ApplicationBuilderExtensions.cs`
- EF Configuration включают `HasData()` для seed-данных и `HasIndex` с кастомными именами
- CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)

## Соглашения по PlantUML

- ER-диаграммы: `/docs/diagrams/er/{entity}.puml`
- Sequence-диаграммы: `/docs/diagrams/sequence/{flow}.puml`
- Class-диаграммы: `/docs/diagrams/class/{service}.puml`
- PlantUML онлайн: `https://www.plantuml.com/plantuml/uml/{encoded}`
- Или локально: `java -jar plantuml.jar docs/diagrams/**/*.puml`

## Соглашения по фронтенду (shadcn/ui + Tailwind CSS v4)

- **Design tokens**: CSS-кастомные свойства в HSL (colors.css, typography.css, spacing.css)
- **Компоненты**: `components/ui/` — примитивы shadcn (не редактировать), `components/` — проектные
- **Иконки**: Lucide React. См. DESIGN.md §6 для маппинга фича→иконка, размеров, цветов (`currentColor` по умолчанию, College Blue при наведении, семантические цвета для статусов), и a11y (aria-label для иконок без текста, aria-hidden когда рядом есть текст)
- **Адаптивность**: mobile-first, брейкпоинты `sm:`, `md:`, `lg:`
- **Touch targets**: минимум 44×44px для интерактивных элементов
- **Доступность**: семантический HTML, ARIA, навигация с клавиатуры, `focus-visible:ring-2`, `sr-only`
- **Формы**: labels всегда видны, ошибки под полем, loading state на submit
- **Контейнер**: `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8`

## Репозитории для справки

- База заметок по .NET — https://github.com/artemovsergey/.NET/wiki
- Репозиторий по организации работы с брокером сообщений — https://github.com/artemovsergey/ModuleBankApp
- Заметки по микросервисной архитектуре — https://github.com/artemovsergey/SampleApp/wiki
- Репозиторий для примера кода MinimalAPI — https://github.com/artemovsergey/TicTacToe
- Материалы по UML — https://github.com/artemovsergey/UML
- Репозитории с примерами кода — https://github.com/artemovsergey/VendingAppFinal, https://github.com/artemovsergey/ProfApp
- Конфигурации для развертывания — https://github.com/artemovsergey/VPS
- Технический журнал React Native — https://github.com/artemovsergey/ReactNative
- Расписание для преподавателей. Тестовый вариант — https://github.com/artemovsergey/scheduleTeacher

**Замечание**: при нахождении лучшего решения, чем в примерах, обязательно спросить
