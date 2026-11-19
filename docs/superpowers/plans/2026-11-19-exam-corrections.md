# Экзамены в корректировках расписания — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить третью операцию «Экзамен» в пакет корректировок так, чтобы после применения экзамен попадал и в расписание, и в XLSX/журнал/уведомление, а XLSX-строка совпадала с образцом диспетчера.

**Architecture:** Экзамен — это та же позиция `CorrectionPosition`, но с `ChangeType=Exam`, `NumberPair=0` и `Subject="ЭКЗАМЕН"`; он проходит существующий конвейер «пакет → apply → history → Max → XLSX → revert». В БД это обычная запись `ScheduleEntry` с `LessonType=Exam` и парой 0, а в ответе расписания она выносится в отдельный массив `Exams` рядом с `Inserts`. Новых таблиц и миграций нет: `ScheduleChangeType` хранится строкой, `NumberPair` — `int` без CHECK-ограничения диапазона.

**Tech Stack:** .NET 10 / ASP.NET Core Web API, EF Core (Npgsql), ClosedXML, xUnit + Moq + Bogus, Next.js 14 (App Router, TypeScript, Tailwind CSS 4, shadcn/ui), Playwright, MAX Bot API.

**Spec:** `docs/superpowers/specs/2026-11-19-exam-corrections-design.md` — утверждённый дизайн, эталон формата XLSX — фото пользователя.

## Global Constraints

- Работа ведётся в ветке `feat/exam-corrections`, созданной от `master` **до** первого изменения продуктового кода; по завершении — коммит → merge в `master` → push (правила `AGENTS.md`). Docker-команды — только из `/home/user1/CollegeLMS`.
- `Result<T>` везде; никаких try-catch в контроллерах и сервисах; `CancellationToken ct` на всех async-методах; `AsNoTracking()` на чтении; ручные мапперы.
- Сообщения валидации и пользовательские подписи — **на русском**.
- Перечисления: `HasConversion<string>()` + `HasMaxLength()` → добавление `Exam` **не требует миграции**.
- `ScheduleEntry.NumberPair` — `int NOT NULL`, CHECK-диапазона в БД нет (`Data/Configurations/ScheduleEntryConfiguration.cs:17`), поэтому 0 допустим; **новые CHECK-ограничения и миграции не создавать**.
- Константы Opera: `CorrectionApplyEngine.MaxPair = 7`. Для обычных позиций пара — 1–7; `NumberPair == 0` разрешён **только** для `Exam`. При импорте XLSX допустим диапазон 1–8 (существующая проверка).
- Каноничное значение предмета экзамена — строка `ЭКЗАМЕН` (для сравнений — `OrdinalIgnoreCase` + trim).
- Позиция Exam: `TeacherId`/`TeacherName`/`Note`/`Removed*` = `null`, `Room` не используется. `GroupId`/`GroupName` обязательны; `Week`/`DayOfWeek` берутся из даты пакета.
- Экспорт XLSX уже пишет пустую ячейку «№» при `NumberPair == 0` (`Services/ScheduleCorrectionService.cs:124-129`) — **шаблон `Корректировка.xlsx` не менять**. Ограничение файла — ≤10 МБ.
- Swagger-примеры и `docs/spec/CollegeLMS.postman_collection.json` обязательны для каждого затронутого endpoint.
- Формат строки эталона: `Группа | (пусто) | ЭКЗАМЕН | (пусто) | (пусто №) | (пусто примечание)`.
- Фронтенд: перед правками HTML/CSS/клиентского JS — скилл `modern-web-guidance`. Проверки: `npm run build`, `npm run check:design-system`.

## Review Focus

| Риск | Ожидаемое поведение | Где закреплено тестом |
|---|---|---|
| Старый XLSX диспетчера: в строке «ЭКЗАМЕН» заполнена колонка «№» или преподаватель | Импорт не падает, номер нормализуется в 0, преподаватель игнорируется, создаётся позиция Exam | Задача 4, шаг 1 |
| Откат экзамена (`DELETE /api/schedule/history/{id}`) | Удаляется только запись экзамена и сама история; соседние пары и недели не трогаются | Задача 2, шаг 1 (тест `RevertExamRemovesOnlyExamEntry`) |
| Повторный apply пакета с экзаменом | Поведение как у остальных позиций: уже применённый пакет не применяется повторно (409 / без дублей) | Задача 2, шаг 1 (тест `ReapplyExamBatchNoDuplicate`) |
| Экзамен в нумерованном списке пар | В `Entries` экзамена нет; он живёт в `Exams`, без номера и времени; в Max-дайджесте нет строки `*0.*` | Задача 5, шаг 1; Задача 7, шаг 1; Задача 8, шаг 1 |
| Нерабочая дата / воскресенье | Экзамен, как и пара, не применяется; ошибка 400, пакет остаётся в Draft | Задача 2, шаг 1 (тест `ApplyExamOnNonWorkingDateFails`) |

