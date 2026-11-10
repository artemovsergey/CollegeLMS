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

Затем скопировать актуальный конфиг профиля из репозитория `secrets`:

```powershell
Copy-Item <путь-к-secrets>\dsh.patch.yml `
  "$env:USERPROFILE\.dsh\profiles\desktop\cordis.patch.yml"
```

И открыть **новую** сессию: политика моделей и права фиксируются при создании.
Bundle в профиле не используется — все MCP-серверы и настройки находятся в
этом одном `cordis.patch.yml`.

## Конфиг профиля

- Модель — `opencode-go/deepseek-v4.1-flash`, права — `danger-full-access` +
  `approval: never`, MCP — `github`, `context7`, `ms_learn`, `playwright`.
- Секретов в конфиге нет: `OPENCODE_GO_API_KEY` — в credential store DSH,
  GitHub-токен — в keyring `gh`.
- Проектные скиллы подключаются junction'ом `.dsh/skills` →
  `~/.dsh/local/collegelms-skills` (или `.opencode/skills` в рабочей копии).
  `customSkillDirs` в профиле пресетами не наследуется.

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
- Скрипты не меняют профиль DSH: конфиг копируется из `secrets` отдельно.
