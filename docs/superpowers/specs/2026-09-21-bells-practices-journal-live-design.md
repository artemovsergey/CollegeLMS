# Дизайн-спецификация: звонки по дням, практики, журнал по группам, live-дашборд

**Дата:** 2026-09-21
**Статус:** утверждена пользователем (ответы в чате, 2026-09-21)
**Цель:** дать диспетчеру профили звонков с привязкой к дням недели и конкретным датам (праздники, рабочие субботы), расширить практики (название, несколько преподавателей, пары УП по дням, уведомления бота), добавить в журнал преподавателя группу, а также экспериментальный экран «текущая пара у всех преподавателей и групп».

**Источник требований:** сообщение пользователя от 2026-09-21 (5 тем), `docs/spec/task-schedule-management.md` (UC-SCH-11/12/13/14/17/33/34), `docs/spec/task-schedule-reference-data.md`.

---

## 1. Границы итерации

Четыре под-среза, выполняются строго по порядку, каждый — отдельный коммит с зелёными гейтами:

| Под-срез | Состав |
|----------|--------|
| **3A** | Профили звонков (+ большая перемена) с привязкой к дням недели и датам; «рабочие дни» (зеркало нерабочих); 5-дневное отображение расписания (Пн–Пт) с условными Сб/Вс; динамические колонки семестрового экспорта |
| **3B** | Практики: `Name`, несколько преподавателей, дни УП с числом пар, удаление `Organization`, пары УП в расписании, уведомления бота о начале/окончании, переименование раздела в «Практики» |
| **3C** | Журнал преподавателя: фильтр и группировка по группе (карточка на пару «группа + предмет») |
| **3D** | Экспериментальный раздел «Экспериментальное»: live-состояние пар по всем преподавателям и группам |