---

### Task 1: Тип операции и валидация позиции

**Files:**
- Modify: `CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs:3-9`
- Modify: `CollegeLMS.API/Services/CorrectionBatchService.cs:811-833`
- Modify: `CollegeLMS.API/Services/CorrectionApplyEngine.cs:265-320`
- Test: `CollegeLMS.Tests/Unit/Services/ExamCorrectionValidationTests.cs`

**Interfaces:**
- Consumes: ничего (первая задача).
- Produces: `ScheduleChangeType.Exam`; правило «`NumberPair == 0` только для `Exam`, `Subject` обязательно равен `ЭКЗАМЕН` (ignore case), остальные типы — 1–7».

- [ ] **Step 1: Write the failing test**

Создать `CollegeLMS.Tests/Unit/Services/ExamCorrectionValidationTests.cs` с тремя тестами на публичные точки валидации (`CorrectionBatchService.ValidatePositionAsync` через добавление позиции в пакет и `CorrectionApplyEngine.ValidateBatchAsync`):

```csharp
[Fact]
public async Task ExamPositionWithZeroPairIsValid()
{
    var result = await service.AddPositionAsync(batchId, new CreateCorrectionPositionRequest
    {
        ChangeType = ScheduleChangeType.Exam, GroupId = group.Id, GroupName = group.Name,
        NumberPair = 0, Subject = "ЭКЗАМЕН",
    }, CancellationToken.None);
    Assert.False(result.IsFailure);
}

[Fact]
public async Task ExamPositionWithPairOneIsRejected()
{
    // ChangeType = Exam, NumberPair = 1 → IsFailure, ошибка содержит «Пара для экзамена должна быть 0»
}

[Fact]
public async Task AddPositionWithZeroPairIsStillRejected()
{
    // ChangeType = Add, NumberPair = 0 → IsFailure, прежнее сообщение «Номер пары должен быть от 1 до 7.»
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionValidationTests`
Expected: FAIL — `ScheduleChangeType` не содержит `Exam` (ошибка компиляции).

- [ ] **Step 3: Implement**

1. В `ScheduleChangeType.cs` добавить `Exam,` после `Move,`.
2. В `CorrectionBatchService.ValidatePositionAsync` (строка 816) заменить проверку на: если `request.ChangeType == Exam` — требовать `request.NumberPair == 0` и `string.Equals(request.Subject?.Trim(), "ЭКЗАМЕН", OrdinalIgnoreCase)`, иначе сохранить прежнюю проверку 1–`MaxPair`. Сообщения на русском.
3. В `CorrectionApplyEngine.ValidateBatchAsync`: в проверке строк 279–285 разрешить `NumberPair == 0` только при `position.ChangeType == Exam` (иначе прежняя ошибка «некорректный № пары»); в проверке 299–312 пропустить требование «предмет ↔ преподаватель» для Exam; в проверке 314 оставить требование предмета, но для Exam требовать точное `ЭКЗАМЕН`.
4. Никаких изменений в `ScheduleEntryConfiguration` и миграций.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionValidationTests`
Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs CollegeLMS.API/Services/CorrectionBatchService.cs CollegeLMS.API/Services/CorrectionApplyEngine.cs CollegeLMS.Tests/Unit/Services/ExamCorrectionValidationTests.cs
git commit -m "feat: add Exam correction type and pair-zero validation"
```

---

### Task 2: Применение и откат экзамена в расписании

