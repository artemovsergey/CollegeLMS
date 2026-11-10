---
name: dsh-environment
description: Устройство рабочей среды DeepSeek Harness для CollegeLMS — единый cordis.patch.yml, MCP-серверы, политика моделей субагентов, скиллы через junction, секреты и локальные утилиты; как добавить MCP/скилл, проверить среду и перенести конфиг.
---

# Среда DeepSeek Harness

## Состав

| Компонент | Где | Что даёт |
|---|---|---|
| Профиль `desktop` | `~/.dsh/profiles/desktop` | GUI-профиль DSH |
| Единый конфиг | `~/.dsh/profiles/desktop/cordis.patch.yml` (копия — `secrets/dsh.patch.yml`) | модель, права, MCP, модели субагентов |
| Скиллы проекта | `~/.dsh/local/collegelms-skills` + junction `.dsh/skills` | 21 проектный скилл |
| Скиллы OpenCode/Superpowers | `~/.dsh/skills` (44 junction'а) | planning/debugging/TDD и др. |
| Секреты | `scripts/dsh/sync-secrets.ps1` | `.env` из репозитория `secrets` |

Bundle `dsh-collegelms` больше не используется, отдельных код-плагинов нет.
`dsh-our-free-model` удалён; провайдер — только `opencode-go`.
Права по умолчанию — `danger-full-access` + `approval: never`.

## MCP-серверы

В едином конфиге подключены (проверено: 77 инструментов):

| Сервер | Транспорт | Инструментов |
|---|---|---|
| `github` | stdio через `scripts/dsh/github-mcp.cmd` | 46 |
| `playwright` | stdio, `node` + `@playwright/mcp` | 26 |
| `ms_learn` | streamable-http | 3 |
| `context7` | streamable-http | 2 |

- Инструменты видны агентам, созданным после загрузки конфига; в открытой
  сессии каталог может обновиться не сразу.
- GitHub-токен не в конфиге: обёртка берёт его из `gh auth token`.
- Недоступный сервер не валит сессию, но его инструментов не будет.
- Добавление сервера: новый `insert`-ряд в
  `~/.dsh/profiles/desktop/cordis.patch.yml` → HMR или перезапуск DSH →
  **новая** сессия. Bundle и `plugin_manager` для этого не нужны.

## Модели субагентов

Политика разрешает в **новых** сессиях: `deepseek-v4.1-flash`,
`kimi-k2.7-code`, `kimi-k3`, `glm-5.3-flash`, `gpt-6-luna`, `qwen3.8-flash`
(все `opencode-go`). В текущей сессии субагент наследует модель родителя.

- Перед делегацией: `list_subagent_models`.
- Модель передаётся в `subagent` полями `provider` + `model`.
- Раскладка ролей — в скилле `collegelms-roles`.
- `kimi-k3.1-pro` и `gpt-6.1-sol` в каталоге отсутствуют; используй `kimi-k3`
  и `gpt-6-luna`, пока они не появятся.

## Скиллы

- Проектные: `.opencode/skills` (21), DSH видит через junction
  `.dsh/skills` → `~/.dsh/local/collegelms-skills`.
- `customSkillDirs` в профиле **не наследуется пресетами агентов**, поэтому
  без junction проектные скиллы не видны.
- Глобальные: `~/.dsh/skills` (44 junction'а на `~/.config/opencode/skills` и
  Superpowers), создаются `scripts/dsh/link-skills.ps1`.
- Новый скилл: `.opencode/skills/<kebab-name>/SKILL.md` с frontmatter `name` и
  `description`; тело до 8192 символов. Скилл появляется без перезапуска.

## Секреты

- `pwsh -File scripts/dsh/sync-secrets.ps1` собирает `.env` из
  `secrets/.env` + `secrets/collegelms.env`, сохраняя локальные ключи
  (`OPENCODE_*`, `AGENTBRIDGE_PORT`). Значения не печатает.
- `.env` в `.gitignore`; `.env.bak-*` не создавать.
- `OPENCODE_GO_API_KEY` — в credential store DSH (Settings → Models).
- GitHub-токен — в keyring `gh`; в конфиг и чат не копировать.
- Прод-значения (`collegelms.prod.env`) локально не используются.

## Локальные утилиты

- **DSH Session Analytics** — `scripts/dsh/session-analytics.mjs`, задача
  планировщика Windows каждые 10 минут; отчёт —
  `~/.dsh/session-analytics/latest.md`.
- **Радар плагинов DSH** — `~/.dsh/plugin-radar`, еженедельная задача DSH;
  отчёт в `~/.dsh/plugin-radar/reports/`.
- Это не часть профиля DSH и не меняет конфиг.

## Проверка среды

1. В `cordis.patch.yml` есть `mcp-github`, `mcp-context7`, `mcp-ms-learn`,
   `mcp-playwright` и `subagent-model-selection-settings`.
2. Новый субагент перечисляет `mcp__*` — 4 сервера, 77 инструментов.
3. `list_subagent_models` в новой сессии — 6 моделей.
4. Каталог скиллов содержит `collegelms-roles`, `course-materials`,
   `dsh-environment`, `vps-bb-status`.
5. `pwsh -File scripts/dsh/sync-secrets.ps1` — 29 ключей из secrets, 0 ошибок.

## Перенос на другую машину

1. Скопировать `secrets/dsh.patch.yml` в
   `%USERPROFILE%\.dsh\profiles\desktop\cordis.patch.yml`.
2. Скопировать `dsh/github-mcp.cmd` в
   `%USERPROFILE%\.dsh\local\scripts\dsh\github-mcp.cmd`.
3. Установить DSH, Node.js, `gh`, `@playwright/mcp`,
   `github-mcp-server.exe`; выполнить `gh auth login`.
4. Добавить `OPENCODE_GO_API_KEY` в Settings → Models.
5. Скопировать `.opencode/skills` в `~/.dsh/local/collegelms-skills` и создать
   junction `.dsh/skills` в проекте.
6. Открыть новую сессию.

## Ограничения

- Профиль `desktop` управляется Electron: CLI `dsh --profile desktop` не
  годится. Конфиг правится в `cordis.patch.yml`, изменения подхватывает HMR.
- Конфиг рассчитан на Windows (`cmd.exe`, `%USERPROFILE%`, `%APPDATA%`).
- 77 MCP-инструментов и ~80 скиллов заметно расходуют контекст; лишние
  серверы отключай точечно.
- Права `danger-full-access`: подтверждений на действия нет — это осознанный
  выбор пользователя.
