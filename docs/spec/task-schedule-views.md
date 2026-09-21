# ТЗ: Виды расписания и слои в веб и мини-приложении

> **Формулировка по факту реализации.** Документ описывает итерацию «Виды расписания»,
> реализованную в ветке `feature/schedule-views` (коммиты `84cd093`…`1e3331a`): серверные виды
> `GET /api/schedule?view=day|week|semester|calendar`, экспорт дня и недели (`scope`, FILE-3),
> четыре режима страницы `/schedule` и слои в мини-приложении Max. Схема БД не менялась
> (миграций нет). Источники: код `CollegeLMS.API/` (Services, Controllers, Dtos),
> `CollegeLMS.Next/`, спека `docs/superpowers/specs/2026-09-21-schedule-views-design.md`,
> план `docs/superpowers/plans/2026-09-21-schedule-views.md`.

## 1. Цель

Собрать расписание на сервере одним запросом для каждого вида — **день**, **неделя**,
**календарь месяца**, **семестр** — с единым слиянием слоёв: звонки, вставки, практики,
нерабочие дни, бейджи корректировок. Веб-страница `/schedule` получает 4 режима с состоянием
в URL и персональным контекстом; мини-приложение Max — слои в существующих экранах «День»
и «Неделя». Экспорт дня и недели использует те же слои и имена файлов FILE-3.

## 2. Роли и доступ

| Роль | Права |
|---|---|
| `AllowAnonymous` | `GET /api/schedule` (виды `view=…` и постраничный список), `GET /api/schedule/meta`, `GET /api/schedule/export` (rate-limit `ExportPolicy`) |
| Все авторизованные | `GET /api/schedule/context` — личный контекст (группа студента / преподаватель) |
| Веб-страница `/schedule` | Внутри `(authenticated)`; кнопка «Экспорт» доступна авторизованному пользователю |

## 3. Модель данных

Схема БД не изменялась. Виды читают существующие сущности:

| Сущность | Использование в видах |
|---|---|
| `NonWorkingDay` | нерабочие даты (`DateFrom…DateTo`, `Title`) — приоритет после воскресенья |
| `Practice` | практики УП/ПП (`Kind`, `GroupId`, `TeacherId`, `DateFrom`, `DateTo`) |
| `ScheduleInsert` | активные вставки дня (`DayOfWeek`, `Course?`, `IsActive`) |
| `ScheduleEntry` | пары (`DayOfWeek`, `NumberPair`, `Weeks`) по фильтрам |
| `ScheduleHistory` | бейджи корректировок недели (`Add`/`Remove`/`Replace`/`Move`) |
| `BellSlot` | время пар по номеру (`IBellScheduleService.GetTimeMapAsync`) |

Семестр — константы `StudyWeek`: `SemesterStart = 01.09.2026`, `TotalWeeks = 17`;
неделя 1 начинается с понедельника **31.08.2026**, последний день семестра — **27.12.2026**
(`MondayOf(SemesterStart) + 17·7 − 1`). `WeekOf` для дат вне семестра даёт ≥ 1, в видах
значение клампится в `1…17`.

## 4. API (REST)

Все ответы — `Result<T>` (200). Личные/серверные сообщения об ошибках — на русском.
API-контракт `view` реализован в `ScheduleViewService`, диспетчеризация — в `ScheduleController.GetAll`.

### 4.1. День — `view=day`

```
GET /api/schedule?view=day&date=YYYY-MM-DD&groupId=&teacherId=&room=
```

| Поле ответа `ScheduleDayViewResponse` | Значение |
|---|---|
| `date` | целевая дата; `date` не задан → сегодня (UTC) |
| `week` | `Math.Clamp(WeekOf(date), 1, 17)` |
| `dayOfWeek` | `(int)date.DayOfWeek` (0 — Вс) |
| `isSunday`, `isNonWorking`, `nonWorkingTitle` | признаки дня |
| `practices[]` | `PracticeResponse[]` |
| `inserts[]` | `ScheduleInsertResponse[]` |
| `entries[]` | `ScheduleResponse[]` (с `changeTags` недели) |

Слои — по приоритету §5. Невалидный формат `date` (не `YYYY-MM-DD`) → `400` «Неверный формат даты.»
в обёртке `Result<T>`.

### 4.2. Неделя — `view=week`

```
GET /api/schedule?view=week&week=4&date=&groupId=&teacherId=&room=
```