Не входит: организации студентов, отдельная сущность организации, расширение прав журнала, E2E для новых экранов (ручная проверка на 3 viewport'ах).

---

## 2. Подтверждённые решения

| Вопрос | Решение |
|--------|---------|
| Модель звонков | **Профили**: `BellProfile` (Обычный/Понедельник/Четверг/любые свои) со слотами и большой переменой; привязка к дням недели и/или диапазонам дат. Приоритет: дата → день недели → default |
| Пары УП | Дочерняя таблица `PracticeDay(practiceId, date, pairCount)`; в форме раскрывается по учебным дням диапазона с числом пар 6, редактируемым по каждому дню |
| ПП | Только карточка периода, пары не создаются |
| Организация | Поле `Organization` удаляется из практик; организации студентов не ведутся |
| Уведомления практик | В день начала и в день окончания, по времени дайджеста (`NotifyTime`); получатели — группа и все преподаватели практики |
| Дашборд | Раздел «Экспериментальное» в меню диспетчера (`/dispatcher/live`), роли Dispatcher/Admin; статусы «Идут / Закончились / Нет пар / Ожидание», автообновление 30 с |
| Переименование | Раздел называется «Практики» (вид остаётся «УП»/«ПП») |
| 5-дневка | Пн–Пт всегда; Сб/Вс — только если день сделан рабочим или есть непустой контент. В календаре месяца — 7 колонок. Экспорт семестра — динамические 5–7 колонок |
| Рабочие дни | Отдельная сущность `WorkingDayOverride` — зеркало `NonWorkingDay` + `SubstituteDayOfWeek` |

---

## 3. 3A. Звонки, рабочие дни, 5-дневное отображение

### 3.1. Модель данных

- `BellProfile : Entity` — `Name` (max 100), `IsDefault`, `DaysOfWeek` (`int[]`, 1..7; у default пусто).
- `BellProfileDate : Entity` — `ProfileId`, `DateFrom`, `DateTo` (диапазон; индексы по датам).
- `BellSlot` — +`Guid ProfileId`; уникальность `(ProfileId, NumberPair)` (`ix_bell_slots_profile_pair`); прежний `ix_bell_slots_number_pair` удаляется.
- `BigBreak` — +`Guid ProfileId`.
- `WorkingDayOverride : Entity` — `DateFrom`, `DateTo`, `int? SubstituteDayOfWeek` (1..5), `Title` (max 200).
- Сид: default-профиль (8 пар 08:30–20:55, перемена после 4-й 14:20–15:05), профиль «Понедельник» (6 пар с 09:10, значения из `ScheduleImportService.PairTimeSlots`), профиль «Четверг» (6 пар). Fallback на `PairTimeSlots` сохраняется.
- Миграция `AddBellProfilesAndWorkingDays`.

### 3.2. Резолв профиля

```
GetTimeMapAsync(DayOfWeek day, ct)                        // для paged-списка без даты: профиль по дню недели → default
GetTimeMapAsync(DateTime date, int? substituteDay, ct)     // профиль по дате → профиль substituteDay → профиль date.DayOfWeek → default
```

- Пустой справочник → пустой словарь (потребители сохраняют сохранённое время / fallback).
- `GetAsync/UpdateAsync` работают с default-профилем (обратная совместимость с `ScheduleEntryDialog`, мини-аппом, ботом).

### 3.3. API

```
GET    /api/bells                       → Result<BellProfileResponse>        (default; совместимость)
PUT    /api/bells                       → Result<BellProfileResponse>        (обновить default: slots + bigBreak)
GET    /api/bells/profiles              → Result<List<BellProfileResponse>>
POST   /api/bells/profiles              → Result<BellProfileResponse>        (создать, любой не-default)
PUT    /api/bells/profiles/{id}         → Result<BellProfileResponse>        (name, daysOfWeek, slots, bigBreak, dates)
DELETE /api/bells/profiles/{id}         → Result<null>                       (default удалять нельзя → 400)
GET    /api/bells/resolved?date=...     → Result<BellProfileResponse>        (резолв на дату)

GET    /api/working-days?from=&to=&page=&pageSize=  → Result<PagedResponse<WorkingDayResponse>>
POST   /api/working-days                → Result<WorkingDayResponse>         (Dispatcher,Admin)
PUT    /api/working-days/{id}           → Result<WorkingDayResponse>
DELETE /api/working-days/{id}           → Result<null>
```

`BellProfileResponse { id, name, isDefault, daysOfWeek[], slots[], bigBreak?, dates[] }`, `BellProfileDateResponse { id, dateFrom, dateTo }`.
Валидация: имя non-empty ≤100; `daysOfWeek` только 1..7; слоты — правила текущего `Validate`; default-профиль не удаляется и всегда существует; пересечение `WorkingDayOverride` с `NonWorkingDay` по датам → 409.

### 3.4. Отображение (5 дней + условные выходные + большая перемена)

- `ScheduleViewService`: Пн–Пт в неделе/семестре всегда; Сб/Вс — только при `WorkingDayOverride` на дату или непустом контенте (пары для соответствующего дня, практика, вставки). Воскресенье без override — пусто.
- Рабочий день: `entries` фильтруются по `SubstituteDayOfWeek ?? date.DayOfWeek`; в `ScheduleDayViewResponse` +`IsWorkingDay`, `SubstituteDayOfWeek`, `WorkingDayTitle`. Корректировки на Сб/Вс разрешены при наличии override.
- `ScheduleDayViewResponse.BigBreak` (`BigBreakResponse?`) — заполняется из разрешённого профиля; рендер в web (после пары `AfterPair`), мини-аппе (`DayFeed`), боте (`MessageFormatter`), экспорте.
- Экспорт семестра: колонки строятся динамически (5–7) по фактическому составу недель.

### 3.5. UI

- `/dispatcher/bells`: табы/селект профилей, редактор 8 строк слотов и большой перемены, выбор дней недели и диапазонов дат, создание/удаление профиля (кнопка удаления недоступна для default), сохранение.
- `/dispatcher/holidays` → заголовок «Календарь», две вкладки: «Нерабочие дни» (как есть) и «Рабочие дни» (период, день Пн–Пт, название). Меню `layout.tsx` → «Календарь».
- Неделя/семестр/день в web: 5 колонок Пн–Пт (+ колонки выходных только при контенте); бейдж «за понедельник» для рабочих суббот.
- Календарь месяца: 7 колонок, рабочие выходные — бейджем.

---

## 4. 3B. Практики

### 4.1. Модель данных

- `Practice` — +`Name` (max 100, «УП 01», «ПП 09»); −`Organization`; `TeacherId` → `ICollection<PracticeTeacher>`.
- `PracticeTeacher : Entity` — `PracticeId`, `TeacherId`; уникальный `(PracticeId, TeacherId)`.
- `PracticeDay : Entity` — `PracticeId`, `Date`, `PairCount` (1..8); уникальный `(PracticeId, Date)`; только для УП.
- CHECK `PairCount BETWEEN 1 AND 8` — идемпотентно в `Data/DbConstraints.cs` (PL/pgSQL), не в миграции.
- Миграция `AddPracticeNameTeachersDays` (drop `organization`).

### 4.2. API и импорт

- `PracticeRequest/Response`: `Name`, `TeacherIds[]`/`Teachers[]` (id + ФИО), `Days[]` (`date`, `pairCount`), `DateFrom`, `DateTo`, `Kind`, `GroupId/GroupName`, `Note`.
- Валидация: `Name` обязателен ≤100; ≥1 преподаватель; все преподаватели существуют; даты в семестре, `from <= to`; для УП `Days[]` непуст, каждая дата внутри периода, `pairCount` 1..8; пересечение периодов группы → 409 (как сейчас).
- Импорт XLSX: колонки «Вид, Название, Группа, Дата начала, Дата окончания, Преподаватели (через `;`), Примечание»; «Организация» удаляется; для УП число пар по дням не импортируется (задаётся вручную, по умолчанию 6).

### 4.3. Расписание и рендер

- УП-день (`PracticeDay` на дату при `Kind=Up`): `entries` = пары 1..`PairCount` с временем из разрешённого профиля звонков, `LessonType=Practice`, в `ScheduleResponse` +`PracticeName`; карточка практики остаётся.
- ПП-день — только карточка периода (как сейчас).
- Рендеры: web (`ScheduleLayers`, `ScheduleTable`, `ScheduleMonthCalendar`), мини-апп (`DayFeed`, `WeekFeed`), бот (`MessageFormatter`), экспорт (`ScheduleExportService`) — цвет/метка практики, название вместо «УП/ПП» при наличии.

### 4.4. Уведомления бота

- Новый `CollegeLMS.MaxBot/Services/PracticeNotifier.cs : BackgroundService` по образцу `ScheduleNotifier`: раз в цикл на дату рассылки, в `NotifyTime`:
  - день начала (`DateFrom == today`) → «🎓 Началась практика {Name} ({Kind}) · группа … (с … по …)»;
  - день окончания (`DateTo == today`) → «🏁 Закончилась практика {Name} …».
- Получатели: `UserSettings` с `GroupId == practice.GroupId`, плюс все `TeacherId` из практики. Нерабочий день не блокирует уведомление о практике.
- Идемпотентность — таблица `practice_notifications(practice_id, event, sent_on, UNIQUE(practice_id, event, sent_on))`, модель `Models/PracticeNotification.cs`, `DbSet` в `MaxBotDbContext`, идемпотентный DDL в `MaxBot/Program.cs`, регистрация hosted-сервиса там же.
- `MessageFormatter` +`FormatPracticeStarted/Finished`.
- `pageSize=100` в `GetPracticesAsync` — при необходимости добавить проход по страницам.

### 4.5. Переименование

`layout.tsx:56`, `dispatcher/practices/page.tsx:374`, `loading.tsx:4`, `error.tsx:13` → «Практики». Метки вида «УП — учебная»/«ПП — производственная» сохраняются.

---

## 5. 3C. Журнал преподавателя

- `IScheduleService.GetJournalAsync(Guid teacherId, string? subject, Guid? groupId, CancellationToken ct)`; группировка по `(GroupId, Subject)`; `JournalSubjectGroup` +`GroupId`, `GroupName`.
- `GET /api/schedule/journal?teacherId=&subject=&groupId=` — права не расширяются (Teacher — только свой, Admin/Dispatcher — любой, Student — нет).
- Web `teacher/journal/page.tsx`: селект «Группа» после выбора преподавателя, чипы предметов, карточка «Группа · Предмет». Мини-апп `max/JournalView.tsx` — то же.
- TS: `fetchJournal(teacherId?, subject?, groupId?)`.

---

## 6. 3D. Live-дашборд «Экспериментальное»

- `GET /api/dispatcher/live` (`Dispatcher,Admin`) → `{ now, date, week, isWorkingDay, teachers[], groups[] }`; элемент `{ id, name, status, currentPair?, nextPair?, entries[] }`.
- Статусы: `InLesson` («Идут»), `Finished` («Закончились»), `NoPairs` («Нет пар»), `Waiting` («Ожидание») — расчёт по времени разрешённого профиля звонков и практикам на дату.
- `Services/LiveDashboardService.cs`, `Dtos/LiveDashboardDtos.cs`, регистрация в `DashboardController`.
- UI: раздел «Экспериментальное» в `dispatcherMenu` и `lib/menus.ts` (`adminRoleMap["/dispatcher/live"] = ["Dispatcher","Admin"]`), страница `/dispatcher/live`: счётчики, плитки преподавателей и групп, автообновление 30 с, токены `primary`/`warning`/`muted`.

---

## 7. Тесты

- Backend unit: `BellScheduleServiceTests` (резолв профиля, приоритеты, валидация, default), `WorkingDayServiceTests` (CRUD, конфликт 409), `ScheduleViewServiceTests` (5 дней, условные Сб/Вс, рабочий день с подменой, BigBreak, пары УП), `PracticeServiceTests` (Name, несколько преподавателей, дни УП, валидация), `LiveDashboardServiceTests` (4 статуса).
- Backend integration: `BellScheduleControllerTests`, `WorkingDayControllerTests`, `PracticeControllerTests` (teachers/days), `ScheduleControllerTests` (journal groupId, live).
- Frontend: `npx tsc --noEmit`, `npm run lint`, затронутые Playwright-спеки.
- MaxBot: `PracticeNotifierTests`, `MessageFormatterTests` (форматы практик), `ScheduleViewClientTests`.

---

## 8. Риски

1. Сквозная смена сигнатуры `GetTimeMapAsync` — обновить 5 потребителей и тестовые заглушки.
2. Смена уникального индекса `bell_slots` — миграция должна корректно пересоздать индекс.
3. MaxBot — отдельная БД на `EnsureCreated` + ручной идемпотентный DDL: новую таблицу добавлять и в модель, и в SQL.
4. `pageSize=100`: `ScheduleViewService.LoadRangeAsync:230` и `CollegeLmsApiClient.GetPracticesAsync:551` — при >100 практик нужна пагинация.
5. Часовой пояс Europe/Moscow для «началась/закончилась»; даты практик — date-only.
6. Дубли уведомлений — защита журналом отправок.
7. Уникальность/пересечения практик валидируются только сервисом — без БД-констрейнта (гонки возможны, как и раньше).
