# Dispatcher Corrections (Пакеты корректировок) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Persist dispatcher corrections as batch+position entities, add "сам.р." per-week badge semantics for Remove, aggregate MaxBot notifications per subscriber.

**Architecture:** Two new entities `CorrectionBatch` (header) + `CorrectionPosition` (row) with a new `CorrectionBatchService`/`ICorrectionBatchService` and a `CorrectionBatchController`. Apply logic reused from `ScheduleCorrectionService.ApplyEntryAsync`. MaxBot aggregates revisions per recipient.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core (Npgsql, snake_case), FluentValidation, ClosedXML, Next.js 14 + Tailwind 4.

**Spec:** docs/superpowers/specs/2026-09-16-dispatcher-corrections-design.md (см. дизайн в чате)

## Global Constraints

- Всё на русском (сообщения, комментарии, Swagger).
- `Result<T>` — везде, без try-catch в контроллерах/сервисах.
- Primary constructor DI, `CancellationToken ct`, `AsNoTracking()` на чтении.
- GUID PK `ValueGeneratedNever()`, enum `HasConversion<string>()` + `HasMaxLength()`.
- CHECK constraints — в `Data/DbConstraints.cs`, индексы — `HasIndex` в Configuration.
- Мапперы — статические расширения, `Mappers/`.
- DI — в `Extensions/ServiceCollectionExtensions.cs`, не в Program.cs.
- Сам.р. действует только для `Remove`: пара не удаляется, неделя сохраняется, бейдж по `ScheduleHistory.Note`.

---

### Task 1: Entities + enums + configs + DbContext + migration

**Files:**
- Create: `CollegeLMS.API/Entities/Enums/CorrectionBatchStatus.cs`
- Create: `CollegeLMS.API/Entities/Enums/CorrectionPositionStatus.cs`
- Create: `CollegeLMS.API/Entities/CorrectionBatch.cs`
- Create: `CollegeLMS.API/Entities/CorrectionPosition.cs`
- Create: `CollegeLMS.API/Data/Configurations/CorrectionBatchConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/CorrectionPositionConfiguration.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs` (2 DbSet)
- Migration: `AddCorrectionBatchAndPosition`

Enums:

```csharp
// CorrectionBatchStatus.cs
public enum CorrectionBatchStatus { Draft, Applied, Cancelled }

// CorrectionPositionStatus.cs
public enum CorrectionPositionStatus { Draft, Applied }
```

`CorrectionBatch` fields: Id, CorrectionDate (DateTime), Week (int), DayOfWeek (int), Status, CreatedByUserId (Guid?), AppliedByUserId (Guid?), AppliedAt (DateTime?), Positions (nav, [JsonIgnore]).

`CorrectionPosition` fields: Id, BatchId (Guid), Row (int), ChangeType (ScheduleChangeType), GroupId (Guid), GroupName (string), DayOfWeek (int), Week (int), NumberPair (int), Subject?, TeacherId?, TeacherName?, RemovedSubject?, RemovedTeacherId?, RemovedTeacherName?, RemovedNumberPair (int?), Note?, Status, HistoryId (Guid?), Batch (nav, [JsonIgnore]).

Verification: `dotnet build` + `dotnet ef migrations add`.

---

### Task 2: DTOs + mapper + interface + service + controller + DI

**Files:**
- Create: `CollegeLMS.API/Dtos/CorrectionBatchDtos.cs`
- Create: `CollegeLMS.API/Mappers/CorrectionBatchMapper.cs`
- Create: `CollegeLMS.API/Interfaces/ICorrectionBatchService.cs`
- Create: `CollegeLMS.API/Services/CorrectionBatchService.cs`
- Create: `CollegeLMS.API/Controllers/CorrectionBatchController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (DI)
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (extract reusable apply helpers)

Endpoints (route `api/schedule/correction/batches`):
- POST `` (create batch from date)
- GET ``?status=Draft (list)
- GET `{id}` (batch + positions)
- DELETE `{id}`
- POST `{id}/positions`
- PUT `{id}/positions/{positionId}`
- DELETE `{id}/positions/{positionId}`
- POST `{id}/import` (multipart XLSX)
- POST `{id}/export` (XLSX download, timestamp)
- POST `{id}/apply`

---

### Task 3: "сам.р." semantics in apply

Modify `ScheduleCorrectionService.ApplyEntryAsync` Remove branch: if `IsSelfStudyNote(entry.Note)` → do NOT remove week from `ScheduleEntry.Weeks`, keep entry, still record `ScheduleHistory` (ChangeType=Remove, Note="сам.р."). Add validation: "сам.р." only valid for Remove.

Frontend badge: extend `ChangeTagBadge` + `MessageFormatter.AppendChangeMarkers` to render self-study badge for Remove when note == "сам.р.".

---

### Task 4: Notification aggregation

- Add `CorrectionDate` (DateTime) to `ScheduleChangeDto` (API) and `NotifyChangeDto` (MaxBot).
- MaxBot: add `correction_date TIMESTAMPTZ` column to `schedule_revisions` (idempotent raw SQL in Program.cs).
- `ChangeNotifier`: group revisions per recipient; `MessageFormatter.FormatCorrectionDigest(date, positions)`.

---

### Task 5: Tests

- Unit: `CorrectionBatchService` (validation, сам.р.), `ChangeNotifier.SelectRecipients` (aggregation), `MessageFormatter` digest.
- Integration (WebApplicationFactory): batch/position CRUD, apply flow.

---

### Task 6: Frontend

- `api/correction.ts`: add batch/position API functions.
- `types/correction.ts`: batch/position types.
- New components: `CorrectionBatchList`, `CorrectionPositionEditor`, `RemovePairPicker`.
- Rework `dispatcher/correction/page.tsx` tabs: Пакеты / Редактор / Импорт / Журнал.
- `ChangeTagBadge`: сам.р. badge for Remove.

---

### Task 7: Docs

