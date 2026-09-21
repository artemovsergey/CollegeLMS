# ТЗ: Бот Max и мини-приложение — завершение (онбординг, слои расписания, XLSX файлом, дайджест, диспетчерский экран)

> **Формулировка по факту реализации.** Документ описывает доработку бота Max и
> мини-приложения, реализованную коммитами `022917d`–`4310d03` ветки
> `feature/bot-miniapp-completion`: онбординг и защита диспетчерского режима (UC-SCH-37),
> слои расписания в дне/неделе и лимит календаря (UC-SCH-38), отправка XLSX файлом
> в каналы whitelist и серверный автофилл «вм.X» (UC-SCH-40), диспетчерский экран
> мини-приложения (UC-SCH-43, UC-SCH-27), идемпотентная ежедневная рассылка (UC-SCH-36).
> Источник: код `CollegeLMS.MaxBot/`, `CollegeLMS.Next/`, спека
> `docs/superpowers/specs/2026-09-21-bot-miniapp-completion-design.md`.
> Внешние API CollegeLMS и схема основной БД в этой итерации **не менялись**.

## 1. Цель

Закрыть оставшиеся требования эталонного ТЗ по боту Max и мини-приложению:

- `/start` проводит нового пользователя через выбор роли, группы/преподавателя и подтверждение;
- вход диспетчера по паролю ограничен по частоте (rate limit);
- день и неделя в боте строятся на серверных видах `view=day`/`view=week` со слоями (практики, вставки, нерабочие дни); календарь ограничен `totalWeeks` из `/api/schedule/meta`; ошибка запроса отличается от пустого ответа и предлагает «Повторить»;
- XLSX расписания в каналы whitelist отправляется файлом с кнопкой-ссылкой; примечание переноса «вм.X» заполняет сервер;
- мини-приложение показывает диспетчерский экран только после ввода пароля, требует подтверждения перед применением импорта и автоматически скачивает итоговый XLSX;
- ежедневная рассылка использует `view=day`, пропускает выходные/нерабочие дни и защищена от дублей в БД (`last_notified_on`).

## 2. Роли

| Роль | Права/поведение |
|---|---|
| Пользователь Max | `/start`, `/help`, `/settings`, `/dispatcher`; день, неделя, календарь; дайджест |
| Студент (`role=student`) | онбординг: выбор группы; в тексте дня/недели — «Группа» |
| Преподаватель (`role=teacher`) | онбординг: выбор преподавателя; в тексте — «Преподаватель», у пар показывается группа |
| Диспетчер бота | `/dispatcher` + пароль (`POST /api/dispatcher/login`); визард корректировки; отправка XLSX в чаты `MaxBot:DispatchChatIds` |
| Диспетчер мини-приложения | доступ к `/max/dispatcher` только после ввода пароля (ветка ТЗ «после ввода пароля диспетчера») |

## 3. Схема БД бота

Бот использует собственную БД (`MaxBotDbContext`, `EnsureCreated`; миграций нет) и
идемпотентный raw SQL в `Program.cs`.

| Сущность | Таблица | Изменение | Правила |
|---|---|---|---|
| `UserSettings` | `user_settings` | Новое поле `LastNotifiedOn` (`DateOnly?`) | Колонка `last_notified_on DATE` (`UserSettingsConfiguration`); EF-миграции не применяются — `ALTER TABLE user_settings ADD COLUMN IF NOT EXISTS last_notified_on DATE;` в `Program.cs` |
| Индекс | `ix_user_settings_max_user_id` | Без изменений | UNIQUE по `max_user_id` |

Прочие поля настроек: `role` (≤20, строка), `group_id`, `teacher_id`, `notify_enabled`,
`notify_days` (массив дней недели 1–7), `notify_time` (`interval`, по умолчанию 07:30 МСК),
`created_at`, `updated_at`.

## 4. Изменения по слоям

### 4.1. Бот Max

**Онбординг `/start` (`HandleBotStartedAsync`).** Пользователю без профиля создаётся
`UserSettings` (роль `student`, уведомления Пн–Пт, 07:30). Если `GroupId`/`TeacherId` пусты —
приветствие «👋 Привет! Я бот расписания.» и выбор роли (`onboard:role:student|teacher`);
иначе — сразу главное меню. После выбора роли:
- студент → список групп (`onboard:group`, страницы по 5, избранное, поиск, переиспользование
  существующего выбора групп);
- преподаватель → список преподавателей (`onboard:teacher`, страницы по 5, избранное, поиск).