**Files:**
- Modify: `CollegeLMS.API/Services/CorrectionApplyEngine.cs:11-34, 401-548, 508-546, 1315-1342`
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs:2190-2237`
- Test: `CollegeLMS.Tests/Unit/Services/ExamCorrectionApplyTests.cs`

**Interfaces:**
- Consumes: `ScheduleChangeType.Exam`, правило пары 0 из Задачи 1; `SimulatedEntry` (строки 11–34); `BuildHistory(...)` (строки 1093–1128); `CreateEntity(...)` (строки 1315–1342).
- Produces: в БД — `ScheduleEntry { LessonType = Exam, NumberPair = 0, Subject = "ЭКЗАМЕН", Weeks = [week] }` и `ScheduleHistory { ChangeType = Exam }`; откат удаляет только их.

- [ ] **Step 1: Write the failing tests**

В `ExamCorrectionApplyTests.cs` (4 теста, образец — `CollegeLMS.Tests/Unit/Services/CorrectionBatchReapplyTests.cs`):

```csharp
[Fact]
public void ApplyExamCreatesEntryWithExamLessonType()
// Apply батча с Exam → ScheduleEntries содержит запись с LessonType == LessonType.Exam,
// NumberPair == 0, Subject == "ЭКЗАМЕН", Weeks == [week], TeacherId == null,
// ScheduleHistory содержит запись ChangeType == Exam и ChangeTags дают «Добавлено».

[Fact]
public async Task RevertExamRemovesOnlyExamEntry()
// После revert: записи экзамена нет, истории нет, соседняя пара недели 3 осталась нетронутой.

[Fact]
public async Task ReapplyExamBatchNoDuplicate()
// Повторный Apply того же батча не создаёт вторую запись экзамена (409/без дубля).

[Fact]
public async Task ApplyExamOnNonWorkingDateFails()
// Дата из NonWorking → IsFailure, ошибка содержит «Дата нерабочая:», позиция остаётся Draft.
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionApplyTests`
Expected: FAIL — `switch` движка не знает `Exam`, запись не создаётся.

- [ ] **Step 3: Implement**

1. В `SimulatedEntry` добавить `public bool IsExam { get; set; }`.
2. Извлечь из блока `case Add` (строки 505–546) приватный метод с сигнатурой:
   `private void ApplyCreatedEntry(CorrectionPosition position, Group group, SimulatedEntry entry, DateTime utcNow, DateTime? correctionDate, Guid appliedByUserId, ScheduleChangeType changeType, Dictionary<DayOfWeek, Dictionary<int, (TimeSpan Start, TimeSpan End)>> bellByDay, SimulationResult result)`
   Он делает: `CreateEntity` → `entry.Entity = entity` → `db.ScheduleEntries.Add` → `BuildHistory(position, changeType, ...)` → `ApplyOrAppendHistory` → `result.Changes.Add(new ScheduleChangeDto { ChangeType = nameof(changeType), ... })`. Вызвать его из существующего цикла `Add` с `changeType: ScheduleChangeType.Add` — поведение пар не меняется.
3. В `switch (position.ChangeType)` (строка 401) добавить ветку **до** `case Add`:
   `case ScheduleChangeType.Exam:` — собрать один `SimulatedEntry` с `GroupId/GroupName/DayOfWeek/Week` из позиции, `NumberPair = 0`, `Subject = "ЭКЗАМЕН"`, `Room = ""`, `TeacherId/TeacherName/Note = null`, `IsExam = true`, `PendingChangeType = nameof(ScheduleChangeType.Exam)`; добавить его в `list` и при `execute` вызвать `ApplyCreatedEntry(..., changeType: ScheduleChangeType.Exam, ...)`.
4. В `CreateEntity` (1321–1338): если `entry.IsExam`, положить `start = end = TimeSpan.Zero` и `LessonType = LessonType.Exam`; иначе — прежний расчёт времени и `LessonType.None`.
5. В `ScheduleCorrectionService.RevertCoreAsync` (строка 2196) добавить `case ScheduleChangeType.Exam:` сразу над `case ScheduleChangeType.Add:` (fall-through на то же тело). `FindEntryAsync` уже ищет по `NumberPair`, `Subject`, `Week` — для экзамена это 0 / «ЭКЗАМЕН» / неделя.
6. Сообщение revert для экзамена остаётся формой «Занятие «ЭКЗАМЕН» … убрано» — отдельной ветки не делать.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionApplyTests`
Expected: PASS (4/4).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Services/CorrectionApplyEngine.cs CollegeLMS.API/Services/ScheduleCorrectionService.cs CollegeLMS.Tests/Unit/Services/ExamCorrectionApplyTests.cs
git commit -m "feat: apply and revert exam corrections"
```

---

### Task 3: Экзамен в PNG-корректировке и уведомлении Max

**Files:**
- Modify: `shared/ChangeVocabulary.cs:29-38`
- Modify: `CollegeLMS.API/Services/CorrectionImageService.cs:142-153`
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs:38-56, 369-389`
- Test: `CollegeLMS.MaxBot.Tests/ExamNotificationFormattingTests.cs`

