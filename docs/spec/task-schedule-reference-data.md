# ТЗ: Справочники расписания (звонки, нерабочие дни, вставки, практики) и интеграция слоёв

> **Формулировка по факту реализации.** Документ описывает backend-фичи, реализованные
> коммитами `1290aca` (справочники расписания) и `9834231` (семестровый экспорт,
> бейджи журнала), и их влияние на расписание, корректировки, экспорт, веб и бота Max.
> Источник: код `CollegeLMS.API/`, `CollegeLMS.MaxBot/`, спека
> `docs/superpowers/specs/2026-09-18-schedule-reference-features-design.md`.

## 1. Цель

Единый источник времени пар, управление нерабочими датами, специальными вставками и
практиками УП/ПП; отображение нерабочих дней и практик во всех интерфейсах; семестровый
экспорт расписания; бейджи корректировок в журнале преподавателя.

## 2. Роли

| Роль | Права |
|---|---|
| `AllowAnonymous` | `GET /api/bells`, `GET /api/schedule/inserts`, `GET /api/practices`, `GET /api/schedule/export` |
| Все авторизованные | `GET /api/non-working-days`, `GET /api/working-days`, `GET /api/schedule/journal` (Teacher/Admin/Dispatcher) |
| `Dispatcher`, `Admin` | `PUT /api/bells`, CRUD звонков-профилей, нерабочих и рабочих дней, вставок, практик, импорт практик |

## 3. Модель данных

| Сущность | Таблица | Поля | Правила/индексы |
|---|---|---|---|
| `BellProfile` | `bell_profiles` | `Name` (≤100), `IsDefault`, `DaysOfWeek` (`int[]`, 1–7) | `ix_bell_profiles_name` UNIQUE; базовый профиль один |
| `BellProfileDate` | `bell_profile_dates` | `ProfileId`, `DateFrom`, `DateTo` | FK cascade; индексы `ix_bell_profile_dates_date_from/date_to` |
| `BellSlot` | `bell_slots` | `ProfileId`, `NumberPair` (1–8), `StartTime`, `EndTime` | `ix_bell_slots_profile_pair` UNIQUE (`ProfileId`, `NumberPair`); 07:00–21:00; возрастание; без пересечений |
| `BigBreak` | `big_breaks` | `ProfileId`, `AfterPair` (1–8), `StartTime`, `EndTime` | не более одной на профиль; 07:00–21:00; `StartTime < EndTime` |
| `NonWorkingDay` | `non_working_days` | `DateFrom`, `DateTo`, `Title` (≤200) | `DateFrom ≤ DateTo`; индексы `ix_non_working_days_date_from/date_to` |
| `WorkingDayOverride` | `working_day_overrides` | `DateFrom`, `DateTo`, `SubstituteDayOfWeek?` (1–5), `Title` (≤200) | `DateFrom ≤ DateTo`; пересечение с нерабочими датами → `409`; индексы по датам |
| `ScheduleInsert` | `schedule_inserts` | `Title` (≤200), `DayOfWeek`, `StartTime`, `EndTime`, `Course?` (1–4), `IsActive` | `DayOfWeek` хранится строкой (`HasConversion<string>`, ≤20); `ix_schedule_inserts_day_of_week` |
| `Practice` | `practices` | `Kind` (`Up`/`Pp`), `Name` (≤100), `GroupId`, `DateFrom`, `DateTo`, `Note?` (≤500) | `Kind` строкой; `ix_practices_group_id/date_from`; FK `Group` cascade; преподаватели — через `PracticeTeacher`, без `Organization` |
| `PracticeTeacher` | `practice_teachers` | `PracticeId`, `TeacherId` | join многие-ко-многим; `ix_practice_teachers_practice_teacher` UNIQUE; FK `Practice`/`Teacher` cascade |
| `PracticeDay` | `practice_days` | `PracticeId`, `Date`, `PairCount` (1–8) | только для УП; `ix_practice_days_practice_date` UNIQUE; CHECK `ck_practice_days_pair_count_range`; FK `Practice` cascade |

Все сущности наследуют `Entity` (`Id` GUID `ValueGeneratedNever()`, `CreatedAt`, `UpdatedAt`).

### 3.1. Сиды

