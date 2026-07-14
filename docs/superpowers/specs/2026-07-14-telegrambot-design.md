# Design: Telegram Bot (CollegeLMS.Bot.Telegram)

## Обзор

Telegram-бот с полным функционалом OpenCode TUI: чат с агентом, файловые операции, команды, MCP — через OpenCode HTTP API. Бот — прослойка между Telegram и OpenCode сервером (`localhost:4096`).

## Архитектура проектов

```
CollegeLMS.Bot/                    # Class library — shared logic
  OpenCodeClient.cs                # HTTP-клиент к OpenCode API
  SessionManager.cs               # chatId ↔ sessionId (in-memory + Redis)
  EventHandlerService.cs          # SSE /event → callback IBotPlatform
  IBotPlatform.cs                  # Абстракция платформы
  Models/                          # DTO для OpenCode API

CollegeLMS.Bot.Telegram/          # Executable — Telegram
  TelegramBotService.cs           # Telegram.Bot + long polling
  TelegramMessageFormatter.cs     # Markdown → Telegram HTML/MarkdownV2
  Program.cs                      # Host builder, DI
  appsettings.json
```

## Компоненты

### OpenCodeClient

HTTP-клиент для OpenCode REST API (`localhost:4096`).

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| `CreateSessionAsync` | `POST /session` | Создать сессию |
| `GetSessionAsync` | `GET /session/:id` | Получить сессию |
| `SendMessageAsync` | `POST /session/:id/prompt_async` | Отправить сообщение асинхронно |
| `GetMessagesAsync` | `GET /session/:id/message` | Список сообщений |
| `ExecuteCommandAsync` | `POST /session/:id/command` | Выполнить команду |
| `ReadFileAsync` | `GET /file/content?path=` | Прочитать файл |
| `ListFilesAsync` | `GET /file?path=` | Список файлов |
| `SearchFilesAsync` | `GET /find/file?query=` | Поиск файлов |
| `AbortSessionAsync` | `POST /session/:id/abort` | Прервать выполнение |
| `GetAgentAsync` | `GET /agent` | Список агентов |
| `GetEventsAsync` | `GET /event` | SSE-стрим событий |

### SessionManager

- In-memory `ConcurrentDictionary<long, string>` (chatId → sessionId)
- При старте бота: использует OpenCode сессию для каждого чата
- Сессия живёт пока не вызвана `/new` или не удалена в OpenCode

### EventHandlerService

- Подключается к `GET /event` (SSE)
- Фильтрует события по `sessionID`
- Вызывает `IBotPlatform.SendOrEditMessageAsync()` для стриминга текста
- При tool calls (permission requests) → `IBotPlatform.RequestPermissionAsync()`

### IBotPlatform

```csharp
interface IBotPlatform
{
    Task SendMessageAsync(string chatId, string text);
    Task EditMessageAsync(string chatId, int messageId, string text);
    Task RequestPermissionAsync(string chatId, string permissionId, string description);
    Task SendFileListAsync(string chatId, IEnumerable<FileNode> files);
}
```

### TelegramBotService

- `BackgroundService` с long polling через `Telegram.Bot`
- Регистрирует handler на `Message` и `CallbackQuery`
- Маппит Telegram chatId (`long`) в строку для `IBotPlatform`
- Обрабатывает `/` команды: `/new`, `/files`, `/help`, `/undo`

## Flow сообщения

```
Пользователь → Telegram → BotService → SessionManager → OpenCodeClient.SendMessageAsync()
                                                                  ↓
OpenCode API ─→ 204 (prompt_async) ─→ EventHandlerService (SSE /event)
                                               ↓
                                    Новый текст? → TelegramBotService.EditMessageAsync()
                                    Tool call? → TelegramBotService.InlineKeyboard()
                                    Завершено → финальное сообщение
```

### Обработка разрешений (tool calls)

Когда OpenCode запрашивает разрешение (например, выполнить команду):
1. `EventHandlerService` получает событие `permission.request`
2. TelegramBotService отправляет сообщение с inline keyboard: `✅ Разрешить` / `❌ Запретить`
3. Пользователь нажимает кнопку
4. Bot вызывает `POST /session/:id/permissions/:permissionID`
5. OpenCode продолжает выполнение

## Форматирование ответов

- **HTML** парсинг в Telegram (не MarkdownV2 — экранирование сложнее)
- Код: `<code>`, блоки: `<pre>`
- Диффы: `<pre>` с подсветкой строк
- Максимум 4096 символов на сообщение (Telegram лимит) — разбивать на части
- Стриминг: одно сообщение, редактируется с новым текстом (пока не превысит лимит → новое сообщение)

## Команды Telegram

| Команда | Описание |
|---------|----------|
| `/start` | Приветствие, создание сессии |
| `/new` | Новая сессия (сброс контекста) |
| `/files` | Просмотр файлов проекта |
| `/read <path>` | Чтение файла |
| `/search <query>` | Поиск по файлам |
| `/undo` | Откат последнего изменения |
| `/help` | Справка |
| `/cancel` | Прервать текущий запрос |

Без команды — любое сообщение отправляется агенту как промпт.

## Конфигурация (appsettings.json)

```json
{
  "OpenCode": {
    "ServerUrl": "http://host.docker.internal:4096",
    "Password": "",
    "ProjectDirectory": "/home/user1/CollegeLMS"
  },
  "Telegram": {
    "BotToken": "${BOT_TOKEN}"
  }
}
```

## Деплой (docker-compose.yml)

```yaml
telegrambot:
  build:
    context: .
    dockerfile: CollegeLMS.Bot.Telegram/Dockerfile
  environment:
    - BOT_TOKEN=${TELEGRAM_BOT_TOKEN}
    - OPENCODE_URL=http://host.docker.internal:4096
  extra_hosts:
    - "host.docker.internal:172.18.0.1"
  profiles:
    - bot
```

## Что не портируем из TUI

- Визуальный редактор кода (бесполезен в Telegram)
- Темы, keybinds, настройки UI
- Превью изображений (кроме ссылок)
- Интерактивный diff (только текстом)

## Ограничения

- **Telegram лимит 4096 символов** на сообщение — длинные ответы разбиваются
- **WebSocket не поддерживается** OpenCode API — используем SSE `/event`
- **Tool calls требуют подтверждения** — inline keyboard
- **Нет поддержки файлов >20MB** в Telegram

