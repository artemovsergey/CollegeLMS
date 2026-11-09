# Среда DeepSeek Harness для CollegeLMS

Дата проверки: 2026-11-09. Профиль `desktop`, хост Windows.

## Что сделано

| Область | Результат |
|---|---|
| Модели | В `opencode-go` доступны `deepseek-v4.1-flash`, `kimi-k2.7-code`, `kimi-k3`, `glm-5.3-flash`, `gpt-6-luna`, `qwen3.8-flash`; субагентам разрешён выбор в новых сессиях. |
| MCP | Подключены `github` (46 инструментов), `playwright` (26), `ms_learn` (3), `context7` (2) — проверено субагентом, 77 инструментов. |
| Скиллы | 17 проектных + 4 новых (`collegelms-roles`, `course-materials`, `dsh-environment`, `vps-bb-status`), 29 глобальных OpenCode и 15 Superpowers — всего 65 в каталоге DSH (плюс встроенные office/sandbox). |
| Агенты | Роли Backend/Frontend/Tester/DevOps/Docs описаны скиллом `collegelms-roles`; именованных агентов в DSH нет. |
| Секреты | `.env` собран из `secrets/.env` + `secrets/collegelms.env`: 29 ключей, 15 добавлено, локальные `OPENCODE_*`/`AGENTBRIDGE_PORT` сохранены. |
| Автоматизации | По решению пользователя не создавались; задачи расписания удалены. |
| Свои плагины | Не разрабатывались; `dsh-collegelms` — конфигурационный bundle для MCP и моделей, не код-плагин. |

## Наработки OpenCode и bb

### OpenCode (десктоп)

- 4 агента в `.opencode/agents/` (Backend, Frontend, Tester, DevOps): жёсткие
  границы файлов, обязательные скиллы, гейты. В DSH это перенесено в скилл
  `collegelms-roles`.
- 19 проектных скиллов + 29 глобальных в `~/.config/opencode/skills` +
  15 Superpowers. Все подключены к DSH, дублирования нет.
- Плагины: `superpowers`, `opencode-notifier`, `opencode-dcp`; MCP: playwright,
  github, context7, ms_learn, chrome-devtools. В DSH первые четыре, без
  notifier (в DSH есть свои уведомления) и без chrome-devtools (дубль
  Playwright).
- Конфиг OpenCode в secrets и локальный совпадают по строкам (различие — только
  переводы строк). Глобальный `AGENTS.md` разошёлся: локальная копия знает, что
  прямой SSH теперь работает, копия в secrets — устарела. Нужна синхронизация
  формулировки (сначала прямой SSH, при блокировке — прокси).

### bb на VPS

- `bb` 0.47.1 (`bb-app.service`, порт 38886), 99 тредов: 89 на
  `acp-opencode`, 6 на kilocode, 4 на copilot; 97 архивных, 10 в `error`.
  Активных — 2 (в т.ч. тред про herdr/orca).
- Проект CollegeLMS: `proj_ise48szvgw`, каталог `bb-workspace/CollegeLMS`.
- 39 установленных плагинов bb и 5 своих: `vps-monitor` (CPU/RAM/диск/Docker +
  страница и `bb vps-monitor`), `maxbot-stats` (аудитория бота MAX),
  `vpn` (переключение Xray), `free-quota` (квота free-моделей),
  `copilot-credits` (кредиты Copilot). Это готовые примеры плагинов
  «RPC + UI + CLI + skill» — ориентир для будущих DSH-плагинов.
- Автоматизации bb: ежедневный бэкап `bb.db` (14 дней), еженедельный
  deps-audit, ежедневное обновление версий, ежедневный vps-digest.
  DSH-задачи не дублируют бэкап и обновления; аудит зависимостей DSH
  дополняет bb тем, что предлагает план исправления.
- Проблемы: диск 90% (3.1 ГБ свободно), load average 5–7,
  `bb-healthcheck.service` в `failed`. Перед деплоем Next нужно освободить
  место (`journalctl --vacuum-size=300M`).

### Главный риск: незапушенная ветка coursekit

В рабочем каталоге bb-треда `thr_4az9whr6t5` (тема «Инструменты для создания
курсов») лежит ветка `feature/course-pipeline` репозитория `mdk0104_IB`:
**17 коммитов, не запушенных в origin**, и `tools/coursekit` (440 КБ, 56
файлов) — нормализация Markdown, DOCX/pandoc, PDF/Typst, PPTX, Mermaid,
сборник, манифест, QA-отчёт и CI. В `master` на GitHub этого нет: там только
старый `tools/generate_lecture_pptx.py`.

Это единственная копия большой работы. Первый шаг — `git push -u origin
feature/course-pipeline` и PR (вручную или по отдельной просьбе). DSH-скилл
`course-materials` уже описывает команды и коды QA этого конвейера.

