# Поток выбора бот ↔ мини-апп — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Выбор группы/преподавателя становится общим состоянием бота MAX и мини-приложения: бот показывает текущий выбор и меняет его по сценарию «Студент/Преподаватель», мини-апп сохраняет выбор через API (свежий JWT), бот получает сообщение о выборе.

**Architecture:** Единственная точка записи — бот-БД `user_settings`. Мини-апп вызывает `POST /api/auth/max/selection` (JWT) → API вызывает бота `POST /maxbot/internal/selection` (`X-Internal-Secret`) → бот сохраняет выбор и отправляет экран выбора в чат → API возвращает свежий JWT + профиль. Дополнительно: приветствие/меню бота переструктурированы, бейджи и фильтры мини-аппа упрощены.

**Tech Stack:** .NET 10 (ASP.NET Core Minimal API + BackgroundService), EF Core/Npgsql, Next.js 14 + TypeScript + Tailwind v4, xUnit/Moq, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-22-bot-miniapp-selection-design.md`

## Global Constraints

- Тексты, комментарии, Swagger-описания, сообщения об ошибках — на русском.
- `CancellationToken ct` во всех async-методах; `AsNoTracking()` на чтении; `List<T>` вместо `IEnumerable<T>`; primary-constructor DI; file-scoped namespaces; без try/catch в контроллерах/сервисах (только периметр — бот-клиент и отправка в MAX fail-safe).
- Время уведомлений: `NotificationTimeRules.Min = 07:30`, `Max = 08:30`, шаг 5 минут; дефолт 07:30 — **не менять**.
- Типы целей: `"student"` ↔ `GroupId`, `"teacher"` ↔ `TeacherId`; выбор ровно один.
- Подписи бейджей: `Добавлено` / `Снято` / `Замена` / `Перенос`; неделя в бейджах не пишется.
- После задач — `dotnet csharpier format .`, `dotnet build CollegeLMS.slnx`, таргетные тесты, `npm run build`.

---

### Task 1: Бот — общий экран главного меню с выбором

**Files:**
- Create: `CollegeLMS.MaxBot/Services/BotScreens.cs`
- Modify: `CollegeLMS.MaxBot/Services/MiniAppButtons.cs`
- Test: `CollegeLMS.MaxBot.Tests/MiniAppButtonsTests.cs`, `CollegeLMS.MaxBot.Tests/BotScreensTests.cs` (новый)

**Interfaces:**
- Produces: `MiniAppButtons.OpenSchedule(MaxBotOptions options, Guid? groupId = null, Guid? teacherId = null) : MaxButton`; `BotScreens.MainMenuWithSelection(string role, Guid? groupId, Guid? teacherId, string? groupName, string? teacherName, MaxBotOptions options) : (string Text, List<List<MaxButton>> Buttons)`.

- [ ] **Step 1: Тест payload с выбором** — `OpenSchedule(options, groupId: g)` кладёт `today-g-{g}` в `Payload`; без выбора — `today`.
- [ ] **Step 2: Тест BotScreens** — при `role="student"` текст содержит `Группа: ИС-21`, кнопки: `open_app` + `settings`; при `role="teacher"` — `Преподаватель: Иванов И. И.`; при неизвестном имени — `Группа: выбрана`.
- [ ] **Step 3: Реализация** `OpenSchedule` с двумя опциональными аргументами (обратная совместимость вызовов `OpenSchedule(_options)`), `BotScreens` — статический класс, текст `$"🏠 *Главное меню*\n\n{entity}"`, кнопки `[OpenSchedule(options, groupId, teacherId)]`, `[⚙️ Настройки → "settings"]`.
- [ ] **Step 4:** `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MiniAppButtons|FullyQualifiedName~BotScreens` → PASS.
- [ ] **Step 5: Commit** `feat(max): общий экран меню с текущим выбором`.

---

### Task 2: Бот — приветствие, меню, поиск, настройки, уведомления

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs` (методы 272–341, 406–520, 525–566, 680–738, 747–931)
- Modify: `CollegeLMS.MaxBot/Services/MaxBotRoleFlow.cs` (удалить `ApplyRole`)
- Test: `CollegeLMS.MaxBot.Tests/MaxBotRoleFlowTests.cs`

**Interfaces:**
- Consumes: `BotScreens.MainMenuWithSelection` (Task 1).
- Produces: экраны бота; payload-и `role:student|teacher`, `settings:student|teacher`, `menu`, `settings`, `notifications`, `notify:*`, `notifyday:*`, `notifytime:*`.

