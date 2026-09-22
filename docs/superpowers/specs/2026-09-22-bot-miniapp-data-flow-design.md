# Поток данных между ботом MAX и мини-приложением

Дата: 2026-09-22

## Контекст

После разделения бота и мини-приложения (спек `2026-09-22-bot-miniapp-split.md`)
бот отвечает только за уведомления и настройки, а мини-приложение — за
расписание, избранное и изменения. Проверка фактического состояния выявила
разрывы в связке бот ↔ мини-апп ↔ API:

- Бот **не открывает мини-приложение**: все кнопки — `callback`, `open_app`
  не используется, `MiniAppUrlBuilder.BuildStartPayload` не вызывается ни в
  одном продакшн-пути. В уведомлении об изменении ссылка уходит простым
  текстом (`MessageFormatter.cs:359`), а не кнопкой.
- Мини-приложение требует CRM-JWT из `localStorage`: `max-context.tsx`
  вызывает `GET /api/schedule/context` только при наличии токена, иначе
  `isAuthed=false` и пустой профиль. MAX-гость без CRM-сессии не видит ничего,
  вкладка «Журнал» недоступна.
- `initData` MAX нигде не проверяется — читается только `start_param`.
- Выбор группы/преподавателя в мини-приложении пишется только в
  `localStorage` и не доходит до бота.
- В `GET /api/schedule/context` и `GET /api/schedule/journal` личность
  определяется по CRM-записи (`Students`/`Teachers` по `userId`); у гостя
  такой записи нет → `Role="Other"`, журнал недоступен.

Пользовательские решения:

- Идентичность — MAX `initData`, подпись проверяет **API**; связка с
  CRM-аккаунтом не обязательна, без неё пользователь — гость на данных бота.
- Источник профиля (роль/группа/преподаватель) — бот-БД `user_settings`.
  Мини-приложение профиль только **читает**.
- Настройки уведомлений остаются только в боте (дублирование не вводим).
- Гость листает расписание без онбординга; на первом заходе текущий выбор
  (viewContext) пуст.
- Кнопка в уведомлении ведёт на **конкретный день** изменения.
- Контекст и журнал на API — **claims-aware**: при отсутствии CRM-записи
  личность берётся из подписанных claims JWT.

## Решение

```
Бот ──open_app(payload=route/date[/group|teacher])──▶ start_param ──▶ мини-апп
мини-апп ──POST /api/auth/max {initData}──▶ API
API ──GET /maxbot/internal/users/{maxUserId} (X-Internal-Secret)──▶ бот
API ──JWT(claims: role, groupId/teacherId, max_user_id)+profile──▶ мини-апп
мини-апп ──Bearer JWT──▶ /api/schedule/*, /context, /journal
API ──webhook /notify*──▶ бот ──уведомление + кнопка open_app──▶ MAX
```

### 1. Бот: реальные кнопки `open_app`

- `MaxBotOptions` дополняется:
  - `BotPublicName` — публичное имя бота для поля `web_app` (напр.
    `teacher_scc_bot`; в тестах уже используется как эталон);
  - `InternalSecret` — секрет внутреннего endpoint профиля.
- Главное меню (`MaxBotService.ShowMainMenuAsync`): кнопка
  `📱 Открыть расписание` — `Type="open_app"`, `WebApp=BotPublicName`,
  `Payload=MiniAppUrlBuilder.BuildStartPayload("today")`.
- Экран настроек (`ShowSettingsAsync`): кнопка `📱 Расписание в мини-приложении`
  с тем же payload.
- Уведомление об изменении: вместо текстовой ссылки — кнопка
  `📅 Открыть день` с `Payload=BuildStartPayload("day", date, groupId, teacherId)`
  (маркеры `g-`/`t-`).
- `MessageFormatter.FormatRevisionCard` (`:414`): убрать строку
  `✅ Применено: ...`.
- `ChangeNotifier.NotifyAsync`: отправлять дайджест через
  `SendInlineKeyboardAsync` (сейчас — `SendMessageAsync` без клавиатуры).
- `BuildStartPayload` перестаёт быть мёртвым кодом. Query-ссылка
  (`Build`) остаётся web-фолбэком.

### 2. Бот: внутренний endpoint профиля

- `GET /maxbot/internal/users/{maxUserId:long}` в `CollegeLMS.MaxBot/Program.cs`.
- Guard: заголовок `X-Internal-Secret`, сравнение constant-time с
  `MaxBotOptions.InternalSecret`; при несовпадении — 401.
- Чтение `UserSettings` по `MaxUserId`, резолв имён группа/преподаватель
  через существующий API-клиент бота.
