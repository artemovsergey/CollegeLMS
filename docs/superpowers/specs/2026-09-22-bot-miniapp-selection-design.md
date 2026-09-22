# Выбор группы/преподавателя: бот MAX ↔ мини-приложение

Дата: 2026-09-22

## Контекст

Предыдущая задача (`2026-09-22-bot-miniapp-data-flow-design.md`) связала бота,
мини-приложение и API: мини-апп входит через MAX `initData` и получает JWT,
бот умеет открывать мини-апп кнопкой `open_app`, API читает профиль из
бот-БД `user_settings` (`GET /maxbot/internal/users/{maxUserId}`).

Осталось три разрыва:

- **Текущий выбор** (группа/преподаватель) в мини-приложении живёт только в
  `localStorage` (`max-view-context`) и не доходит до бота: бот не знает, что
  пользователь смотрит, а мини-апп не знает выбора, сделанного в боте.
- **Меню бота** не отражает текущий выбор и не даёт сменить его сценарием
  «Студент / Преподаватель»: роль выбирается один раз в онбординге, повторный
  вход в поиск стирает уже выбранную группу (или преподавателя) до
  подтверждения.
- **Оформление мини-аппа** перегружено: бейджи изменений содержат «· нед. N»,
  во вкладке «Изменения» есть лишний фильтр по неделям, а дата задаётся
  отдельным полем ввода вместо компактной иконки.

Пользовательские решения:

- `/start` при сохранённом выборе показывает главное меню, онбординг — только
  когда выбор пуст.
- Время уведомлений: 07:30–08:30 с шагом 5 минут, по умолчанию 07:30
  (границы `NotificationTimeRules` не меняются).
- Выбор в мини-апп меняет роль по типу цели: группа → `student`,
  преподаватель → `teacher` (как в боте).
- Когда выбор задан из мини-аппа, бот присылает в чат экран выбора
  («Группа: …» / «Преподаватель: …») с кнопками «Открыть расписание» и
  «Настройки».

## Решение

```
Бот ──open_app(payload=today[-g-<groupId>|-t-<teacherId>])──▶ мини-апп
мини-апп ──POST /api/auth/max/selection {groupId|teacherId} + Bearer JWT──▶ API
API ──POST /maxbot/internal/selection (X-Internal-Secret)──▶ бот
бот: user_settings.SelectGroup/SelectTeacher ──▶ сообщение в чат (MAX)
бот ──InternalUserProfile──▶ API ──свежий JWT + профиль──▶ мини-апп
```

Единственная точка записи выбора — бот-БД `user_settings`. Мини-апп не хранит
собственную копию выбора: после успешного сохранения он получает свежий JWT и
профиль, а `viewContext` в `localStorage` используется как кэш для мгновенной
отрисовки и для deep-link-ов.

### 1. Бот: приветствие и экраны

**Онбординг** (`RequiresOnboarding`: нет записи, либо нет ни группы, ни
преподавателя). Вместо `ShowRoleSelectionAsync` — приветствие
(`ShowWelcomeAsync`): название бота, описание, основные функции, затем главное
меню. Текст-плейсхолдер:

```
📚 *Расписание колледжа*

Я бот расписания: слежу за изменениями и присылаю расписание на день.

Что умею:
• 📅 открывать расписание на день и неделю в мини-приложении;
• 🔔 присылать расписание на день и уведомления об изменениях;
• ⭐ хранить избранные группы и преподавателей.

Выбери, кто ты, и найди себя поиском.
```

**Главное меню** (`ShowMainMenuAsync`) — один экран, два состояния:

| Условие | Текст | Кнопки (ряды) |
|---|---|---|
| Выбор пуст | `🏠 *Главное меню*` + `Текущий выбор: не задан` + подсказка | `[🎓 Студент, 👨🏫 Преподаватель]`, `[⚙️ Настройки]` |
| Выбор есть | `🏠 *Главное меню*` + `Группа: ИС-21` (или `Преподаватель: Иванов И. И.`) | `[📱 Открыть расписание]`, `[⚙️ Настройки]` |