- [ ] **Step 1: `HandleBotStartedAsync`** — при найденной записи всегда обновлять `MaxChatId = chatId` и `UpdatedAt` (SaveChanges); если `RequiresOnboarding` → `ShowWelcomeAsync(chatId, ct)` + `ShowMainMenuAsync`, иначе `ShowMainMenuAsync`.
- [ ] **Step 2: Приветствие** — заменить `ShowRoleSelectionAsync` на `ShowWelcomeAsync(long chatId, CancellationToken ct)`, текст:

```
📚 *Расписание колледжа*

Я бот расписания: слежу за изменениями и присылаю расписание на день.

Что умею:
• 📅 открывать расписание на день и неделю в мини-приложении;
• 🔔 присылать расписание на день и уведомления об изменениях;
• ⭐ хранить избранные группы и преподавателей.

Выбери, кто ты, и найди себя поиском.
```

  Кнопок нет (меню придёт следующим сообщением).
- [ ] **Step 3: Главное меню** (`ShowMainMenuAsync`) — если `(GroupId ?? TeacherId) is null`: текст `$"🏠 *Главное меню*\n\nТекущий выбор: не задан\n\nВыбери, кто ты, и найди себя поиском."`, кнопки `[🎓 Студент → "role:student"]`, `[👨‍🏫 Преподаватель → "role:teacher"]`, `[⚙️ Настройки → "settings"]`. Иначе — `BotScreens.MainMenuWithSelection(settings.Role, settings.GroupId, settings.TeacherId, groupName, teacherName, _options)`; имя резолвить как сейчас через `_api.GetGroupsAsync`/`GetTeachersAsync`, при null — `BotScreens` подставит «выбрана»/«выбран».
- [ ] **Step 4: Поиск** (`StartSearchAsync`) — убрать вызов `MaxBotRoleFlow.ApplyRole` (роль меняется только при подтверждении выбора); оставить чтение settings (если null — return), `_searchStates[userId] = new SearchState { Target = target }` и текущий prompt + `🔙 Отмена` (`menu`).
- [ ] **Step 5: Подтверждение выбора** — `HandleGroupSelectionAsync`/`HandleTeacherSelectionAsync`: убрать промежуточный `SendMessageAsync` («✅ Готово! …»), оставить `SelectGroup`/`SelectTeacher` + SaveChanges + `_searchStates.Remove` + `ShowMainMenuAsync`.
- [ ] **Step 6: Настройки** (`ShowSettingsAsync`) — текст `$"⚙️ *Настройки*\n\nТекущий выбор: {groupName ?? teacherName ?? "не задан"}"`, кнопки: `[🔔 Уведомления → "notifications"]`, `[🎓 Студент → "settings:student"]`, `[👨‍🏫 Преподаватель → "settings:teacher"]`, `[🔙 Меню → "menu"]`. Кнопку «Открыть расписание» из настроек убрать (мини-апп открывается из главного меню).
- [ ] **Step 7: Уведомления** (`ShowNotificationsAsync`) — текст:

```
🔔 *Уведомления*

Расписание на день: {включено ✅|выключено ❌}
Дни: {Дни недели}
⏰ Время до начала занятий: {hh:mm} (МСК)

Нажми на день, чтобы включить или выключить его. Время меняется кнопками ±5 минут (07:30–08:30).
```

  Кнопка возврата: `[🔙 Назад → "settings"]` вместо `menu`.
- [ ] **Step 8: Callback-роутер** — удалить `case "onboard"` и `case "role-choice"`, удалить `HandleOnboardingCallbackAsync`; `HandleRoleSelectionAsync` оставить (payload `role:student|teacher`).
- [ ] **Step 9: `MaxBotRoleFlow`** — удалить `ApplyRole`; правка `MaxBotRoleFlowTests` (тест на `ApplyRole` удалить, остальные оставить).
- [ ] **Step 10:** `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxBotRoleFlow` → PASS; `dotnet build CollegeLMS.slnx`.
- [ ] **Step 11: Commit** `feat(max): приветствие, меню с выбором и настройки бота`.

---

### Task 3: Бот — endpoint выбора из мини-аппа