- **Профили звонков** (`BellProfile`): «Обычный» (базовый, 8 пар —
  1) 08:30–09:50, 2) 10:00–11:20, 3) 11:30–12:50, 4) 13:00–14:20,
  5) 15:05–16:25, 6) 16:35–17:55, 7) 18:05–19:25, 8) 19:35–20:55;
  большая перемена после 4-й пары 14:20–15:05), «Понедельник» (6 пар с 09:10)
  и «Четверг» (6 пар) — времена из легаси-сетки `ScheduleImportService.PairTimeSlots`.
- **Вставки** (`ScheduleInsert`): «Разговор о важном» — Пн 08:30–09:00 (все курсы, активна);
  «Классный час» — Чт 12:10–13:00 (все курсы, активна).
- **Нерабочие дни** и **практики**: сидов нет, наполняются через API.

## 4. API (REST)

### 4.1. Звонки — `/api/bells`

| Метод | Роль | Описание |
|---|---|---|
| `GET /api/bells` | AllowAnonymous | `Result<BellProfileResponse>` базового профиля: `slots[]`, `bigBreak` или `null` |
| `PUT /api/bells` | Dispatcher/Admin | Полная замена слотов базового профиля. Тело `UpdateBellScheduleRequest { slots[], bigBreak? }`; время — `HH:mm:ss` |
| `GET /api/bells/profiles` | AllowAnonymous | `Result<List<BellProfileResponse>>` — все профили |
| `POST /api/bells/profiles` | Dispatcher/Admin | Создать профиль (`BellProfileRequest { name, daysOfWeek[], slots[], bigBreak?, dates[] }`) |
| `PUT /api/bells/profiles/{id}` | Dispatcher/Admin | Изменить профиль (`400` — некорректные данные, `404` — не найден) |
| `DELETE /api/bells/profiles/{id}` | Dispatcher/Admin | Удалить профиль (`400` — базовый профиль удалить нельзя) |
| `GET /api/bells/resolved?date=` | AllowAnonymous | `Result<BellProfileResponse>` — профиль, действующий на дату |

Валидации `400` (сообщения на русском): не менее одной пары и не более 8; номер пары 1–8;
дубликаты номеров; время в 07:00–21:00; `StartTime < EndTime`; возрастание и отсутствие
пересечений; для большой перемены — `AfterPair` 1–8, диапазон, `Start < End`; имя профиля
не пустое и ≤100; `daysOfWeek` — 1–7 без дублей.
Резолв профиля на дату: привязка к дате → подменяемый день недели → день недели даты → базовый профиль.
`BellScheduleService.GetTimeMapAsync(day|date, …)` — время пар; пустой словарь, если справочник не задан.

### 4.2. Нерабочие дни — `/api/non-working-days` (`[Authorize]`)

| Метод | Роль | Описание |
|---|---|---|
| `GET /?from=&to=&page=&pageSize=` | все | `PagedResponse<NonWorkingDayResponse>`; фильтр пересечения периода; pageSize 1–100 (default 20) |
| `POST /` | Dispatcher/Admin | Тело `NonWorkingDayRequest { dateFrom, dateTo, title }` |
| `PUT /{id:guid}` | Dispatcher/Admin | `404` — не найдено |
| `DELETE /{id:guid}` | Dispatcher/Admin | `404` — не найдено |

Валидации `400`: `Title` не пустой и ≤200; `DateFrom`/`DateTo` заданы; `DateFrom.Date ≤ DateTo.Date`.
Даты нормализуются до `.Date`.

### 4.2.1. Рабочие дни — `/api/working-days` (`[Authorize]`)

| Метод | Роль | Описание |
|---|---|---|
| `GET /?from=&to=&page=&pageSize=` | все | `PagedResponse<WorkingDayResponse>`; фильтр пересечения периода; pageSize 1–100 (default 20) |
| `POST /` | Dispatcher/Admin | Тело `WorkingDayRequest { dateFrom, dateTo, substituteDayOfWeek?, title }` |
| `PUT /{id:guid}` | Dispatcher/Admin | `404` — не найдено; `409` — пересечение с нерабочими датами |
| `DELETE /{id:guid}` | Dispatcher/Admin | `404` — не найдено |