**Поиск** (`StartSearchAsync`): сообщение «Напиши номер или название группы —
я найду её в списке.» (для преподавателя — ФИО) и кнопка `🔙 Отмена` (payload
`menu`). `StartSearchAsync` больше **не** вызывает `MaxBotRoleFlow.ApplyRole`:
роль и цель меняются только в момент подтверждения выбора, поэтому «Отмена»
не стирает прежний выбор. `ApplyRole` удаляется как мёртвый код.

**После выбора** группы/преподавателя бот сразу показывает главное меню с
выбором (без промежуточного «✅ Готово!»).

**Настройки** (`ShowSettingsAsync`): статус выбора, кнопка `🔔 Уведомления`
(переносится сюда из главного меню), `🎓 Студент`, `👨🏫 Преподаватель`,
`🔙 Меню`.

**Уведомления** (`ShowNotificationsAsync`): текст переформулируется под
«расписание на день» и «время до начала занятий (07:30–08:30)», кнопка
возврата — `🔙 Назад` (payload `settings`, а не `menu`).

**`bot_started`**: при каждом запуске обновляется `MaxChatId` существующей
записи (иначе выбор, сделанный из мини-аппа до первого `/start`, окажется без
адреса доставки). Запись создаётся, если её нет (как сейчас).

**Кнопки `open_app`**: `MiniAppButtons.OpenSchedule(options, groupId, teacherId)`
кладёт текущий выбор в payload (`today[-g-…|-t-…]`), чтобы мини-апп открывался
сразу на нужном расписании.

### 2. Бот: внутренний endpoint выбора

`POST /maxbot/internal/selection`

```json
{ "maxUserId": 123456, "groupId": "guid|null", "teacherId": "guid|null" }
```

- Guard как у `GET /maxbot/internal/users/{id}`: пустой `InternalSecret` → 401
  (fail-closed), иначе проверка заголовка `X-Internal-Secret`.
- Ровно одна цель (`groupId` xor `teacherId`), иначе 400.
- Новый `InternalSelectionService.SetAsync(...)`:
  1. находит `UserSettings` по `MaxUserId`; если записи нет — создаёт
     (`MaxChatId = 0`, уведомления по умолчанию);
  2. применяет `MaxBotRoleFlow.SelectGroup` / `SelectTeacher` (тип цели
     задаёт роль);
  3. сохраняет и, **если значение изменилось** и `MaxChatId > 0`, отправляет
     экран выбора через `MaxApiClient.SendInlineKeyboardAsync` (экран общий с
     ботом — вынесен в `BotScreens.MainMenuWithSelection`);
  4. возвращает `InternalUserProfile` (для выдачи свежего JWT на API).
- Если сообщение отправить не удалось — это не ошибка: выбор сохранён, в лог
  пишется warning (клиент уже умеет фолбэк на текст без кнопок).

### 3. API

- `POST /api/auth/max/selection` в `MaxAuthController`: `[Authorize]`, тело
  `MaxSelectionRequest { Guid? GroupId, Guid? TeacherId }`, ответ
  `Result<MaxAuthResponse>` (свежий JWT + профиль). Личность — `max_user_id`
  из claims (`ClaimsPrincipalExtensions.GetMaxUserId` уже есть).
- `MaxAuthService.SelectAsync(long maxUserId, MaxSelectionRequest, ct)`:
  валидация «ровно одна цель» (400) → `MaxBotHttpClient.SetSelectionAsync` →
  если профиль не получен/`Found = false` → `Result.Fail("Не удалось сохранить
  выбор. Попробуйте позже.", 503)`. Никаких «оптимистичных» ответов: иначе
  бот-БД и мини-апп разъезжаются.
- Выдача токена выносится в общий приватный `BuildResponse`, который
  переиспользуют `LoginAsync` и `SelectAsync`.
- `MaxBotHttpClient.SetSelectionAsync(long maxUserId, Guid? groupId,
  Guid? teacherId, ct)` — по образцу `GetInternalUserAsync` (fail-safe,
  `null` при ошибке).

### 4. Мини-приложение: выбор как действие

