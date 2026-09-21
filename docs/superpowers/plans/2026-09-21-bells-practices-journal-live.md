# План: звонки по дням, практики, журнал по группам, live-дашборд (Срез 3)

> **Для агентов:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` (рекомендуется) или `superpowers:executing-plans`. Выполнять по задачам, отмечая шаги `[ ]`.
> **Спека:** `docs/superpowers/specs/2026-09-21-bells-practices-journal-live-design.md`.

**Цель:** профили звонков с привязкой к дням/датам + рабочие дни + 5-дневное отображение; практики с названием, несколькими преподавателями и парами УП по дням + уведомления бота; журнал по группе; live-экран «текущая пара у всех».

**Архитектура:** новые сущности `BellProfile`/`BellProfileDate`/`WorkingDayOverride`/`PracticeTeacher`/`PracticeDay`; `BellScheduleService` получает резолв профиля на дату и становится единственным источником времени пар; `ScheduleViewService` учитывает рабочие дни, условные выходные и пары УП; `PracticeNotifier` в MaxBot шлёт уведомления о начале/окончании; `LiveDashboardService` считает текущее состояние; в `CollegeLMS.MaxBot` отдельная БД — новую таблицу добавлять и в модель, и в идемпотентный DDL.

**Стек:** .NET 10, EF Core (Npgsql), ClosedXML, QuestPDF, Next.js 14 (App Router), TypeScript, Tailwind CSS 4, shadcn/ui, xUnit/Moq/Bogus.

**Ветка:** `master` (проект ведётся коммитами в `master`; CI — `pull_request`, полный прогон — через PR в конце итерации).

## Глобальные ограничения

- `Result<T>` во всех сервисах, без try-catch в контроллерах/сервисах; сообщения на русском.
- `AsNoTracking()` на чтении, `CancellationToken ct` во всех async-методах, `List<T>` вместо `IEnumerable<T>`.
- Primary constructor DI, плоские DTO, file-scoped namespaces, ручные мапперы.
- GUID PK `ValueGeneratedNever()`, строки `HasMaxLength()`, enum-ы строками, nav `[JsonIgnore]`.
- Индексы — в EF Configuration (`HasIndex` + `HasDatabaseName`); CHECK — идемпотентный PL/pgSQL в `Data/DbConstraints.cs`.
- Swagger: `[SwaggerOperation]`, `[ProducesResponseType]`, русские XML-комментарии; Postman обновляется.
- Гейты каждой задачи: `dotnet build CollegeLMS.slnx`; таргетные тесты; `npx tsc --noEmit`; `dotnet csharpier format .`; финал — полный `dotnet test` + `npm run build` + PR.
- Локальный Docker не поднимаем.

---

## Структура файлов

**Создаются (3A):**
- `CollegeLMS.API/Entities/BellProfile.cs`, `Entities/BellProfileDate.cs`, `Entities/WorkingDayOverride.cs`
- `CollegeLMS.API/Data/Configurations/BellProfileConfigurations.cs`, `Data/Configurations/WorkingDayOverrideConfiguration.cs`
- `CollegeLMS.API/Dtos/BellProfileDtos.cs`, `Dtos/WorkingDayDtos.cs`
- `CollegeLMS.API/Interfaces/IWorkingDayService.cs`, `Services/WorkingDayService.cs`
- `CollegeLMS.API/Controllers/WorkingDayController.cs`
- `CollegeLMS.API/SwaggerExamples/BellProfileExample.cs`
- `CollegeLMS.Tests/Unit/Services/WorkingDayServiceTests.cs`, `Integration/Controllers/WorkingDayControllerTests.cs`, сценарии профилей в `Unit/Services/BellScheduleServiceTests.cs`
- `CollegeLMS.Next/api/workingDays.ts`, `app/(authenticated)/dispatcher/holidays/WorkingDaysTab.tsx`

**Создаются (3B):**
- `CollegeLMS.API/Entities/PracticeTeacher.cs`, `Entities/PracticeDay.cs`
- `CollegeLMS.MaxBot/Models/PracticeNotification.cs`, `CollegeLMS.MaxBot/Services/PracticeNotifier.cs`, `CollegeLMS.MaxBot.Tests/PracticeNotifierTests.cs`

**Создаются (3C/3D):**
- `CollegeLMS.API/Dtos/LiveDashboardDtos.cs`, `Interfaces/ILiveDashboardService.cs`, `Services/LiveDashboardService.cs`
- `CollegeLMS.Tests/Unit/Services/LiveDashboardServiceTests.cs`
- `CollegeLMS.Next/app/(authenticated)/dispatcher/live/page.tsx`, `loading.tsx`, `error.tsx`, `api/live.ts`

**Изменяются:** `API/Entities/BellSlot.cs`, `BigBreak.cs`, `Data/Configurations/BellScheduleConfigurations.cs`, `Data/AppDbContext.cs`, `Data/DbConstraints.cs`, `Interfaces/IBellScheduleService.cs`, `Services/BellScheduleService.cs`, `Controllers/BellScheduleController.cs`, `Dtos/BellScheduleDtos.cs`, `Dtos/ScheduleViewDtos.cs`, `Services/ScheduleViewService.cs`, `Services/ScheduleService.cs`, `Services/ScheduleImportService.cs`, `Services/ScheduleExportService.cs`, `Services/CorrectionApplyEngine.cs`, `Services/ScheduleCorrectionService.cs`, `Services/CorrectionBatchService.cs`, `Entities/Practice.cs`, `Data/Configurations/PracticeConfiguration.cs`, `Dtos/PracticeDtos.cs`, `Interfaces/IPracticeService.cs`, `Services/PracticeService.cs`, `Controllers/PracticeController.cs`, `Dtos/JournalDtos.cs`, `Interfaces/IScheduleService.cs`, `Controllers/ScheduleController.cs`, `Controllers/DashboardController.cs`, `Extensions/ServiceCollectionExtensions.cs`, `CollegeLMS.MaxBot/Services/MessageFormatter.cs`, `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs`, `CollegeLMS.MaxBot/Program.cs`, `CollegeLMS.MaxBot/Data/MaxBotDbContext.cs`, `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs`, `CollegeLMS.Next/api/bells.ts`, `api/practices.ts`, `api/schedule.ts`, `app/(authenticated)/layout.tsx`, `lib/menus.ts`, `dispatcher/bells/page.tsx`, `dispatcher/holidays/page.tsx`, `dispatcher/practices/page.tsx`, `teacher/journal/page.tsx`, `components/ScheduleWeekView.tsx`, `ScheduleWeekView`, `WeekNavigation.tsx`, `ScheduleLayers.tsx`, `ScheduleMonthCalendar.tsx`, `ScheduleEntryDialog.tsx`, `components/max/*`, `types/schedule.ts`, `lib/max-lesson.ts`, `docs/spec/task-schedule-management.md`, `docs/spec/task-schedule-reference-data.md`, `docs/spec/CollegeLMS.postman_collection.json`.

**Удаляются:** колонка `practices.organization` (миграция), `ix_bell_slots_number_pair`.

---

## 3A. Звонки, рабочие дни, 5-дневное отображение

### Task 1: Модель данных и миграция

- [ ] `BellProfile` (`Name` max 100, `IsDefault`, `DaysOfWeek int[]`), `BellProfileDate` (`ProfileId`, `DateFrom`, `DateTo`), `WorkingDayOverride` (`DateFrom`, `DateTo`, `SubstituteDayOfWeek int?`, `Title` max 200).
- [ ] `BellSlot`/`BigBreak` +`Guid ProfileId`; конфигурации: FK cascade, уникальный `ix_bell_slots_profile_pair (ProfileId, NumberPair)`, убрать `ix_bell_slots_number_pair`; сид default + «Понедельник» (6 пар с 09:10) + «Четверг» (6 пар) из `ScheduleImportService.PairTimeSlots`.
- [ ] `AppDbContext` DbSets: `BellProfiles`, `BellProfileDates`, `WorkingDayOverrides`.
- [ ] Миграция: `dotnet ef migrations add AddBellProfilesAndWorkingDays --project CollegeLMS.API -- --provider Npgsql`.
- [ ] DoD: `dotnet build CollegeLMS.slnx` — 0 ошибок.

### Task 2: Сервисы и API

- [ ] `IBellScheduleService`: `GetTimeMapAsync(DayOfWeek, ct)`, `GetTimeMapAsync(DateTime, int? substituteDay, ct)`, `GetProfilesAsync`, `CreateProfileAsync`, `UpdateProfileAsync`, `DeleteProfileAsync`, `GetResolvedAsync(date, ct)`; хелпер резолва профиля (дата → подмена → день недели → default).
- [ ] `BellScheduleService`: профили, валидация (имя, дни 1..7, слоты по текущим правилам, default не удаляется), `ToProfileDto`.
- [ ] `IWorkingDayService`/`WorkingDayService`/`WorkingDayController` (`api/working-days`) по образцу `NonWorkingDayService` + проверка конфликта с `NonWorkingDay` → 409.
- [ ] `BellScheduleController`: `GET/PUT /api/bells` (default), `GET/POST /api/bells/profiles`, `PUT/DELETE /api/bells/profiles/{id}`, `GET /api/bells/resolved?date=`, Swagger.
- [ ] DI: `IWorkingDayService` в `ServiceCollectionExtensions.cs`.
- [ ] DoD: `dotnet build` — 0 ошибок; таргетные `BellScheduleServiceTests`, новые `WorkingDayServiceTests`.

### Task 3: Потребители, условные дни, BigBreak

- [ ] Перевести 5 потребителей на date-aware резолв: `ScheduleViewService.cs:262`, `ScheduleService.cs:84,429,481-484,550-553`, `ScheduleImportService.cs:407,536`, `ScheduleExportService.cs:123,657`, `CorrectionApplyEngine.cs:189-191`.
- [ ] `ScheduleViewService`: Пн–Пт всегда; Сб/Вс только при `WorkingDayOverride` или непустом контенте; рабочий день → `entries` по `SubstituteDayOfWeek`, поля `IsWorkingDay`/`SubstituteDayOfWeek`/`WorkingDayTitle`; `BigBreak` в `ScheduleDayViewResponse`.
- [ ] `ScheduleExportService`: динамические колонки 5–7 в семестровом экспорте; `BigBreak` в дне/неделе.
- [ ] Корректировки: разрешить Сб/Вс при наличии override (`ScheduleCorrectionService.cs:36,211`, `CorrectionBatchService.cs:33`).
- [ ] DoD: `dotnet build` + `ScheduleViewServiceTests`, `ScheduleExportServiceTests`, корректировочные тесты зелёные.

### Task 4: Frontend

- [ ] `/dispatcher/bells`: профили (табы), слоты, большая перемена, дни недели, диапазоны дат, создание/удаление (default защищён).
- [ ] `/dispatcher/holidays` → «Календарь», вкладки «Нерабочие дни»/«Рабочие дни», `api/workingDays.ts`; меню → «Календарь».
- [ ] 5 колонок Пн–Пт: `ScheduleWeekView.tsx:214`, `schedule/loading.tsx:26`, `WeekNavigation.tsx:32`; условные выходные и бейдж «за понедельник»; большая перемена в дне/неделе; мини-апп `DayFeed`/`WeekFeed`.
- [ ] DoD: `npx tsc --noEmit` + `npm run lint` — 0; ручная проверка на 3 viewport'ах.

### Task 5: Тесты, доки, коммит 3A

- [ ] Тесты профилей/рабочих дней/5-дневки/BigBreak; Postman (`bells/profiles`, `working-days`, `resolved`); обновить `task-schedule-reference-data.md`.
- [ ] Гейты: `dotnet csharpier format .`, `dotnet test` (таргетно), `npx tsc --noEmit`.
- [ ] Коммит `feat: профили звонков, рабочие дни и 5-дневное расписание`; push.

---

## 3B. Практики

### Task 6: Модель данных и API практик

- [ ] `Practice` +`Name`, −`Organization`, nav `Teachers`; `PracticeTeacher`, `PracticeDay`; конфигурации, индексы, CHECK в `DbConstraints.cs`.
- [ ] Миграция `AddPracticeNameTeachersDays`.
- [ ] DTO/сервис/контроллер: `Name`, `TeacherIds[]`, `Days[]`; валидация (≥1 преподаватель, дни УП в периоде, 1..8, пересечения 409); импорт XLSX с новыми колонками.
- [ ] DoD: `dotnet build` + `PracticeServiceTests` зелёные.

### Task 7: Пары УП в расписании и рендерах

- [ ] `ScheduleViewService.BuildDay`: УП-день → пары 1..`PairCount` с временем профиля, `LessonType=Practice`, +`PracticeName` в `ScheduleResponse`; ПП-день — карточка.
- [ ] Экспорт/бот/мини-апп/веб — название практики и цвет пар.
- [ ] DoD: `dotnet build` + тесты расписания зелёные, `npx tsc --noEmit`.

### Task 8: PracticeNotifier в MaxBot

- [ ] Модель `PracticeNotification` + `DbSet` + идемпотентный DDL в `Program.cs`.
- [ ] `PracticeNotifier : BackgroundService` (начало/окончание, получатели группа + преподаватели, МСК), `MessageFormatter.FormatPracticeStarted/Finished`, регистрация hosted.
- [ ] Тесты `PracticeNotifierTests` + `MessageFormatterTests`.
- [ ] DoD: `dotnet build CollegeLMS.MaxBot.csproj` + `dotnet test CollegeLMS.MaxBot.Tests` зелёные.

### Task 9: Frontend практик + переименование

- [ ] Форма: название, мультиселект преподавателей, дни УП с числом пар (раскрытие по учебным дням), без организации; таблица и импорт.
- [ ] Переименование в «Практики» (`layout.tsx:56`, `page.tsx:374`, `loading.tsx:4`, `error.tsx:13`).
- [ ] DoD: `npx tsc --noEmit` + `npm run lint` — 0.

### Task 10: Тесты, доки, коммит 3B

- [ ] Postman, `task-schedule-management.md` (практики), E2E мок практик при необходимости.
- [ ] Коммит `feat: практики — название, несколько преподавателей, пары УП по дням и уведомления бота`; push.

---

## 3C. Журнал по группам

### Task 11

- [ ] `GetJournalAsync(..., groupId, ct)`, группировка `(GroupId, Subject)`, `JournalSubjectGroup.GroupId/GroupName`; контроллер + Swagger.
- [ ] Web и мини-апп: селект группы, `fetchJournal(teacherId?, subject?, groupId?)`.
- [ ] Тесты журнала + коммит `feat: журнал преподавателя — группировка и фильтр по группе`.

---

## 3D. Live-дашборд

### Task 12

- [ ] `LiveDashboardService` + `GET /api/dispatcher/live` + DTO (4 статуса, практики, разрешённый профиль звонков).
- [ ] Раздел «Экспериментальное» (`dispatcherMenu`, `lib/menus.ts`), страница `/dispatcher/live` с автообновлением 30 с.
- [ ] Тесты + коммит `feat: экспериментальный экран текущих пар`.

---

## Task 13: Финал

- [ ] Полный `dotnet test CollegeLMS.slnx`, `npm run build`, `dotnet csharpier check .`.
- [ ] Обновить `docs/spec/task-schedule-management.md`, `userstories.md`, Postman.
- [ ] Создать PR (`gh pr create`) и убедиться, что `Quality` зелёный.