**Interfaces:**
- Consumes: `ScheduleHistory { ChangeType = Exam, Subject = "ЭКЗАМЕН", NumberPair = 0 }` из Задачи 2.
- Produces: подпись «Экзамен» в PNG и Max, строка дайджеста `🎓 ЭКЗАМЕН` без номера пары и времени, маркер `🟢 ЭКЗАМЕН`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void ExamEntryHasNoPairNumberInDigest()
// Форматтер для ScheduleResponse { LessonType = "Exam", NumberPair = 0, Subject = "ЭКЗАМЕН" }
// возвращает строку, содержащую «ЭКЗАМЕН», но не содержащую «*0.*».

[Fact]
public void ChangeVocabularyLabelsExamAsExam()
Assert.Equal("Экзамен", ChangeVocabulary.Label("Exam"));
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter FullyQualifiedName~ExamNotificationFormattingTests`
Expected: FAIL — `Label("Exam")` возвращает «Изменено», в дайджесте есть `*0.*`.

- [ ] **Step 3: Implement**

1. `ChangeVocabulary.Label`: добавить `"Exam" => "Экзамен"`.
2. `CorrectionImageService.PairLabel`: для `position.ChangeType == Exam` вернуть `"—"` (не номер пары). `Describe` уже отдаёт «снимается = «—», вводится = «ЭКЗАМЕН»» через `Combine(position.Subject, position.TeacherName)`.
3. `MessageFormatter`: в обоих циклах вывода пар (строки ~40–56 и ~174, ~247) если `e.LessonType == "Exam"`, вывести `$"🎓 {e.Subject}"` и **не** выводить время/кабинет/номер пары; в `AppendChangeMarkers` (369–389) для экзамена маркер — `🟢 ЭКЗАМЕН` вместо `🟢 добавлено`.
4. `ScheduleNotifier` не менять: он передаёт уже сформированные `ScheduleResponse` из API.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter FullyQualifiedName~ExamNotificationFormattingTests`
Expected: PASS (2/2). Затем `dotnet build CollegeLMS.slnx` — успешно.

- [ ] **Step 5: Commit**

```bash
git add shared/ChangeVocabulary.cs CollegeLMS.API/Services/CorrectionImageService.cs CollegeLMS.MaxBot/Services/MessageFormatter.cs CollegeLMS.MaxBot.Tests/ExamNotificationFormattingTests.cs
git commit -m "feat: show exams in Max digest and correction PNG"
```

---

### Task 4: XLSX — импорт строки «ЭКЗАМЕН» (экспорт уже совпадает с образцом)

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs:590-744`
- Test: `CollegeLMS.Tests/Unit/Services/ExamCorrectionXlsxTests.cs`
- Important: `CorrectionBatchService.ToManualRow` (строки 881–904) **уже** отдаёт `AddedSubject = p.Subject`, `NumberPair = 0` → пустая «№»; изменять его не нужно, только покрыть тестом.

**Interfaces:**
- Consumes: правило Задачи 1 (`Subject == "ЭКЗАМЕН"`, пара 0); `ParsePair` (строки 1131–1144); `NormalizeSubjectCell` (1128).
- Produces: позиция `CorrectionPosition { ChangeType = Exam, NumberPair = 0, Subject = "ЭКЗАМЕН", TeacherId = null, Note = null }` при импорте; XLSX-строка образца без изменений в шаблоне.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void ImportExamRowCreatesExamPosition()
// XLSX: A=«РЭУ242», D=«ЭКЗАМЕН», F пусто → Preview[0].ChangeType == Exam,
// NumberPair == 0, Subject == "ЭКЗАМЕН", TeacherId == null, Note == null; ошибок нет.

[Fact]
public void ImportExamRowWithFilledPairDoesNotFail()
// Тот же файл, но F=«4» → та же позиция Exam с NumberPair == 0, ошибок нет.

[Fact]
public void ExportExamRowHasEmptyPairCellAndExamSubject()
// ManualCorrectionRow{ AddedSubject="ЭКЗАМЕН", NumberPair=0 } → в ячейке 4 «ЭКЗАМЕН»,
// в ячейке 6 пусто (проверить через ToManualRow/ExportAsync).
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionXlsxTests`
Expected: FAIL — импорт отвергает строку с пустой/чужой парой («не указан/некорректен № пары»).

