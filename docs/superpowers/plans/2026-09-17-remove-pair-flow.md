# «Снять пару» — направляющий флоу (Remove) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить табличную форму ручной корректировки расписания направляющим флоу «Снять пару» (веб + max mini-app) на базе нового endpoint `GET /api/schedule/correction/day`.

**Architecture:** Новый endpoint `GET /api/schedule/correction/day?groupId=&date=` возвращает пары указанного дня, активные на неделю `StudyWeek.ForDate(date)`, с `changeTags` (реюз `ScheduleService.GetAllAsync`). Применение — существующий `POST /confirm` с одной записью `Remove`. Клиент (веб и mini-app) строит запись через общий `buildRemoveEntry`. Неделя вычисляется сервером и берётся клиентом из ответа дня.

**Tech Stack:** ASP.NET Core Web API (.NET 10), EF Core 9 (InMemory в тестах), xUnit + FluentAssertions, WebApplicationFactory, Next.js 14 (App Router) + shadcn/ui + @maxhub/max-ui.

## Global Constraints

- Все сообщения в коде/UI — на русском; Swagger summaries на русском.
- `Result<T>` везde в сервисах/контроллерах, без try-catch в сервисах.
- `AsNoTracking()` на чтении; `CancellationToken ct` на всех async-методах.
- Первичный конструктор DI; мапперы — статические расширения.
- Форматирование: CSharpier (`dotnet csharpier format .`), git-префиксы `feat:`/`test:`/`refactor:`.
- Коммиты с `git add -A`; после финала — push в origin/master (AGENTS.md).
- Спека: `docs/superpowers/specs/2026-09-17-remove-pair-flow-design.md` (согласована 2026-09-17).

---

### Task 1: Backend — DTO, интерфейс и `GetDayAsync` (unit TDD)

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs` (добавить `CorrectionDayResponse`)
- Modify: `CollegeLMS.API/Interfaces/IScheduleCorrectionService.cs` (добавить `GetDayAsync`)
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (конструктор + реализация `GetDayAsync`)
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs`

**Interfaces:**
- Produces: `Task<Result<CorrectionDayResponse>> GetDayAsync(Guid groupId, DateTime date, CancellationToken ct)`.
- Produces: `CorrectionDayResponse { DateTime Date; int Week; int DayOfWeek; List<ScheduleResponse> Entries }`.
- Consumes: `IScheduleService.GetAllAsync(Guid? groupId, ..., DateTime? date, ...)` (неделя+день вычисляются внутри по дате), `StudyWeek.ForDate(date)`.

- [ ] **Step 1: Добавить DTO**

В `CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs` добавить класс (в конец файла):

```csharp
public class CorrectionDayResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public List<ScheduleResponse> Entries { get; set; } = [];
}
```

- [ ] **Step 2: Добавить метод в интерфейс**

В `CollegeLMS.API/Interfaces/IScheduleCorrectionService.cs` после `ConfirmAsync` добавить:

```csharp
    Task<Result<CorrectionDayResponse>> GetDayAsync(
        Guid groupId,
        DateTime date,
        CancellationToken ct
    );
```

- [ ] **Step 3: Обновить хелперы unit-тестов**

В `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs`:

1) Заменить `CreateSut` (конструктор сервиса теперь принимает `IScheduleService`):

```csharp
    private ScheduleCorrectionService CreateSut(HttpClient http) =>
        new(
            _db,
            new MaxBotHttpClient(http, NullLogger<MaxBotHttpClient>.Instance),
            new ScheduleService(_db, new ScheduleExportService(_db))
        );
```

2) Добавить параметр дня в `SeedEntryAsync` (подпись) и переиспользовать в теле:

```csharp
    private async Task<ScheduleEntry> SeedEntryAsync(
        Guid groupId,
        Guid teacherId,
        string subject,
        int pair,
        List<int> weeks,
        DayOfWeek dayOfWeek = DayOfWeek.Tuesday
    )
```

В теле заменить `DayOfWeek = DayOfWeek.Tuesday,` на `DayOfWeek = dayOfWeek,`.

3) Добавить хелпер истории в конец класса (перед закрывающей скобкой):

```csharp
    private async Task SeedHistoryAsync(
        Guid groupId,
        ScheduleChangeType changeType,
        int week,
        int numberPair,
        string? note = null
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = changeType,
                AppliedAt = utcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = groupId,
                Subject = "Физика",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = numberPair,
                Week = week,
                Note = note,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }
```

