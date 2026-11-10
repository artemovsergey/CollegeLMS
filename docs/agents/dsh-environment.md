# Среда DeepSeek Harness для CollegeLMS

Дата проверки: 2026-11-10. Профиль `desktop`, хост Windows.

## Что сделано

| Область | Результат |
|---|---|
| Конфиг | Один `cordis.patch.yml` в профиле; копия — `secrets/dsh.patch.yml`. Bundle `dsh-collegelms` больше не используется. |
| Модели | `opencode-go`: `deepseek-v4.1-flash`, `kimi-k2.7-code`, `kimi-k3`, `glm-5.3-flash`, `gpt-6-luna`, `qwen3.8-flash`; выбор субагентом — в новых сессиях. |
| MCP | `github` (46), `playwright` (26), `ms_learn` (3), `context7` (2) — 77 инструментов, проверено субагентом. |
| Скиллы | 21 проектный + 29 глобальных OpenCode + 15 Superpowers; проектные — через junction `.dsh/skills`. |
| Агенты | Роли Backend/Frontend/Tester/DevOps/Docs — скилл `collegelms-roles`; именованных агентов в DSH нет. |
| Секреты | `.env` собирается из `secrets`; в конфиге секретов нет. |
| Утилиты | DSH Session Analytics (каждые 10 минут) и Radar плагинов (еженедельно). |
| Свои плагины | Не разрабатывались; код-плагинов и bundle в профиле нет. |

## Конфиг

`~/.dsh/profiles/desktop/cordis.patch.yml`, он же в secrets как `dsh.patch.yml`:

- модель по умолчанию — `opencode-go/deepseek-v4.1-flash`, `reasoningEffort: max`;
- `llm-pi-ai` → `opencode-go`, ключ `OPENCODE_GO_API_KEY`;
- права: `read-only`, `workspace-write`, `danger-full-access`; по умолчанию
  `danger-full-access` + `approval: never`;
- локаль — `en`;
- `subagent-model-selection-settings` — шесть разрешённых моделей;
- `insert` — четыре MCP-сервера.

Профиль без bundle: в `package.json` только `@deepseek-ai/dsh-base` и
`@deepseek-ai/dsh-web-app`. `dsh-our-free-model` удалён.

## MCP

| Сервер | Транспорт | Инструментов | Примечание |
|---|---|---|---|
| `github` | stdio, `cmd.exe` → `github-mcp.cmd` | 46 | токен из `gh auth token` |
| `playwright` | stdio, `node` + `@playwright/mcp` | 26 | `PLAYWRIGHT_BROWSERS_PATH=0` |
| `ms_learn` | streamable-http | 3 | `https://learn.microsoft.com/api/mcp` |
| `context7` | streamable-http | 2 | `https://mcp.context7.com/mcp` |

Все с `failOnStartupError: false`. Пути к `node.exe` и `@playwright/mcp`
считаются от `%ProgramFiles%` и `%APPDATA%`.

## Модели и роли

| Роль | Модель | Почему |
|---|---|---|
| Основная, тесты, рутина | `deepseek-v4.1-flash` | дешёвая, 1M контекста |
| Backend, сложный код | `kimi-k2.7-code` | сильный coding-профиль |
| Frontend, тяжёлый UI | `kimi-k3` | лучшее качество, дороже |
| DevOps, исследование | `glm-5.3-flash` | быстрая и дешёвая |
| Документы, материалы | `gpt-6-luna` | сильный текст |

`kimi-k3.1-pro` и `gpt-6.1-sol` в каталоге `opencode-go` отсутствуют.
Политика моделей фиксируется при создании сессии; в открытой сессии субагент
наследует модель родителя.

## Скиллы и секреты

- Проектные скиллы: `.opencode/skills` (21), DSH видит через junction
  `.dsh/skills` → `~/.dsh/local/collegelms-skills`.
- Глобальные: `~/.dsh/skills` (44 junction'а), создаются
  `scripts/dsh/link-skills.ps1`.
- `customSkillDirs` в профиле не наследуется пресетами агентов — нужен
  junction.
- `.env` собирает `scripts/dsh/sync-secrets.ps1` (29 ключей из secrets,
  локальные `OPENCODE_*`/`AGENTBRIDGE_PORT` сохраняются).
- GitHub-токен — в keyring `gh`, `OPENCODE_GO_API_KEY` — в credential store.

## Локальные утилиты

- **DSH Session Analytics** — `scripts/dsh/session-analytics.mjs`, задача
  планировщика Windows каждые 10 минут; отчёт
  `~/.dsh/session-analytics/latest.md`.
- **Radar плагинов DSH** — `~/.dsh/plugin-radar`, еженедельная задача DSH;
  отчёт в `~/.dsh/plugin-radar/reports/`.

Это вспомогательные скрипты, а не плагины профиля.

## Наработки OpenCode и bb

- OpenCode: 4 агента в `.opencode/agents/`, 19 проектных скиллов, глобальные
  скиллы и Superpowers; конфиг синхронизирован с secrets.
- bb 0.47.1: 99 тредов (89 opencode, 10 в error, 97 архивных), 39
  установленных плагинов и 5 своих (`vps-monitor`, `maxbot-stats`, `vpn`,
  `free-quota`, `copilot-credits`). Автоматизации: бэкап `bb.db`,
  deps-audit, обновление версий, vps-digest.
- Главный риск: ветка `feature/course-pipeline` репозитория `mdk0104_IB`
  (17 коммитов, `tools/coursekit`) не запушена и существует только в
  рабочем каталоге bb-треда `thr_4az9whr6t5`.

## Проверка

```powershell
# конфиг и MCP
# в cordis.patch.yml: mcp-* и subagent-model-selection-settings
subagent: «перечисли mcp__*»      # 4 сервера, 77 инструментов

# модели (в новой сессии)
list_subagent_models               # 6 моделей opencode-go

# секреты
pwsh -File scripts/dsh/sync-secrets.ps1   # 29 ключей, 0 ошибок
```

## Риски и ограничения

- 77 MCP-схем и ~80 скиллов расходуют контекст; лишние серверы отключать
  точечно.
- Пути к Node.js и Playwright привязаны к стандартной установке Windows.
- VPS: диск занят на 90% (около 3 ГБ свободно), load average 5–7,
  `bb-healthcheck` в `failed`; перед сборкой Next нужно освободить место.
- На default-ветке GitHub — 13 уязвимостей Dependabot (2 critical, 9 high,
  2 moderate).
- Права `danger-full-access`: подтверждений нет, включая запись вне проекта.
