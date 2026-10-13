# CollegeLMS

Проектные пути и команды репозитория. Общие правила и конвенции — в глобальном `AGENTS.md`.

## Пути

- Backend: `CollegeLMS.API/` (решение `CollegeLMS.slnx`), тесты `CollegeLMS.Tests/`, `CollegeLMS.MaxBot.Tests/`
- Frontend: `CollegeLMS.Next/` (Next.js 14, App Router); бот Max: `CollegeLMS.MaxBot/`
- Спека: `docs/spec/task.md`, `docs/spec/userstories.md`; дизайн: `DESIGN.md`, `PRODUCT.md`
- Postman: `docs/spec/CollegeLMS.postman_collection.json`

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