- [ ] **Step 3: Implement**

1. После `var pairNumber = ParsePair(pairValue);` (строка 598) ввести `var isExam = string.Equals(addSubject, "ЭКЗАМЕН", StringComparison.OrdinalIgnoreCase);`.
2. В проверке `if (pairNumber is not { } pair || pair < 1 || pair > 8)` (618) добавить условие `&& !isExam`; локальную `pair` объявить как `var pair = pairNumber ?? 0;` до проверки, чтобы последующий код компилировался (для Exam она не используется).
3. В проверке «предмет ↔ преподаватель» для ввода (647) добавить `&& !isExam`.
4. В выборе `changeType` (739–744) добавить первый рукав: `(false, true) when isExam => ScheduleChangeType.Exam`, оставив `(false, true) => Add`.
5. Для Exam принудительно обнулить `addTeacherId`, `note` и `removed*` перед созданием `CorrectionPreviewEntry`; `noteOldPair` из колонки «Примеч.» для Exam игнорировать.
6. Ветку `if (changeType == ScheduleChangeType.Add)` (763) не трогать — Exam идёт мимо неё.
7. Никаких правок шаблона, размера файла и экспортной ячейки «№».

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ExamCorrectionXlsxTests`
Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Services/ScheduleCorrectionService.cs CollegeLMS.Tests/Unit/Services/ExamCorrectionXlsxTests.cs
git commit -m "feat: import exam rows from dispatcher XLSX"
```

---

### Task 5: View API — отдельный массив `Exams` в дне/неделе

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleViewDtos.cs:24-27`
- Modify: `CollegeLMS.API/Services/ScheduleViewService.cs:459-567, 603-689`
- Test: `CollegeLMS.Tests/Integration/Controllers/ScheduleViewControllerTests.cs` (добавить тест)

**Interfaces:**
- Consumes: `ScheduleEntry { LessonType = Exam, NumberPair = 0 }` из Задачи 2.
- Produces: `ScheduleDayViewResponse.Exams : List<ScheduleResponse>`; `Entries` больше не содержит экзамены.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task DayResponseContainsExamsSeparatelyFromEntries()
// После применённого экзамена GET /api/schedule/day?date=... возвращает:
// Exams.Length == 1, Exams[0].LessonType == "Exam", Exams[0].NumberPair == 0,
// Exams[0].Subject == "ЭКЗАМЕН", Entries.All(e => e.NumberPair >= 1 && e.LessonType != "Exam").
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~DayResponseContainsExamsSeparatelyFromEntries`
Expected: FAIL — свойства `Exams` нет (ошибка компиляции).

- [ ] **Step 3: Implement**

1. В `ScheduleDayViewResponse` после `Inserts` добавить `public List<ScheduleResponse> Exams { get; set; } = [];`.
2. В `BuildEntries` (612) добавить в `.Where(...)` условие `&& e.LessonType != LessonType.Exam`.
3. Добавить приватный метод с сигнатурой
   `private static List<ScheduleResponse> BuildExams(ScheduleRangeData data, DateTime target, int week, DayOfWeek effectiveDay)` —
   тот же проект, что и первая выборка `BuildEntries` (строки 612–630): фильтр по дню/неделе + `LessonType == Exam`, `e.ToDto(ScheduleChangeTags.ForPair(tags, ...))`, `LessonType = nameof(LessonType.Exam)`, `StartTime = EndTime = TimeSpan.Zero`, `NumberPair = 0`.
4. В `BuildDay` назначить `Exams = BuildExams(data, target, week, effectiveDay)` в трёх ветках, где есть записи: рабочая суббота (`effectiveDay` из ветки 470–472), практики (543, `target.DayOfWeek`), обычный день (565, `target.DayOfWeek`). В ветках «нерабочий», «воскресенье», «вне семестра» оставить значение по умолчанию `[]`.
5. `Inserts`, `Practices`, `BigBreak`, сортировка `OrderBy(e => e.NumberPair)` — не менять.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~DayResponseContainsExamsSeparatelyFromEntries`
Expected: PASS. Затем `dotnet build CollegeLMS.slnx` — успешно.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Dtos/ScheduleViewDtos.cs CollegeLMS.API/Services/ScheduleViewService.cs CollegeLMS.Tests/Integration/Controllers/ScheduleViewControllerTests.cs
git commit -m "feat: expose exams as separate schedule day array"
```

---

### Task 6: Frontend — тип операции, бейджи и стили

