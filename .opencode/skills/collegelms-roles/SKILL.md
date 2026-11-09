---
name: collegelms-roles
description: Распределение работы по ролям CollegeLMS (Backend, Frontend, Tester, DevOps, Docs) между субагентами DSH — модель, обязательные скиллы, границы файлов, гейт приёмки и формат отчёта.
---

# Роли CollegeLMS в DeepSeek Harness

Используй этот скилл, когда задача больше одной подсистемы или когда нужно
делегировать работу субагенту. Роли повторяют `.opencode/agents/*.md`, но
реализованы через `subagent` + модель + скиллы: отдельных именованных агентов
в DSH нет.

## Когда какая роль

| Роль | Модель (`opencode-go/…`) | Файлы | Гейт |
|---|---|---|---|
| Backend | `kimi-k2.7-code` | `CollegeLMS.API/**` | `dotnet build CollegeLMS.slnx`, `dotnet csharpier check .` |
| Frontend | `kimi-k3` (сложный UI) или `kimi-k2.7-code` | `CollegeLMS.Next/**` | `npm run build`, `npm run check:design-system`, затронутый Playwright-спек |
| Tester | `deepseek-v4.1-flash` | `CollegeLMS.Tests/**`, `CollegeLMS.MaxBot.Tests/**`, `CollegeLMS.Next/e2e/**` | таргетный `dotnet test --filter …`, `npx playwright test` |
| DevOps | `glm-5.3-flash` | `docker-compose*.yml`, `loadbalancer/**`, `.github/**`, `**/Dockerfile`, `scripts/**` | `docker compose ps`, сборка затронутого сервиса |
| Docs / материалы | `gpt-6-luna` | `docs/**`, репозитории `mdk*` | чек-лист `course-materials`, сборка/экспорт |

Выбор модели работает только в **новой** сессии: политика маршрутов фиксируется
при создании сессии. В текущей сессии субагент наследует модель родителя.
Перед выбором проверь `list_subagent_models`.

## Обязательные скиллы по ролям

- Backend: `dotnet-entity`, `dotnet-endpoint`, `dotnet-webapi`,
  `fluent-validation`, `swagger-docs`, `result-pattern`, `aspnet-core`,
  `efcore-patterns`.
- Frontend: `design-system`, `nextjs-page`, `impeccable`, `frontend-design`,
  `shadcn`, `vercel-react-best-practices`, `web-design-guidelines`.
- Tester: `dotnet-test`, `test-driven-development`, `systematic-debugging`,
  `playwright`, `playwright-interactive`.
- DevOps: `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`, `gh-fix-ci`.
- Docs: `course-materials`, `office-docx`, `office-export-pdf`,
  `office-render` (по необходимости).

## Шаблон промпта субагенту

```text
Роль: BackendAgent CollegeLMS.
Ветка: feat/<имя> (уже создана, переключись на неё; новую не создавай).
Задача: <одна вертикальная задача>.
Контекст: docs/spec/task.md §<…>, <файлы>.
Обязательно загрузи скиллы: <список>.
Границы: меняй только <путь>; не трогай <пути>; не коммить и не пушь.
Гейт: <команды> — приложи фактический вывод.
Отчёт: файлы, endpoints/компоненты, вывод гейта, риски, что осталось.
```

## Порядок вертикального среза

1. План и контракт API (родитель, `writing-plans` / `brainstorming`).
2. Backend и Frontend можно запускать **параллельно**, если контракт
   зафиксирован; иначе Frontend ждёт Backend.
3. Tester — после появления рабочего кода; сначала падающий тест, затем фикс.
4. DevOps — последним, если менялись compose/CI/прод.
5. Родитель проверяет гейты, делает коммит в рабочую ветку, push/merge — только
   по явному запросу.

## Границы, которые нельзя нарушать

- Субагенты не выполняют `git add/commit/push` — это делает родитель.
- Секреты не попадают в код, чат и коммиты; `.env` собирается скриптом
  `scripts/dsh/sync-secrets.ps1`.
- Backend не правит frontend и тесты; Tester не правит production-код; DevOps
  не правит C# и TS.
- Прод-каталог и рабочий каталог — разные копии одного compose-проекта:
  docker-команды только из `/home/user1/CollegeLMS`.

## Параллельность

Независимые делегации запускай одним сообщением. Пока они работают, делай
полезную локальную работу; когда остаётся только ждать — заверши ход, результат
придёт уведомлением. Не опрашивай субагентов в цикле.
