# План: бот Max и мини-приложение — завершение (UC-SCH-36–38, 40, 43)

> **Для агентов:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` (рекомендуется) или `superpowers:executing-plans`. Выполнять по задачам, отмечая шаги `[ ]`.
> **Спека:** `docs/superpowers/specs/2026-09-21-bot-miniapp-completion-design.md`.

**Цель:** онбординг `/start` и rate limit диспетчера, недельный вид со слоями и лимит календаря по `totalWeeks`, XLSX файлом в каналы, гейт/подтверждение/автоскачивание в мини-аппе, идемпотентный дайджест со слоями.

**Архитектура:** бот и мини-апп — тонкие клиенты существующих серверных видов `view=day|week|meta`; изменения только в `CollegeLMS.MaxBot`, `CollegeLMS.MaxBot.Tests`, `CollegeLMS.Next/components/max/` и e2e; API-контракты и основная БД не меняются.

**Стек:** .NET 10, EF Core (Npgsql, InMemory для тестов), Max Bot API, Next.js 14, Playwright.

**Ветка:** `feature/bot-miniapp-completion` (создаётся в Task 1; основной БД миграций нет).

## Глобальные ограничения

- `Result<T>`-стиль API-ответов не затрагивается; в боте — fail-safe: сбой Max-уведомлений не влияет на корректировки.
- Сообщения и подписи — на русском; CSharpier (`dotnet csharpier format .` перед коммитом).
- Тесты: `dotnet test CollegeLMS.MaxBot.Tests`, `dotnet test CollegeLMS.Tests`; гейты фронта — `npm run build`, `npx playwright test e2e/max-miniapp.spec.ts` (если локально браузер недоступен — см. Task 8).
- Схема бота: `EnsureCreated` + идемпотентный raw SQL (паттерн `Program.cs`), EF-миграции не добавляем.
- Локальный Docker не запускаем — сборка стека в CI/CD.

---

## Структура файлов

**Создаются:**
- `CollegeLMS.MaxBot/Services/DispatcherLoginThrottle.cs` — ограничение попыток входа диспетчера.
- `CollegeLMS.MaxBot.Tests/DispatcherLoginThrottleTests.cs`
- `CollegeLMS.MaxBot.Tests/ScheduleViewClientTests.cs` — view-методы клиента (ошибка/успех).
- `CollegeLMS.MaxBot.Tests/ScheduleNotifierIdempotencyTests.cs` (или дополнение `ScheduleNotifierTests`)
- `docs/diagrams/sequence/bot-onboarding.puml`, `docs/diagrams/sequence/bot-digest.puml`
- `docs/spec/task-bot-miniapp-completion.md`

**Изменяются:**
- `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs` — `ScheduleDayViewDto`, `ScheduleWeekViewDto`.
- `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs` — `GetDayViewAsync`, `GetWeekViewAsync`.
- `CollegeLMS.MaxBot/Clients/MaxApiClient.cs` — `UploadFileAsync`, `SendDocumentAsync`.
- `CollegeLMS.MaxBot/Services/CallbackPayload.cs` — payloads «Повторить».
- `CollegeLMS.MaxBot/Services/MessageFormatter.cs` — день/неделя из DTO со слоями.
- `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs` — view=day, БД-идемпотентность, гард.
- `CollegeLMS.MaxBot/Models/UserSettings.cs`, `Data/Configurations/UserSettingsConfiguration.cs`, `Program.cs` — `LastNotifiedOn` + ALTER.
- `CollegeLMS.MaxBot/Bot/MaxBotService.cs` — онбординг, rate limit, день/неделя/календарь, XLSX файлом, «вм.X».
- `CollegeLMS.Next/components/max/DispatcherView.tsx`, `DispatcherImport.tsx`, `DispatcherResult.tsx`, `api/dispatcher.ts`.
- `CollegeLMS.Next/e2e/max-miniapp.spec.ts` — гейт и подтверждение.
- `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`, `MaxBotDispatcherFlowTests.cs`, `ScheduleNotifierTests.cs`.

---

## Task 1: Клиент бота — view-методы дня и недели

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs`, `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs`
- Create: `CollegeLMS.MaxBot.Tests/ScheduleViewClientTests.cs`

**Interfaces:**
- Produces: DTO `ScheduleDayViewDto`, `ScheduleWeekViewDto`, `ScheduleWeekDayDto`; методы `GetDayViewAsync(DateTime? date, Guid? groupId, Guid? teacherId, CancellationToken ct) → Task<ScheduleDayViewDto?>`, `GetWeekViewAsync(int? week, Guid? groupId, Guid? teacherId, CancellationToken ct) → Task<ScheduleWeekViewDto?>`; `null` — ошибка запроса/невалидный ответ (отличается от валидного пустого дня/недели).