После выбора вызывается `MaxBotRoleFlow` (`SelectGroup` сбрасывает `TeacherId`, `SelectTeacher`
сбрасывает `GroupId`), отправляется «✅ Готово! Группа: {название}.» /
«✅ Готово! Преподаватель: {ФИО}.» и открывается главное меню. `/settings`
(`settings:group|teacher`) сохраняет прежнее поведение — возврат в «Настройки».

**Rate limit пароля диспетчера (`DispatcherLoginThrottle`).** In-memory словарь на процесс:
после 5 неудачных попыток подряд — блок 15 минут с сообщением
«❌ Слишком много попыток. Повторите через N мин.», успешный вход сбрасывает счётчик,
блокировка истекает по времени. API `/api/dispatcher/login` продолжает лимитировать по IP
(без изменений).

**День и неделя со слоями.** `ShowDayAsync` использует
`GET /api/schedule?view=day&date=…&groupId=|teacherId=`; `ShowWeekAsync` —
`view=week&week=N`. Форматирование из DTO: практика заменяет пары, вставки — строки
«`HH:mm–HH:mm Название`», нерабочий день — «🎉 Нерабочий день: {название}», пустой день —
«Пар нет.», воскресенье — «Расписания нет — выходной!»; у преподавателя показывается группа.
Длинная неделя (> 4000 символов) разбивается: заголовок «📅 Неделя NN · dd.MM–dd.MM» +
сообщение по каждому дню.

**Календарь.** Верхняя граница — `semesterStart + totalWeeks` из `GET /api/schedule/meta`;
fallback — константа `StudyWeek` (16 недель), если meta недоступен.

**Ошибки и «Повторить».** Клиент различает ошибку (сеть/5xx/невалидный `Result`, метод
возвращает `null`) и валидный пустой результат. При ошибке — «❌ Не удалось загрузить
расписание.» + inline-кнопка «🔄 Повторить» с сохранением контекста
(`dayretry:yyyy-MM-dd`, `weekretry:yyyy-MM-dd`, `calretry:yyyy-MM`).

