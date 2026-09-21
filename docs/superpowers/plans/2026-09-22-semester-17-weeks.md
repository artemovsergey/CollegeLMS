# Семестр 17 недель (фикс импорта расписания) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Импорт полного расписания принимает недели 1–17: константа семестра `StudyWeek.TotalWeeks` меняется с 16 на 17, зависящие тесты, фронтенд-фолбэки и живые спеки приводятся в соответствие.

**Architecture:** Точечная правка одной константы; все потребители (`ScheduleService`, `ScheduleViewService`, `ScheduleExportService`, `ScheduleImportService`, `ScheduleValidators`, `PracticeService`, `CorrectionBatchService`) считают диапазон динамически от `TotalWeeks` и код не меняют. Вынос семестра в конфигурацию (CFG-1) остаётся отложенным.

**Tech Stack:** .NET 10 / ASP.NET Core, xUnit + FluentAssertions + ClosedXML, Next.js 14 + TypeScript, Playwright.

**Spec:** `docs/spec/task-schedule-management.md:84` (семестр — начало и количество недель; CFG-1 отложен), `docs/spec/task-schedule-crud-import.md` §5.2.

## Global Constraints

- Данные, комментарии, сообщения об ошибках — на русском.
- `dotnet csharpier format .` перед коммитом; проверка — `dotnet csharpier check .`.
- Commit-префиксы: `fix:` / `docs:`; пуш в `master` только при заданном `GITHUB_TOKEN`/`GH_TOKEN`.
- Даты семестра: старт 01.09.2026, неделя 1 = Пн 31.08–06.09, неделя 17 = 21.12–27.12.2026, последний день семестра 27.12.2026.
- Файл `import/schedule/Расписание.xlsx` закоммичен: «Лист1», недели 1–17, ячейка C6 = `232 История (1-17) Петренко В.Б.`.

---

### Task 1: Backend — TotalWeeks 16→17 + тесты

**Files:**
- Modify: `CollegeLMS.API/Services/StudyWeek.cs:32`
- Modify: `CollegeLMS.Tests/Unit/Services/StudyWeekTests.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleImportServiceTests.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs`
- Modify: `CollegeLMS.Tests/Integration/Controllers/ScheduleDateApiTests.cs`

**Interfaces:**
- Consumes: `StudyWeek.TotalWeeks` (static int) — единственная точка правки.
- Produces: диапазон недель 1–17 во всех сервисах, сообщениях и мете API.

- [ ] **Step 1 (Red):** `StudyWeekTests.cs` — в теорию `WeekOf_ReturnsExpected` добавить `[InlineData("2026-12-21", 17)]`; в `IsInSemester_ReturnsExpected` заменить `[InlineData("2026-12-21", false)]` на `[InlineData("2026-12-21", true)]` и добавить `[InlineData("2026-12-28", false)]`.
- [ ] **Step 2:** `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~StudyWeekTests` → FAIL (16 ≠ 17; IsInSemester 21.12 = false).
- [ ] **Step 3 (Green):** `StudyWeek.cs:32` → `public static int TotalWeeks { get; } = 17;`.
- [ ] **Step 4:** повторный запуск → PASS.
- [ ] **Step 5:** добавить регрессионный тест в `ScheduleImportServiceTests.cs` (после `ParseScheduleMatrix_WeekAboveSemester_ReturnsErrorWithNewFormat`):

```csharp
[Fact]
public void ParseScheduleMatrix_Week17_Parsed()
{
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Расписание");

    ws.Cell(5, 3).Value = "ПО 262";
    ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
    ws.Cell(6, 2).Value = 1;
    ws.Cell(6, 3).Value = "232 История (1-17) Петренко В.Б.";

    var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

    errors.Should().BeEmpty();
    entries.Should().ContainSingle();
    entries[0].Weeks.Should().BeEquivalentTo(Enumerable.Range(1, 17));
}
```

- [ ] **Step 6:** обновить ожидания существующих тестов:
  - `ScheduleImportServiceTests.cs:318`, `:492`: `диапазоне 1–16` → `1–17`
  - `:837`: `неделя 17 вне семестра (1–16)` → `неделя 18 вне семестра (1–17)`
  - `:859`, `:945`: `вне семестра (1–16)` → `вне семестра (1–17)`
  - `ScheduleViewServiceTests.cs:567`: комментарий `+ 16 недель → конец 20.12.2026` → `+ 17 недель → конец 27.12.2026`; `:569` дата `2026-12-21` → `2026-12-28`
  - `ScheduleDateApiTests.cs:58`: `Assert.Equal(16, ...)` → `17`
  - Тесты с ячейками `(1-16)` и динамическим `StudyWeek.TotalWeeks` не менять.
- [ ] **Step 7:** `dotnet csharpier format .`; `dotnet build CollegeLMS.slnx`; таргетные прогоны `--filter` для `StudyWeekTests`, `ScheduleImportServiceTests`, `ScheduleViewServiceTests`, `ScheduleServiceTests`, `ScheduleDateApiTests` → PASS.
- [ ] **Step 8:** commit: `fix(schedule): считать семестр 17-недельным (импорт недель 1–17)`

### Task 2: Frontend — фолбэк 17 + E2E-мок

**Files:**
- Modify: `CollegeLMS.Next/components/max/ScheduleView.tsx:218,238`
- Modify: `CollegeLMS.Next/e2e/max-miniapp.spec.ts:8`

- [ ] **Step 1:** `meta?.totalWeeks ?? 16` → `meta?.totalWeeks ?? 17` (оба места).
- [ ] **Step 2:** в моке `totalWeeks: 16` → `totalWeeks: 17`.
- [ ] **Step 3:** `cd CollegeLMS.Next; npm run build` → PASS; `npx playwright test max-miniapp` → PASS.
- [ ] **Step 4:** commit: `fix(max): фолбэк totalWeeks = 17`

### Task 3: Docs + Postman

**Files:**
- Modify: `docs/spec/task-schedule-crud-import.md` (строки 50, 87, 112, 114, 116, 190)
- Modify: `docs/spec/task-schedule-views.md` (строки 40–43, 59, 101, 119, 144–145, 214)
- Modify: `docs/spec/task-schedule-reference-data.md` (строки 122, 142)
- Modify: `docs/spec/CollegeLMS.postman_collection.json` (~1454, 1475, 1513, 1647, 1691, 1697, 1715, 1778, 1796)

- [ ] **Step 1:** заменить в живых спеках `16` → `17`, `1–16`/`1…16` → `1–17`/`1…17`, `20.12.2026` → `27.12.2026`, `16 недель` → `17 недель`, `16·7 − 1` → `17·7 − 1`; в примерах ошибок: `неделя 17 вне семестра (1–16)` → `неделя 18 вне семестра (1–17)`, в Postman `"totalWeeks": 16` → `17`.
- [ ] **Step 2:** исторические `docs/superpowers/**` и `docs/spec/task-bot-miniapp-completion.md` не трогать.
- [ ] **Step 3:** commit: `docs(spec): семестр 17 недель`

### Task 4: Финал

- [ ] **Step 1:** `dotnet csharpier check .`, `dotnet build CollegeLMS.slnx`, таргетные тесты, `cd CollegeLMS.Next; npm run build` — свежие выводы.
- [ ] **Step 2:** push в `master` (при наличии токена), `gh run list --limit 5` — quality → deploy запущены.
- [ ] **Step 3:** скилл `verification-before-completion` перед утверждением «готово».
