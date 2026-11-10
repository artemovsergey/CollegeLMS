# Аналитика сессий DSH

Автоматический разбор каждой сессии DeepSeek Harness: модель, токены, скиллы,
MCP, плагины и юниты DSH.

## Что считается

| Раздел | Источник в сессии |
|--------|-------------------|
| Модель и токены | `assistant/message.data.usage`: вход без кэша, кэш-чтение, выход, reasoning, всего — по каждой модели |
| Ходы, шаги, ошибки | `turn/start`, `step/start`, `turn/end`, `assistant/attempt`, `llm/retry` |
| Инструменты и плагины | `tool/call` → юниты DSH (файлы, PowerShell, субагенты, расписание, `plugin_manager`, workflow, present, web, skill, MCP) |
| Скиллы | вызовы инструмента `skill` + косвенные чтения `SKILL.md` |
| MCP | `request/header.tools` (`mcp__server__tool`) — что было доступно; `tool/call` — что реально вызывалось |
| Плагины | операции `plugin_manager` (install/remove/list) с результатом |
| Субагенты | `subagent/descriptor`, `subagent/catalog`, `parentSession` дочерних сессий |
| Профиль | `~/.dsh/profiles/desktop`: бандлы, вставки конфига, MCP-серверы |

Копии сессий (DSH сохраняет дубли с суффиксом «(2)», а также снимки диалога)
показываются в таблице с пометкой `⧉`, но в суммах учитываются один раз.

## Запуск

```powershell
& "$env:USERPROFILE\.dsh\session-analytics\run.cmd"
```

| Команда | Что делает |
|---------|------------|
| `run.cmd` | Инкрементальный разбор + обновление `latest.md` |
| `run.cmd --reindex` | Полный пересбор всех сессий |
| `run.cmd --session=6ad92aa8` | Подробности одной сессии (id или префикс) |
| `run.cmd --since=7d` | Только сессии за последние 7 дней |
| `run.cmd --watch` | Следить за изменениями в реальном времени |
| `run.cmd --json` | Машиночитаемая сводка в stdout |
| `run.cmd --quiet` | Без вывода в консоль (для планировщика) |

`run.cmd` сам находит `node.exe` в рантайме DSH, если его нет в PATH.

## Файлы

| Файл | Назначение |
|------|------------|
| `session-analytics.mjs` | анализатор (единственный источник логики) |
| `run.cmd` | лаунчер с поиском node |
| `latest.md` | сводный отчёт по всем сессиям |
| `reports/YYYY-MM-DD.md` | снимок отчёта за день |
| `data/summary.json` | машиночитаемая сводка (модели, токены, MCP, юниты) |
| `data/sessions/<id>.json` | метрики отдельной сессии |
| `data/state.json` | инкрементальный кеш (`size` + `mtime`) |

## Расписание

Задача планировщика **«DSH Session Analytics»** — каждые 10 минут
(создаётся через `schtasks`, повышение прав не нужно). После входа в систему
первый прогон происходит в течение 10 минут.

Запуск скрытый: `powershell.exe -NoProfile -WindowStyle Hidden … run-background.ps1`.
Повторные запуски защищены локом `data/.lock`.

Проверить и запустить вручную:

```powershell
schtasks /Query /TN "DSH Session Analytics" /V /FO LIST
schtasks /Run   /TN "DSH Session Analytics"
```

Удалить автоматизацию:

```powershell
schtasks /Delete /TN "DSH Session Analytics" /F
Remove-Item "$env:USERPROFILE\.dsh\session-analytics" -Recurse -Force
```

## Как это устроено

Сессии лежат в
`~/.dsh/sessions/<workspace>/<session>/session.v4.jsonl.zstd` — это поток
zstd-кадров, по одному кадру на событие. Node распаковывает только первый кадр,
поэтому анализатор режет поток по magic-байтам `28 B5 2F FD` и распаковывает
кадры по отдельности. Недописанный последний кадр (сессия ещё идёт)
пропускается до следующего запуска.

Формат проверен на DSH 0.2.2-rc.1. При обновлении DSH, если изменится схема
событий, отчёт покажет меньше данных — запустите `run.cmd --reindex` и сверьте
`tool/result`/`assistant/message`.

## Приватность

Отчёты содержат заголовки сессий, пути и начала промптов. Они лежат только
локально в `~/.dsh/session-analytics` и не коммитятся в git.

## Обновление

Источник — `scripts/dsh/session-analytics.mjs` в репозитории CollegeLMS.
Обновить установленную копию и задачу:

```powershell
pwsh -File scripts/dsh/install-session-analytics.ps1
```