**Files:**
- Create: `CollegeLMS.MaxBot/Services/InternalSelectionService.cs`, `CollegeLMS.MaxBot/Models/InternalSelectionRequest.cs`
- Modify: `CollegeLMS.MaxBot/Program.cs` (регистрация + endpoint после `GET /maxbot/internal/users/{maxUserId:long}`)
- Test: `CollegeLMS.MaxBot.Tests/InternalSelectionServiceTests.cs` (новый)

**Interfaces:**
- Consumes: `BotScreens.MainMenuWithSelection` (Task 1), `InternalProfileService.GetAsync` (существующий).
- Produces: `InternalSelectionService.SetAsync(long maxUserId, Guid? groupId, Guid? teacherId, CancellationToken ct) : Task<InternalUserProfile>`; `POST /maxbot/internal/selection` body `InternalSelectionRequest { long MaxUserId; Guid? GroupId; Guid? TeacherId; }`.

- [ ] **Step 1: Тесты** (InMemory `MaxBotDbContext`, Moq на `MaxApiClient`/`CollegeLmsApiClient`, `NullLogger`):
  - запись отсутствует → создаётся, роль по типу цели, `Found = true`;
  - выбор изменился и `MaxChatId > 0` → `SendInlineKeyboardAsync` вызван один раз с текстом, содержащим `Группа:`;
  - выбор тот же → отправки нет;
  - `MaxChatId == 0` → отправки нет, выбор сохранён.
- [ ] **Step 2: Реализация** — сервис: найти/создать `UserSettings`; `changed` = сравнение до записи; `SelectGroup`/`SelectTeacher`; `UpdatedAt`; SaveChanges; `profile = await profileService.GetAsync(maxUserId, ct)`; если `changed && settings.MaxChatId > 0` → `BotScreens.MainMenuWithSelection(...)` + `SendInlineKeyboardAsync` в try/catch с `LogWarning`; вернуть `profile`.
- [ ] **Step 3: Endpoint** в `Program.cs`: guard как у GET (пустой `InternalSecret` → 401, `WebhookSecretValidator.IsValid` по `X-Internal-Secret`), `(request.GroupId is null) == (request.TeacherId is null)` → `Results.BadRequest(new { error = "Нужно указать ровно одну цель: группу или преподавателя." })`, иначе `Results.Ok(await service.SetAsync(...))`. Регистрация сервиса в DI (scoped).
- [ ] **Step 4:** `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~InternalSelectionService` → PASS.
- [ ] **Step 5: Commit** `feat(max): внутренний endpoint выбора группы/преподавателя`.

---

### Task 4: API — смена выбора из мини-аппа

**Files:**
- Create: `CollegeLMS.API/Dtos/MaxSelectionRequest.cs`
- Modify: `CollegeLMS.API/Services/MaxBotHttpClient.cs`, `CollegeLMS.API/Interfaces/IMaxAuthService.cs`, `CollegeLMS.API/Services/MaxAuthService.cs`, `CollegeLMS.API/Controllers/MaxAuthController.cs`
- Test: `CollegeLMS.Tests/Unit/Services/MaxBotHttpClientTests.cs`, `CollegeLMS.Tests/Integration/Controllers/MaxAuthApiTests.cs`

**Interfaces:**
- Consumes: bot `POST /maxbot/internal/selection` (Task 3), `MaxInternalUserDto`.
- Produces: `MaxBotHttpClient.SetSelectionAsync(long maxUserId, Guid? groupId, Guid? teacherId, CancellationToken ct) : Task<MaxInternalUserDto?>`; `IMaxAuthService.SelectAsync(long maxUserId, MaxSelectionRequest request, CancellationToken ct) : Task<Result<MaxAuthResponse>>`; `POST /api/auth/max/selection`.