## Как устроено

- **Bundle** `tools/dsh/collegelms-bundle` установлен в профиль `desktop` через
  `plugin_manager` (не ручной правкой `cordis.patch.yml`). Это конфигурационный
  bundle: MCP-ряды + переопределение `subagent-model-selection-settings`.
- **GitHub MCP** запускается через `scripts/dsh/github-mcp.cmd`: токен берётся
  из keyring `gh auth token` и не попадает в конфиг.
- **Скиллы проекта**: junction `.dsh/skills → .opencode/skills` (DSH сканирует
  `.dsh/skills` у корня git-репозитория). Глобальные скиллы — 44 junction'а в
  `~/.dsh/skills`, создаются `scripts/dsh/link-skills.ps1`.
- **Секреты**: `scripts/dsh/sync-secrets.ps1` — идемпотентный merge, значения не
  печатает, локальные ключи не теряет.
- **Политика моделей** фиксируется в сессии при её создании. В уже открытой
  сессии субагент наследует модель родителя; для ролей откройте новую сессию.

## Модели и роли

| Роль | Модель | Почему |
|---|---|---|
| Основная работа, тесты, рутина | `opencode-go/deepseek-v4.1-flash` | дешёвая, 1M контекста, хороша для массовых правок |
| Backend, сложный код | `opencode-go/kimi-k2.7-code` | сильный coding-профиль, 262k контекста |
| Frontend, тяжёлый UI | `opencode-go/kimi-k3` | лучшее качество среди доступных, дороже |
| DevOps, исследование | `opencode-go/glm-5.3-flash` | быстрая и дешёвая для инфраструктуры |
| Документы, учебные материалы | `opencode-go/gpt-6-luna` | сильный текст, низкая цена |

`kimi-k3.1-pro` и `gpt-6.1-sol` из старого `AGENTS.md` в каталоге `opencode-go`
отсутствуют — заменены на `kimi-k3` и `gpt-6-luna`. Вернуть после появления в
каталоге.

## MCP: что выбрать и почему

| Сервер | Зачем CollegeLMS | Цена контекста |
|---|---|---|
| `github` | issues/PR/Actions/код, работа с `mdk*` и `secrets` | 46 схем, самый тяжёлый |
| `playwright` | E2E, визуальная проверка UI, стенд `localhost` | 26 схем |
| `ms_learn` | официальные .NET/ASP.NET Core/EF Core материалы | 3 схемы |
| `context7` | актуальные доки Next.js, Playwright, EF Core | 2 схемы |

Не подключены осознанно: `chrome-devtools` (дублирует Playwright),
`sequential-thinking` (мало пользы для кода), `memory` (заменяется
`session-search`), Postgres MCP (пока нет безопасного read-only URL).

Кандидаты на добавление: `deepwiki` (разбор чужих репозиториев),
`postgres` (read-only роль в БД), `vps` (собственный MCP, см. ниже).

## Автоматизации и свои плагины

По решению пользователя (2026-11-09) не настраивались: задачи расписания
удалены, разработка собственных плагинов отложена. При необходимости:
`schedule_create` — для автоматизаций, `plugin_manager install_bundle` — для
bundle, отдельный пакет — для host-плагина.

Идеи на будущее (без реализации): `dsh-vps`, `dsh-bb`, `dsh-coursekit`,
`dsh-secrets-guard`, интеграция bb ↔ DSH через ACP.

## Проверка

```powershell
# bundle и MCP
plugin_manager → list_bundles        # dsh-collegelms enabled
subagent: «перечисли mcp__*»          # 4 сервера, 77 инструментов

# модели (в новой сессии)
list_subagent_models                  # 6 моделей opencode-go

# скиллы
# каталог содержит collegelms-roles, course-materials, dsh-environment, vps-bb-status

# секреты
pwsh -File scripts/dsh/sync-secrets.ps1   # 29 ключей, 0 ошибок
```

## Риски и ограничения

- 77 MCP-схем + ~80 скиллов заметно расходуют контекст. На слабых моделях
  отключайте `github` или `playwright` точечно (правка bundle + переустановка).
- Пути к `node.exe`, `@playwright/mcp`, `github-mcp-server.exe` привязаны к
  этой машине.
- VPS: диск занят на 90% (3.1 ГБ свободно), load average 5–7, `bb-healthcheck`
  в `failed`. Это ниже порога сборки Next (3 ГБ) — перед деплоем чистить
  journal и старые артефакты.
- Скиллы OpenCode и Superpowers подключены junction'ами: обновление источника
  видно сразу, но при удалении кеша OpenCode ссылки станут битыми —
  перезапустить `link-skills.ps1`.