- [ ] **Step 1: Тесты (падающие)**

`ScheduleViewClientTests` по образцу `MaxBotDispatcherFlowTests` (`StubHandler` + `BuildClient`):
- `GetDayViewAsync_Success_ParsesLayers` — ответ `{"isSuccess":true,"data":{...}}` с `entries/inserts/practices/isNonWorking`; ассерты по слоям.
- `GetDayViewAsync_BadRequest_ReturnsNull`.
- `GetWeekViewAsync_Success_ParsesSixDays`.
- `GetDayViewAsync_EmptyEntries_ReturnsDayWithEmptyEntries` (не `null`).

- [ ] **Step 2: Тесты падают**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter FullyQualifiedName~ScheduleViewClientTests`
Expected: FAIL (методы/DTO отсутствуют).

- [ ] **Step 3: DTO**

```csharp
public record ScheduleDayViewDto
{
    public DateTime Date { get; init; }
    public int Week { get; init; }
    public int DayOfWeek { get; init; }
    public bool IsSunday { get; init; }
    public bool IsNonWorking { get; init; }
    public string? NonWorkingTitle { get; init; }
    public List<PracticeDto> Practices { get; init; } = [];
    public List<ScheduleInsertDto> Inserts { get; init; } = [];
    public List<ScheduleResponse> Entries { get; init; } = [];
}

public record ScheduleWeekViewDto
{
    public int Week { get; init; }
    public DateTime WeekStart { get; init; }
    public List<ScheduleDayViewDto> Days { get; init; } = [];
}
```

- [ ] **Step 4: Методы клиента**

`GetDayViewAsync`: `GET /api/schedule?view=day&date={yyyy-MM-dd}[&groupId=][&teacherId=]`; разбор `Result<ScheduleDayViewDto>`; при `!IsSuccess` или ошибке HTTP — `null` (логируется). `GetWeekViewAsync`: `view=week&week=N` (или `date`), ответ — `ScheduleWeekViewDto`.

- [ ] **Step 5: Тесты зелёные, коммит**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter FullyQualifiedName~ScheduleViewClientTests`
```powershell
dotnet csharpier format .
git add -A; git commit -m "feat(bot): view-методы клиента — день и неделя со слоями"
```

---

## Task 2: Max API — отправка файла

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Clients/MaxApiClient.cs`
- Create: `CollegeLMS.MaxBot.Tests/MaxApiFileTests.cs`

**Interfaces:**
- Produces: `UploadFileAsync(string uploadUrl, byte[] content, string fileName, string contentType, CancellationToken ct) → Task<JsonElement>`; `SendDocumentAsync(long chatId, JsonElement payload, string caption, List<List<MaxButton>> buttons, CancellationToken ct) → Task<MaxMessageResponse?>`.

- [ ] **Step 1: Тесты**

`MaxApiFileTests` (StubHandler, как в `CorrectionImageSenderTests`):
- `UploadFileAsync_PostsMultipartWithFileNameAndContentType`;
- `SendDocumentAsync_SendsFileAndKeyboardAttachments` — тело содержит `"type":"file"` и `"inline_keyboard"`;
- `SendDocumentAsync_NotReady_RetriesThenSucceeds` — первые ответы 400 с `attachment.not.ready`, затем 200 (проверка числа попыток).

- [ ] **Step 2: Тесты падают**, затем **Step 3: реализация**

`UploadFileAsync` — обобщение `UploadImageAsync` (тот делегирует в новый метод с `image/png`). `SendDocumentAsync` — цикл до 3 попыток, пауза 3 с при `attachment.not.ready` (1:1 паттерн `SendImageAsync`), `attachments: [file, inline_keyboard]`.

- [ ] **Step 4: Тесты зелёные, коммит**

```powershell
dotnet test CollegeLMS.MaxBot.Tests --filter FullyQualifiedName~MaxApiFileTests
git add -A; git commit -m "feat(bot): отправка файлов в Max API (type=file + клавиатура)"
```

---

## Task 3: Онбординг `/start` и rate limit диспетчера

**Файлы:**
- Create: `CollegeLMS.MaxBot/Services/DispatcherLoginThrottle.cs`, `CollegeLMS.MaxBot.Tests/DispatcherLoginThrottleTests.cs`
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`