**Files:**
- Modify: `CollegeLMS.Next/types/correction.ts:3`
- Modify: `CollegeLMS.Next/lib/change-tags.ts:47-77`
- Modify: `CollegeLMS.Next/lib/status-style.ts:70`
- Test: `CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts` (новый сценарий) + `npm run build`

**Interfaces:**
- Consumes: `CorrectionChangeType` из API (Задача 1); `ScheduleDayViewResponse.Exams` (Задача 5).
- Produces: `CorrectionChangeType` включает `"Exam"`; `changeTagKind("Exam") === "Add"`; `CHANGE_LABEL.Exam === "Добавлено"`; стиль бейджа Exam.

- [ ] **Step 1: Write the failing test**

Добавить в `e2e/dispatcher-corrections.spec.ts` сценарий `exam operation is available in position dialog`, проверяющий, что в диалоге есть карточка «Экзамен», а после её выбора в превью строки появляется «ЭКЗАМЕН». Пока карточки нет — тест красный. Дополнительно: `npm run build` должен упасть на неполном `Record<CorrectionChangeType, ...>` после добавления значения в тип (проверить на шаге 3).

- [ ] **Step 2: Run test to verify it fails**

Run: `cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam operation"`
Expected: FAIL — карточки «Экзамен» нет.

- [ ] **Step 3: Implement**

1. `types/correction.ts:3` — `export type CorrectionChangeType = "Add" | "Remove" | "Replace" | "Move" | "Exam"`.
2. `lib/change-tags.ts`: `CHANGE_LABEL` → `Exam: "Добавлено"`; `PRIORITY` → `Exam: 4` (экзамен не перебивает снятие и сам.р.); `changeTagKind` не менять — тернарка уже даёт `Add`.
3. `lib/status-style.ts:70` — в `CHANGE_TYPE_STYLE` добавить `Exam` с тем же стилем, что `Add` (успех/зелёный).
4. `lib/correction-row.ts` и `api/correction.ts` правок не требуют — тип подхватится.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam operation"` и `cd CollegeLMS.Next && npm run build`
Expected: тест зелёный, сборка успешна (значит, все `Record<CorrectionChangeType, …>` дополнены).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.Next/types/correction.ts CollegeLMS.Next/lib/change-tags.ts CollegeLMS.Next/lib/status-style.ts CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts
git commit -m "feat: add Exam correction type to frontend models and badges"
```

---

### Task 7: Диалог позиции — третья операция «Экзамен»

**Files:**
- Modify: `CollegeLMS.Next/components/CorrectionPositionDialog.tsx:56-82, 98-150, 505-534, 640-715, 1130-1150`
- Test: `CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts` (продолжение сценария)

**Interfaces:**
- Consumes: `CorrectionChangeType` с `"Exam"` (Задача 6).
- Produces: payload позиции
  `{ changeType: "Exam", numberPair: 0, subject: "ЭКЗАМЕН", teacherId: null, teacherName: null, note: null, removedSubject: null, removedTeacherId: null, removedTeacherName: null, removedNumberPair: null }`.

- [ ] **Step 1: Write the failing test**

Продолжить сценарий из Задачи 6: выбрать «Экзамен» → выбрать группу →submit → перехватить POST `/api/schedule/correction/batches/{id}/positions` и проверить тело payload (точные значения выше). Плюс: при выборе «Экзамен» не видны шаги «Пара», «Преподаватель», «Предмет», «Примечание».

- [ ] **Step 2: Run test to verify it fails**

Run: `cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam operation"`
Expected: FAIL — нет ни карточки, ни нужного payload.

- [ ] **Step 3: Implement**

1. `type CorrectionOperation = "add" | "remove" | "exam"`.
2. В `OPERATION_CARDS` добавить третью карточку: `value: "exam"`, `label: "Экзамен"`, `hint: "Экзамен в этот день, без номера пары"`, `className: choiceBlock("warning")` (или другой существующий вариант токена — проверить `choiceBlock` в `lib/status-style.ts`). Обновить комментарий «Операций всего две» → «Операции: добавить, снять, экзамен».
3. В `payload` (513–534) добавить ветку: если `draft.operation === "exam"` — вернуть точный payload из Interfaces (groupId/groupName из выбранной группы), иначе прежняя логика `isRemove ? Remove : Add`.
4. В `Draft`-состоянии (139+) для exam не требовать `targetPair`, `subject`, `teacherIds` — валидация `handleSubmit` (556) должна пропускать их.
5. В разметке (640–715, 792–841, 948–979) обернуть секции «Пара», «Преподаватель», «Предмет», «Примечание» условием `draft.operation !== "exam"`; оставить выбор группы и предпросмотр строки → «Группа — ЭКЗАМЕН».
6. При редактировании существующей позиции Exam (`position.changeType === "Exam"`) инициализировать `operation: "exam"` (функция загрузки позиции ~210–232).