Рабочий день делает выходную/праздничную дату рабочей и подставляет расписание дня
`SubstituteDayOfWeek` (1–5, Пн–Пт), если он задан. Валидации `400`: `Title` не пустой и ≤200;
даты заданы; `DateFrom.Date ≤ DateTo.Date`; `SubstituteDayOfWeek` 1–5 или `null`.
Пересечение с любым нерабочим днём → `409` «Дата уже отмечена как нерабочая.».

### 4.3. Вставки — `/api/schedule/inserts`

| Метод | Роль | Описание |
|---|---|---|
| `GET /?dayOfWeek=&course=&activeOnly=true` | AllowAnonymous | `Result<List<ScheduleInsertResponse>>`; при `course` отдаются вставки курса и общие (`Course == null`) |
| `POST /` | Dispatcher/Admin | Тело `ScheduleInsertRequest { title, dayOfWeek (int), startTime, endTime, course?, isActive }` |
| `PUT /{id:guid}` | Dispatcher/Admin | `404` — не найдено |
| `DELETE /{id:guid}` | Dispatcher/Admin | `404` — не найдено |

В ответе `dayOfWeek` — **число** (`(int)DayOfWeek`). Валидации `400`: `Title` ≤200; `DayOfWeek ≠ Sunday`
(воскресенье запрещено); `StartTime < EndTime`; `Course` 1–4 или `null`.

### 4.4. Практики — `/api/practices`

| Метод | Роль | Описание |
|---|---|---|
| `GET /?groupId=&teacherId=&kind=&from=&to=&page=&pageSize=` | AllowAnonymous | `PagedResponse<PracticeResponse>` (`kind` — `Up`/`Pp` строкой; `name`, `groupName`, `teacherIds[]`, `teachers[] { id, name }`, `days[] { date, pairCount }`); фильтр `teacherId` — по связи с преподавателями; pageSize 1–100 |
| `POST /` | Dispatcher/Admin | `PracticeResponse`; `409` — пересечение практик группы |
| `PUT /{id:guid}` | Dispatcher/Admin | `404` — не найдено; `409` — пересечение; преподаватели и дни УП заменяются целиком |
| `DELETE /{id:guid}` | Dispatcher/Admin | `404` — не найдено |
| `POST /import/preview` | Dispatcher/Admin | multipart `file` (`.xlsx`, ≤10 МБ). `PracticeImportPreviewResponse { totalRows, rows[], errors[] }` |
| `POST /import/confirm` | Dispatcher/Admin | `PracticeImportConfirmRequest { rows[] }` → `{ imported, practices[] }`; только без ошибок, одна транзакция |

Валидации CRUD `400`: `Kind` определён; `Name` не пусто и ≤100; период задан; `DateFrom.Date ≤ DateTo.Date`;
период в пределах семестра (`StudyWeek`, старт 01.09.2026, 17 недель); группа найдена; ≥1 преподаватель
и все существуют; для УП `Days[]` непуст, каждая дата внутри периода и семестра, `PairCount` 1–8, даты без
дублей; для ПП `Days` игнорируется. Пересечение практик одной группы → `409`
«У группы уже есть практика в этот период.».

Импорт XLSX (ClosedXML): шапка в строке 1 (`Вид | Название | Группа | Дата начала | Дата окончания | Преподаватель | Примечание`),
данные со строки 2; `Kind` — «УП»/«UP» → `Up`, «ПП»/«PP» → `Pp`; даты `dd.MM.yyyy`;
в колонке «Преподаватель» допускается несколько ФИО через «;». Для УП число пар по дням не импортируется —
дни создаются автоматически (Пн–Пт периода, по 6 пар), далее редактируются вручную.
Ошибки формата `ScheduleValidationError { row, column, level="data", message }` с префиксом «строка N: …»
(вид, название, группа, дата начала/окончания, преподаватели). Confirm повторно валидирует строки, при ошибках — `400`
со списком сообщений; запись пакетом в транзакции с rollback при сбое.

### 4.5. Экспорт — `/api/schedule/export`

```
GET /api/schedule/export?scope=day|week|semester&format=pdf|xlsx&layout=grid|daycards
    &groupId=&teacherId=&room=&period=&date=&week=
```