- Ответ:

```json
{
  "found": true,
  "maxUserId": 123456789,
  "role": "student",
  "groupId": "…",
  "groupName": "ИС-21-1",
  "teacherId": null,
  "teacherName": null
}
```

- Записи нет → `200 { "found": false, "role": "student" }`.

### 3. API: `POST /api/auth/max`

- Анонимный эндпоинт. Тело: `{ "initData": "<строка WebAppData>" }`.
- Проверка подписи (алгоритм MAX, см. раздел ниже) на `MaxAuth:BotToken`.
- Из `initData` извлекаются `user.id` (long) и `start_param`.
- Профиль запрашивается у бота: `GET /maxbot/internal/users/{maxUserId}` с
  `X-Internal-Secret` (переиспользуем `MaxBotHttpClient`, база —
  `MaxBot:BaseUrl`).
- Токен сессии: `JwtTokenService.GenerateCustomToken(...)`. Так как гостю
  нужны доп. claims, метод расширяется необязательным параметром
  `IEnumerable<Claim>? extraClaims`.
  - `nameIdentifier` — стабильный синтетический Guid, выведенный из
    `maxUserId` (например, MD5/SHA над `"max:{id}"`, приведённый к формату
    Guid);
  - claims: `max_user_id`, `role`, и `groupId` **или** `teacherId`.
- Ответ:

```json
{
  "token": "<jwt>",
  "profile": {
    "maxUserId": 123456789,
    "fullName": "Иван Иванов",
    "role": "Student",
    "groupId": "…",
    "groupName": "ИС-21-1",
    "teacherId": null,
    "teacherName": null
  }
}
```

- Нет профиля у бота → `role: "Other"`, `token` всё равно выдаётся (гость
  смотрит анонимные разделы расписания).

Роли: бот хранит `user_settings.Role` в нижнем регистре (`student`/`teacher`),
а ответ `/api/auth/max` и мини-приложение используют `Student`/`Teacher`/`Other`
(тип `MaxRole`). API выполняет это отображение; claim `role` содержит
PascalCase-значение (`UserRole.GetRoles()`).

### 4. API: claims-aware контекст и журнал

- `ScheduleController.GetContext` (`:167`) и `GetJournal` (`:200`): если
  CRM-запись по `userId` не найдена, контекст строится из claims подписанного
  JWT (`role`, `groupId`/`teacherId`) — токен выдан API, значит доверенный.
- `GetJournal`: для гостя-преподавателя `effectiveTeacherId` берётся из
  claim `teacherId`, проверка несовпадения с CRM-контекстом пропускается.
- `ClaimsPrincipalExtensions`: хелперы чтения `groupId`, `teacherId`,
  `max_user_id`.
- Мини-приложение продолжает звать `/context` и `/journal` без изменений.

### 5. Мини-приложение

- `lib/max-context.tsx`, при монтировании:
  1. если есть `window.WebApp?.initData` → `POST /api/auth/max`; сохранить
     `token` в `localStorage`, заполнить `profile` из ответа,
     `isAuthed=true`;
  2. иначе — текущий путь: CRM-JWT из `localStorage` → `/api/schedule/context`.
- `MaxProfile`: заполнять `id` (= `maxUserId`) и `fullName`.
- `lib/api.ts`: при 401 внутри раздела `/max` **не** редиректить на `/login`
  (гость не должен выкидываться); редирект сохраняется для CRM-разделов.
- Deep-link через `start_param` не переписывается — теперь он реально
  приходит. UI настроек уведомлений не добавляем.

### 6. Вычистка

- Неиспользуемая таблица `bot_favorites` убирается из идемпотентного DDL в
  `CollegeLMS.MaxBot/Program.cs` (низкий приоритет, решение — по мере правки).
- `CollegeLMS.MaxBot/README.md:255-258` приводится в соответствие
  (сейчас ложно утверждает, что кнопки уже `open_app`).

## Алгоритм проверки `initData` (MAX)

Источник: `https://dev.max.ru/docs/webapps/validation`.

1. Значение для серверной проверки — строка `window.WebApp.initData`
   (URL-encoded `WebAppData`).
2. Разбить по `&` на пары `key=value`.
3. Убедиться, что ровно один `hash`; запомнить его и исключить из набора.
4. URL-декодировать значения.
5. Отсортировать пары по ключу (a→z).
6. `launch_params` = строки `key=value`, соединённые `\n` (0x0A).
7. `secret_key = HMAC-SHA256(key="WebAppData", message=BOT_TOKEN)`
   (в MAX порядок обратный телеграмному: ключ — `"WebAppData"`, сообщение —
   токен бота).