- [ ] **Step 4: Написать падающие unit-тесты GetDayAsync**

Добавить секцию перед закрывающей скобкой класса (после хелпера истории):

```csharp
    // --- Задача: GET /api/schedule/correction/day ---

    [Fact]
    public async Task GetDayAsync_Weekend_ReturnsBadRequest()
    {
        var weekend = new DateTime(2026, 9, 12); // суббота

        var result = await _sut.GetDayAsync(Guid.NewGuid(), weekend, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("выходной");
    }

    [Fact]
    public async Task GetDayAsync_UnknownGroup_ReturnsBadRequest()
    {
        var result = await _sut.GetDayAsync(
            Guid.NewGuid(),
            new DateTime(2026, 9, 8),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("не найдена");
    }

    [Fact]
    public async Task GetDayAsync_ReturnsWeekAndDayOnlyForGivenDate()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var onWeek2 = await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2, 3]);
        await SeedEntryAsync(group.Id, teacher.Id, "Химия", 3, [3]); // не на неделе 2

        var result = await _sut.GetDayAsync(
            group.Id,
            new DateTime(2026, 9, 8), // вторник, неделя 2
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Week.Should().Be(2);
        result.Data!.DayOfWeek.Should().Be((int)DayOfWeek.Tuesday);
        var dto = result.Data!.Entries.Should().ContainSingle().Subject;
        dto.Id.Should().Be(onWeek2.Id);
        dto.ChangeTags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_IncludesRemoveChangeTagForWeek()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var entry = await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2, 3]);
        await SeedHistoryAsync(group.Id, ScheduleChangeType.Remove, 2, 2, "сам.р.");

        var result = await _sut.GetDayAsync(
            group.Id,
            new DateTime(2026, 9, 8),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var dto = result.Data!.Entries.Should().ContainSingle().Subject;
        dto.Id.Should().Be(entry.Id);
        var tag = dto.ChangeTags.Should().ContainSingle().Subject;
        tag.ChangeType.Should().Be(ScheduleChangeType.Remove);
        tag.Week.Should().Be(2);
        tag.Note.Should().Be("сам.р.");
    }

    [Fact]
    public async Task GetDayAsync_EmptyDay_ReturnsZeroEntries()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(
            group.Id,
            teacher.Id,
            "Физика",
            2,
            [2],
            DayOfWeek.Wednesday // среда, а запрашиваем вторник
        );

        var result = await _sut.GetDayAsync(
            group.Id,
            new DateTime(2026, 9, 8),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Entries.Should().BeEmpty();
    }
```

- [ ] **Step 5: Запустить тесты — убедиться что падают (RED)**

Run: `dotnet test CollegeLMS.Tests --filter "GetDayAsync" -v minimum`
Expected: сборка упадёт (нет метода `GetDayAsync` в интерфейсе/сервисе).

- [ ] **Step 6: Реализовать `GetDayAsync` + конструктор**

В `CollegeLMS.API/Services/ScheduleCorrectionService.cs`:

1) Конструктор (строка 14) заменить на:

```csharp
public class ScheduleCorrectionService(
    AppDbContext db,
    MaxBotHttpClient maxBot,
    IScheduleService scheduleService
) : IScheduleCorrectionService
{
```

2) После метода `GetHistoryAsync` (конец класса) добавить:

```csharp
    public async Task<Result<CorrectionDayResponse>> GetDayAsync(
        Guid groupId,
        DateTime date,
        CancellationToken ct
    )
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return Result<CorrectionDayResponse>.Fail(
                "Корректировка не может быть на выходной день.",
                400
            );

        var groupExists = await db.Groups.AsNoTracking().AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            return Result<CorrectionDayResponse>.Fail("Группа не найдена.", 400);

        var week = StudyWeek.ForDate(date);

        var scheduleResult = await scheduleService.GetAllAsync(
            groupId: groupId,
            date: date,
            pageSize: 200,
            ct: ct
        );
        if (!scheduleResult.IsSuccess)
            return Result<CorrectionDayResponse>.Fail(
                scheduleResult.ErrorMessage ?? "Ошибка загрузки расписания.",
                scheduleResult.StatusCode
            );

        return Result<CorrectionDayResponse>.Ok(
            new CorrectionDayResponse
            {
                Date = date.Date,
                Week = week,
                DayOfWeek = (int)date.DayOfWeek,
                Entries = scheduleResult.Data!.Items,
            }
        );
    }
```

- [ ] **Step 7: Запустить тесты (GREEN)**

