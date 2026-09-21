# План: справочники расписания и интеграция слоёв

> **Для агентов:** выполнять по задачам, отмечая шаги `[ ]`. Спека: `docs/superpowers/specs/2026-09-18-schedule-reference-features-design.md`.

**Цель:** звонки, нерабочие дни, вставки, практики, семестровый экспорт, веб «Изменения» и журнал с бейджами.

**Стек:** .NET 10, EF Core (Npgsql), ClosedXML, QuestPDF, Next.js 14.

## Глобальные ограничения

- `Result<T>`, без try-catch в сервисах; сообщения на русском; `HasMaxLength`; snake_case; `ValueGeneratedNever`.
- Все новые endpoint — `[ProducesResponseType]`/`[SwaggerResponse]` + русские XML-комментарии.
- Гейты: `dotnet build`, `dotnet csharpier check .`, `dotnet test` (оба проекта), `npm run build`.
- Локальный Docker не запускается — сборка стека в CI/CD.

---

### Task 1: Entity + конфигурации + миграция

**Файлы:** `Entities/{BellSlot,BigBreak,NonWorkingDay,Practice,ScheduleInsert}.cs`, `Entities/Enums/PracticeKind.cs`, `Data/Configurations/*Configuration.cs`, `Data/AppDbContext.cs`, миграция.

- [ ] Создать сущности (наследуют `Entity`), enum `PracticeKind { Up, Pp }`.
- [ ] EF-конфигурации: `ToTable`, `HasMaxLength`, enum-строки, индексы, FKs (Group/Teacher — Restrict/SetNull).
- [ ] DbSet-ы, `dotnet ef migrations add AddScheduleReferenceData`.
- [ ] `dotnet build`.

---

### Task 2: Звонки (сервис, API, интеграция времени)

**Файлы:** `Dtos/BellScheduleDtos.cs`, `Services/BellScheduleService.cs`, `Interfaces/IBellScheduleService.cs`, `Controllers/BellScheduleController.cs`, `DataSeeder.cs` (сид), `Services/ScheduleService.cs`, `Services/ScheduleExportService.cs`, `Services/CorrectionApplyEngine.cs`.

- [ ] `GET/PUT /api/bells`, валидации (07:00–21:00, порядок, пересечения, большая перемена), сид 8 пар + перемена после 4-й.
- [ ] Чтение расписания отображает время из справочника (подмена по `NumberPair`).
- [ ] Корректировки создают пары с временем из справочника (fallback — прежние слоты).
- [ ] Экспорт использует справочник.
- [ ] `dotnet build` + коммит.

---

### Task 3: Нерабочие дни (CRUD + интеграция)

**Файлы:** `Entities/NonWorkingDay.cs`, `Dtos/NonWorkingDayDtos.cs`, `Services/NonWorkingDayService.cs`, `Controllers/NonWorkingDayController.cs`, `Services/CorrectionBatchService.cs`, `Services/ScheduleService.cs`, `Services/ScheduleExportService.cs`, `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs`, `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs`.

- [ ] CRUD + список с фильтром/пагинацией.
- [ ] Корректировка на нерабочую дату → `400` (создание и применение).
- [ ] Расписание дня на нерабочую дату пустое; бот не отправляет рассылку; экспорт помечает.
- [ ] `dotnet build` + коммит.

---

### Task 4: Вставки (CRUD + сид + вывод)

**Файлы:** `Entities/ScheduleInsert.cs`, `Dtos/ScheduleInsertDtos.cs`, `Services/ScheduleInsertService.cs`, `Controllers/ScheduleInsertController.cs` (`api/schedule/inserts`), `DataSeeder.cs`.

- [ ] CRUD, валидации, сид «Разговор о важном»/«Классный час».
- [ ] Отдача в расписание/бот/экспорт отдельной строкой дня.
- [ ] `dotnet build` + коммит.

---

### Task 5: Практики (CRUD + импорт XLSX)

**Файлы:** `Entities/Practice.cs`, `Dtos/PracticeDtos.cs`, `Services/PracticeService.cs`, `Services/PracticeImportService.cs`, `Controllers/PracticeController.cs`.

- [ ] CRUD, валидация периода/пересечений (409), фильтры, пагинация.
- [ ] Импорт: превью со всеми ошибками «Строка N: …», подтверждение в транзакции.
- [ ] `dotnet build` + коммит.

---

### Task 6: Семестровый экспорт

**Файлы:** `Services/ScheduleExportService.cs`, `Controllers/ScheduleController.cs`, `CollegeLMS.Next/api/schedule.ts`.

- [ ] `scope=semester`: матрица «недели × дни», бейджи, вставки, практики, нерабочие дни, FILE-3.
- [ ] Кнопка экспорта в режиме «Семестр» на `/schedule`.
- [ ] `dotnet build` + коммит.

---

### Task 7: Журнал с бейджами

**Файлы:** `Dtos/JournalDtos.cs`, `Services/ScheduleService.cs`, `Controllers/ScheduleController.cs`, `CollegeLMS.Next/app/(authenticated)/teacher/journal/page.tsx`, `CollegeLMS.Next/components/max/JournalView.tsx`.

- [ ] Фильтр `subject`, бейджи `Add/Replace/Move/сам.р.`, скрытие снятых дат.
- [ ] Веб-страница `/teacher/journal`; бейджи в мини-приложении.
- [ ] `dotnet build`, `npm run build` + коммит.

---

### Task 8: Веб-раздел «Изменения»

**Файлы:** `CollegeLMS.Next/app/(authenticated)/changes/page.tsx`, `components/ChangeCard.tsx`, `components/ChangeFilters.tsx`, `api/correction.ts`, `components/max/ChangesView.tsx`.

- [ ] Страница `/changes`: карточки, фильтры (неделя, дата/период, группа, преподаватель, тип), пагинация, состояния UI-1…UI-3.
- [ ] Фильтры даты/группы/преподавателя в мини-приложении.
- [ ] `npm run build` + коммит.

---

### Task 9: Страницы диспетчера

**Файлы:** `app/(authenticated)/dispatcher/{bells,holidays,inserts,practices}/page.tsx`, `api/*`, `components/*`.

- [ ] `/dispatcher/bells` — форма звонков + большая перемена.
- [ ] `/dispatcher/holidays` — список/периоды + форма.
- [ ] `/dispatcher/inserts` — таблица + форма.
- [ ] `/dispatcher/practices` — таблица + форма + импорт XLSX.
- [ ] `npm run build` + коммит.

---

### Task 10: Тесты и документация

- [ ] Unit/интеграционные тесты на все новые сервисы и endpoint-ы; MaxBot — пропуск рассылки в нерабочий день.
- [ ] `dotnet test`, `npm run build`, merge + push.

## Порядок

1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10. Задачи 7–9 параллелизуемы после Task 6.