8. `signature = hex(HMAC-SHA256(key=secret_key, message=launch_params))`.
9. Сравнить `signature` с сохранённым `hash` (constant-time).
10. Проверить свежесть `auth_date` (не старше 1 часа) — защита от повторного
    использования.

`initDataUnsafe` для проверки не подходит — сервер его игнорирует.

## Конфигурация

| Слой | Ключ | Назначение |
|------|------|------------|
| Бот | `MaxBot:BotPublicName` (`MAX_BOT_PUBLIC_NAME`) | поле `web_app` у `open_app` |
| Бот | `MaxBot:InternalSecret` (`MAXBOT_INTERNAL_SECRET`) | guard `/maxbot/internal/*` |
| API | `MaxAuth:BotToken` (`MAX_BOT_TOKEN`) | проверка подписи `initData` |
| API | `MaxBot:BaseUrl` (есть) | запрос профиля у бота |
| API | `MaxBot:InternalSecret` (`MAXBOT_INTERNAL_SECRET`) | заголовок `X-Internal-Secret` |

Проброс — в `docker-compose*.yml` и `.github/workflows/deploy.yml`.

## Обработка ошибок

- Невалидный/просроченный `initData` → `401` с понятным кодом; мини-приложение
  остаётся гостем в режиме чтения (разделы расписания `[AllowAnonymous]`).
- Бот недоступен при запросе профиля → `200` с `role: "Other"`; расписание
  всё равно доступно.
- Записи `UserSettings` нет → `found: false`, гость.
- Гость-преподаватель без `teacherId` claim → журнал недоступен (как и сейчас
  без роли).

## Изменённые области

| Область | Файлы |
|---------|-------|
| Бот: кнопки и формат | `Bot/MaxBotService.cs`, `Services/MessageFormatter.cs`, `Services/ChangeNotifier.cs`, `Services/MiniAppUrlBuilder.cs`, `MaxBotOptions.cs` |
| Бот: внутренний endpoint | `CollegeLMS.MaxBot/Program.cs` |
| API: аутентификация MAX | новый `Controllers/MaxAuthController.cs`, новый `Services/MaxInitDataValidator.cs`, `Services/JwtTokenService.cs`, `Interfaces/ITokenService.cs`, `Services/MaxBotHttpClient.cs`, `Extensions/ServiceCollectionExtensions.cs`, `appsettings.json` |
| API: claims-aware | `Controllers/ScheduleController.cs`, `Services/ScheduleService.cs`, `Extensions/ClaimsPrincipalExtensions.cs` |
| Мини-апп | `lib/max-context.tsx`, `lib/api.ts`, `api/auth.ts` (новый) |
| E2E | `e2e/max-miniapp.spec.ts` |

## Тесты

- Backend xUnit:
  - валидация `initData` на эталонном векторе (подпись ок / подпись битая /
    старый `auth_date` / дубль `hash`);
  - выдача и разбор JWT с доп. claims;
  - guard `/maxbot/internal` (правильный/неправильный секрет);
  - `/context` и `/journal` для гостя по claims.
- Бот:
  - кнопка меню и кнопка уведомления — `open_app` с `web_app` и `payload`
    (`MaxApiKeyboardTests` уже покрывает сериализацию);
  - payload round-trip в `resolveMaxDeepLink` (уже есть).
- Frontend:
  - e2e мини-аппа: мок `/api/auth/max`, гость видит расписание, вкладка
    «Журнал» для роли Teacher.

## Проверки

- `dotnet build CollegeLMS.slnx`; таргетные тесты MaxBot и API.
- `cd CollegeLMS.Next && npm run build && npm run lint`.
- `npx playwright test e2e/max-miniapp.spec.ts`.
- Ручная проверка в MAX: кнопка меню открывает мини-апп; гость видит
  расписание без CRM-логина; уведомление ведёт на конкретный день.

## Риски и последствия

- Новый общий секрет `MAXBOT_INTERNAL_SECRET` нужно синхронизировать между
  ботом и API при деплое; при рассинхроне гости получат `role: "Other"`, но
  расписание останется доступным.
- Синтетический Guid вместо CRM-идентификатора означает, что MAX-сессия не
  видит персональные CRM-данные (это и есть режим «гость»).
- `bot_favorites` и `schedule_revisions` в схеме БД не трогаем (идемпотентный
  DDL), кроме опционального удаления неиспользуемой `bot_favorites`.
- В браузере без MAX Bridge проверка `initData` невозможна — остаётся
  query-фолбэк и анонимный просмотр.
