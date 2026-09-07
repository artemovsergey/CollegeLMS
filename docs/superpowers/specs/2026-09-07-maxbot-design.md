# MaxBot — дизайн-спецификация

**Дата:** 2026-09-07
**Статус:** утверждено пользователем
**Цель:** довести WIP-бота `CollegeLMS.MaxBot` (мессенджер MAX, бывший ICQ) до работоспособного состояния и задеплоить на VPS в compose вместе с API.

## Контекст

WIP-бот написан на VPS в ветке `feature/schedule-views` (незакоммиченные файлы + untracked `CollegeLMS.MaxBot/`), состоит из:

- `Bot/MaxBotService.cs` — BackgroundService: цикл `/updates`, команды, callback-кнопки, пагинация групп/преподавателей
- `Clients/MaxApiClient.cs` — клиент платформы MAX
- `Clients/CollegeLmsApiClient.cs` (+`CollegeLmsApiDtos.cs`) — клиент `/api/schedule`, `/api/groups`, `/api/teachers`
- `Services/MessageFormatter.cs`, `Data/MaxBotDbContext.cs` (+ EF config), `Models/`

WIP не соответствует реальному API платформы MAX и содержат критические баги. Перед реализацией контракт API платформы сверен с официальными типами SDK `max-messenger/max-bot-api-client-ts` (main, `src/core/network/api/modules/*/types.ts` и `src/core/network/api/types/*.ts`).

## Решения (одобрены пользователем)

| Вопрос | Решение |
|--------|---------|
| Подход | Доделать WIP (не переписывать) |
| БД настроек | Отдельная БД `collegelms_maxbot` |
| Уведомления 7:30 | Включить в scope и починить |

## Подтверждённый контракт MAX API

Базовый URL: `https://platform-api2.max.ru`, заголовок `Authorization: <token>`. Ответы-ошибки: `{ success: false, message }`, успех: `{ success: true }` (`ActionResponse`).

| Метод | Формат | Комментарий |
|-------|--------|-------------|
| `GET /me` | — | `BotInfo` (User + `commands?`) |
| `GET /updates` | query: `marker`, `limit`, `timeout`, `types` | ответ `{ updates: Update[], marker: number }`; при marker=null возвращается только последнее событие |
| `POST /messages` | query: `chat_id`, `user_id`, `disable_link_preview`; body: `{ text, format: markdown\|html, notify, ... }` | ответ `{ message }` |
| `POST /answers` | query: `callback_id`; body: `{ message?: { text, format, ... } }` | ack callback / смена клавиатуры |
| `PATCH /me/commands` | body: `{ commands: [{ name, description }] }` | ответ `{ commands }` |
| `POST /subscriptions` | body: `{ url, update_types, secret }` | webhook (в проде, отдельная задача) |

### Update (дискриминирующий `update_type`)

Каждое событие: `{ update_type, timestamp, ...payload }`. Нужные боту:

- `bot_started` — `{ chat_id, user, payload? }`
- `message_created` — `{ message: { sender?, recipient: { chat_id, chat_type, user_id, post_id }, body: { mid, seq, text }, ... } }`
- `message_callback` — `{ callback: { timestamp, callback_id, payload?, user }, message? }`

### Кнопки

`CallbackButton = { type: 'callback', text, payload }`, `LinkButton = { type: 'link', text, url }`. Пары `type/payload` из `callback.payload` (не из attachments).

## Баги WIP под выбранный подход

| # | Баг | Фикс |
|---|-----|------|
| B1 | `GetUpdatesAsync(null)` в цикле каждый 1 с → повторная обработка одного события | marker хранить в поле, передавать между вызовами; long-poll `timeout=30` |
| B2 | `BotCommand.Command` → на проводе `{command}` вместо `{name}` | переименовать в `Name` |
| B3 | `AnswerCallbackAsync` шлёт `callback_id` в body | в query `?callback_id=...`; body `{ message: { text, format } }` |
| B4 | `MaxUpdate` не парсит `callback` (`message_callback` без payload; payload из attachments) | модель с `Callback { callback_id, payload, user }`; читать `callback.payload` |
| B5 | Сдвиг дня: `(int)DateTime.Now.DayOfWeek + 1` (Пн→2, Вс→7) | MSK-сегодня → API-конвенция: `day = (int)DayOfWeek`, Sun→0 (выходной, API не дёргать) |
| B6 | `/week` и уведомления без `week` → показываются пары всех недель | в `CollegeLmsApiClient` добавить `week=` (вычисление из SEMESTER_START, см. ниже) |
| B7 | `TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")` падает на Linux | `Europe/Moscow` из конфига; фолбэк на Windows-id |
| B8 | `EnsureCreated` на существующей БД `collegelms` не создаст `user_settings` | connection string `Database=collegelms_maxbot` |
| B9 | Junk: `excel_lin_vreg.dat`, `run.sh.1`, каталог `{Bot,Clients,Services,Data` | удалить |
| B10 | `tsconfig.json`: include `/tmp/opencode/next-build/.next/types` | откатить include |
| B11 | Notify в 7:30 может пропустить окно и дублировать | пересчёт по расписанию (target time), in-memory guard «отправлено за день» |