**XLSX файлом (UC-SCH-40).** По кнопке «📊 Отправить XLSX расписания» → выбор чата из
`MaxBot:DispatchChatIds` → `_dispatcherTokens[userId]` (иначе повторный ввод пароля) →
`GetScheduleXlsxAsync` → `POST /uploads?type=file` → multipart-загрузка (поле `data`,
ASCII-имя `schedule_yyyy-MM-dd_HH-mm-ss.xlsx`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`) →
`POST /messages?chat_id={target}` с вложениями `file` и `inline_keyboard` (кнопка `link`
«📥 Скачать XLSX» → `MiniAppUrlBuilder.BuildScheduleExportXlsxUrl`). Повтор при
`attachment.not.ready` — до 3 попыток с паузой 3 с. Текстовая ссылка больше не отправляется;
при сбое — «❌ Не удалось отправить XLSX.» (fail-safe, корректировки не затрагиваются).

**«вм.X».** Бот не предзаполняет `Note` переноса (`DispatcherCorrectionWizard.BuildPosition`
передаёт примечание как есть); сервер (`CorrectionBatchService.ResolveNote`) сам подставляет
`вм.{RemovedNumberPair}`, когда для `Move` примечание пусто. В предпросмотре визарда
примечание отображается из выбранной старой пары, если оно не задано.

### 4.2. Мини-приложение

**Гейт диспетчера (`DispatcherView` + `DispatcherGate`).** При отсутствии `dispatcherToken()`
рендерится форма пароля (`dispatcherLogin` → `sessionStorage.dispatcherToken` → событие
`max:dispatcher`); после входа — режимы «Файл XLSX» / «Вручную». `handleDispatcherAuthError`
при `401/403` диспетчерских API выполняет `dispatcherLogout()` и событием возвращает к гейту.

**Подтверждение импорта (`DispatcherImport`).** Кнопка «Применить изменения» открывает шторку
«Применить N изменений?» («Отмена» / «Применить», склонение по числу); `apply` вызывается
только после подтверждения. Ручной режим использует существующую `ConfirmOpsSheet`.

**Автоскачивание XLSX (`DispatcherResult`, UC-SCH-27).** Сразу после успешного применения —
однократное (`useRef` по `batchId`) автоматическое скачивание
`Корректировка_dd.MM.yyyy_HH-mm-ss.xlsx`; кнопка ручного скачивания сохраняется.

**Scoped 401 (`lib/api.ts`).** Response-перехватчик не очищает CRM-сессию и не редиректит на
`/login`, если 401 пришёл от `/api/dispatcher/login` (неверный пароль) или от диспетчерских
маршрутов (`/api/dispatcher/*`, `/api/schedule/correction/*`) внутри `/max`; такие ошибки
обрабатываются на месте. Остальные 401 — прежнее поведение.

### 4.3. Ежедневная рассылка (`ScheduleNotifier`, UC-SCH-36)

- Источник — `GetDayViewAsync` (`view=day`); текст — существующий формат дня.
- Суббота/воскресенье: цикл спит до понедельника 00:30. В будний день воркер ждёт
  `min(NotifyTime)` подписчиков, затем каждые 30 с отправляет тем, кто попал в окно ±15 минут.
- Общий нерабочий день (`GET /api/non-working-days`): рассылка пропускается, запись
  `last_notified_on` не создаётся. Нерабочий день в `ScheduleDayView` пропускает конкретного
  пользователя.
- Гард: `GroupId == null && TeacherId == null` → пользователь пропускается.
- Идемпотентность: перед отправкой проверяется `LastNotifiedOn != today`; после успешной
  отправки в БД пишется `last_notified_on = today`. In-memory словарь `_lastSentPerUser`
  удалён.
- Ошибка загрузки дня (`null`) — без отметки, повтор в следующем цикле.

## 5. Внутренние контракты

Внешние контракты CollegeLMS не менялись; ниже — то, чем пользуются бот и мини-приложение.

### 5.1. CollegeLMS API (используется, не изменялся)

| Метод | Назначение |
|---|---|
| `GET /api/schedule?view=day&date=&groupId=\|teacherId=` | `ScheduleDayViewDto`: `date`, `week`, `dayOfWeek`, `isSunday`, `isNonWorking`, `nonWorkingTitle`, `practices[]`, `inserts[]`, `entries[]` |
| `GET /api/schedule?view=week&week=N&groupId=\|teacherId=` | `ScheduleWeekViewDto`: `week`, `weekStart`, `days[]` (6 дней Пн–Сб) |
| `GET /api/schedule/meta` | `semesterStart`, `totalWeeks`, `currentWeek` |
| `POST /api/dispatcher/login` | `{ password }` → `token`, `expiresAt`; `401` — неверный пароль, `429` — лимит по IP |
| `GET /api/schedule/export?format=xlsx&groupId=` | XLSX-выгрузка для кнопки `link` (мини-апп-прокси) |
| `/api/groups`, `/api/teachers`, `/api/non-working-days` | выбор и слои расписания |
| `/api/schedule/correction/batches/*` | превью/правка/применение/экспорт пакета корректировок |

### 5.2. Max Bot API (клиент `MaxApiClient`)

| Метод | Назначение |
|---|---|
| `POST /uploads?type=file` | URL загрузки; ответ `{ url, token? }` |
| `POST {uploadUrl}` (multipart, поле `data`) | payload вложения — JSON с `token` |
| `POST /messages?chat_id={id}` | `attachments: [{type: file}, {type: inline_keyboard}]`; повтор при `attachment.not.ready` (3 попытки, 3 с) |
| `POST /messages?chat_id={id}` | обычный текст дайджеста, inline-клавиатура |

### 5.3. Callback payload

| Payload | Действие |
|---|---|
| `onboard:role:student\|teacher` | роль в онбординге |
| `onboard:group[:page]`, `onboard:group:{groupId}` | страница/выбор группы в онбординге |
| `onboard:teacher[:page]`, `onboard:teacher:{teacherId}` | страница/выбор преподавателя |
| `dayretry:yyyy-MM-dd`, `weekretry:yyyy-MM-dd`, `calretry:yyyy-MM` | повтор загрузки после ошибки |
| `dispatcher:send:{chatId}` | отправка XLSX в чат whitelist |

### 5.4. Мини-приложение (клиентские контракты)

| Элемент | Поведение |
|---|---|
| `sessionStorage.dispatcherToken` | парольный токен диспетчера (не пересекается с CRM-токеном `localStorage.token`) |
| `/api/dispatcher/login` в `api/dispatcher.ts` | вход, сохранение токена, событие `max:dispatcher` |
| `handleDispatcherAuthError` | `401/403` → `dispatcherLogout()` + событие → гейт; возвращает `true` |
| `lib/api.ts` | 401 для `/api/dispatcher/login` и диспетчерских маршрутов в `/max` не редиректит на `/login` |

## 6. Требования по UC

| UC | Критерии приёмки (реализовано) |
|---|---|
| UC-SCH-37 | `/start` у ненастроенного пользователя ведёт «роль → группа/преподаватель → подтверждение → меню»; настроенный сразу получает меню; 5 неверных паролей — блок 15 минут, успех/истечение сбрасывают счётчик |
| UC-SCH-38 | День и неделя — серверные виды со слоями; практика заменяет пары; вставки отдельными строками; нерабочий и пустой день подписаны; календарь ограничен `totalWeeks` (fallback 16); ошибка отличается от пустого ответа и даёт «Повторить» |
| UC-SCH-40 | XLSX уходит файлом (`type=file`) с кнопкой-ссылкой в чаты `DispatchChatIds`; повтор `attachment.not.ready`; «вм.X» подставляет сервер; вход — по паролю |
| UC-SCH-43 | `/max/dispatcher` без токена показывает форму пароля; после входа — режимы импорта; применение — только после подтверждения; `401/403` возвращает к гейту |
| UC-SCH-27 | В мини-приложении после применения XLSX скачивается автоматически один раз; ручная кнопка сохраняется |
| UC-SCH-36 | Дайджест по `view=day`; пропуск выходных/нерабочих и пользователей без выбора; окно ±15 минут; повторный прогон не дублирует отправку (`last_notified_on`) |

## 7. Тестирование

| Уровень | Файлы/покрытие |
|---|---|
| Unit (`CollegeLMS.MaxBot.Tests`) | `DispatcherLoginThrottleTests` (5 неудач → блок, сброс успехом/временем); `ScheduleViewClientTests` (день/неделя: успех, ошибка vs пустой ответ, кодирование query); `ScheduleNotifierTests` (гард без выбора, пропуск нерабочего, повторный прогон не дублирует, `LastNotifiedOn` обновляется, окно ±15 минут); `MessageFormatterTests` (слои дня/недели, практика заменяет пары, «вм.X»); `MaxApiFileTests` (`type=file`, multipart, `attachment.not.ready`); `DispatcherCorrectionWizardTests` (позиция `Move` без `Note`) |
| Integration | Не требуется — API CollegeLMS не менялся |
| E2E (Playwright, `CollegeLMS.Next/e2e/max-miniapp.spec.ts`) | Гейт без токена; режимы с токеном; шторка подтверждения («Отмена» / «Применить») и автоскачивание XLSX; истёкший `dispatcherToken` → возврат к гейту без редиректа на `/login`; слои дня/недели |

## 8. Документация

- Спека дизайна: `docs/superpowers/specs/2026-09-21-bot-miniapp-completion-design.md`;
  план: `docs/superpowers/plans/2026-09-21-bot-miniapp-completion.md`.
- Postman не меняется — внешние API без изменений.

## 9. Известные ограничения

1. **At-least-once дайджеста.** `last_notified_on` пишется после успешной отправки; сбой между
   отправкой и `UPDATE` даст повтор в следующем цикле (в пределах окна ±15 минут). Выбрано
   осознанно: «лучше повтор, чем пропуск».
2. **ASCII-имя файла.** Max API отклоняет загрузку с кириллическим именем, поэтому файл
   называется `schedule_yyyy-MM-dd_HH-mm-ss.xlsx`; человекочитаемое имя — в тексте сообщения
   и по кнопке-ссылке.
3. **Ветка «пароль» вместо роли для мини-аппа.** `/api/schedule/context` различает только
   Student/Teacher/Other, поэтому доступ к диспетчерскому экрану — по паролю диспетчера
   (разрешённая ветка ТЗ «либо после ввода пароля диспетчера»). Пароль открывает только
   диспетчерский экран.
4. **Rate limit — in-memory.** Счётчик живёт в процессе бота: перезапуск сбрасывает блок,
   при нескольких репликах лимит действует на реплику. API по IP не затронут.
5. **Подтверждение — только в файловом режиме.** Ручной режим использует прежнюю
   `ConfirmOpsSheet`.
6. **Календарь.** Если `/api/schedule/meta` недоступен, используется fallback `StudyWeek`
   (16 недель) — без учёта реального семестра.

## 10. Трассируемость

| UC | Покрытие |
|---|---|
| UC-SCH-36 | `ScheduleNotifier`: `view=day`, пропуск выходных/нерабочих/без выбора, окно ±15 минут, `last_notified_on` |
| UC-SCH-37 | `HandleBotStartedAsync`, `onboard:*`-payload, `MaxBotRoleFlow`, `DispatcherLoginThrottle` |
| UC-SCH-38 | `GetDayViewAsync`/`GetWeekViewAsync`, `MessageFormatter.FormatDaySchedule/FormatWeekSchedule`, лимит календаря по meta, `RetryButtons` |
| UC-SCH-40 | `SendDispatcherXlsxAsync`, `MaxApiClient.SendDocumentAsync`, ASCII-имя, `BuildScheduleExportXlsxUrl`, `ResolveNote` на API |
| UC-SCH-43 | `DispatcherGate`, `DispatcherImport` (шторка), `handleDispatcherAuthError`, scoped 401 в `lib/api.ts` |
| UC-SCH-27 | `DispatcherResult` — автоскачивание по `batchId` |