- `week` — приоритетный параметр; если не задан, вычисляется по `date`, иначе — текущая;
  значение клампится в `1…TotalWeeks`. `room` поддерживается.
- Ответ `ScheduleWeekViewResponse`: `week`, `weekStart` (понедельник) и `days[6]` — Пн–Сб;
  каждый день — структура `view=day`.

### 4.3. Календарь месяца — `view=calendar`

```
GET /api/schedule?view=calendar&month=YYYY-MM&groupId=&teacherId=&room=
```

- `month` не задан → текущий месяц; невалидный формат → `400` «Неверный формат месяца.».
- Ответ `ScheduleMonthViewResponse`: `year`, `month`, `days[]` всех дат месяца; день:
  `date`, `dayOfWeek`, `isSunday`, `isNonWorking`, `nonWorkingTitle`, `practiceKinds`
  (`PracticeKind[]` — `Up`/`Pp`), `isOutOfSemester`, `pairCount`.
- `pairCount` — число пар по правилам §5; `0` в воскресенье, нерабочий день, практику и вне
  семестра. Сетка 7×N (ведущие/замыкающие дни) достраивается на клиенте.

### 4.4. Семестр — `view=semester`

```
GET /api/schedule?view=semester&groupId=|teacherId=
```

- Требуется ровно один из `groupId`/`teacherId`, иначе `400`
  «Укажите одну группу или одного преподавателя.».
- Ответ `ScheduleSemesterViewResponse`: `totalWeeks` (17) и `weeks[]`
  (`week`, `weekStart`, `days[6]`); каждая ячейка — структура `view=day`.
- Данные загружаются одним диапазоном на весь семестр (без N+1).

### 4.5. Обратная совместимость

- `view` не задан или неизвестен → прежний постраничный список
  `IScheduleService.GetAllAsync` (`page`, `pageSize`, `dayOfWeek`, `period`, `week`, `date`).
- `view=calendar` без `month` больше **не** возвращает «шаблон недели» — это календарь
  текущего месяца. `CalendarResponse`/`CalendarDayResponse` и `IScheduleService.GetCalendarAsync`
  удалены.
- Бот Max и мини-приложение `view=calendar` не вызывают.

## 5. Правила слоёв (канонический приоритет на дату)

1. **Воскресенье** → `isSunday = true`; `entries`, `inserts`, `practices` пусты.
2. **Нерабочий день** (дата входит в `DateFrom…DateTo`) → `isNonWorking = true`,
   `nonWorkingTitle = Title`; `entries`, `inserts`, `practices` пусты (в том числе вне семестра).
3. **Дата вне семестра** (до 31.08.2026 или после 27.12.2026) → пустой день; `week` клампится.
4. **Практика** (для группы — по `GroupId`, для преподавателя — по `TeacherId`, дата в периоде)
   → `practices[]`; `entries` и `inserts` пусты.
5. **Обычный день**:
   - `inserts` — только активные (`IsActive`), по дню недели; при заданной группе
     `Course == null || Course == Course группы`; без группы — все активные;
   - `entries` — по фильтрам `groupId`/`teacherId`/`room`, неделя входит в `Weeks`; сортировка
     по `NumberPair`; `StartTime`/`EndTime` подменяются из справочника звонков;
   - `changeTags` — из `ScheduleHistory` недели (`ScheduleChangeTags`), пары с примечанием
     «сам.р.» присутствуют с бейджем.

Слои дня, недели и семестра собираются из общей загрузки `LoadRangeAsync` (один диапазон,
один `BuildDay` на дату). Месячный вид считает `pairCount`/`practiceKinds` по тем же правилам.

## 6. Экспорт — `/api/schedule/export`

```
GET /api/schedule/export?scope=day|week|semester&format=pdf|xlsx&layout=grid|daycards
    &groupId=&teacherId=&room=&date=&week=
```

| `scope` | Поведение | Имя файла (FILE-3) |
|---|---|---|
| не задан | легаси-экспорт всего расписания по фильтрам (бот/мини-апп) | `Расписание_dd.MM.yyyy_HH-mm-ss.xlsx\|pdf` |
| `day` | день по `date` (по умолчанию сегодня) со слоями | `Расписание_день_dd.MM.yyyy_HH-mm-ss.xlsx\|pdf` |
| `week` | неделя Пн–Сб по `week` (1–17, по умолчанию текущая) или `date` | `Расписание_неделя_dd.MM.yyyy_HH-mm-ss.xlsx\|pdf` |
| `semester` | матрица «недели 1–17 × Пн–Сб»; ровно один из `groupId`/`teacherId` | `Расписание_семестр_dd.MM.yyyy_HH-mm-ss.xlsx\|pdf` |