Оставить как есть: `[AllowAnonymous]` на GET Group/Teacher/Schedule (уже в 37dac4e diff), inline_keyboard через `attachments`, SSL-bypass российского CA (задокументировать в README).

## Архитектура решения

- **`MaxUpdate` + вложенные модели** приведены к контракту; `MaxBody.CallbackData`/`MaxAttachmentPayload.callback_*` удаляются, payload живёт в `Callback.Payload`.
- **Маркер-цикл**: `MaxApiClient.GetUpdatesAsync(long? marker, int timeoutS, ct)`; `MaxBotService` хранит `_lastMarker` (начинается с null — первичный запрос возвращает последнее событие), прогоняет updates, сохраняет новый marker.
- **Дни**: конвенция **1=Пн … 6=Сб**, `ParseDayOfWeek` без изменений; хелпер `MskClock.Now()` → `DayOfWeekForApi()`: `(int)now.DayOfWeek` (Sun=0 → «Сегодня выходной»), API `dayOfWeek=1..6`.
- **Недели**: C#-порт `getMondayOfWeek`/`getCurrentWeek` из `ScheduleTable.tsx`: `SEMESTER_START = 2026-09-01` (MSK), `week = max(1, floor((monday(now) - monday(start)) / 7 дней) + 1)`. Используется в `/week` (добавлять `&week=N`) и уведомлениях.
- **Notifier**: расчёт следующего target `сегодня|завтра 07:30 MSK`, при наступлении — отправка всем подписчикам (`NotifyEnabled && NotifyDays.Contains(day)`) и guard-дата в памяти; логика в `MaxBot:NotifyHour/Minute`.
- **Конфиг**: `MaxBot:TimeZone = Europe/Moscow`, `MaxBot:AccessToken`/`MAX_BOT_TOKEN` из env; `CollegeLmsApi:BaseUrl = http://api:8080` в compose.
- **БД**: `ConnectionStrings:DefaultConnection` → `Database=collegelms_maxbot` (EnsureCreated создаст БД и таблицу `user_settings`); идентичные настройки в compose.

## Файлы

### Изменяемые
- `CollegeLMS.MaxBot/Models/Max/MaxUpdate.cs` — Callback, чистка старых полей
- `CollegeLMS.MaxBot/Models/Max/BotCommand.cs` — `Command` → `Name`
- `CollegeLMS.MaxBot/Clients/MaxApiClient.cs` — marker/timeout, `/answers`, `/me/commands`
- `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs` — параметр `week`
- `CollegeLMS.MaxBot/Bot/MaxBotService.cs` — маркер-цикл, day/week, callback payload, guard notify
- `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs` — таймзона, день, week, target-schedule
- `CollegeLMS.MaxBot/Services/MessageFormatter.cs` — день «сегодня» (без сдвига)
- `CollegeLMS.MaxBot/Program.cs` — ServiceCollectionExtensions-Bootstrap (без изменений логики)
- `CollegeLMS.MaxBot/appsettings.json` — `TimeZone`, БД
- `docker-compose.yml` — `MaxBot__TimeZone: Europe/Moscow`, `ConnectionStrings__DefaultConnection` → `collegelms_maxbot`
- `CollegeLMS.MaxBot/README.md` — инструкция, токен, SSL-заметка

### Новые
- `CollegeLMS.MaxBot/Services/MskClock.cs` — таймзона MSK, `Today`, `Now`, `DayOfWeekForApi`, `CurrentStudyWeek`
- `CollegeLMS.MaxBot/Services/StudyWeek.cs` — порт формул недели (или частью MskClock)

### Удалить
- `excel_lin_vreg.dat`, `run.sh.1`, каталог `{Bot,Clients,Services,Data` (пустой)

### Откатить
- `CollegeLMS.Next/tsconfig.json` (include `/tmp/opencode/next-build`)

## Команды проверки

```
dotnet build CollegeLMS.MaxBot
dotnet csharpier format CollegeLMS.MaxBot --check
docker compose --profile max-bot build
docker compose --profile max-bot up -d   (локально: БД postgres для collegelms_maxbot)
```

## Связи сервисов

```
MaxApiClient ──(platform-api2.max.ru)── MAX Bot API
CollegeLmsApiClient ──(http://api:8080)── CollegeLMS API (/api/schedule, /api/groups, /api/teachers)
MaxBotDbContext ──(PostgreSQL)── collegelms_maxbot (user_settings)
MaxBotService (BackgroundService, long-poll)
ScheduleNotifier (BackgroundService, 07:30 MSK)
```

## Вне скоупа (будущие задачи)

- Webhook-подписки `POST /subscriptions` на nginx (prod) вместо long polling
- Миграции EF вместо `EnsureCreated`
- Логирование в Serilog в Docker
- MinIO для файлов (общее с API)