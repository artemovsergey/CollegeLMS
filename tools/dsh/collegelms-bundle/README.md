# dsh-collegelms

Конфигурационный bundle DeepSeek Harness для работы над CollegeLMS.

## Что делает

| Слой | Что добавляет |
|---|---|
| `subagent-model-selection-settings` | Разрешает субагентам выбирать модели `opencode-go` из белого списка: `deepseek-v4.1-flash`, `kimi-k2.7-code`, `kimi-k3`, `glm-5.3-flash`, `gpt-6-luna`, `qwen3.8-flash`. Без этого субагент может работать только на модели родителя. |
| `mcp-github` | GitHub MCP: issues, PR, Actions, поиск по коду. Токен читается `scripts/dsh/github-mcp.cmd` из keyring `gh`, в конфиг не попадает. |
| `mcp-context7` | Context7 MCP: актуальная документация библиотек. |
| `mcp-ms-learn` | Microsoft Learn MCP: официальная документация .NET, ASP.NET Core, EF Core. |
| `mcp-playwright` | Playwright MCP: браузер, проверка UI и E2E. |

Серверы стартуют с `failOnStartupError: false`: недоступный MCP не мешает запуску
сессии, но и не даёт инструментов. После изменения конфигурации проверяйте
инструменты `mcp__<server>__*`.

## Требования

- GitHub CLI `gh` — авторизован (`gh auth status`).
- `github-mcp-server.exe` — `C:\Users\asv\.config\opencode\bin\github-mcp-server.exe`.
- Node.js и `@playwright/mcp` — `C:\Users\asv\AppData\Roaming\npm\node_modules\@playwright\mcp`.
- Сеть до `mcp.context7.com` и `learn.microsoft.com`.

## Установка

Устанавливается через `plugin_manager` (`action: install_bundle`, `target` —
абсолютный путь к этой папке). Вручную `package.json` профиля и
`cordis.patch.yml` не редактируются.

## Проверка

1. `plugin_manager` → `list_bundles` → `dsh-collegelms` включён.
2. В новом сообщении доступны `mcp__github__*`, `mcp__context7__*`,
   `mcp__ms_learn__*`, `mcp__playwright__*`.
3. `list_subagent_models` возвращает все шесть моделей — только в **новой**
   сессии: политика моделей фиксируется при создании сессии.

## Откат

`plugin_manager` → `remove_bundle` → `dsh-collegelms`. MCP-серверы и политика
моделей исчезнут; остальная конфигурация профиля не меняется.

## Ограничения

- Пути к `node.exe`, `@playwright/mcp` и `github-mcp-server.exe` привязаны к
  этой машине. При переносе поправьте `cordis.patch.yml`.
- `context7` и `ms_learn` — внешние сервисы; при недоступности сети их
  инструменты не появятся, остальные серверы продолжат работать.
