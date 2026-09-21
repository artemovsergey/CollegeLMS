# План: расписание — ручной CRUD и импорт (UC-SCH-06/07/08)

> **Для агентов:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` (рекомендуется) или `superpowers:executing-plans`. Выполнять по задачам, отмечая шаги `[ ]`.
> **Спека:** `docs/superpowers/specs/2026-09-21-schedule-crud-import-design.md`.

**Цель:** время пар из справочника звонков при записи и импорте; валидация недель и пересечений по неделям; корректный разбор XLSX с полным списком ошибок формата «Лист, строка, столбец»; строгий `confirm` с отчётом `{imported, groups, teachers}`.

**Архитектура:** правки backend-сервисов расписания и импорта + два фронт-диалога на `/schedule`; API-контракты `Create/Update` меняются (время убирается), `preview/confirm` — статусы и тела; схема БД не меняется.

**Стек:** .NET 10, EF Core (Npgsql), ClosedXML, FluentValidation, Next.js 14.

**Ветка:** `feature/schedule-crud-import` (создаётся в Task 1; миграций нет).

## Глобальные ограничения

- `Result<T>`, без try-catch в сервисах (исключение — существующий блок транзакции импорта); сообщения на русском.
- `AsNoTracking()` на чтении; `ct` во всех async.
- CSharpier перед коммитом; гейты: `dotnet build`, `dotnet test CollegeLMS.Tests`, `dotnet test CollegeLMS.MaxBot.Tests`, `npm run build`.
- Схема БД не меняется; Swagger/Postman обновляются.
- Локальный Docker не запускаем.

---

## Структура файлов

**Изменяются:**
- `CollegeLMS.API/Dtos/ScheduleDtos.cs` — `CreateScheduleRequest`/`UpdateScheduleRequest` без времени.
- `CollegeLMS.API/Mappers/ScheduleMapper.cs` — `ToEntity` без времени (или параметры времени).
- `CollegeLMS.API/Validators/ScheduleValidators.cs` — убрать `StartTime<EndTime`, добавить диапазон недель.
- `CollegeLMS.API/Services/ScheduleService.cs` — время из звонков, недели, пересечения по неделям.
- `CollegeLMS.API/Services/ScheduleImportService.cs` — пары 1–8, недели, ошибки, превью, confirm, нормализация.
- `CollegeLMS.API/Dtos/ScheduleImportDtos.cs` — `Sheet` в ошибке, `ConfirmResult.Errors/Groups/Teachers`.
- `CollegeLMS.API/Controllers/ScheduleController.cs` — статусы превью/confirm, Swagger.
- `CollegeLMS.Next/api/schedule.ts`, `components/ScheduleEntryDialog.tsx`, `components/ScheduleImportDialog.tsx`, `app/(authenticated)/schedule/page.tsx`.
- `CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs`, `CollegeLMS.Tests/Unit/Services/ScheduleImportServiceTests.cs`.

**Создаются:**
- `docs/spec/task-schedule-crud-import.md` (пост-фактум ТЗ).

---

## Task 1: Время из звонков, недели, пересечения (backend CRUD)

**Файлы:**
- Modify: `CollegeLMS.API/Dtos/ScheduleDtos.cs`, `Mappers/ScheduleMapper.cs`, `Validators/ScheduleValidators.cs`, `Services/ScheduleService.cs`
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs`

**Interfaces:**
- Consumes: `IBellScheduleService.GetTimeMapAsync(ct)`; `ScheduleImportService.GetPairTime(DayOfWeek, int)` (internal static, fallback); `StudyWeek.TotalWeeks`.
- Produces: `CreateScheduleRequest`/`UpdateScheduleRequest` без `StartTime`/`EndTime`; `ScheduleService.CreateAsync`/`UpdateAsync` пишут время из звонков (fallback), нормализуют недели, проверяют пересечение недель.

- [ ] **Step 1: Тесты (падающие)**

Добавить в `ScheduleServiceTests` (реальные `BellScheduleServiceStub`, `TestDbContextFactory`):