- Новый `api/selection.ts`: `saveMaxSelection({ groupId | teacherId })` →
  `POST /api/auth/max/selection`, возвращает `{ token, profile }`.
- `lib/max-context.tsx`:
  - `setViewContext` остаётся локальным (deep-link, быстрая смена картинки);
  - новый `makeCurrentSelection(target)`: оптимистично ставит `viewContext`,
    шлёт `saveMaxSelection`, сохраняет новый токен и профиль (`fullName`
    мержится из прежнего профиля — в selection-запросе нет `initData`), при
    ошибке откатывает `viewContext` и пробрасывает ошибку для показа;
  - после сохранения — `router.refresh()` не нужен, компоненты читают
    `viewContext` из контекста.
- `components/max/SearchSheet.tsx`: действие элемента — `Выбрать`
  (`makeCurrentSelection` + закрытие шита), звёздочка-избранное сохраняется
  отдельно; у элемента, совпадающего с текущим выбором, — отметка «Текущий».
- `components/max/FavoritesView.tsx`: `Открыть` теперь тоже делает элемент
  текущим (единая семантика «открыл = выбрал»).
- `components/max/ScheduleView.tsx`: пустое состояние переформулировать —
  «Выбор ещё не задан. Найдите группу или преподавателя — выбор сохранится в
  боте, и уведомления начнут приходить.»

### 5. Оформление мини-приложения

- `ChangeBadge`: убрать `· нед. N`; подписи `Добавлено` / `Снято` / `Замена` /
  `Перенос` (как в `ChangeCard`), для сам.р. — основной бейдж плюс компактный
  `Сам.р.`.
- `ChangesView`: удалить фильтр по неделям (состояние, `select`, параметр
  запроса); фильтр по дате — компактная иконка-календарь (`CalendarDays` +
  скрытый `input[type=date]` + `showPicker()`, как в `ScheduleView`), при
  выбранной дате — чип с датой и сбросом.
- `app/max/max.css`: компактные бейджи и стили фильтра-иконки.

### 6. Тесты

- Бот: `InternalSelectionServiceTests` (создание записи, обновление и роль по
  типу цели, отсутствие сообщения при том же значении и при `MaxChatId = 0`),
  правка `MaxBotRoleFlowTests` (удаление `ApplyRole`), `MiniAppButtonsTests`
  (payload с выбором).
- API: `MaxBotHttpClientTests` (новый метод, fail-safe), интеграционные
  `MaxAuthApiTests` (401 без токена, 400 при 0/2 целях, 200 со свежим
  профилем, 503 при недоступном боте).
- Фронтенд: `npm run build`; E2E `e2e/max-miniapp.spec.ts` — бейджи без недели,
  отсутствие фильтра недель, компактный календарь, выбор из поиска.

### 7. Вне скоупа

- Настройка уведомлений из мини-аппа (остаётся только в боте).
- Сброс выбора отдельной кнопкой (`/start` сохраняет выбор; сменить можно
  через «Студент»/«Преподаватель»).
- Хранение избранного на сервере (остаётся `localStorage`).

### 8. Затрагиваемые файлы

- `CollegeLMS.MaxBot/`: `Bot/MaxBotService.cs`, `Services/MiniAppButtons.cs`,
  `Services/InternalSelectionService.cs` (новый), `Services/BotScreens.cs`
  (новый), `Services/MaxBotRoleFlow.cs`, `Program.cs`, `README.md`.
- `CollegeLMS.API/`: `Controllers/MaxAuthController.cs`,
  `Services/MaxAuthService.cs`, `Services/MaxBotHttpClient.cs`,
  `Interfaces/IMaxAuthService.cs`, `Dtos/MaxSelectionRequest.cs` (новый).
- `CollegeLMS.Next/`: `lib/max-context.tsx`, `api/selection.ts` (новый),
  `components/max/{SearchSheet,FavoritesView,ScheduleView,ChangesView,ChangeBadge}.tsx`,
  `app/max/max.css`, `e2e/max-miniapp.spec.ts`.
- `CollegeLMS.MaxBot.Tests/`, `CollegeLMS.Tests/` — тесты.