- Состав day/week — из `IScheduleViewService` (те же слои): время звонков, бейджи, вставки,
  практики, нерабочие дни; в XLSX/PDF: `grid` — таблица «№ пары × дни», `daycards` — блоки дней.
- Ошибки: нерабочий день → `400` «Нерабочий день: {Title}»; воскресенье или пустой день/неделя
  → `404` «Нет данных для экспорта»; неизвестный `scope` → `400`
  «Укажите scope: day, week или semester.».
- Параметр `period` удалён (замена — `scope` + `date`/`week`) во всех вызовах.

## 7. Веб `/schedule`

### 7.1. Режимы и состояние

Переключатель «День / Неделя / Календарь / Семестр» (`ScheduleViewSwitcher`). Состояние в URL
через `history.replaceState`:

- `?view=day&date=2026-09-21`
- `?view=week&week=4`
- `?view=calendar&month=2026-09`
- `?view=semester`

Старые deep-link'и из «Изменений» (`?week=5&day=3` без `view`) конвертируются в «День»:
дата = понедельник недели 1 + (week − 1)·7 + смещение дня (Пн = 0…Вс = 6; `day=7` — воскресенье).

### 7.2. Поведение

| Режим | Содержание |
|---|---|
| **День** | `DayNavigation` (‹ / пикер / › / «Сегодня»), карточки пар (`ScheduleTable`, «Сейчас идёт»), вставки отдельной строкой, карточка практики, «Нерабочий день: {название}» / «Выходной»; диспетчерские «Добавить»/редактирование/удаление |
| **Неделя** | `WeekNavigation` по `meta.totalWeeks`; ≥lg — 6 колонок, <lg — вертикальные дни; в дне вставки, пары с бейджами, практика, «Не работает: {название}», пусто — «Нет пар» |
| **Календарь** | сетка месяца: число, `pairCount`, маркеры праздника (иконка с подсказкой) и практик УП/ПП; воскресенья и даты вне семестра приглушены; клик → «День» |
| **Семестр** | матрица «недели × Пн–Сб», sticky-столбец недель, горизонтальный скролл; в ячейке пары (номер, предмет, аудитория, бейджи), вставки, практика, «Не работает»; клик → «День»; без фильтра — приглашение выбрать (запрос не выполняется) |

### 7.3. Общее

- **Дефолт пользователя:** `GET /api/schedule/context`; студент → своя группа, преподаватель →
  он сам; «Сбросить» возвращает к дефолту; для остальных ролей — без фильтра.
- **Семестр:** `semesterStart`/`totalWeeks` — из `GET /api/schedule/meta`; хардкод
  `2026-09-01`/`52` из компонентов удалён.
- **Экспорт-меню:** PDF/XLSX × «Сетка»/«По дням»; scope по режиму (`day`+`date`,
  `week`+`week`, `semester`); в «Календаре» экспорт недоступен.
- **UI-состояния:** UI-1 загрузка (скелетон/спиннер), UI-2 «Нет данных», UI-3 ошибка +
  «Повторить», UI-4 тосты после мутаций.
- **Адаптивность:** 393px, 1366×768, 1920+; touch-target ≥ 44×44.

### 7.4. Компоненты и клиент

- Новые: `ScheduleViewSwitcher`, `WeekNavigation`, `ScheduleDayView`, `ScheduleWeekView`,
  `ScheduleMonthCalendar`, `ScheduleSemesterMatrix`, `ScheduleLayers`.
- Удалены: `DayTabs.tsx`, `SemesterView.tsx`.
- Клиент: `api/schedule.ts` — `fetchDayView`, `fetchWeekView`, `fetchSemesterView`,
  `fetchMonthView`, `exportSchedule(filters, format, layout, scope, { date, week })`;
  `fetchScheduleCalendar` удалён. Типы — в `types/schedule.ts`
  (`ScheduleDayView`, `ScheduleWeekView`, `ScheduleSemesterView`, `ScheduleMonthView`).

## 8. Мини-приложение Max

- Данные — из `view=day` (`fetchDayView`) и `view=week` (`fetchWeekView`); deep-link'и
  `today`/`day`/`week`/`schedule` сохранены, `view=calendar` не используется.
