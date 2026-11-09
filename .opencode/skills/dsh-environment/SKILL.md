---
name: dsh-environment
description: Устройство рабочей среды DeepSeek Harness для CollegeLMS — профиль desktop, bundle dsh-collegelms, MCP-серверы, политика моделей субагентов, каталоги скиллов, скрипты секретов и автоматизации; как добавить MCP/скилл и как проверить среду.
---

# Среда DeepSeek Harness

## Состав

| Компонент | Где | Что даёт |
|---|---|---|
| Профиль `desktop` | `~/.dsh/profiles/desktop` | GUI-профиль, правится только через `plugin_manager` |
| Bundle `dsh-collegelms` | `tools/dsh/collegelms-bundle` | MCP + политика моделей субагентов |
| Скиллы проекта | `.opencode/skills` | 20+ проектных скиллов, видны и OpenCode, и DSH |
| Junction `.dsh/skills` | `.dsh/skills → .opencode/skills` | проектное обнаружение скиллов в DSH |
| Скиллы OpenCode/Superpowers | `~/.dsh/skills` (44 junction'а) | глобальные скиллы, включая planning/debugging/TDD |
| Секреты | `scripts/dsh/sync-secrets.ps1` | `.env` из GitHub-репозитория `secrets` |

## MCP-серверы

После установки bundle доступны (проверено: 77 инструментов):

- `mcp__github__*` — issues, PR, Actions, код, релизы (46 инструментов);
- `mcp__context7__*` — документация библиотек (2);
- `mcp__ms_learn__*` — Microsoft Learn (3);
- `mcp__playwright__*` — браузер и E2E (26).

Правила:

- Инструменты `mcp__*` видны агентам, созданным после установки/перезагрузки;
  в уже открытой сессии каталог может обновиться не сразу.
- GitHub-токен не лежит в конфиге: `scripts/dsh/github-mcp.cmd` берёт его
  через `gh auth token`.
- Недоступный сервер не валит сессию (`failOnStartupError: false`), но его
  инструментов не будет. Проверка: `gh auth status`, наличие
  `C:\Users\asv\.config\opencode\bin\github-mcp-server.exe`,
  `C:\Program Files\nodejs\node.exe`, `@playwright/mcp`.
- Добавление сервера: новый `insert`-ряд в
  `tools/dsh/collegelms-bundle/cordis.patch.yml` → переустановка bundle через
  `plugin_manager` → **новая** сессия. Вручную `cordis.patch.yml` профиля не
  редактируй.

## Модели субагентов

Политика разрешает в **новых** сессиях: `deepseek-v4.1-flash`,
`kimi-k2.7-code`, `kimi-k3`, `glm-5.3-flash`, `gpt-6-luna`, `qwen3.8-flash`
(все `opencode-go`). В текущей сессии субагент наследует модель родителя.

- Перед делегацией: `list_subagent_models`.
- Модель передаётся в `subagent` полями `provider` + `model` (и при
  необходимости `reasoning_effort`).
- Раскладка ролей — в скилле `collegelms-roles`.
- `kimi-k3.1-pro` и `gpt-6.1-sol` из старого `AGENTS.md` в каталоге
  `opencode-go` отсутствуют; используй `kimi-k3` и `gpt-6-luna`, пока они не
  появятся.

## Добавление скилла

1. Создай `.opencode/skills/<kebab-name>/SKILL.md`.
2. Frontmatter: `name` (kebab-case, совпадает с папкой) и `description`
   (одно предложение с триггером «когда использовать»).
3. Тело — до 8192 символов, иначе DSH обрежет результат.
4. Скилл появится в каталоге без перезапуска (наблюдение за каталогом).
5. Для глобального скилла — `~/.dsh/skills/<name>` (junction или копия).

## Секреты

- `pwsh -File scripts/dsh/sync-secrets.ps1` — собирает `.env` из
  `secrets/.env` + `secrets/collegelms.env`, сохраняя локальные ключи
  (`OPENCODE_*`, `AGENTBRIDGE_PORT`). Значения не печатает, в git не пишет.
- `.env` в `.gitignore`; `.env.bak-*` не создавать.
- Токен GitHub — в keyring `gh`; в конфиги и чат не копировать.
- Прод-значения (`collegelms.prod.env`) локально не используются.

## Автоматизации

Задачи расписания DSH создаются через `schedule_create` и доставляются в
сессии, пока хост запущен. Текущий набор — в `docs/agents/dsh-environment.md`:
утренний health-check VPS, вечерняя проверка CI CollegeLMS, недельная проверка
учебных материалов, месячная гигиена секретов и квот.

## Проверка среды

1. `plugin_manager` → `list_bundles`: `dsh-collegelms` включён.
2. Новый субагент перечисляет `mcp__*` — должны быть 4 сервера.
3. `list_subagent_models` в новой сессии — шесть моделей.
4. Каталог скиллов содержит `collegelms-roles`, `course-materials`,
   `dsh-environment`, `vps-bb-status`.
5. `pwsh -File scripts/dsh/sync-secrets.ps1` — 29 ключей из secrets, 0 ошибок.

## Ограничения

- Профиль `desktop` монопольно управляется Electron: CLI `dsh --profile
  desktop` не годится, только `plugin_manager`.
- Bundle установлен ссылкой на каталог репозитория: правки `cordis.patch.yml`
  требуют переустановки/перезапуска, чтобы применились.
- Пути к `node.exe`, `@playwright/mcp`, `github-mcp-server.exe` привязаны к
  этой машине.
- 77 MCP-инструментов + 80 скиллов заметно расходуют контекст; при работе на
  слабых моделях отключай лишние серверы точечно.