- [ ] **Step 4: Run test to verify it passes**

Run: `cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam operation"` и `cd CollegeLMS.Next && npm run build`
Expected: PASS, сборка успешна.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.Next/components/CorrectionPositionDialog.tsx CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts
git commit -m "feat: exam operation in correction position dialog"
```

---

### Task 8: Отображение экзамена в расписании и редакторе позиций

**Files:**
- Modify: `CollegeLMS.Next/types/schedule.ts:69-77`
- Modify: `CollegeLMS.Next/components/ScheduleDayView.tsx:155`
- Modify: `CollegeLMS.Next/components/ScheduleWeekView.tsx:85`
- Modify: `CollegeLMS.Next/components/ScheduleTable.tsx:141-145`
- Modify: `CollegeLMS.Next/components/ScheduleSemesterMatrix.tsx:73`
- Modify: `CollegeLMS.Next/lib/schedule-merge.ts:105`
- Modify: `CollegeLMS.Next/components/CorrectionPositionEditor.tsx:76-80, 524`
- Modify: `CollegeLMS.Next/components/max/ScheduleView.tsx:211,222,462`, `max/DayFeed.tsx:44`, `max/WeekFeed.tsx:27,42,64`
- Test: `CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts` + `npm run check:design-system`

**Interfaces:**
- Consumes: `data.exams : ScheduleResponse[]` (Задача 5); `changeTagKind`/`CHANGE_LABEL` (Задача 6).
- Produces: отдельная карточка/строка «Экзамен» в дне, неделе, семестровой матрице, мини-приложении и Max; бейдж и фильтр «Экзамен» в редакторе позиций.

- [ ] **Step 1: Write the failing test**

В `e2e/dispatcher-corrections.spec.ts` добавить `exam is visible in day schedule and position editor`: после применения пакета с экзаменом страница дня содержит текст «ЭКЗАМЕН» вне нумерованных пар, а в редакторе позиций в фильтре типов есть «Экзамен» и бейдж позиции — «Добавлено».

- [ ] **Step 2: Run test to verify it fails**

Run: `cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam is visible"`
Expected: FAIL — массив `exams` не используется, карточки нет.

- [ ] **Step 3: Implement**

1. `types/schedule.ts:69-77` — добавить в `ScheduleDayView` поле `exams: ScheduleResponse[]` (ответ API Задачи 5).
2. `ScheduleDayView.tsx:155` — передать `exams={data.exams}` в компонент строк; отрисовать отдельную карточку «🎓 ЭКЗАМЕН» с цветом из токена `--lesson-exam` (уже есть в `app/globals.css`, строки 59/109/293) и без номера пары/времени.
3. `ScheduleWeekView.tsx:85`, `ScheduleTable.tsx:141-145`, `ScheduleSemesterMatrix.tsx:73`, `lib/schedule-merge.ts:105` — по образцу `inserts`: merged-строки экзамена идут отдельной строкой дня, не смешиваясь с `entries` (иначе сломается группировка 1–7).
4. `CorrectionPositionEditor.tsx`: `CHANGE_TYPE_META` (76–80) дополнить `Exam` (подпись «Экзамен»), фильтр (524) — `["Add", "Remove", "Exam"]`; `PositionTypeBadge` и `renderTitle` должны показывать «Экзамен» и «Группа — ЭКЗАМЕН».
5. `components/max/ScheduleView.tsx`, `DayFeed.tsx`, `WeekFeed.tsx` — передавать/отображать `exams` рядом с `inserts`, той же карточкой, что и в вебе (без `*0.*`).
6. Новые CSS-классы не вводить — использовать существующий `--lesson-exam`.

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
cd CollegeLMS.Next && npx playwright test e2e/dispatcher-corrections.spec.ts --grep "exam is visible"
cd CollegeLMS.Next && npm run check:design-system
cd CollegeLMS.Next && npm run build
```
Expected: все три команды успешны.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.Next/components CollegeLMS.Next/lib/schedule-merge.ts CollegeLMS.Next/e2e/dispatcher-corrections.spec.ts
git commit -m "feat: render exams in schedule views and position editor"
```