**Interfaces:**
- Produces: `DispatcherLoginThrottle` — `bool IsBlocked(long userId, DateTime now)`, `TimeSpan RetryAfter(long userId, DateTime now)`, `void RegisterFailure(long userId, DateTime now)`, `void Reset(long userId)`; константы `MaxAttempts = 5`, `BlockDuration = 15 min`.
- Consumes: существующие payload-и выбора: `settings:group`/`settings:teacher` и экраны списков; добавляются `onboard:role:student|teacher`, `onboard:group[:страница]`, `onboard:teacher[:страница]`.

- [ ] **Step 1: Тесты throttle**

```csharp
[Fact] public void FiveFailures_BlocksFifteenMinutes() { /* 5× RegisterFailure → IsBlocked true; RetryAfter ≈ 15 мин */ }
[Fact] public void Success_ResetsCounter() { }
[Fact] public void Block_ExpiresAfterDuration() { }
[Fact] public void DifferentUsers_AreIndependent() { }
```

- [ ] **Step 2: Реализация throttle** (словарь `(int Failures, DateTime? BlockedUntil)`, lock, без зависимостей).

- [ ] **Step 3: Онбординг в `MaxBotService`**

- `HandleBotStartedAsync`: если у пользователя нет `GroupId` и `TeacherId` → отправить выбор роли (`onboard:role:*`) вместо простого приветствия; иначе — главное меню.
- Обработка `onboard:role:student|teacher`: установить роль (через `MaxBotRoleFlow.ApplyRole`), показать список групп/преподавателей в режиме онбординга.
- Обработка выбора `onboard:group:<id>` / `onboard:teacher:<id>`: сохранить выбор (существующие `SelectGroup/SelectTeacher`), отправить подтверждение и главное меню.
- Проверка блокировки в `HandleDispatcherPasswordAsync`: если `IsBlocked` → «❌ Слишком много попыток. Повторите через N мин.»; при неверном пароле — `RegisterFailure`; при успешном входе — `Reset`.

- [ ] **Step 4: Тесты MaxBot.Tests зелёные, коммит**

Run: `dotnet test CollegeLMS.MaxBot.Tests`
```powershell
git add -A; git commit -m "feat(bot): онбординг /start и rate limit входа диспетчера"
```

---

## Task 4: День, неделя и календарь в боте

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs`, `CollegeLMS.MaxBot/Services/CallbackPayload.cs`, `CollegeLMS.MaxBot/Bot/MaxBotService.cs`
- Modify: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`

**Interfaces:**
- Consumes: `GetDayViewAsync`, `GetWeekViewAsync`, `GetScheduleMetaAsync`.
- Produces: `MessageFormatter.FormatDaySchedule(ScheduleDayViewDto day, string entityName, bool showGroup)`; `FormatWeekSchedule(ScheduleWeekViewDto week, string entityName, bool showGroup)`; `CallbackPayload.RetryDayDay(DateTime) / RetryWeek(DateTime) / RetryCal(DateTime)`.

- [ ] **Step 1: Тесты форматтера (падающие)**

- `FormatDaySchedule_NonWorking_ShowsTitle`;
- `FormatDaySchedule_Practice_ReplacesPairs`;
- `FormatDaySchedule_Inserts_RenderedBeforePairs`;
- `FormatWeekSchedule_NonWorkingDay_MarkedInDayBlock`;
- `FormatWeekSchedule_PracticeDay_NoPairs`;
- `FormatDaySchedule_EmptyEntries_ShowsNoPairs`.

- [ ] **Step 2: Реализация форматтера** — день: практика → «🎓 *Практика*» + строка; иначе вставки (`HH:mm–HH:mm Название`) + пары (существующий формат с бейджами); нерабочий — «🎉 Нерабочий день: {название}»; воскресенье — «Расписания нет — выходной!». Неделя: заголовок «📅 Неделя N · dd.MM–dd.MM» + блоки дней Пн–Сб (без пар — «Пар нет.»). Существующие перегрузки `FormatDaySchedule(List<ScheduleResponse> …)` удаляются, если после миграции вызовов не остаётся (проверить `rg "FormatDaySchedule"`).

- [ ] **Step 3: MaxBotService**

- `ShowDayAsync` → `GetDayViewAsync` (удалить параллельные запросы practices/schedule/inserts); при `null` — «❌ Не удалось загрузить расписание.» + кнопка «🔄 Повторить» (`dayretry:<date>`).
- `ShowWeekAsync` → `GetWeekViewAsync`; разбивка длинных сообщений — по дням (как сейчас); при `null` — сообщение + «Повторить» (`weekretry:<date старта>`).
- `ShowCalendarAsync`: `maxMonth` = `semesterStart + totalWeeks` из `GetScheduleMetaAsync` (fallback `StudyWeek`); при ошибке meta — прежнее поведение.
- Обработка callback'ов retry.