Run: `dotnet test CollegeLMS.Tests --filter "GetDayAsync" -v minimum`
Expected: 5 тестов PASS.

- [ ] **Step 8: Полный прогон + формат**

Run: `dotnet test CollegeLMS.Tests -v minimum` (все тесты зелёные), затем `dotnet csharpier format .`
Expected: все тесты PASS, форматирование без ошибок.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: GET /api/schedule/correction/day — пары дня с changeTags (GetDayAsync)"
```

### Task 2: Backend — контроллер `GET /correction/day` (integration TDD)

**Files:**
- Modify: `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs`
- Test: `CollegeLMS.Tests/Integration/Controllers/ScheduleCorrectionControllerTests.cs`

**Interfaces:**
- Consumes: `service.GetDayAsync(Guid, DateTime, CancellationToken)`.
- Produces: `GET /api/schedule/correction/day?groupId={guid}&date={yyyy-MM-dd}` → `200 Result<CorrectionDayResponse>` / `400` / `401` / `403`.

- [ ] **Step 1: Написать падающие integration-тесты**

Добавить в конец класса `ScheduleCorrectionControllerTests` (перед закрывающей скобкой):

```csharp
    // --- Задача: GET /api/schedule/correction/day ---

    private static string GardenDate => "2026-09-08";

    [Fact]
    public async Task GetDay_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(
            $"/api/schedule/correction/day?groupId={Guid.NewGuid()}&date={GardenDate}"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDay_StudentToken_ReturnsForbidden()
    {
        SetAuthHeader(GetToken(UserRole.Student));

        var response = await Client.GetAsync(
            $"/api/schedule/correction/day?groupId={Guid.NewGuid()}&date={GardenDate}"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDay_Weekend_ReturnsBadRequest()
    {
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync(
            $"/api/schedule/correction/day?groupId={Guid.NewGuid()}&date=2026-09-13"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDay_ValidGroupsAndDate_ReturnsEntries()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var utcNow = DateTime.UtcNow;
            db.ScheduleEntries.Add(
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    TeacherId = teacher.Id,
                    Subject = "Физика",
                    Room = "303",
                    DayOfWeek = DayOfWeek.Tuesday,
                    NumberPair = 2,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(9, 30, 0),
                    Weeks = [2],
                    LessonType = LessonType.Practice,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            await db.SaveChangesAsync();
        }
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync(
            $"/api/schedule/correction/day?groupId={group.Id}&date={GardenDate}"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<CorrectionDayResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(2, body.Data!.Week);
        Assert.Equal((int)DayOfWeek.Tuesday, body.Data!.DayOfWeek);
        var entry = Assert.Single(body.Data!.Entries);
        Assert.Equal("Физика", entry.Subject);
        Assert.Equal("ПО-262", entry.GroupName);
    }
```

- [ ] **Step 2: Запустить тесты — RED**

Run: `dotnet test CollegeLMS.Tests --filter "GetDay_" -v minimum`
Expected: падают (404 — endpoint нет).

- [ ] **Step 3: Реализовать endpoint**

В `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs` после метода `GetHistory` (в конец класса) добавить:

```csharp
    /// <summary>
    /// Пары дня для направляющего флоу «Снять пару».
    /// </summary>
    /// <remarks>
    /// Возвращает занятия указанной группы на дату, активные на учебную неделю
    /// (week = StudyWeek.ForDate(date)), с тегами изменений changeTags.
    /// </remarks>
    /// <response code="200">Пары дня получены</response>
    /// <response code="400">Группа не найдена или дата — выходной день</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("correction/day")]
    [SwaggerOperation(Summary = "Пары дня для снятия пары")]
    [SwaggerResponse(200, "Пары дня получены", typeof(Result<CorrectionDayResponse>))]
    [SwaggerResponse(400, "Некорректные входные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCorrectionDay(
        [FromQuery] Guid groupId,
        [FromQuery] DateTime date,
        CancellationToken ct
    )
    {
        var result = await service.GetDayAsync(groupId, date, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }
```

- [ ] **Step 4: Запустить тесты — GREEN**

Run: `dotnet test CollegeLMS.Tests --filter "GetDay_" -v minimum`
Expected: 4 теста PASS.

- [ ] **Step 5: Полный прогон + формат**

Run: `dotnet test CollegeLMS.Tests -v minimum && dotnet csharpier format .`
Expected: все тесты PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: контроллер GET /api/schedule/correction/day (Swagger + integration tests)"
```

### Task 3: Frontend — типы, API-клиент и `buildRemoveEntry`

**Files:**
- Modify: `CollegeLMS.Next/types/correction.ts`
- Modify: `CollegeLMS.Next/api/correction.ts`
- Create: `CollegeLMS.Next/lib/correction.ts`

**Interfaces:**
- Produces: `getCorrectionDay(groupId: string, date: string): Promise<CorrectionDayResponse>`.
- Produces: `buildRemoveEntry(day: CorrectionDayResponse, entry: ScheduleResponse, note: string): CorrectionPreviewEntry`.
- Consumes: `types/schedule.ScheduleResponse`, `types/correction.CorrectionPreviewEntry`.

- [ ] **Step 1: Добавить тип `CorrectionDayResponse`**

В `CollegeLMS.Next/types/correction.ts` в начало файла добавить импорт и интерфейс (после `CorrectionChangeType`):

```typescript
import type { ScheduleResponse } from "@/types/schedule"

export interface CorrectionDayResponse {
  date: string
  week: number
  dayOfWeek: number
  entries: ScheduleResponse[]
}
```

> Примечание: файл `types/schedule.ts` уже импортирует `ChangeTag` из `correction.ts`; тип-импорт не создаёт runtime-цикла.

- [ ] **Step 2: Добавить `getCorrectionDay`**

В `CollegeLMS.Next/api/correction.ts`: добавить `CorrectionDayResponse` в импорт типов из `@/types/correction` и функцию в конец файла:

```typescript
export async function getCorrectionDay(
  groupId: string,
  date: string,
): Promise<CorrectionDayResponse> {
  const qs = new URLSearchParams({ groupId, date })
  return unwrap(
    await api.get<Result<CorrectionDayResponse>>(
      `/api/schedule/correction/day?${qs.toString()}`,
    ),
  )
}
```

- [ ] **Step 3: Создать `lib/correction.ts`**

Новый файл `CollegeLMS.Next/lib/correction.ts`:

```typescript
import type { CorrectionDayResponse, CorrectionPreviewEntry } from "@/types/correction"
import type { ScheduleResponse } from "@/types/schedule"

export function buildRemoveEntry(
  day: CorrectionDayResponse,
  entry: ScheduleResponse,
  note: string,
): CorrectionPreviewEntry {
  return {
    row: 0,
    changeType: "Remove",
    groupId: entry.groupId,
    groupName: entry.groupName,
    dayOfWeek: day.dayOfWeek,
    week: day.week,
    numberPair: entry.numberPair,
    subject: null,
    teacherId: null,
    teacherName: null,
    removedSubject: entry.subject,
    removedTeacherId: entry.teacherId,
    removedTeacherName: entry.teacherName,
    removedNumberPair: null,
    note: note.trim() || null,
  }
}
```

- [ ] **Step 4: Build-проверка**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: сборка успешна.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: frontend типы, getCorrectionDay и buildRemoveEntry"
```

### Task 4: Frontend (веб) — компонент `RemovePairFlow` и замена таблицы

**Files:**
- Create: `CollegeLMS.Next/components/RemovePairFlow.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/dispatcher/correction/page.tsx` (рендер вкладки «Вручную»)
- Delete: `CollegeLMS.Next/components/ManualCorrectionForm.tsx`

**Interfaces:**
- Consumes: `getCorrectionDay`, `buildRemoveEntry`, `confirmCorrection`, `CorrectionPreviewDialog`, `api.get("/api/groups")`.
- Produces: default-компонент `RemovePairFlow` (без пропсов).

- [ ] **Step 1: Создать `RemovePairFlow.tsx`**

```tsx
"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Ban, CalendarX2, Loader2 } from "lucide-react"
import { toast } from "sonner"
import api, { unwrap } from "@/lib/api"
import { getCorrectionDay } from "@/api/correction"
import type { GroupResponse, Result } from "@/types"
import type {
  CorrectionDayResponse,
  CorrectionPreviewEntry,
  ConfirmResult,
} from "@/types/correction"
import type { ScheduleResponse } from "@/types/schedule"
import { buildRemoveEntry } from "@/lib/correction"
import { extractErrorMessage } from "@/lib/utils"
import { dayLabelFromInt, formatTime } from "@/lib/max-lesson"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  NativeSelect,
  NativeSelectItem,
} from "@/components/ui/native-select"
import { CorrectionPreviewDialog } from "@/components/CorrectionPreviewDialog"

type ReasonKey = "remove" | "self" | "custom"

const REASON_OPTIONS: { key: ReasonKey; label: string }[] = [
  { key: "remove", label: "Удалить из расписания" },
  { key: "self", label: "Самостоятельная работа (сам.р.)" },
  { key: "custom", label: "Своё примечание" },
]

function isWeekend(value: string): boolean {
  const day = new Date(`${value}T00:00:00`).getDay()
  return day === 0 || day === 6
}

export default function RemovePairFlow() {
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [groupId, setGroupId] = useState("")
  const [date, setDate] = useState("")
  const [day, setDay] = useState<CorrectionDayResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [selected, setSelected] = useState<ScheduleResponse | null>(null)
  const [reason, setReason] = useState<ReasonKey>("remove")
  const [customNote, setCustomNote] = useState("")
  const [pending, setPending] = useState<CorrectionPreviewEntry | null>(null)

  useEffect(() => {
    api
      .get<Result<GroupResponse[]>>("/api/groups")
      .then(unwrap)
      .then(setGroups)
      .catch(() => toast.error("Не удалось загрузить список групп"))
  }, [])

  const fetchDay = useCallback(async () => {
    if (!groupId || !date || isWeekend(date)) {
      setDay(null)
      return
    }
    setLoading(true)
    setLoadError(null)
    try {
      setDay(await getCorrectionDay(groupId, date))
    } catch (err) {
      setDay(null)
      setLoadError(
        extractErrorMessage(err) ?? "Не удалось загрузить расписание дня",
      )
    } finally {
      setLoading(false)
    }
  }, [groupId, date])

  useEffect(() => {
    void fetchDay()
  }, [fetchDay])

  const isLocked = (entry: ScheduleResponse, week: number): boolean =>
    entry.changeTags.some(
      (t) =>
        t.week === week &&
        (t.changeType === "Remove" || t.changeType === "Replace"),
    )

  const note = useMemo(() => {
    if (reason === "self") return "сам.р."
    if (reason === "custom") return customNote
    return ""
  }, [reason, customNote])

  const openConfirm = () => {
    if (!selected || !day) return
    setPending(buildRemoveEntry(day, selected, note))
  }

  const handleConfirmed = async (result: ConfirmResult) => {
    toast.success(`Применено изменений: ${result.applied}`)
    setPending(null)
    setSelected(null)
    setReason("remove")
    setCustomNote("")
    await fetchDay()
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <CalendarX2 className="size-4" /> Снять пару
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <label className="grid gap-1 text-sm font-medium">
          Группа
          <NativeSelect
            value={groupId}
            onValueChange={(value) => setGroupId(value)}
            placeholder="Выберите группу"
          >
            {groups.map((group) => (
              <NativeSelectItem key={group.id} value={group.id}>
                {group.name}
              </NativeSelectItem>
            ))}
          </NativeSelect>
        </label>

        <label className="grid gap-1 text-sm font-medium">
          Дата (пн–пт)
          <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </label>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-8 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" /> Загружаем расписание…
          </div>
        ) : loadError ? (
          <p className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {loadError}
          </p>
        ) : !day ? null : day.entries.length === 0 ? (
          <div className="flex items-center justify-center gap-2 rounded-md border border-dashed py-8 text-sm text-muted-foreground">
            <Ban className="size-4" />
            На {dayLabelFromInt(day.dayOfWeek)} {day.week}-й неделе занятий нет
          </div>
        ) : (
          <div className="grid gap-2">
            <p className="text-sm text-muted-foreground">
              {dayLabelFromInt(day.dayOfWeek)} · {day.week}-я учебная неделя
            </p>
            {day.entries
              .slice()
              .sort((a, b) => a.numberPair - b.numberPair)
              .map((entry) => {
                const locked = isLocked(entry, day.week)
                const active = selected?.id === entry.id
                return (
                  <button
                    key={entry.id}
                    type="button"
                    disabled={locked}
                    onClick={() => setSelected(entry)}
                    className={`flex items-center gap-3 rounded-md border p-3 text-left text-sm transition-colors ${
                      active
                        ? "border-primary bg-primary/5"
                        : locked
                          ? "cursor-not-allowed border-dashed opacity-50"
                          : "hover:bg-muted/50"
                    }`}
                  >
                    <span className="font-semibold">{entry.numberPair}</span>
                    <span className="text-muted-foreground">
                      {formatTime(entry.startTime)}–{formatTime(entry.endTime)}
                    </span>
                    <span className="flex-1">
                      <span className="font-medium">{entry.subject}</span>
                      <span className="ml-2 text-muted-foreground">
                        {entry.teacherName ? `${entry.teacherName} · ` : ""}
                        {entry.room}
                      </span>
                    </span>
                    {locked && (
                      <span className="text-xs text-muted-foreground">
                        Уже снято
                      </span>
                    )}
                  </button>
                )
              })}
          </div>
        )}

        {selected && day ? (
          <div className="grid gap-3 rounded-md border p-4">
            <p className="text-sm font-medium">Причина снятия</p>
            {REASON_OPTIONS.map((option) => (
              <label key={option.key} className="flex items-center gap-2 text-sm">
                <input
                  type="radio"
                  name="reason"
                  checked={reason === option.key}
                  onChange={() => setReason(option.key)}
                />
                {option.label}
              </label>
            ))}
            {reason === "custom" && (
              <Input
                value={customNote}
                placeholder="Своё примечание"
                onChange={(e) => setCustomNote(e.target.value)}
              />
            )}
            <Button
              onClick={openConfirm}
              disabled={reason === "custom" && !customNote.trim()}
            >
              Снять пару
            </Button>
          </div>
        ) : null}

        <CorrectionPreviewDialog
          open={pending !== null}
          entries={pending ? [pending] : []}
          errors={[]}
          onConfirm={(result) => void handleConfirmed(result)}
          onCancel={() => setPending(null)}
        />
      </CardContent>
    </Card>
  )
}
```

- [ ] **Step 2: Подключить в странице**

В `CollegeLMS.Next/app/(authenticated)/dispatcher/correction/page.tsx`:

1) Заменить импорт (строка 37):
```tsx
import RemovePairFlow from "@/components/RemovePairFlow"
```

2) Заменить рендер вкладки «Вручную» (строка ~270):
```tsx
      {tab === "manual" ? (
        <RemovePairFlow />
      ) : tab === "import" ? (
```

- [ ] **Step 3: Удалить `ManualCorrectionForm.tsx`**

Run: `Remove-Item CollegeLMS.Next/components/ManualCorrectionForm.tsx`
> `exportManualCorrection` и `ManualCorrectionRow` в `api/correction.ts` остаются (backend `ExportManualAsync` используется импортом/шаблонами).

Проверить, что никто больше не импортирует удалённый компонент:
Run: `rg "ManualCorrectionForm" CollegeLMS.Next`
Expected: нет совпадений.

- [ ] **Step 4: Build-проверка**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: сборка успешна.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: веб-флоу «Снять пару» (RemovePairFlow) вместо табличной формы"
```

### Task 5: Frontend (max mini-app) — `DispatcherManual` направляющий флоу

**Files:**
- Modify: `CollegeLMS.Next/components/max/DispatcherManual.tsx` (полная замена содержимого)

**Interfaces:**
- Consumes: `getCorrectionDay`, `buildRemoveEntry`, `ConfirmOpsSheet`, `searchSchedule` (GroupPicker).
- Produces: default-компонент `DispatcherManual({ onApplied })`.

- [ ] **Step 1: Переписать `DispatcherManual.tsx`**

Полностью заменить содержимое файла `CollegeLMS.Next/components/max/DispatcherManual.tsx`:

```tsx
"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Search, Users } from "lucide-react"
import { Button, Input, Typography } from "@maxhub/max-ui"
import { getCorrectionDay } from "@/api/correction"
import { searchSchedule } from "@/api/schedule"
import type {
  CorrectionDayResponse,
  ConfirmResult,
} from "@/types/correction"
import type { ScheduleResponse } from "@/types/schedule"
import { buildRemoveEntry } from "@/lib/correction"
import { extractErrorMessage } from "@/lib/utils"
import { dayLabelFromInt, formatTime } from "@/lib/max-lesson"
import ConfirmOpsSheet from "@/components/max/ConfirmOpsSheet"

type ReasonKey = "remove" | "self" | "custom"

const REASON_OPTIONS: { key: ReasonKey; label: string }[] = [
  { key: "remove", label: "Удалить из расписания" },
  { key: "self", label: "Самостоятельная работа (сам.р.)" },
  { key: "custom", label: "Своё примечание" },
]

function isWeekend(value: string): boolean {
  const day = new Date(`${value}T00:00:00`).getDay()
  return day === 0 || day === 6
}

export default function DispatcherManual({
  onApplied,
}: {
  onApplied: (result: ConfirmResult) => void
}) {
  const [groupId, setGroupId] = useState("")
  const [groupName, setGroupName] = useState("")
  const [date, setDate] = useState("")
  const [day, setDay] = useState<CorrectionDayResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [selected, setSelected] = useState<ScheduleResponse | null>(null)
  const [reason, setReason] = useState<ReasonKey>("remove")
  const [customNote, setCustomNote] = useState("")
  const [confirmOpen, setConfirmOpen] = useState(false)

  const fetchDay = useCallback(async () => {
    if (!groupId || !date || isWeekend(date)) {
      setDay(null)
      return
    }
    setLoading(true)
    setLoadError(null)
    try {
      setDay(await getCorrectionDay(groupId, date))
    } catch (err) {
      setDay(null)
      setLoadError(
        extractErrorMessage(err) ?? "Не удалось загрузить расписание дня",
      )
    } finally {
      setLoading(false)
    }
  }, [groupId, date])

  useEffect(() => {
    void fetchDay()
  }, [fetchDay])

  const isLocked = (entry: ScheduleResponse, week: number): boolean =>
    entry.changeTags.some(
      (t) =>
        t.week === week &&
        (t.changeType === "Remove" || t.changeType === "Replace"),
    )

  const note = useMemo(() => {
    if (reason === "self") return "сам.р."
    if (reason === "custom") return customNote
    return ""
  }, [reason, customNote])

  const entry = useMemo(() => {
    if (!selected || !day) return null
    return buildRemoveEntry(day, selected, note)
  }, [selected, day, note])

  return (
    <div className="max-app__dispatcher-manual">
      <GroupPicker
        value={{ id: groupId, name: groupName }}
        onChange={(id, name) => {
          setGroupId(id)
          setGroupName(name)
        }}
      />

      <label className="max-app__field">
        <span className="max-app__form-label">Дата (пн–пт)</span>
        <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </label>

      {loading ? (
        <Typography.Body className="max-app__note">
          Загружаем расписание…{" "}
        </Typography.Body>
      ) : loadError ? (
        <Typography.Body className="max-app__error">{loadError}</Typography.Body>
      ) : !day ? null : day.entries.length === 0 ? (
        <Typography.Body className="max-app__note">
          На {dayLabelFromInt(day.dayOfWeek)} {day.week}-й неделе занятий нет
        </Typography.Body>
      ) : (
        <>
          <Typography.Body className="max-app__note">
            {dayLabelFromInt(day.dayOfWeek)} · {day.week}-я учебная неделя
          </Typography.Body>
          <div className="max-app__schedule-options">
            {day.entries
              .slice()
              .sort((a, b) => a.numberPair - b.numberPair)
              .map((entry) => {
                const locked = isLocked(entry, day.week)
                return (
                  <label
                    key={entry.id}
                    className={`max-app__option${
                      selected?.id === entry.id ? " max-app__option--on" : ""
                    }`}
                  >
                    <input
                      type="radio"
                      name="remove"
                      value={entry.numberPair}
                      checked={selected?.id === entry.id}
                      disabled={locked}
                      onChange={() => setSelected(entry)}
                    />
                    <span>
                      <strong>{entry.numberPair} пара</strong> ·{" "}
                      {formatTime(entry.startTime)}–{formatTime(entry.endTime)}{" "}
                      · {entry.subject}
                      {entry.teacherName ? ` · ${entry.teacherName}` : ""}
                      {entry.room ? ` · ${entry.room}` : ""}
                      {locked ? " · (уже снято)" : ""}
                    </span>
                  </label>
                )
              })}
          </div>
        </>
      )}

      {selected && day ? (
        <>
          <span className="max-app__form-label">Причина снятия</span>
          <div className="max-app__schedule-options">
            {REASON_OPTIONS.map((option) => (
              <label
                key={option.key}
                className={`max-app__option${
                  reason === option.key ? " max-app__option--on" : ""
                }`}
              >
                <input
                  type="radio"
                  name="reason"
                  checked={reason === option.key}
                  onChange={() => setReason(option.key)}
                />
                <span>{option.label}</span>
              </label>
            ))}
          </div>

          {reason === "custom" ? (
            <label className="max-app__field">
              <span className="max-app__form-label">Своё примечание</span>
              <Input
                value={customNote}
                placeholder="Введите причину"
                onChange={(e) => setCustomNote(e.target.value)}
              />
            </label>
          ) : null}

          <Button
            stretched
            onClick={() => setConfirmOpen(true)}
            disabled={reason === "custom" && !customNote.trim()}
          >
            Снять пару
          </Button>
        </>
      ) : null}

      <ConfirmOpsSheet
        open={confirmOpen}
        ops={entry ? [entry] : []}
        onCancel={() => setConfirmOpen(false)}
        onApplied={(result) => {
          onApplied(result)
          setConfirmOpen(false)
          setSelected(null)
          setReason("remove")
          setCustomNote("")
          void fetchDay()
        }}
      />
    </div>
  )
}

function GroupPicker({
  value,
  onChange,
}: {
  value: { id: string; name: string }
  onChange: (id: string, name: string) => void
}) {
  const [query, setQuery] = useState("")
  const [options, setOptions] = useState<{ id: string; name: string }[]>([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    const q = query.trim()
    if (q.length === 0) {
      setOptions([])
      setOpen(false)
      return
    }
    const timer = window.setTimeout(async () => {
      setLoading(true)
      try {
        const res = await searchSchedule(q)
        const groups = res.isSuccess
          ? (res.data?.groups ?? [])
              .slice(0, 6)
              .map((g) => ({ id: g.id, name: g.name }))
          : []
        setOptions(groups)
        setOpen(groups.length > 0)
      } catch {
        setOptions([])
        setOpen(false)
      } finally {
        setLoading(false)
      }
    }, 250)
    return () => window.clearTimeout(timer)
  }, [query])

  return (
    <div className="max-app__field">
      <span className="max-app__form-label">Группа</span>
      {value.id ? (
        <div className="max-app__picked">
          <span>
            <Users size={14} aria-hidden /> {value.name}
          </span>
          <Button
            size="xsmall"
            variant="ghost"
            aria-label="Выбрать другую группу"
            onClick={() => {
              onChange("", "")
              setQuery("")
            }}
          >
            Изменить
          </Button>
        </div>
      ) : (
        <>
          <Input
            value={query}
            placeholder="Начните вводить название группы"
            onChange={(e) => setQuery(e.target.value)}
            iconBefore={<Search size={18} aria-hidden />}
          />
          {loading ? (
            <Typography.Body className="max-app__note">Поиск…</Typography.Body>
          ) : open && options.length > 0 ? (
            <div className="max-app__search-item">
              {options.map((g) => (
                <Button
                  key={g.id}
                  size="small"
                  variant="secondary"
                  onClick={() => {
                    onChange(g.id, g.name)
                    setOpen(false)
                  }}
                >
                  {g.name}
                </Button>
              ))}
            </div>
          ) : open ? (
            <Typography.Body className="max-app__note">
              Группа не найдена
            </Typography.Body>
          ) : null}
        </>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Проверить отсутствие мёртвых импортов**

Run: `rg "DispatcherManual" CollegeLMS.Next` — используется только в `DispatcherView.tsx` (без изменений, проп `onApplied` сохранён).
Run: `rg "NoteChips" CollegeLMS.Next` — если больше нигде не используется, файл можно оставить (используется веб-диалогами/импортами); изменения не требуются.

- [ ] **Step 3: Build-проверка**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: сборка успешна.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: max mini-app — направляющий флоу «Снять пару» вместо табличной формы"
```

### Task 6: Финальная верификация и push

**Files:** нет (только команды).

- [ ] **Step 1: Полный backend**

Run: `dotnet build CollegeLMS.API && dotnet test CollegeLMS.Tests -v minimum`
Expected: сборка и все тесты PASS.

- [ ] **Step 2: Полный frontend**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: сборка успешна.

- [ ] **Step 3: CSharpier check**

Run: `dotnet csharpier format . --check`
Expected: без ошибок. Если есть — `dotnet csharpier format .`.

- [ ] **Step 4: Проверка DoD по спеке**

- [ ] `GET /api/schedule/correction/day` работает и валидирует входы (Swagger: http://localhost/swagger)
- [ ] Веб-вкладка «Вручную» = направляющий флоу (без таблицы и XLSX-кнопки)
- [ ] Mini-app `DispatcherManual` = направляющий флоу
- [ ] «сам.р.» → пара остаётся; без причины → неделя снимается (покрыто unit `ConfirmAsync_*`)
- [ ] Повторное снятие снятой пары заблокировано на UI (`isLocked` по `changeTags`)

- [ ] **Step 5: Commit (если есть незакоммиченное) и push**

```bash
git add -A
git commit -m "chore: финальная проверка флоу «Снять пару»" --allow-empty
git push origin master
```

Run: `gh run watch` (если `GH_TOKEN`/`GITHUB_TOKEN` установлен) — CI зелёный.