```csharp
[Fact] public async Task CreateAsync_UsesBellTimes_NotRequest() { /* bells[1]=(10:00,11:20) → entry.StartTime 10:00 */ }
[Fact] public async Task CreateAsync_NoBellEntry_FallsBackToDefaultSlot() { /* пустая карта → GetPairTime(day, pair) */ }
[Fact] public async Task CreateAsync_WeekZero_Returns400() { /* Weeks=[0] → 400, текст «диапазоне 1–16» */ }
[Fact] public async Task CreateAsync_WeekAboveSemester_Returns400() { /* Weeks=[17] → 400 */ }
[Fact] public async Task CreateAsync_OverlapSameWeeks_Returns409() { /* две записи неделя 1, группа → 409 */ }
[Fact] public async Task CreateAsync_OverlapDisjointWeeks_Allowed() { /* недели 1 и 2 → успех */ }
[Fact] public async Task UpdateAsync_OverlapDisjointWeeks_AllowsSelfUpdate() { /* excludeId работает */ }
```

Существующие тесты, передающие `StartTime/EndTime` в DTO, обновить (полей больше нет).

- [ ] **Step 2: RED**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleServiceTests`
Expected: ошибка компиляции (поля/поведение).

- [ ] **Step 3: DTO, маппер, валидаторы**

- Убрать `StartTime`/`EndTime` из `CreateScheduleRequest` и `UpdateScheduleRequest` (`ScheduleDtos.cs`).
- `ScheduleMapper.ToEntity(request)` — без времени; время задаёт сервис.
- `ScheduleValidators`: удалить правила `StartTime.LessThan(EndTime)`; добавить
  `RuleForEach(x => x.Weeks).InclusiveBetween(1, StudyWeek.TotalWeeks).WithMessage($"Неделя должна быть в диапазоне 1–{StudyWeek.TotalWeeks}")` для обоих валидаторов (using `CollegeLMS.API.Services`).

- [ ] **Step 4: Сервис**

`CreateAsync`:

```csharp
var weeks = request.Weeks.Distinct().OrderBy(w => w).ToList();
if (weeks.Count == 0 || weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
    return Result<ScheduleResponse>.Fail(
        $"Недели должны быть в диапазоне 1–{StudyWeek.TotalWeeks}", 400);

var overlap = await CheckOverlapAsync(null, request.GroupId, request.TeacherId, request.Room,
    request.DayOfWeek, request.NumberPair, weeks, ct);
...
var bellTimes = await bells.GetTimeMapAsync(ct);
var (start, end) = bellTimes.TryGetValue(request.NumberPair, out var t)
    ? t
    : ScheduleImportService.GetPairTime(request.DayOfWeek, request.NumberPair);

var entry = request.ToEntity();
entry.Weeks = weeks;
entry.StartTime = start;
entry.EndTime = end;
```

`UpdateAsync` — аналогично с `excludeId = id`. `CheckOverlapAsync` — новая сигнатура `(Guid? excludeId, Guid groupId, Guid? teacherId, string room, DayOfWeek day, int numberPair, List<int> weeks, CancellationToken ct)`:

```csharp
var candidates = await db.ScheduleEntries.AsNoTracking()
    .Where(s => s.Id != (excludeId ?? Guid.Empty)
        && s.DayOfWeek == day && s.NumberPair == numberPair
        && (s.GroupId == groupId || (teacherId.HasValue && s.TeacherId == teacherId) || s.Room == room))
    .Select(s => new { s.GroupId, s.TeacherId, s.Room, s.Weeks })
    .ToListAsync(ct);

var overlapping = candidates.Where(c => c.Weeks.Intersect(weeks).Any()).ToList();
if (overlapping.Any(c => c.GroupId == groupId)) return "У группы уже есть занятие в эту пару";
if (teacherId.HasValue && overlapping.Any(c => c.TeacherId == teacherId)) return "У преподавателя уже есть занятие в эту пару";
if (overlapping.Any(c => c.Room == room)) return "Аудитория уже занята в эту пару";
return null;
```

- [ ] **Step 5: GREEN + коммит**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleServiceTests`
```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat(schedule): время из звонков, валидация недель и пересечения по неделям"
```

---

## Task 2: Импорт — парсер, ошибки, превью, нормализация

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs`, `Dtos/ScheduleImportDtos.cs`, `Controllers/ScheduleController.cs`
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleImportServiceTests.cs`

**Interfaces:**
- Produces: `ScheduleValidationError { Sheet, Row, Column, Level, Message }` (Message — с полным префиксом «Лист {Sheet}, строка N, столбец M: …»); `PreviewAsync` → `IsSuccess=true` с `Entries`+`Errors` при разобранном файле.
- Consumes: `IBellScheduleService.GetTimeMapAsync` (внедрить в конструктор сервиса), `StudyWeek.TotalWeeks`.

- [ ] **Step 1: Тесты (падающие/обновляемые)**

- `ParseScheduleMatrix_Pair8_Parsed` — строка пары 8 даёт запись (сейчас игнорируется);
- `ParseScheduleMatrix_WeekAboveSemester_ReturnsErrorWithNewFormat` — ошибка `Level="data"`, `Message` начинается с «Лист » и содержит «столбец», «вне семестра (1–16)»;
- `ParseScheduleMatrix_NoGroups_ReturnsStructureError` — `Level="structure"`, без раннего выхода при наличии дней;
- `ParseScheduleMatrix_UnknownDay_ReturnsStructureError`;
- `ParseScheduleMatrix_SameCell_ReturnsEntriesAndErrors` — часть ячеек валидна, часть — нет: обе коллекции непусты;
- `ParseScheduleMatrix_PairTimeFromBells` — bells[1]=(10:00,11:20) → время записи 10:00; пустая карта → `GetPairTime`;
- `NormalizeTeacherName_NormalizesInitialsSpacing` — «Иванов И. И.» → «Иванов И.И.»;
- `PreviewAsync_WithErrors_ReturnsSuccessWithEntriesAndErrors`;
- обновить существующий `ReturnsError_WhenWeeksExceed52` (новый текст) и тесты времени (fallback сохраняет старые ожидания).

- [ ] **Step 2: RED**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleImportServiceTests`
Expected: FAIL.

- [ ] **Step 3: DTO и ошибки**

`ScheduleValidationError` += `public string Sheet { get; set; } = string.Empty;`. Хелпер в сервисе:

```csharp
private static ScheduleValidationError Error(string sheet, int row, int column, string level, string message) =>
    new() { Sheet = sheet, Row = row, Column = column, Level = level,
            Message = $"Лист {sheet}, строка {row}, столбец {column}: {message}" };
```

Все сообщения — через хелпер; `sheet = ws.Name`.

- [ ] **Step 4: Парсер**

- `pairRows`: `num >= 1 && num <= 8`; ветка `>7` заменяется на проверку 1..8 (номер 9+ → ошибка `data`).
- Недели: `w < 1 || w > StudyWeek.TotalWeeks` → `Error(..., "data", $"неделя {badWeek} вне семестра (1–{StudyWeek.TotalWeeks})")`.
- Структура: `groupColumns.Count == 0` и `dayBlocks.Count == 0` — ошибки `structure` без `return (entries, errors)` (продолжать, если возможно) либо осмысленный ранний выход только при полной невозможности разбора, но с `Level="structure"`;
- Неизвестный день: строки, где `A` непусто, не в `DayMap`, и `B` — число → `Error(sheet, row, 1, "structure", $"неизвестный день недели \"{text}\"")`.
- Try/catch вокруг `ParseScheduleMatrix` внутри `PreviewAsync`: `catch (Exception)` → `Preview` с одной структурной ошибкой «Не удалось прочитать лист: …» (без 500).
- Время: `var (start, end) = bellTimes.TryGetValue(pairNum, out var t) ? t : GetPairTime(day, pairNum);` — `IBellScheduleService` внедрить в конструктор; обновить DI-регистрацию при необходимости.

- [ ] **Step 5: Превью и контроллер**

`PreviewAsync`: при `errors.Count > 0` — `IsSuccess = true`, `Preview = { TotalEntries = entries.Count, Entries = entries, Errors = errors }`. Контроллер: `400` только при `!result.IsSuccess` (FILE-1/нечитаемый), иначе `200 Result<PreviewResponse>`.

- [ ] **Step 6: Нормализация**

`NormalizeTeacherName`: `Regex.Replace(name.Trim(), @"\s+", " ")` + `Regex.Replace(v, @"\.\s*\.", "..")` + пробел после точки между инициалами: итог «Иванов И.И.». `NormalizeSubject`: сохранить regex-правила, сгруппировать в `SubjectSynonyms` словарь, применяемый первым (регистронезависимо).

- [ ] **Step 7: GREEN + коммит**

```powershell
dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleImportServiceTests
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat(import): пары 1–8, недели семестра, полные ошибки и превью с записями"
```

---

## Task 3: Confirm — валидация, курс, преподаватели, отчёт

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs`, `Dtos/ScheduleImportDtos.cs`, `Controllers/ScheduleController.cs`
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleImportServiceTests.cs`

**Interfaces:**
- Produces: `ConfirmResult { IsSuccess, Imported, Groups, Teachers, Errors, Schedule }`; `ConfirmAsync` возвращает `IsSuccess=false` + `Errors` при невалидных позициях; контроллер `400` в этом случае, иначе `200`.

- [ ] **Step 1: Тесты**

- `ConfirmAsync_InvalidPair_ReturnsErrors400` (проверить `IsSuccess=false`, `Errors` непуст);
- `ConfirmAsync_InvalidWeeks_ReturnsErrors`;
- `ConfirmAsync_EmptyEntries_ReturnsError`;
- `ConfirmAsync_CreatesGroupsWithCourseFromFirstDigit` («ИС-21» → 2; «1-11» → 1);
- `ConfirmAsync_LinksTeacherToExistingUser` (User без Teacher → создаётся Teacher с тем же UserId, нового User нет);
- `ConfirmAsync_CreatesTeacherWhenUserMissing` (User+Teacher);
- `ConfirmAsync_Report_CountsCreatedGroupsAndTeachers` (`Groups`/`Teachers` — только созданные);
- `ConfirmAsync_ReplacesScheduleAndHistory` (существующий тест обновить: полная замена сохраняется).

- [ ] **Step 2: RED**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleImportServiceTests`
Expected: FAIL.

- [ ] **Step 3: Валидация позиций**

```csharp
private static List<ScheduleValidationError> ValidateConfirmEntries(List<SchedulePreviewEntry> entries)
{
    var errors = new List<ScheduleValidationError>();
    for (var i = 0; i < entries.Count; i++)
    {
        var e = entries[i];
        if (e.Pair < 1 || e.Pair > 8)
            errors.Add(ConfirmError(i, "номер пары должен быть от 1 до 8"));
        if (!Enum.TryParse<DayOfWeek>(e.Day, true, out _))
            errors.Add(ConfirmError(i, $"неизвестный день недели \"{e.Day}\""));
        if (string.IsNullOrWhiteSpace(e.GroupName)) errors.Add(ConfirmError(i, "не указана группа"));
        if (string.IsNullOrWhiteSpace(e.Subject)) errors.Add(ConfirmError(i, "не указан предмет"));
        if (string.IsNullOrWhiteSpace(e.Room)) errors.Add(ConfirmError(i, "не указана аудитория"));
        if (e.Weeks.Count == 0 || e.Weeks.Any(w => w < 1 || w > StudyWeek.TotalWeeks))
            errors.Add(ConfirmError(i, $"недели должны быть в диапазоне 1–{StudyWeek.TotalWeeks}"));
    }
    return errors;
}
```

`ConfirmError` использует «Лист, строка, столбец» с `Sheet = "импорт"` и `Row = i + 2` (условная строка позиции). При `errors.Count > 0` — возврат `IsSuccess=false` до открытия транзакции.

- [ ] **Step 4: Курс и преподаватели**

- Курс: `var digits = new string(name.Where(char.IsDigit).ToArray()); var course = digits.Length > 0 ? digits[0] - '0' : 1; course = Math.Clamp(course, 1, 4);`
- Преподаватели: загрузить `Users` с ролью `UserRole.Teacher` (`Id`, `FullName`), построить словарь по `NormalizeTeacherName(FullName)`; для каждого уникального имени: найден → найти/создать `Teacher`; не найден → создать `User`+`Teacher`. Счётчики `createdGroups`/`createdTeachers` инкрементировать только при создании.
- Время записей — из карты звонков (уже передаётся в Task 2); fallback `GetPairTime`.
- `ConfirmResult` дополняется `Groups`, `Teachers`, `Errors`; `Schedule` сохраняется.

- [ ] **Step 5: Контроллер**

`ConfirmImport`: `if (!result.IsSuccess) return BadRequest(result); return Ok(result);` — Swagger: `400` (ошибки позиций), `200`.

- [ ] **Step 6: GREEN + коммит**

```powershell
dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleImportServiceTests
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat(import): валидация confirm, курс групп, привязка преподавателей и отчёт"
```

---

## Task 4: Фронтенд диалогов расписания

**Файлы:**
- Modify: `CollegeLMS.Next/components/ScheduleEntryDialog.tsx`, `components/ScheduleImportDialog.tsx`, `api/schedule.ts`, `app/(authenticated)/schedule/page.tsx`

**Interfaces:**
- Consumes: `fetchScheduleMeta()` (totalWeeks), `api/bells.ts` (`getBells`), `ScheduleImportResult { imported, groups, teachers, ... }`, `ScheduleValidationError { sheet, row, column, level, message }`.

- [ ] **Step 1: `ScheduleEntryDialog`**

- Убрать поля `StartTime`/`EndTime` (`type="time"`, `:112-114, :246-253`) и передачу в `createSchedule/updateSchedule`.
- Загрузить звонки (`api/bells.ts`), показывать строку «🕐 08:30–09:50 (из справочника звонков)» для выбранного `NumberPair`; обновлять при смене номера.
- Проп `totalWeeks` из `page.tsx` (meta); валидация недель по диапазону `1…totalWeeks`, placeholder `1…{totalWeeks}`, ошибка под полем.

- [ ] **Step 2: `ScheduleImportDialog`**

- Ошибки: брать `message` как есть (убрать собственный префикс `:211-214`); показывать ошибки из `200` (`preview.errors`) и из `400` (через `err.response.data.errors`, добавить извлечение в `catch`).
- Кнопка «Загрузить» (вместо «Импортировать»), `disabled` при `errors.length > 0`.
- Отчёт после успеха: «Загружено пар: N · Группы: M · Преподаватели: K» (UI-4 тост сохраняется).

- [ ] **Step 3: Типы и сборка**

`api/schedule.ts`: `CreateScheduleRequest`/`UpdateScheduleRequest` без `time`; `ScheduleImportResult { imported; groups; teachers; schedule; errors }`; тип ошибки с `sheet`.

Run (в `CollegeLMS.Next`): `npm run build`
Expected: PASS.

- [ ] **Step 4: Ручная проверка и коммит**

`npm run dev`: `/schedule` — диалог пары (время из звонков, недели по семестру), импорт с ошибками и успешный импорт с отчётом; viewport'ы 393/1366/1920.

```powershell
git add -A; git commit -m "feat(frontend): диалоги расписания — время из звонков, недели семестра, ошибки и отчёт импорта"
```

---

## Task 5: Документация, гейты, merge

**Файлы:**
- Create: `docs/spec/task-schedule-crud-import.md`
- Modify: `docs/spec/CollegeLMS.postman_collection.json`, `docs/diagrams/sequence/*import*.puml` (существующий файл импорта, если есть; иначе создать `schedule-import.puml`)

- [ ] **Step 1: Postman**

`Create/Update schedule` — тело без времени; `import/preview` — 200 с `entries`+`errors`; `import/confirm` — успех с `{imported, groups, teachers}` и 400 со списком ошибок.

- [ ] **Step 2: PlantUML и пост-фактум ТЗ**

Sequence импорта: диалог → preview (записи+ошибки) → подтверждение → confirm (валидация → транзакция → отчёт). `docs/spec/task-schedule-crud-import.md` — по образцу `task-schedule-reference-data.md`.

- [ ] **Step 3: Полные гейты**

```powershell
dotnet build
dotnet csharpier check .
dotnet test CollegeLMS.Tests
dotnet test CollegeLMS.MaxBot.Tests
npm run build --prefix CollegeLMS.Next
```

- [ ] **Step 4: Коммит и merge**

```powershell
git add -A; git commit -m "docs: ручной CRUD и импорт — Postman, диаграмма и пост-фактум ТЗ"
git checkout master
git merge feature/schedule-crud-import
```

- [ ] **Step 5: Push и CI/CD** (после подтверждения пользователя)

`git push origin master` → проверить quality/deploy.

---

## Самопроверка плана

- **Покрытие спеки:** §2 — T1, T2; §3 — T1, T4; §4 — T2; §5 — T3, T4; §6 — тесты в T1–T4, гейты T5; §7 — T5; §8–9 — T5.
- **Плейсхолдеров нет:** сигнатуры, тесты и формулы приведены; тексты сообщений заданы.
- **Согласованность:** `ScheduleValidationError.Sheet/Level/Message`, `ConfirmResult.Errors/Groups/Teachers`, `totalWeeks`-проп, `GetPairTime` как fallback — сквозные по задачам.
