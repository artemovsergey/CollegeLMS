# Скрипты среды DeepSeek Harness

| Скрипт | Назначение |
|---|---|
| `setup.ps1` | Разовая идемпотентная настройка: junction скиллов, глобальные скиллы, `.env`, проверка зависимостей. |
| `link-skills.ps1` | Junction'ы глобальных скиллов OpenCode и Superpowers в `~/.dsh/skills`. |
| `sync-secrets.ps1` | Сборка `.env` из `secrets/.env` + `secrets/collegelms.env` с сохранением локальных ключей. |
| `github-mcp.cmd` | Запуск GitHub MCP server с токеном из `gh auth token` (без секрета в конфиге). |
| `session-analytics.mjs` | Анализ сессий DSH: модели, токены, скиллы, MCP, плагины, субагенты. |
| `install-session-analytics.ps1` | Установка анализатора в `~/.dsh/session-analytics` и задача планировщика каждые 10 минут. |

## Порядок

```powershell
pwsh -File scripts/dsh/setup.ps1
```

Затем установить bundle `tools/dsh/collegelms-bundle` через `plugin_manager`
(`install_bundle`, target — абсолютный путь) и открыть новую сессию.

## Аналитика сессий

```powershell
pwsh -File scripts/dsh/install-session-analytics.ps1   # установка + задача каждые 10 мин
& "$env:USERPROFILE\.dsh\session-analytics\run.cmd"     # обновить отчёт сейчас
```

Отчёт — `~/.dsh/session-analytics/latest.md`; подробности — в
`~/.dsh/session-analytics/README.md`. Полный пересбор — `run.cmd --reindex`,
одна сессия — `run.cmd --session=<id>`.

## Требования

- Windows PowerShell 7+
- GitHub CLI `gh` — авторизован;
- Node.js (`C:\Program Files\nodejs\node.exe`);
- `github-mcp-server.exe` в `~/.config/opencode/bin/`;
- `@playwright/mcp` в глобальных npm-пакетах.

## Безопасность

- Значения секретов не печатаются и не коммитятся.
- `.env` остаётся в `.gitignore`.
- Скрипты не меняют профиль DSH и не устанавливают bundle — это делает
  `plugin_manager`.