- `scope=semester` — семестровый вид: матрица «недели (1–17) × дни Пн–Сб». В ячейке: нерабочая дата →
  «Не работает: {Title}»; практика → «УП|ПП: {название} · {группа} · {преподаватели}»; иначе вставки
  (`HH:mm–HH:mm {Title}`) и пары (номер, предмет, группа, аудитория, преподаватель, бейджи истории).
- Для `scope=semester` требуется **ровно одна** из `groupId`/`teacherId`, иначе `400`
  «Укажите одну группу или одного преподавателя.».
- Имя файла: `Расписание_семестр_dd.MM.yyyy_HH-mm-ss.xlsx|pdf` (отдельно для XLSX и PDF).
- `period=day` на нерабочую дату → `400` «Нерабочий день: {Title}»; пустой результат → `404` «Нет данных для экспорта».
- Время пар в экспорте подменяется из справочника звонков; rate-limit `ExportPolicy`.

### 4.6. Журнал — `/api/schedule/journal` (Teacher/Admin/Dispatcher)

`GET /api/schedule/journal?teacherId=&subject=` — `Result<JournalResponse>`:
`teacherId`, `teacherName`, `subjects[]` (`subject`, `items[]`, `pairCount`), `totalPairCount`.
`subject` — новый фильтр по точному названию предмета. `items[]`: `week`, `dayOfWeek` (int),
`date`, `numberPairs[]`, `changeTypes[]` — **бейджи корректировок**: `Add`, `Replace`, `Move`, `Remove`,
`SelfStudy` (для снятия с примечанием «сам.р.»). Показываются только проведённые занятия (дата ≤ сегодня, UTC).
`400` — не определён преподаватель; `403` — чужой `teacherId`; `404` — преподаватель не найден.

## 5. Влияние на слои

- **Звонки** — единый источник времени: чтение расписания (`GET /api/schedule`), корректировки,
  семестровый экспорт подменяют `StartTime`/`EndTime` по `NumberPair` через `GetTimeMapAsync`.
- **Нерабочие дни**:
  - создание/применение корректировки на нерабочую дату → `400` «Дата нерабочая: {Title}.»;
  - `GET /api/schedule?date=…` на нерабочую дату → пустой список (0 записей);
  - экспорт дня (`period=day`) → `400`;
  - бот (`ShowDayAsync`) показывает «🎉 Нерабочий день: {Title}», рассылка `ScheduleNotifier` пропускается;
  - в семестровом экспорте ячейка «Не работает: {Title}».
- **Вставки** — отдельная строка дня без номера пары; видны в вебе, мини-приложении, боте (блок
  «🎓 Вставки:») и семестровом экспорте; неактивные не отдаются при `activeOnly=true`.
- **Практики** — в период практики группы обычные пары заменяются карточкой УП/ПП в дне бота
  (`FormatPracticeLine`) и в семестровом экспорте; для УП дополнительно выводятся пары 1..`PairCount`
  (`LessonType=Practice`, `isPractice=true`, `practiceName`) с временем из звонков; в расписании
  преподавателя отображается блок практики.
- **Журнал** — бейджи корректировок, снятые даты скрыты (только проведённые), фильтр по предмету.

## 6. Фронтенд

- Диспетчер: `/dispatcher/bells`, `/dispatcher/holidays`, `/dispatcher/inserts`, `/dispatcher/practices`
  (+ `loading.tsx`/`error.tsx`), API-клиенты `api/bells.ts`, `api/nonWorkingDays.ts`, `api/inserts.ts`, `api/practices.ts`.
- Веб: `/changes` (изменения) и `/teacher/journal` (журнал с бейджами и фильтром по предмету).

## 7. Требования к реализации

- `Result<T>`; `AsNoTracking()` на чтении; сообщения и Swagger — на русском.
- GUID PK `ValueGeneratedNever()`, enum `HasConversion<string>()`, `HasMaxLength()` для строк.
- XLSX — ClosedXML; семестровый экспорт — ClosedXML и QuestPDF; имена файлов — FILE-3.

## 8. Документация

- Postman: `docs/spec/CollegeLMS.postman_collection.json` (папка **Schedule reference data**, обновлённые
  `Get journal` и `Export schedule`).
- Спека дизайна: `docs/superpowers/specs/2026-09-18-schedule-reference-features-design.md`;
  план: `docs/superpowers/plans/2026-09-18-schedule-reference-features.md`.