- **День** (`DayFeed`): блок «Вставки», карточка практики (заменяет пары),
  «Нерабочий день: {название}», воскресенье — «Выходной».
- **Неделя** (`WeekFeed`): маркеры практики (УП/ПП), нерабочего дня и вставок; пустые дни —
  «Нет пар»; диапазон недели — Пн–Сб.
- Тёмная тема, touch-target ≥ 44×44.

## 9. Тестирование

| Уровень | Файл | Покрытие |
|---|---|---|
| Unit | `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs` | приоритет слоёв (воскресенье → нерабочий → практика), фильтр вставок по курсу/активности, время из звонков, бейджи, неделя из даты, 6 дней Пн–Сб, клампинг, месяц (`pairCount`, `practiceKinds`, `isOutOfSemester`), семестр (17 недель × 6 дней, ровно один фильтр) |
| Unit | `CollegeLMS.Tests/Unit/Services/ScheduleExportServiceTests.cs` | `scope=day/week`: слои, `400` нерабочий, `404` пусто, регулярка FILE-3 |
| Интеграционные | `CollegeLMS.Tests/Integration/Controllers/ScheduleControllerTests.cs` | `view=day/week/semester/calendar` (коды и форма), неизвестный `view` → paged-список |
| Регресс | Весь `CollegeLMS.Tests` (566), `CollegeLMS.MaxBot.Tests` (123) | зелёные на HEAD `1e3331a` |
| Frontend | `npm run build` | сборка Next.js |

## 10. Требования к реализации

- `Result<T>` везде, без try-catch в контроллерах/сервисах; сообщения — на русском.
- `AsNoTracking()` на чтении; `CancellationToken ct` во всех async-методах.
- Бейджи корректировок — единый построитель `ScheduleChangeTags.BuildAsync` (без дублирования
  между `ScheduleService` и `ScheduleViewService`).
- Swagger: `[SwaggerOperation]`, `[SwaggerResponse]`, `[ProducesResponseType]`, русские
  XML-комментарии; пример `SwaggerExamples/ScheduleDayViewExample.cs`.
- Имена файлов экспорта — FILE-3 (`BuildFileName`), без параметра `period`.

## 11. Документация

- Postman: `docs/spec/CollegeLMS.postman_collection.json` — папка **Schedule (MAX)**:
  `Get schedule (view=day|week|calendar|semester)` с параметрами и примерами ответов,
  `Export schedule`, `Export schedule (day)`, `Export schedule (week)` с FILE-3.
- Спека дизайна: `docs/superpowers/specs/2026-09-21-schedule-views-design.md`;
  план: `docs/superpowers/plans/2026-09-21-schedule-views.md`.
- ТЗ итерации: этот документ.

## 12. Трассируемость и известные расхождения

| UC | Покрытие итерации |
|----|-------------------|
| UC-SCH-01 | «День»/«Неделя»/«Семестр», дефолт группы студента, время из звонков, нерабочие/воскресенье, UI-1…UI-3 |
| UC-SCH-02 | То же для преподавателя, дефолт «сам», сортировка по номеру пары |
| UC-SCH-03 | Веб-календарь месяца: количество пар, праздники с подсказкой, практики, клик → день; ограничение календаря семестром в боте — вне итерации |
| UC-SCH-04 | Бейджи в вебе (уже были), мини-аппе и экспорте дня/недели (новое) |
| UC-SCH-05 | Вставки в дне/неделе/семестре (веб и мини-апп) отдельной строкой |
| UC-SCH-09 | Экспорт дня/недели: слои, бейджи, FILE-3, `400`/`404` |
| UC-SCH-14 | Нерабочие дни во всех видах и экспорте дня/недели |
| UC-SCH-17 | Практики поверх расписания: карточка вместо пар, маркеры в календаре/семестре |
| UC-SCH-41 | Мини-апп: слои в «Дне» и «Неделе» |

**Расхождения:**

- `UC-SCH-09` требует `AUTH-1` для экспорта — `GET /api/schedule/export` остаётся
  `AllowAnonymous` (зафиксировано в `task-schedule-reference-data.md`); веб-кнопка экспорта
  доступна только авторизованным (страница в `(authenticated)`). Кандидат на отдельное решение.
- Практики в видах запрашиваются с `pageSize = 100` (`IPracticeService` ограничивает 1–100);
  при > 100 практиках в диапазоне возможна неполнота слоя практик — известное ограничение.
