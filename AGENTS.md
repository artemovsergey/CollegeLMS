# CollegeLMS

Проектные пути и команды репозитория. Общие правила — в глобальном `AGENTS.md`; ниже — только проектное.

## Пути

- Backend: `CollegeLMS.API/` (решение `CollegeLMS.slnx`), тесты `CollegeLMS.Tests/`, `CollegeLMS.MaxBot.Tests/`
- Frontend: `CollegeLMS.Next/` (Next.js 14, App Router); бот Max: `CollegeLMS.MaxBot/`
- Спека: `docs/spec/task.md`, `docs/spec/userstories.md`; дизайн: `DESIGN.md`, `PRODUCT.md`
- Postman: `docs/spec/CollegeLMS.postman_collection.json`

## Секреты

- Локальный `.env` собирается из `collegelms.env` в приватном репозитории `secrets`; в git не коммитится.
- `appsettings.json` содержит дефолтные JWT-ключ и пароль БД — на них не полагаться, рабочие значения приходят из `.env`.
- `.env.bak-*` — устаревшие копии: не использовать и не добавлять в репозиторий.

## Ветки

- `master` — только проверенное и задеплоенное. Любая новая работа (дизайн-система, фичи, правки UI/UX) ведётся в **отдельной ветке**: `git checkout -b fix/{краткое-имя}` или `feat/{...}`.
- Ветка создаётся **до** первого изменения файла. Прямые коммиты в `master` запрещены.
- **Порядок завершения работы обязательный и без отдельного согласования:**
  коммит в ветку → проверка (build, гейты, e2e) → merge ветки в `master` →
  `git push origin master`. Дальше срабатывает CI/CD: push в `master`
  запускает `.github/workflows/deploy.yml` — quality-гейты, сборка образов и
  на сервере `git reset --hard origin/master` с пересборкой контейнеров.
  Merge и push не ждут разрешения пользователя: без них работа остаётся
  только в ветке и на сервер не попадает.
- Согласовывать отдельно нужно только то, что **ломает деплой**: миграции БД,
  смена переменных окружения, правки `docker-compose.yml` и workflow.
- Исключение по срокам: если сервер или CI лежат, работа не ждёт — коммит и
  merge делаются, а push повторяется после восстановления.

## Команды

| Задача | Команда |
|--------|---------|
| Build backend | `dotnet build CollegeLMS.slnx` |
| Тесты (таргетно) | `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}` |
| Тесты (полный прогон — в CI) | `dotnet test CollegeLMS.slnx` |
| Миграция | `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql` |
| Frontend | `cd CollegeLMS.Next && npm run dev` / `npm run build` |
| Гейты дизайн-системы | `cd CollegeLMS.Next && npm run check:design-system` (отчёт — `npm run check:design-system:report`) |
| E2E (затронутый спек) | `cd CollegeLMS.Next && npx playwright test {spec}` |
| Merge в master + деплой | `git checkout master && git merge {ветка} && git push origin master` |
| Формат | `dotnet csharpier format .` (проверка — `dotnet csharpier check .`) |

## Прод и стенд, агенты и среда

- Прод, docker-команды, порог диска и логи — `.opencode/skills/vps-bb-status/SKILL.md`.
- Playwright: `baseURL: http://localhost`, `:3000` отдаёт 404 на `/api/*`,
  `next dev` поднимается на 3001 — `.bb/skills/collegelms-dev/SKILL.md`.
- Модели, MCP, DSH-профиль и секреты агентской среды —
  `.opencode/skills/dsh-environment/SKILL.md` и
  `.opencode/skills/collegelms-roles/SKILL.md`.

## Скиллы проекта

- Backend: `dotnet-entity`, `dotnet-endpoint`, `dotnet-webapi`, `fluent-validation`, `swagger-docs`, `result-pattern`, `seed-data`, `cors-security`, `jwt-auth`
- Тесты: `dotnet-test`
- Frontend: `design-system`, `nextjs-page`
- Process: `feature-workflow`, `project-reference`
- DevOps: `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`
- Агентская среда: `collegelms-roles`, `dsh-environment`, `vps-bb-status`
- Учебные материалы: `course-materials`

## Frontend

- Перед правками HTML/CSS/клиентского JS — скилл `modern-web-guidance`: `npx modern-web-guidance search "<паттерн>"`, затем `retrieve <id>`. Скилл глобальный (`~/.agents/skills/modern-web-guidance`), в репозиторий не коммитится; ставится `npx -y skills add GoogleChrome/modern-web-guidance -g`.
- Baseline проекта — **Widely available**: современные API применяем с фолбэками, рассчитанными на старые браузеры аудитории колледжа.

## Конвенции .NET / EF Core

- `Result<T>` везде; никаких try-catch в контроллерах/сервисах — исключения ловит middleware
- `CancellationToken ct` на всех async-методах; `AsNoTracking()` на чтении; `FindAsync()` по PK; предпочитать `List<T>` вместо `IEnumerable<T>`
- Primary constructor DI; плоские DTO; file-scoped namespaces; ручные мапперы (без AutoMapper)
- `Program.cs` минимален: `Add*` → `Extensions/ServiceCollectionExtensions.cs`, `Use*` → `Extensions/ApplicationBuilderExtensions.cs`
- Мапперы → корень `Mappers/`, интерфейсы сервисов → корень `Interfaces/`
- Все entity наследуют базовый `Entities/Entity` (Guid Id, CreatedAt, UpdatedAt = `DateTime.UtcNow`)
- БД: GUID PK `ValueGeneratedNever()`; строки `HasMaxLength()`; enum `HasConversion<string>()` + `HasMaxLength()`; nav `[JsonIgnore]`
- Индексы (UNIQUE, простые) — в EF Configuration (`HasIndex` + `HasDatabaseName`); CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный PL/pgSQL, не через миграции)
- EF Configuration включают `HasData()` для seed-данных

