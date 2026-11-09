# Скрипты среды DeepSeek Harness

| Скрипт | Назначение |
|---|---|
| `setup.ps1` | Разовая идемпотентная настройка: junction скиллов, глобальные скиллы, `.env`, проверка зависимостей. |
| `link-skills.ps1` | Junction'ы глобальных скиллов OpenCode и Superpowers в `~/.dsh/skills`. |
| `sync-secrets.ps1` | Сборка `.env` из `secrets/.env` + `secrets/collegelms.env` с сохранением локальных ключей. |
| `github-mcp.cmd` | Запуск GitHub MCP server с токеном из `gh auth token` (без секрета в конфиге). |

## Порядок

```powershell
pwsh -File scripts/dsh/setup.ps1
```

Затем установить bundle `tools/dsh/collegelms-bundle` через `plugin_manager`
(`install_bundle`, target — абсолютный путь) и открыть новую сессию.

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