- [ ] **Step 1: Тесты API** — `SetSelectionAsync` шлёт POST с `X-Internal-Secret` и телом `{maxUserId, groupId, teacherId}`, возвращает DTO; при 500/исключении — `null`. Интеграционные: без `[Authorize]`-токена → 401; с токеном и двумя целями → 400; с токеном и недоступным ботом → 503; успех → 200 + `profile.groupId`.
- [ ] **Step 2: DTO** — `public sealed record MaxSelectionRequest(Guid? GroupId, Guid? TeacherId);`
- [ ] **Step 3: `SetSelectionAsync`** — по образцу `GetInternalUserAsync` (fail-safe, `LogWarning` + `null`); сериализация `JsonSerializerDefaults.Web`.
- [ ] **Step 4: `MaxAuthService`** — вынести построение ответа в `private Result<MaxAuthResponse> BuildResponse(long maxUserId, string? fullName, MaxInternalUserDto? botProfile)` (используется `LoginAsync`); добавить `SelectAsync`: `(GroupId is null) == (TeacherId is null)` → `Result.Fail("Нужно выбрать ровно одну цель: группу или преподавателя.", 400)`; `botClient.SetSelectionAsync(...)`; `profile is not { Found: true }` → `Result.Fail("Не удалось сохранить выбор. Попробуйте позже.", 503)`; иначе `BuildResponse(maxUserId, null, profile)`.
- [ ] **Step 5: Контроллер** — `[HttpPost("max/selection")] [Authorize] [EnableRateLimiting("AuthPolicy")]`, `var maxUserId = User.GetMaxUserId(); if (maxUserId is null) return Unauthorized();` → `result` → `StatusCode(result.StatusCode, result)` / `Ok(result)`; Swagger `<summary>`/`<response>`.
- [ ] **Step 6:** `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxBotHttpClient|FullyQualifiedName~MaxAuthApi` → PASS; `dotnet build CollegeLMS.slnx`.
- [ ] **Step 7: Commit** `feat(api): сохранение выбора мини-аппа с обновлением JWT`.

---

### Task 5: Мини-апп — выбор как действие

**Files:**
- Create: `CollegeLMS.Next/api/selection.ts`
- Modify: `CollegeLMS.Next/lib/max-context.tsx`, `CollegeLMS.Next/components/max/SearchSheet.tsx`, `CollegeLMS.Next/components/max/FavoritesView.tsx`, `CollegeLMS.Next/components/max/ScheduleView.tsx`

**Interfaces:**
- Consumes: `POST /api/auth/max/selection` (Task 4).
- Produces: `saveMaxSelection({ groupId } | { teacherId }) : Promise<MaxAuthResponse>`; `useMaxContext()` дополняется `makeCurrentSelection(target: { groupId?: string; groupName?: string; teacherId?: string; teacherName?: string }) : Promise<void>`.

- [ ] **Step 1: `api/selection.ts`** — `saveMaxSelection` через общий `api`-инстанс (JWT-интерцептор уже есть) → `ResultEnvelope<MaxAuthResponse>`, бросает при `!isSuccess`, возвращает `data`.
- [ ] **Step 2: `max-context.tsx`** — добавить `makeCurrentSelection`: оптимистично `setViewContext(target)`, `saveMaxSelection`, сохранить `token` (`max-token`) и `setProfile(prev => ({ ...prev, ...data.profile, fullName: data.profile.fullName ?? prev?.fullName }))`, при ошибке — вернуть прежний `viewContext` и `throw`; экспортировать метод в контекст.
- [ ] **Step 3: `SearchSheet`** — кнопка `Выбрать` вызывает `makeCurrentSelection` + `onClose()`; звёздочка-избранное остаётся; у элемента с совпадающим id — бейдж «Текущий».
- [ ] **Step 4: `FavoritesView`** — `Открыть` → `await makeCurrentSelection(...)` + `router.push("/max/schedule")`; отметка «Текущий просмотр» → «Текущий выбор».
- [ ] **Step 5: `ScheduleView`** — текст пустого состояния: «Выбор ещё не задан. Найдите группу или преподавателя — выбор сохранится в боте, и уведомления начнут приходить.»
- [ ] **Step 6:** `cd CollegeLMS.Next; npm run build` → успех.
- [ ] **Step 7: Commit** `feat(max-app): выбор группы и преподавателя синхронизируется с ботом`.

---

### Task 6: E2E и финальная верификация

**Files:**
- Modify: `CollegeLMS.Next/e2e/max-miniapp.spec.ts`, `CollegeLMS.MaxBot/README.md`

- [ ] **Step 1:** дополнить E2E: бейджи без «нед.», отсутствие `#max-week-filter`, видимая кнопка-календарь в «Изменениях».
- [ ] **Step 2:** `cd CollegeLMS.Next; npx playwright test e2e/max-miniapp.spec.ts` → PASS.
- [ ] **Step 3:** `dotnet csharpier format .`; `dotnet build CollegeLMS.slnx`; `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxBot` и `~MaxAuth`.
- [ ] **Step 4: Commit + push** `test(max): e2e и документация потока выбора`; проверить, что CI (quality → deploy) запустился.