---

### Task 9: Документация API и ТЗ

**Files:**
- Modify: `docs/spec/task-dispatcher-corrections.md:4.1, 5.1` (и раздел про журнал/откат)
- Modify: `docs/spec/CollegeLMS.postman_collection.json`
- Modify: Swagger-примеры в `CollegeLMS.API/Controllers/CorrectionBatchController.cs`, `ScheduleCorrectionController.cs`
- Modify (если установлен): проектная копия скилла `collegelms-schedule` — описание операций; файл скилла lives в `/home/user1/.bb/runtime/global-skills/.../skills/collegelms-schedule/SKILL.md` (править только если это локальная рабочая копия, не глобальный артефакт)

**Interfaces:**
- Consumes: все предыдущие задачи (итоговая форма API и данных).
- Produces: актуальные description/example для `changeType=Exam`, `numberPair=0`; обновлённое ТЗ, где «Операций всего две» заменено на «Операции: Добавить, Снять, Экзамен»; Postman-запросы с примером позиции Exam.

- [ ] **Step 1: Verify (no failing test)**

Run: `grep -R "Операций всего две" docs/spec/ CollegeLMS.API/ CollegeLMS.Next/`
Expected: найдены вхождения, требующие правки (это база для шага 3).

- [ ] **Step 2: Check current docs**

Run: `grep -R "numberPair" docs/spec/CollegeLMS.postman_collection.json | head -20`
Expected: примеры позиций с 1–7, примера Exam нет.

- [ ] **Step 3: Implement**

1. `task-dispatcher-corrections.md`: в 4.1 и 5.1 заменить «операций всего две» на три; добавить описание Exam: `changeType=Exam, numberPair=0, subject=ЭКЗАМЕН`, без преподавателя и примечания; в журнале — бейдж «Добавлено» и подпись «Экзамен»; в XLSX — пустая «№». Обновить описание отката.
2. Postman: добавить пример `POST /api/schedule/correction/batches/{id}/positions` с телом Exam и пример ответа; обновить описание коллекции.
3. Swagger: в `CorrectionBatchController.cs` и `ScheduleCorrectionController.cs` дописать `/// <remarks>`/примеры для `changeType=Exam`, включая XLSX-строку образца.
4. Не трогать `docs/superpowers/specs/2026-11-16-group-exam-day-design.md` — это чужая незакоммиченная работа.

- [ ] **Step 4: Verify**

Run:
```bash
dotnet build CollegeLMS.slnx
cd CollegeLMS.Next && npm run build
python3 -m json.tool docs/spec/CollegeLMS.postman_collection.json > /dev/null
```
Expected: сборки успешны, JSON валиден, `grep "Операций всего две"` не даёт вхождений в обновлённых файлах.

- [ ] **Step 5: Commit**

```bash
git add docs/spec/task-dispatcher-corrections.md docs/spec/CollegeLMS.postman_collection.json CollegeLMS.API/Controllers/CorrectionBatchController.cs CollegeLMS.API/Controllers/ScheduleCorrectionController.cs
git commit -m "docs: describe exam corrections in spec, Swagger and Postman"
```

---

## Self-Review

- **Spec coverage:** пп. 5.1 (модель/валидация) → Задача 1; 5.2 (apply/history/revert/уведомление) → Задачи 2, 3; 5.3 (XLSX) → Задача 4; 5.4 (view/UI/бот) → Задачи 5, 7, 8; 5.5 (тесты/Swagger/Postman/ТЗ) → все задачи + Задача 6. Не-цели (отдельная сущность, привязка к паре, изменение вставок) — не реализуются.
- **Step scan:** у каждой задачи тест → красный → минимальная реализация → зелёный → коммит; для Задачи 9 (чистая документация) шаги проверки заменяют тест явно.
- **Type consistency:** `ScheduleChangeType.Exam` / `CorrectionChangeType = "Exam"` / `nameof(ScheduleChangeType.Exam)` / `LessonType.Exam` / `Exams` / `IsExam` / `ApplyCreatedEntry` / `BuildExams` — имена согласованы во всех задачах.
- **Review Focus:** все пять пунктов закрыты тестами в задачах 2, 4, 5, 7, 8.
- **Proportion:** план длиннее дизайна, но состоит главным образом из сигнатур, точных значений и команд; тел алгоритмов почти нет — это план, а не транскрипт кода.
