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

## Прод и стенд

- Прод — **тот же хост**, что и рабочий каталог: сервисы живут в `/home/user1/CollegeLMS`. Оба каталога называются `CollegeLMS`, поэтому compose-проект один и тот же (`collegelms`), а имена контейнеров заданы явно. `docker compose` из рабочего каталога пересоздаёт боевые контейнеры и ссорится с деплоем.
- Значит, docker-команды — только из прод-каталога:
  `cd /home/user1/CollegeLMS && docker compose --profile max-bot up -d --build {api|collegelms-next|maxbot|loadbalancer}`
- Playwright по стенду — с `baseURL: http://localhost` (через loadbalancer). Напрямую на `:3000` пути `/api/*` отдают 404, а `next dev` поднимается на 3001 и без CORS не логинится.
- Перед сборкой Next на `/` нужно минимум 3 ГБ — это порог деплоя. Место чаще всего съедает не Docker, а архив systemd: `sudo journalctl --disk-usage`, чистка `sudo journalctl --vacuum-size=300M` (лимит `SystemMaxUse=300M` уже прописан в `/etc/systemd/journald.conf`).
- Логи контейнеров после деплоя: `docker compose --profile max-bot logs api --tail 50`.
- Ручной `docker compose` из рабочего каталога после merge не нужен: деплой
  приезжает сам через CI/CD и пересобирает контейнеры в прод-каталоге.

## Агенты и модели

- Целевые модели после подключения подписок: `BackendAgent` — `opencode-go/kimi-k2.7-code`, `FrontendAgent` — `kimi-for-coding/k3.1-pro`, `DevOpsAgent` — `opencode-go/glm-5.3-flash`, `TesterAgent` — `opencode-go/deepseek-v4.1-flash`, `plan` — `openai/gpt-6.1-sol`, `build` — `opencode-go/kimi-k2.7-code`.
- OpenCode Go активна. Пока не подключены Codex и Kimi, `plan` и `FrontendAgent` временно работают на `opencode/deepseek-v4.1-flash-free`; остальные агенты уже на Go.
- Агенты лежат в `.opencode/agents/`, проектные скиллы — в `.opencode/skills/`.

### DeepSeek Harness

- Профиль — `desktop`; единый `cordis.patch.yml` (копия — `secrets/dsh.patch.yml`) задаёт модель `opencode-go/deepseek-v4.1-flash`, права `danger-full-access`/`approval: never`, MCP `github`, `context7`, `ms_learn`, `playwright` и список моделей субагентов. Bundle не используется.
- Отдельных именованных агентов в DSH нет: роли задаёт скилл `collegelms-roles` (`deepseek-v4.1-flash` — основная, `kimi-k2.7-code` — backend, `kimi-k3` — frontend, `glm-5.3-flash` — devops, `gpt-6-luna` — документы). Выбор модели субагентом действует только в **новых** сессиях.
- Проектные скиллы — `.opencode/skills`; DSH видит их через junction `.dsh/skills` → `~/.dsh/local/collegelms-skills`. Глобальные OpenCode/Superpowers — `~/.dsh/skills` (создаются `scripts/dsh/link-skills.ps1`).
- Секреты: `scripts/dsh/sync-secrets.ps1` собирает `.env` из `secrets/.env` + `secrets/collegelms.env`; в конфиге секретов нет, `OPENCODE_GO_API_KEY` — в credential store, GitHub-токен — из `gh`.
- Проверка: 4 MCP-сервера (77 инструментов), 6 моделей в новой сессии, скиллы `collegelms-roles`, `course-materials`, `dsh-environment`, `vps-bb-status`. Подробности — `docs/agents/dsh-environment.md`.

## Скиллы проекта

- Backend: `dotnet-entity`, `dotnet-endpoint`, `dotnet-webapi`, `fluent-validation`, `swagger-docs`, `result-pattern`, `seed-data`, `cors-security`, `jwt-auth`
- Тесты: `dotnet-test`
- Frontend: `design-system`, `nextjs-page`
- Process: `feature-workflow`, `project-reference`
- DevOps: `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`
- Агентская среда: `collegelms-roles`, `dsh-environment`, `vps-bb-status`
- Учебные материалы: `course-materials`

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