- [ ] **Step 4: Тесты, коммит**

```powershell
dotnet test CollegeLMS.MaxBot.Tests
git add -A; git commit -m "feat(bot): день и неделя со слоями, лимит календаря и «Повторить»"
```

---

## Task 5: XLSX файлом и «вм.X»

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`
- Modify: `CollegeLMS.MaxBot.Tests/MaxBotDispatcherFlowTests.cs` (или новый тест)

**Interfaces:**
- Consumes: `MaxApiClient.UploadFileAsync/SendDocumentAsync` (Task 2), `CollegeLmsApiClient.GetScheduleXlsxAsync`.

- [ ] **Step 1: Реализация XLSX**

`SendDispatcherXlsxAsync`: `GetUploadUrlAsync("file")` → `UploadFileAsync(url, bytes, $"Расписание_{DateTime.Now:dd.MM.yyyy_HH-mm-ss}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct)` → `SendDocumentAsync(target, payload, caption, buttons)` с кнопкой `link` «📥 Скачать XLSX» (`MiniAppUrlBuilder.BuildScheduleExportXlsxUrl`). Текстовую ссылку не отправлять; ошибки — сообщение диспетчеру (fail-safe).

- [ ] **Step 2: «вм.X»**

Вынести сборку `CreateCorrectionPositionDto` из `CompleteWizardAsync` (~2337) в статический метод `DispatcherCorrectionWizard.BuildPosition(entry)`, где `Note = entry.Note` (без предзаполнения), а `RemovedNumberPair` передаётся как есть; сервер (`CorrectionBatchService.ResolveNote`) сам подставит `вм.{RemovedNumberPair}`. В предпросмотре визарда для Move показывать «вм.{RemovedNumberPair}» (из выбранной старой пары).

- [ ] **Step 3: Тесты**

- `BuildPosition_Move_KeepsNoteNullAndRemovedPair` — `Note == null`, `RemovedNumberPair` = старая пара;
- `BuildPosition_RemoveWithSelfStudy_KeepsNote` — примечание «сам.р.» сохраняется;
- существующие тесты `BuildScheduleExportXlsxUrl` остаются.

- [ ] **Step 4: Коммит**

```powershell
dotnet test CollegeLMS.MaxBot.Tests
git add -A; git commit -m "feat(bot): XLSX файлом в каналы, «вм.X» через серверный автофилл"
```

---

## Task 6: Идемпотентный дайджест со слоями

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Models/UserSettings.cs`, `Data/Configurations/UserSettingsConfiguration.cs`, `Program.cs`, `Services/ScheduleNotifier.cs`
- Modify: `CollegeLMS.MaxBot.Tests/ScheduleNotifierTests.cs`

**Interfaces:**
- Produces: `UserSettings.LastNotifiedOn` (`DateOnly?`, колонка `last_notified_on`); SQL `ALTER TABLE user_settings ADD COLUMN IF NOT EXISTS last_notified_on DATE;` в `Program.cs`.

- [ ] **Step 1: Модель и схема**

Свойство + конфигурация (`HasColumnName("last_notified_on")`, `.HasColumnType("date")`) + ALTER в блоке raw SQL `Program.cs` (рядом с `schedule_revisions`).

- [ ] **Step 2: Тесты (обновить/добавить)**

- `SendDueNotifications_NoEntity_SkipsUser` — `GroupId == null && TeacherId == null` → сообщений нет, `LastNotifiedOn` не меняется.
- `SendDueNotifications_SendsOncePerDay` — два вызова подряд → одно сообщение; после первого `LastNotifiedOn == today`.
- `SendDueNotifications_NonWorkingDay_SendsNothing` — сообщений нет, `LastNotifiedOn` не заполняется (поведение изменено против старого «MarksSent»).
- `SendDueNotifications_UsesDayViewLayers` — API вызван с `view=day`; практика/вставки попадают в текст (проверка через stub тела запроса).

- [ ] **Step 3: Реализация**

- `LoadTodaySubscribersAsync` — без изменений.
- `SendDueNotificationsAsync`: `nonWorking` → `return` (без записи); для каждого пользователя: гард `GroupId/TeacherId`, проверка `LastNotifiedOn != today`, окно ±15 мин, `GetDayViewAsync`, `FormatDaySchedule(day, …)`, отправка, `LastNotifiedOn = today`, `SaveChangesAsync`; `_lastSentPerUser` удалить.
- `NotificationWindow`/`NotificationTimeRules` без изменений.

- [ ] **Step 4: Тесты, коммит**

```powershell
dotnet test CollegeLMS.MaxBot.Tests
git add -A; git commit -m "feat(bot): идемпотентный дайджест со слоями (last_notified_on)"
```

---

## Task 7: Мини-приложение — гейт, подтверждение, автоскачивание

**Файлы:**
- Modify: `CollegeLMS.Next/components/max/DispatcherView.tsx`, `DispatcherImport.tsx`, `DispatcherResult.tsx`, `api/dispatcher.ts`
- Modify: `CollegeLMS.Next/e2e/max-miniapp.spec.ts`

**Interfaces:**
- Produces: `handleDispatcherAuthError(err): boolean` в `api/dispatcher.ts` (401/403 → `dispatcherLogout()` + `notifyDispatcherSession()`).

- [ ] **Step 1: Гейт**

`DispatcherView`: `const [authed, setAuthed] = useState(() => Boolean(dispatcherToken()))`; подписка на `max:dispatcher` (как в других местах) для перерисовки; если `!authed` → `<DispatcherGate onSuccess={() => setAuthed(true)} />`, иначе текущий UI.

- [ ] **Step 2: Подтверждение импорта**

`DispatcherImport`: перед `apply()` — шторка подтверждения («Применить N изменений?» + «Отмена»/«Применить»); `apply` выполняется только после подтверждения; ошибки 401/403 → `handleDispatcherAuthError`.

- [ ] **Step 3: Автоскачивание**

`DispatcherResult`: `useEffect` один раз при появлении `applied` — вызвать существующий `downloadCorrection()`; кнопка ручного скачивания остаётся; 401/403 → `handleDispatcherAuthError`.

- [ ] **Step 4: E2E**

`e2e/max-miniapp.spec.ts`: новый тест — без `dispatcherToken` открыть `/max/dispatcher` → видна форма «Доступ диспетчера»; с `sessionStorage.setItem("dispatcherToken", "...")` → видны вкладки «Файл XLSX»/«Вручную». Существующие тесты обновить при необходимости (мок dispatcher API).

- [ ] **Step 5: Сборка и e2e, коммит**

Run: `npm run build`; `npx playwright test e2e/max-miniapp.spec.ts` (при проблемах с браузером — см. Task 8).
```powershell
git add -A; git commit -m "feat(max): гейт диспетчера, подтверждение импорта, автоскачивание XLSX"
```

---

## Task 8: Документация, гейты, merge

**Файлы:**
- Create: `docs/diagrams/sequence/bot-onboarding.puml`, `docs/diagrams/sequence/bot-digest.puml`, `docs/spec/task-bot-miniapp-completion.md`

- [ ] **Step 1: Диаграммы и пост-фактум ТЗ**

Sequence: онбординг (`/start → роль → выбор → меню`) и дайджест (`notifier → non-working? → view=day → отправка → last_notified_on`). Пост-фактум ТЗ — по образцу `task-schedule-reference-data.md`.

- [ ] **Step 2: Полные гейты**

```powershell
dotnet build
dotnet csharpier check .
dotnet test CollegeLMS.Tests
dotnet test CollegeLMS.MaxBot.Tests
npm run build --prefix CollegeLMS.Next
npx playwright test e2e/max-miniapp.spec.ts   # в CollegeLMS.Next; при TLS-блокировке браузера — временный конфиг с system Chrome, как в прошлой итерации
```

- [ ] **Step 3: Коммит и merge**

```powershell
git add -A; git commit -m "docs: онбординг и рассылка бота — диаграммы и ТЗ"
git checkout master
git merge feature/bot-miniapp-completion
```

- [ ] **Step 4: Push и CI/CD** (после подтверждения пользователя)

```powershell
git push origin master   # → quality + deploy на VPS
```

Проверить статус Actions (quality backend/frontend, deploy) — при падении `gh-fix-ci`.

---

## Самопроверка плана

- **Покрытие спеки:** §2 — Task 3; §3 — Task 1, 4; §4 — Task 2, 5; §5 — Task 7; §6 — Task 1, 6; §7 — тесты в задачах 1–7, гейты Task 8; §8 — Task 8; §9–10 — Task 8.
- **Плейсхолдеров нет:** в задачах даны сигнатуры, имена payload'ов и конкретные ассерты.
- **Согласованность имён:** `GetDayViewAsync/GetWeekViewAsync`, `ScheduleDayViewDto/ScheduleWeekViewDto`, `UploadFileAsync/SendDocumentAsync`, `DispatcherLoginThrottle`, `LastNotifiedOn`, `handleDispatcherAuthError` — сквозные по задачам.
