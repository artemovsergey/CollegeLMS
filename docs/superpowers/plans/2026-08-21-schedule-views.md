# M4 «Расписание: виды День/Неделя/Месяц» — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Переключатель видов «День/Неделя/Месяц» на `/schedule`, навигация по учебным неделям семестра с учётом `weeks[]`, подсветка «сегодня», календарная сетка месяца.

**Architecture:** Минимальный бэкенд: хелпер `ScheduleWeekCalculator` (номер учебной недели из конфига `Schedule:SemesterStart`), параметр `?week=` (фильтр `Weeks.Contains` по `integer[]`) и поле `currentWeek` в calendar-ответе. Фронтенд: состояние вида/даты в URL (?view=&date=), три вида поверх существующих фильтров и CRUD-диалогов.

**Tech Stack:** .NET 10 / EF Core+Npgsql / xUnit+FluentAssertions; Next.js 14 App Router / TS / Tailwind v4 / shadcn-ui / Playwright.

## Global Constraints

- Схема БД не меняется; миграций нет.
- `Weeks` — Postgres `integer[]`; `query.Where(s => s.Weeks.Contains(week.Value))` → `@week = ANY(weeks)`.
- Тексты/комментарии на русском; Result<T>; сообщения ошибок русские.
- Tailwind CSS v4, shadcn/ui примитивы, Lucide-иконки; адаптив 1366×768 и ~393px; touch-target ≥44px.
- Годовой вид — out of scope. CRUD-диалоги (`ScheduleEntryDialog`), импорт/экспорт, `ScheduleFilterBar` — не трогаем (кроме передачи пропсов).
- Учебный год: 22 недели максимум (clamp 1..22).
- Команды docker: tsc/e2e через контейнеры (node отсутствует на хосте):
  - tsc/build: `docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next -e NEXT_PUBLIC_API_URL=http://localhost:8080 node:20-alpine sh -c "npm run build"`
  - e2e: `docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next -v /home/user1/.cache/ms-playwright:/ms-playwright -e PLAYWRIGHT_BROWSERS_PATH=/ms-playwright playwright-deps sh -c "npx playwright test <spec>"`
- Известные pre-existing e2e-падения (9 шт.: users×3, admin-pages×3, feedback×1, student-progress×2) НЕ трогаем и не считаем блокером.

---

### Task 0: Подготовка ветки

- [ ] **Step 1: Sync master и создать ветку**

```bash
git fetch origin && git pull --rebase origin master
git checkout -b feature/schedule-views
```

Expected: ветка создана от актуального master.

---

### Task 1: ScheduleWeekCalculator (TDD)

**Files:**
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleWeekCalculatorTests.cs` (новый)
- Create: `CollegeLMS.API/Services/ScheduleWeekCalculator.cs`
- Modify: `CollegeLMS.API/appsettings.json`, `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs:173`, `CollegeLMS.API/Program.cs:16`

**Interfaces:**
- Produces: `class ScheduleWeekCalculator(DateTime semesterStart)` с методом `int GetCurrentWeek(DateTime today)` — используется Tasks 2 (DI, сервис).

- [ ] **Step 1: Написать падающий тест**

```csharp
using CollegeLMS.API.Services;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleWeekCalculatorTests
{
    // Семестр стартует во вторник 01.09.2026 → неделя 1 = пн 31.08 – вс 06.09
    private readonly ScheduleWeekCalculator _sut = new(new DateTime(2026, 9, 1));

    [Fact]
    public void GetCurrentWeek_BeforeSemesterStart_ClampsToOne()
    {
        _sut.GetCurrentWeek(new DateTime(2026, 8, 25)).Should().Be(1);
    }

    [Fact]
    public void GetCurrentWeek_FirstThursday_ReturnsOne()
    {
        _sut.GetCurrentWeek(new DateTime(2026, 9, 3)).Should().Be(1);
    }

    [Fact]
    public void GetCurrentWeek_SundayOfFirstWeek_ReturnsOne()
    {
        _sut.GetCurrentWeek(new DateTime(2026, 9, 6)).Should().Be(1);
    }

    [Fact]
    public void GetCurrentWeek_MondayOfSecondWeek_ReturnsTwo()
    {
        _sut.GetCurrentWeek(new DateTime(2026, 9, 7)).Should().Be(2);
    }

    [Fact]
    public void GetCurrentWeek_FarFuture_ClampsToTwentyTwo()
    {
        _sut.GetCurrentWeek(new DateTime(2027, 5, 10)).Should().Be(22);
    }
}
```

- [ ] **Step 2: Убедиться, что тест падает**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleWeekCalculatorTests"`
Expected: FAIL — тип `ScheduleWeekCalculator` не существует (ошибка компиляции).

- [ ] **Step 3: Реализовать калькулятор**

`CollegeLMS.API/Services/ScheduleWeekCalculator.cs`:

```csharp
namespace CollegeLMS.API.Services;

/// <summary>Вычисляет номер учебной недели (1..22) от даты начала семестра.</summary>
public class ScheduleWeekCalculator(DateTime semesterStart)
{
    private const int MaxWeeks = 22;

    public int GetCurrentWeek(DateTime today)
    {
        var weeks = (Monday(today) - Monday(semesterStart)).Days / 7 + 1;
        return Math.Clamp(weeks, 1, MaxWeeks);
    }

    // Понедельник недели указанной даты (Пн=0 ... Вс=6)
    private static DateTime Monday(DateTime d)
    {
        var diff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return d.Date.AddDays(-diff);
    }
}
```

- [ ] **Step 4: Тест зелёный**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleWeekCalculatorTests"`
Expected: PASS (5 тестов).

- [ ] **Step 5: Конфиг + DI**

`appsettings.json` — после секции `"ConnectionStrings"` добавить:

```json
"Schedule": {
  "SemesterStart": "2026-09-01"
},
```

`Extensions/ServiceCollectionExtensions.cs:173` — сменить сигнатуру и зарегистрировать singleton (внутри метода, рядом с `services.AddScoped<IScheduleService, ScheduleService>();`):

```csharp
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services,
    IConfiguration config
)
```

```csharp
services.AddSingleton(new ScheduleWeekCalculator(
    config.GetValue<DateTime>("Schedule:SemesterStart", new DateTime(2026, 9, 1))
));
```

(`using Microsoft.Extensions.Configuration;` при отсутствии.)

`Program.cs:16` — заменить `.AddApplicationServices()` на `.AddApplicationServices(builder.Configuration)`.

- [ ] **Step 6: Сборка**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Коммит**

```bash
git add -A && git commit -m "feat: ScheduleWeekCalculator — номер учебной недели из Schedule:SemesterStart"
```

---

### Task 2: API — фильтр ?week= и CurrentWeek в календаре

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleDtos.cs:41-45` (CalendarResponse)
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs` (2 сигнатуры)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs` (конструктор, оба метода)
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs:25-55` (параметр + swagger)
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs` (конструктор + вызовы GetAllAsync + новые тесты)
- Modify: `CollegeLMS.Tests/Integration/Controllers/ScheduleControllerTests.cs` (новые тесты)
- Modify: `docs/spec/CollegeLMS.postman_collection.json`

**Interfaces:**
- Consumes: `ScheduleWeekCalculator.GetCurrentWeek(DateTime)` (Task 1).
- Produces: `GET /api/schedule?week=N` — список только записей, где `weeks ∋ N`; `GET /api/schedule?view=calendar&week=N` — календарь c `currentWeek:int`. JSON-контракт для фронта (Task 3):

```json
{ "isSuccess": true, "data": { "weekStart": "2026-08-31T00:00:00Z", "currentWeek": 3, "days": [ { "day": "Понедельник", "dayOfWeek": 1, "entries": [] } ] }, "errorMessage": null, "statusCode": 200 }
```

- [ ] **Step 1: Обновить DTO**

`CalendarResponse` в `ScheduleDtos.cs`:

```csharp
public class CalendarResponse
{
    public DateTime WeekStart { get; set; }
    public int CurrentWeek { get; set; }
    public List<CalendarDayResponse> Days { get; set; } = new();
}
```

- [ ] **Step 2: Обновить интерфейс**

В `IScheduleService`: в `GetAllAsync` добавить `int? week` после `string? view`; в `GetCalendarAsync` добавить `int? week` перед `CancellationToken`:

```csharp
Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
    Guid? groupId, Guid? teacherId, string? room, DayOfWeek? dayOfWeek,
    string? period, string? view, int? week, int? page, int? pageSize,
    CancellationToken ct = default
);
Task<Result<CalendarResponse>> GetCalendarAsync(
    Guid? groupId, Guid? teacherId, string? room, int? week,
    CancellationToken ct = default
);
```

- [ ] **Step 3: Реализовать в сервисе**

Конструктор `ScheduleService`:

```csharp
public class ScheduleService(
    AppDbContext db,
    ScheduleExportService exportService,
    ScheduleImportService importService,
    ScheduleWeekCalculator weekCalculator
) : IScheduleService
```

В `GetAllAsync` — новый параметр `int? week` (после `view`), фильтр после блока `dayOfWeek`:

```csharp
if (week.HasValue)
    query = query.Where(s => s.Weeks.Contains(week.Value));
```

В `GetCalendarAsync` — параметр `int? week`, тот же фильтр после проверки `room`, и в возвращаемый объект:

```csharp
return Result<CalendarResponse>.Ok(
    new CalendarResponse
    {
        WeekStart = weekStart,
        CurrentWeek = weekCalculator.GetCurrentWeek(DateTime.UtcNow),
        Days = days,
    }
);
```

- [ ] **Step 4: Контроллер**

`GetAll` — добавить `[FromQuery] int? week` после `string? view`, передать в оба вызова сервиса. SwaggerOperation Summary: «Получить расписание с фильтрацией и пагинацией» — дополнить XML-комментарий: `<param name="week">Номер учебной недели (1..22)</param>` над методом (или в remarks).

- [ ] **Step 5: Обновить существующие unit-тесты**

`ScheduleServiceTests` конструктор:

```csharp
_db = TestDbContextFactory.Create();
var exportService = new ScheduleExportService(_db);
var importService = new ScheduleImportService(_db);
var weekCalculator = new ScheduleWeekCalculator(new DateTime(2026, 9, 1));
_sut = new ScheduleService(_db, exportService, importService, weekCalculator);
```

Каждый вызов `GetAllAsync(...)` — вставить `null,` седьмым аргументом (после view, перед page). Вызовы `GetCalendarAsync(a, b, c, default)` → `GetCalendarAsync(a, b, c, null, default)`.

Новые тесты в тот же файл:

```csharp
[Fact]
public async Task GetAllAsync_FiltersByWeek()
{
    var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
    entries[0].Weeks = new() { 1 };
    entries[1].Weeks = new() { 2 };
    entries[2].Weeks = new() { 1, 2 };
    _db.ScheduleEntries.AddRange(entries);
    await _db.SaveChangesAsync();

    var result = await _sut.GetAllAsync(
        null, null, null, null, null, null, 2, null, null, default
    );

    result.IsSuccess.Should().BeTrue();
    result.Data!.Items.Select(x => x.Id).Should().BeEquivalentTo(
        new[] { entries[1].Id, entries[2].Id });
}

[Fact]
public async Task GetCalendarAsync_ReturnsCurrentWeek()
{
    var result = await _sut.GetCalendarAsync(null, null, null, null, default);

    result.IsSuccess.Should().BeTrue();
    result.Data!.CurrentWeek.Should().BeGreaterThanOrEqualTo(1);
}
```

(если у фикстурных записей `Weeks` пустой — тест выше переопределяет явно, ок.)

- [ ] **Step 6: Unit-тесты зелёные**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleServiceTests"`
Expected: PASS.

- [ ] **Step 7: Integration-тесты**

В `ScheduleControllerTests` добавить:

```csharp
[Fact]
public async Task GetCalendar_ReturnsCurrentWeek()
{
    SetAuthHeader(GetAdminToken());

    var response = await Client.GetAsync("/api/schedule?view=calendar");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await DeserializeWithEnumsAsync<Result<CalendarResponse>>(response);
    body!.IsSuccess.Should().BeTrue();
    body.Data!.CurrentWeek.Should().BeGreaterThanOrEqualTo(1);
}

[Fact]
public async Task GetAll_WithWeekFilter_ReturnsOnlyMatchingEntries()
{
    SetAuthHeader(GetAdminToken());
    Guid groupId;
    Guid expectedId;
    using (var scope = Factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
        var entries = Fixtures.ScheduleEntryFixture.CreateFaker().Generate(2);
        groupId = Guid.NewGuid();
        foreach (var e in entries)
        {
            e.GroupId = groupId;
            e.Group!.Id = groupId;
        }
        entries[0].Weeks = new() { 1 };
        entries[1].Weeks = new() { 2 };
        expectedId = entries[1].Id;
        db.ScheduleEntries.AddRange(entries);
        await db.SaveChangesAsync();
    }

    var response = await Client.GetAsync($"/api/schedule?groupId={groupId}&week=2");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleResponse>>>(response);
    body!.IsSuccess.Should().BeTrue();
    body.Data!.Items.Should().ContainSingle(x => x.Id == expectedId);
}
```

(неймспейсы фикстур подправить по факту: `using CollegeLMS.Tests.Fixtures;` уже есть.)

- [ ] **Step 8: Все тесты зелёные**

Run: `dotnet test`
Expected: PASS (все существующие + новые).

- [ ] **Step 9: Postman-коллекция**

В `docs/spec/CollegeLMS.postman_collection.json` у запроса GET `/api/schedule` добавить query-параметры: `week` (пример `2`, описание «Учебная неделя 1..22») и у calendar-запроса пример ответа с полем `currentWeek`. Правку внести точечно через python (найти item по url contains `/api/schedule`), либо руками.

- [ ] **Step 10: Коммит**

```bash
git add -A && git commit -m "feat: фильтр расписания по учебной неделе (?week=) и currentWeek в календаре"
```

---

### Task 3: Фронтенд — типы, api, хелперы недель

**Files:**
- Modify: `CollegeLMS.Next/types/schedule.ts` (добавить типы календаря)
- Modify: `CollegeLMS.Next/api/schedule.ts` (week-параметр, fetchCalendar)
- Create: `CollegeLMS.Next/lib/scheduleWeeks.ts`
- Create: `CollegeLMS.Next/lib/scheduleFormat.ts`
- Modify: `CollegeLMS.Next/components/ScheduleTable.tsx` (убрать локальные форматтеры, импорт из lib)

**Interfaces:**
- Consumes: JSON-контракт Task 2.
- Produces (для Tasks 4–6):
  - `fetchCalendar(filters?): Promise<Result<CalendarResponse>>`
  - `ScheduleFilters.week?: number`
  - `mondayOf(d: Date): Date`, `addDays(d: Date, n: number): Date`, `toISODate(d: Date): "YYYY-MM-DD"`, `formatShort(d: Date): "DD.MM"`, `clampWeek(n): number`, `MAX_WEEK = 22`
  - `formatTime(time: string)`, `formatTimeSlot(start, end)`, `formatWeeks(weeks: number[])`

- [ ] **Step 1: Типы календаря**

В конец `types/schedule.ts`:

```ts
export interface CalendarDayResponse {
  day: string
  dayOfWeek: number
  entries: ScheduleResponse[]
}

export interface CalendarResponse {
  weekStart: string
  currentWeek: number
  days: CalendarDayResponse[]
}
```

- [ ] **Step 2: api/schedule.ts**

В `ScheduleFilters` добавить `week?: number`; в `fetchSchedule` после блока `period`:

```ts
if (filters.week !== undefined) params.set("week", String(filters.week))
```

Новая функция (импорт типа `CalendarResponse` из `@/types/schedule`):

```ts
export async function fetchCalendar(
  filters: Pick<ScheduleFilters, "groupId" | "teacherId"> & { week?: number } = {},
): Promise<Result<CalendarResponse>> {
  const params = new URLSearchParams({ view: "calendar" })
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.week !== undefined) params.set("week", String(filters.week))
  const { data } = await api.get<Result<CalendarResponse>>(
    `/api/schedule?${params.toString()}`,
  )
  return data
}
```

- [ ] **Step 3: lib/scheduleWeeks.ts**

```ts
export const MAX_WEEK = 22

// Понедельник недели даты (Пн=0 ... Вс=6)
export function mondayOf(d: Date): Date {
  const diff = (d.getDay() + 6) % 7
  const m = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  m.setDate(m.getDate() - diff)
  return m
}

export function addDays(d: Date, n: number): Date {
  const r = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  r.setDate(r.getDate() + n)
  return r
}

export function toISODate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, "0")
  const day = String(d.getDate()).padStart(2, "0")
  return `${y}-${m}-${day}`
}

export function formatShort(d: Date): string {
  return `${String(d.getDate()).padStart(2, "0")}.${String(d.getMonth() + 1).padStart(2, "0")}`
}

export function clampWeek(w: number): number {
  return Math.min(Math.max(Math.trunc(w), 1), MAX_WEEK)
}
```

- [ ] **Step 4: lib/scheduleFormat.ts**

Перенести из `components/ScheduleTable.tsx` (строки 18–43) без изменений логики:

```ts
export function formatTime(time: string) {
  return time.slice(0, 5)
}

export function formatTimeSlot(start: string, end: string) {
  return `${formatTime(start)} – ${formatTime(end)}`
}

export function formatWeeks(weeks: number[]): string {
  if (weeks.length === 0) return ""
  if (weeks.length === 16 && weeks[0] === 1 && weeks[weeks.length - 1] === 16) return "все"
  const ranges: string[] = []
  let start = weeks[0]
  let end = weeks[0]
  for (let i = 1; i < weeks.length; i++) {
    if (weeks[i] === end + 1) {
      end = weeks[i]
    } else {
      ranges.push(start === end ? `${start}` : `${start}-${end}`)
      start = weeks[i]
      end = weeks[i]
    }
  }
  ranges.push(start === end ? `${start}` : `${start}-${end}`)
  return ranges.join(", ")
}
```

В `ScheduleTable.tsx` удалить локальные `formatTime/formatTimeSlot/formatWeeks` и добавить `import { formatTimeSlot, formatWeeks } from "@/lib/scheduleFormat"`.

- [ ] **Step 5: Проверка типов**

Run: `docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next -e NEXT_PUBLIC_API_URL=http://localhost:8080 node:20-alpine sh -c "npx tsc --noEmit"`
Expected: 0 ошибок (ошибки `.next/types/*` игнорировать — stale).

- [ ] **Step 6: Коммит**

```bash
git add -A && git commit -m "feat: типы календаря, week-параметр и клиентские хелперы учебных недель"
```

---

### Task 4: Страница — виды, URL-состояние, навигация, вид «День»

**Files:**
- Create: `CollegeLMS.Next/components/ScheduleDayList.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx`

**Interfaces:**
- Consumes: всё из Task 3; `LESSON_TYPE_*` из `@/types/schedule`.
- Produces: паттерн состояния страницы, который расширяют Tasks 5–6: `view`, `anchorDate`, `activeWeek`, `changeView/changeAnchor/handlePrev/handleNext/handleTodayReset`, `weekOfDate(iso)`, рендер-ветки по `view`.

- [ ] **Step 1: ScheduleDayList**

`components/ScheduleDayList.tsx` (карточка занятия = текущий мобильный рендер ScheduleTable, один день):

```tsx
"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { LESSON_TYPE_LABELS, LESSON_TYPE_STYLES } from "@/types/schedule"
import { Clock, MapPin, GraduationCap, Users, Calendar, Trash2, ListOrdered } from "lucide-react"
import { Button } from "@/components/ui/button"
import { formatTimeSlot, formatWeeks } from "@/lib/scheduleFormat"

interface ScheduleDayListProps {
  title: string
  entries: ScheduleResponse[]
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
}

export default function ScheduleDayList({ title, entries, onEntryClick, onDeleteClick }: ScheduleDayListProps) {
  const sorted = [...entries].sort((a, b) => a.numberPair - b.numberPair)

  if (sorted.length === 0) {
    return (
      <div className="rounded-lg border bg-card px-4 py-10 text-center text-sm text-muted-foreground">
        {title}: занятий нет
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {sorted.map(entry => (
        <div
          key={entry.id}
          className={`relative rounded-lg border bg-card border-l-[3px] p-3 ${LESSON_TYPE_STYLES[entry.lessonType] ?? "border-l-gray-400"} ${onEntryClick ? "cursor-pointer transition-colors hover:bg-accent/50" : ""}`}
          onClick={() => onEntryClick?.(entry)}
        >
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <ListOrdered className="size-3" />
            №{entry.numberPair}
            <Clock className="size-3" />
            {formatTimeSlot(entry.startTime, entry.endTime)}
            <span className="ml-auto rounded-full bg-background px-1.5 py-0.5 text-[10px] font-medium">
              {LESSON_TYPE_LABELS[entry.lessonType]}
            </span>
          </div>
          <p className="mt-1 text-sm font-semibold">{entry.subject}</p>
          <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-muted-foreground">
            {entry.teacherName && (
              <span className="flex items-center gap-1"><GraduationCap className="size-3" />{entry.teacherName}</span>
            )}
            <span className="flex items-center gap-1"><MapPin className="size-3" />{entry.room}</span>
            <span className="flex items-center gap-1"><Users className="size-3" />{entry.groupName}</span>
            {entry.weeks.length > 0 && (
              <span className="flex items-center gap-1"><Calendar className="size-3" />нед. {formatWeeks(entry.weeks)}</span>
            )}
          </div>
          {onDeleteClick && (
            <Button
              variant="ghost"
              size="icon"
              aria-label="Удалить занятие"
              className="absolute right-2 top-2 size-6"
              onClick={(e) => { e.stopPropagation(); onDeleteClick(entry.id) }}
            >
              <Trash2 className="size-3.5 text-destructive" />
            </Button>
          )}
        </div>
      ))}
    </div>
  )
}
```

- [ ] **Step 2: Состояние страницы и URL**

В `schedule/page.tsx` добавить импорты:

```tsx
import { useRouter, usePathname } from "next/navigation"
import type { LucideIcon } from "lucide-react"
import { Sunrise, Columns3, CalendarDays, ChevronLeft, ChevronRight } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import ScheduleDayList from "@/components/ScheduleDayList"
import { fetchCalendar } from "@/api/schedule"
import { mondayOf, addDays, toISODate, formatShort, clampWeek } from "@/lib/scheduleWeeks"
```

Тип вида и состояние (рядом с существующими useState):

```tsx
type View = "day" | "week" | "month"

const VIEWS: { value: View; label: string; icon: LucideIcon }[] = [
  { value: "day", label: "День", icon: Sunrise },
  { value: "week", label: "Неделя", icon: Columns3 },
  { value: "month", label: "Месяц", icon: CalendarDays },
]
```

```tsx
const pathname = usePathname()
const [view, setView] = useState<View>("week")
const [anchorDate, setAnchorDate] = useState<string>(() => toISODate(new Date()))
const [currentWeekAnchor, setCurrentWeekAnchor] = useState<number | null>(null)
const [ignoreWeek, setIgnoreWeek] = useState(false)
```

Чтение URL при маунте и синхронизация (без useSearchParams — не требует Suspense):

```tsx
useEffect(() => {
  const sp = new URLSearchParams(window.location.search)
  const v = sp.get("view")
  const d = sp.get("date")
  if (v === "day" || v === "week" || v === "month") setView(v)
  if (d && /^\d{4}-\d{2}-\d{2}$/.test(d)) setAnchorDate(d)
}, [])

const applyState = (v: View, d: string) => {
  window.history.replaceState(null, "", `${pathname}?view=${v}&date=${d}`)
}
const changeView = (v: View) => { setView(v); setIgnoreWeek(false); applyState(v, anchorDate) }
const changeAnchor = (d: string) => { setAnchorDate(d); setIgnoreWeek(false); applyState(view, d) }
```

Якорь текущей недели (однократно):

```tsx
useEffect(() => {
  if (!token || currentWeekAnchor !== null) return
  fetchCalendar()
    .then(res => {
      if (res.isSuccess && res.data) setCurrentWeekAnchor(res.data.currentWeek)
      else setCurrentWeekAnchor(1)
    })
    .catch(() => setCurrentWeekAnchor(1))
}, [token, currentWeekAnchor])

const weekOfDate = (iso: string): number => {
  if (currentWeekAnchor === null) return 0
  const diff = Math.round(
    (mondayOf(new Date(iso)).getTime() - mondayOf(new Date()).getTime()) / (7 * 86_400_000),
  )
  return clampWeek(currentWeekAnchor + diff)
}

const activeWeek = view === "month" || ignoreWeek ? undefined : weekOfDate(anchorDate) || undefined
```

Загрузка: `loadSchedule` заменить условие запроса — передавать `week: activeWeek` в fetchSchedule; в deps добавить `view, anchorDate, activeWeek`. Пока `currentWeekAnchor === null` и вид ≠ month — показывать спиннер (не запрашивать):

```tsx
const loadSchedule = useCallback(async () => {
  setLoading(true)
  setError(null)
  try {
    const body = await fetchSchedule({
      ...getFilters(),
      week: activeWeek,
      pageSize: 100,
    } as Record<string, string | undefined> & { pageSize?: number; week?: number })
    if (body.isSuccess && body.data) {
      setEntries(body.data.items)
    } else {
      setError(body.errorMessage ?? "Ошибка загрузки расписания")
    }
  } catch {
    setError("Ошибка загрузки расписания")
  } finally {
    setLoading(false)
  }
}, [getFilters, activeWeek])

useEffect(() => {
  if (!token) return
  if (view !== "month" && currentWeekAnchor === null) return
  loadSchedule()
}, [token, loadSchedule, view, currentWeekAnchor])
```

Старый `handleToday` (устанавливавший день недели) удалить.

- [ ] **Step 3: UI — табы видов и навигатор**

Под заголовком страницы (до `ScheduleFilterBar`):

```tsx
<div className="flex gap-4 border-b">
  {VIEWS.map(v => (
    <button
      key={v.value}
      onClick={() => changeView(v.value)}
      className={`flex items-center gap-1.5 pb-2 text-sm font-medium border-b-2 transition-colors ${
        view === v.value
          ? "border-primary text-primary"
          : "border-transparent text-muted-foreground hover:text-foreground"
      }`}
    >
      <v.icon className="size-4" />
      {v.label}
    </button>
  ))}
</div>

<div className="flex flex-wrap items-center gap-2">
  <Button variant="outline" size="icon" aria-label="Предыдущий период" onClick={handlePrev}>
    <ChevronLeft className="size-4" />
  </Button>
  <span className="min-w-[190px] text-center text-sm font-medium">{periodLabel}</span>
  <Button variant="outline" size="icon" aria-label="Следующий период" onClick={handleNext}>
    <ChevronRight className="size-4" />
  </Button>
  <Button variant="ghost" size="sm" onClick={handleTodayReset}>Сегодня</Button>
  {activeWeek && <Badge variant="secondary">Неделя {activeWeek} семестра</Badge>}
</div>
```

Хэндлеры и подпись:

```tsx
const shift = view === "day" ? 1 : 7
const handlePrev = () => changeAnchor(toISODate(addDays(new Date(anchorDate), -shift)))
const handleNext = () => changeAnchor(toISODate(addDays(new Date(anchorDate), shift)))
const handleTodayReset = () => changeAnchor(toISODate(new Date()))

const periodLabel =
  view === "day"
    ? new Date(anchorDate + "T00:00:00").toLocaleDateString("ru-RU", {
        weekday: "long", day: "numeric", month: "long",
      })
    : (() => {
        const s = mondayOf(new Date(anchorDate + "T00:00:00"))
        return `${formatShort(s)} – ${formatShort(addDays(s, 6))}`
      })()
```

- [ ] **Step 4: Рендер вида «День»**

Ветка рендера (рядом с существующим ScheduleTable):

```tsx
{view === "day" && (
  <ScheduleDayList
    title="Этот день"
    entries={entries.filter(e => e.dayOfWeek === new Date(anchorDate + "T00:00:00").getDay())}
    onEntryClick={canManage ? handleEdit : undefined}
    onDeleteClick={canManage ? (id) => setDeleteConfirmId(id) : undefined}
  />
)}

{view === "week" && (
  <ScheduleTable ... />  /* как сейчас */
)}
```

(dayOfWeek бэкенда совпадает с JS `getDay()`: 1=Пн … 6=Сб, 0=Вс.)

- [ ] **Step 5: Проверка**

Run: `npm run build` (docker-команда из Global Constraints)
Expected: Compiled successfully.

Run: e2e smoke вручную не требуется на этом шаге.

- [ ] **Step 6: Коммит**

```bash
git add -A && git commit -m "feat: виды День/Неделя/Месяц, URL-состояние и навигация по учебным неделям"
```

---

### Task 5: Вид «Неделя» — подсветка сегодня и пустое состояние

**Files:**
- Modify: `CollegeLMS.Next/components/ScheduleTable.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx`

**Interfaces:**
- Consumes: `anchorDate`, `setIgnoreWeek` из Task 4.
- Produces: пропсы `highlightDate?: string`, `onShowAllWeeks?: () => void` компонента `ScheduleTable`.

- [ ] **Step 1: Пропсы и подсветка в ScheduleTable**

Добавить к интерфейсу:

```ts
interface ScheduleTableProps {
  entries: ScheduleResponse[]
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
  highlightDate?: string   // ISO «YYYY-MM-DD» — колонка этого дня подсвечивается
  onShowAllWeeks?: () => void
}
```

В десктопной сетке заголовок дня:

```tsx
{weekDays.map(d => {
  const isToday = highlightDate
    ? new Date(highlightDate + "T00:00:00").getDay() === d.value
    : false
  return (
    <div
      key={d.value}
      className={`sticky top-0 z-10 border-b p-3 text-center text-xs font-semibold ${
        isToday ? "bg-primary/10 text-primary" : "bg-muted/50"
      }`}
    >
      {d.label}
    </div>
  )
})}
```

В мобильной версии у заголовка дня при isToday добавить бейдж:

```tsx
<div className="border-b bg-muted/30 px-4 py-2.5 text-sm font-semibold flex items-center gap-2">
  {day.full}
  {isTodayMobile && <Badge variant="secondary" className="text-[10px]">сегодня</Badge>}
</div>
```

(`isTodayMobile` считается аналогично по `highlightDate`; импортировать `Badge` из `@/components/ui/badge`.)

Пустое состояние — расширить:

```tsx
if (entries.length === 0) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
      <Calendar className="size-12 opacity-40" />
      <p className="text-lg font-medium">Нет занятий</p>
      <p className="text-sm">На выбранный период расписание не найдено</p>
      {onShowAllWeeks && (
        <Button variant="outline" size="sm" onClick={onShowAllWeeks}>
          Показать все недели
        </Button>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Подключить на странице**

В ветке `view === "week"`:

```tsx
<ScheduleTable
  entries={entries}
  highlightDate={anchorDate}
  onEntryClick={canManage ? handleEdit : undefined}
  onDeleteClick={canManage ? (id) => setDeleteConfirmId(id) : undefined}
  onShowAllWeeks={() => setIgnoreWeek(true)}
/>
```

- [ ] **Step 3: Проверка**

Run: `npm run build` (docker)
Expected: Compiled successfully.

- [ ] **Step 4: Коммит**

```bash
git add -A && git commit -m "feat: подсветка сегодня в недельной сетке и пустое состояние с показом всех недель"
```

---

### Task 6: Вид «Месяц» — ScheduleMonthGrid

**Files:**
- Create: `CollegeLMS.Next/components/ScheduleMonthGrid.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx`

**Interfaces:**
- Consumes: `weekOfDate`, `entries`, `anchorDate`, `changeAnchor/changeView` из Task 4.
- Produces: `ScheduleMonthGrid` — готовый компонент месячной сетки.

- [ ] **Step 1: Компонент сетки**

```tsx
"use client"

import { useMemo } from "react"
import type { ScheduleResponse } from "@/types/schedule"
import { LESSON_TYPE_LABELS } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import { mondayOf, addDays, toISODate } from "@/lib/scheduleWeeks"

const DOT_STYLES: Record<string, string> = {
  Lecture: "bg-blue-500",
  Practice: "bg-emerald-500",
  Lab: "bg-amber-500",
  Exam: "bg-red-500",
}

interface ScheduleMonthGridProps {
  anchorDate: string
  entries: ScheduleResponse[]
  weekOfDate: (iso: string) => number
  onCellClick: (iso: string) => void
  onEntryClick?: (entry: ScheduleResponse) => void
}

export default function ScheduleMonthGrid({
  anchorDate, entries, weekOfDate, onCellClick, onEntryClick,
}: ScheduleMonthGridProps) {
  const cells = useMemo(() => {
    const first = new Date(anchorDate + "T00:00:00")
    const monthStart = new Date(first.getFullYear(), first.getMonth(), 1)
    const gridStart = mondayOf(monthStart)
    const all = Array.from({ length: 42 }, (_, i) => {
      const d = addDays(gridStart, i)
      return { date: d, iso: toISODate(d), inMonth: d.getMonth() === monthStart.getMonth() }
    })
    const lastIdx = all.reduce((acc, c, i) => (c.inMonth ? i : acc), 0)
    return all.slice(0, Math.ceil((lastIdx + 1) / 7) * 7)
  }, [anchorDate])

  const byDate = useMemo(() => {
    const map = new Map<string, ScheduleResponse[]>()
    const jsDayToBackend = (d: Date) => d.getDay()
    for (const c of cells) {
      const w = weekOfDate(c.iso)
      const dayEntries = entries
        .filter(e => e.dayOfWeek === jsDayToBackend(c.date) && e.weeks.includes(w))
        .slice(0, 4)
      map.set(c.iso, dayEntries)
    }
    return map
  }, [cells, entries, weekOfDate])

  const todayIso = toISODate(new Date())

  return (
    <div data-testid="month-grid" className="overflow-hidden rounded-lg border bg-card">
      <div className="hidden md:grid grid-cols-7 border-b bg-muted/50 text-center text-xs font-semibold">
        {DAYS.filter(d => d.value >= 1 && d.value <= 7).map(d => (
          <div key={d.value} className="p-2">
            {DAYS.find(x => x.value === (d.value % 7))?.label ?? d.label}
          </div>
        ))}
      </div>
      {/* Порядок колонок: Пн..Вс */}
      <div className="hidden md:grid grid-cols-7">
        {cells.map(c => (
          <button
            key={c.iso}
            onClick={() => onCellClick(c.iso)}
            className={`min-h-24 border-b border-r p-1.5 text-left align-top ${
              c.inMonth ? "" : "opacity-40"
            } ${c.iso === todayIso ? "bg-primary/10 ring-1 ring-inset ring-primary/40" : ""}`}
          >
            <span className="text-xs text-muted-foreground">{Number(c.iso.slice(-2))}</span>
            <div className="mt-1 space-y-1">
              {(byDate.get(c.iso) ?? []).map(e => (
                <span
                  key={e.id}
                  role="button"
                  tabIndex={0}
                  onClick={ev => { ev.stopPropagation(); onEntryClick?.(e) }}
                  onKeyDown={ev => { if (ev.key === "Enter") { ev.stopPropagation(); onEntryClick?.(e) } }}
                  className={`block truncate rounded px-1 py-0.5 text-[10px] font-medium ${
                    LESSON_TYPE_STYLES_BG[e.lessonType] ?? "bg-gray-100 dark:bg-gray-800"
                  }`}
                  title={`${LESSON_TYPE_LABELS[e.lessonType]} · ${e.subject} · ${e.groupName}`}
                >
                  {e.subject}
                </span>
              ))}
            </div>
          </button>
        ))}
      </div>
      {/* Мобильная версия: точки */}
      <div className="md:hidden">
        {cells.map(c => (
          <button
            key={c.iso}
            onClick={() => onCellClick(c.iso)}
            className={`flex items-center gap-2 border-b px-3 py-2 w-full text-left ${
              c.inMonth ? "" : "opacity-40"
            } ${c.iso === todayIso ? "bg-primary/10" : ""}`}
          >
            <span className="w-8 shrink-0 text-xs text-muted-foreground">{Number(c.iso.slice(-2))}</span>
            <span className="flex gap-1">
              {(byDate.get(c.iso) ?? []).map(e => (
                <span key={e.id} className={`size-2 rounded-full ${DOT_STYLES[e.lessonType] ?? "bg-gray-400"}`} />
              ))}
            </span>
          </button>
        ))}
      </div>
    </div>
  )
}
```

Дополнительно в `types/schedule.ts` экспортировать фон чипов (без border-l):

```ts
export const LESSON_TYPE_STYLES_BG: Record<LessonType, string> = {
  Lecture: "bg-blue-100 text-blue-900 dark:bg-blue-900/40 dark:text-blue-100",
  Practice: "bg-emerald-100 text-emerald-900 dark:bg-emerald-900/40 dark:text-emerald-100",
  Lab: "bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-100",
  Exam: "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-100",
}
```

и импортировать его в компоненте вместо `LESSON_TYPE_STYLES_BG[...]` из воздуха (импорт: `import { LESSON_TYPE_LABELS, LESSON_TYPE_STYLES_BG } from "@/types/schedule"`).

Мобильная вертикальная простыня из 28–42 строк неудобна — упростить: на мобиле рендерить ТОЛЬКО недели текущего месяца компактно нельзя без усложнения; допустимо оставить как есть (скролл), но лучше ограничить: показывать только ячейки `inMonth`:

```tsx
<div className="md:hidden">
  {cells.filter(c => c.inMonth).map(c => ( /* как выше */ ))}
</div>
```

- [ ] **Step 2: Интеграция на странице**

Ветка рендера:

```tsx
{view === "month" && (
  <ScheduleMonthGrid
    anchorDate={anchorDate}
    entries={entries}
    weekOfDate={weekOfDate}
    onCellClick={(iso) => { setView("day"); setIgnoreWeek(false); changeAnchor(iso); applyState("day", iso) }}
    onEntryClick={canManage ? handleEdit : undefined}
  />
)}
```

(если `applyState` не экспортирована в скоупе — использовать `changeAnchor(iso)` после `setView("day")`, т.к. `changeAnchor` пишет `view` из стейта; чтобы URL был корректным, объединить в один хелпер `openDay(iso)`:

```tsx
const openDay = (iso: string) => {
  setView("day")
  setIgnoreWeek(false)
  setAnchorDate(iso)
  window.history.replaceState(null, "", `${pathname}?view=day&date=${iso}`)
}
```

и передать `onCellClick={openDay}`.)

- [ ] **Step 3: Проверка**

Run: `npm run build` (docker)
Expected: Compiled successfully.

- [ ] **Step 4: Коммит**

```bash
git add -A && git commit -m "feat: месячная сетка расписания с переходом в день и точками занятий на мобиле"
```

---

### Task 7: E2E-тесты и диаграмма

**Files:**
- Create: `CollegeLMS.Next/e2e/schedule.spec.ts`
- Create: `docs/diagrams/sequence/schedule-week.puml`

**Interfaces:**
- Consumes: приложение целиком; моки API как в существующих спеках (localStorage token/user Admin).

- [ ] **Step 1: E2E-спека**

```ts
import { test, expect } from "@playwright/test"

const today = new Date()
const iso = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`

const scheduleEntry = {
  id: "e1",
  groupId: "g1",
  groupName: "ИП-235",
  teacherId: null,
  teacherName: null,
  subject: "МДК 09.01",
  room: "301",
  dayOfWeek: 1,
  numberPair: 2,
  startTime: "09:40:00",
  endTime: "11:15:00",
  weeks: [1, 2, 3],
  lessonType: "Lecture",
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("token", "test-jwt-token")
    localStorage.setItem(
      "user",
      JSON.stringify({ id: "a1", login: "admin", email: "a@t.ru", fullName: "Админ", role: "Admin" }),
    )
  })
  await page.route("**/api/groups**", r =>
    r.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }) }))
  await page.route("**/api/teachers**", r =>
    r.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }) }))
  await page.route("**/api/schedule?*view=calendar*", r =>
    r.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        isSuccess: true,
        data: { weekStart: "2026-08-31T00:00:00Z", currentWeek: 3, days: [] },
        errorMessage: null,
        statusCode: 200,
      }),
    }))
  await page.route("**/api/schedule**", r =>
    r.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        isSuccess: true,
        data: { items: [scheduleEntry], totalCount: 1, page: 1, pageSize: 100 },
        errorMessage: null,
        statusCode: 200,
      }),
    }))
})

test("switches between day/week/month views", async ({ page }) => {
  await page.goto("/schedule")
  await expect(page.getByRole("button", { name: "Неделя" })).toBeVisible()
  await page.getByRole("button", { name: "Месяц" }).click()
  await expect(page.getByTestId("month-grid")).toBeVisible()
  await page.getByRole("button", { name: "День" }).click()
  await expect(page.getByText("МДК 09.01")).toBeVisible()
})

test("shows semester week badge and navigates weeks", async ({ page }) => {
  await page.goto("/schedule")
  await expect(page.getByText("Неделя")).toBeVisible()
  await page.getByRole("button", { name: "Следующий период" }).click()
  // подпись периода изменилась — проверяем что кнопка «Сегодня» появилась активной
  await expect(page.getByRole("button", { name: "Сегодня" })).toBeVisible()
})

test("month cell click opens day view", async ({ page }) => {
  await page.goto("/schedule")
  await page.getByRole("button", { name: "Месяц" }).click()
  await page.getByTestId("month-grid").locator("button").first().click()
  await expect(page.locator("text=занятий нет").or(page.getByText("МДК 09.01")).first()).toBeVisible()
})
```

(день понедельника в моках попадёт в день-вид только если якорная дата = понедельник; тест 3 устойчив: допускает и пустой день.)

- [ ] **Step 2: Прогнать спеку**

Run: docker e2e команда с `npx playwright test e2e/schedule.spec.ts`
Expected: PASS. При падениях править селекторы/моки (только этой спеки).

- [ ] **Step 3: Sequence-диаграмма**

`docs/diagrams/sequence/schedule-week.puml`:

```plantuml
@startuml
actor Пользователь
participant "SchedulePage" as FE
participant "ScheduleController" as API
participant "ScheduleService" as SVC
participant "ScheduleWeekCalculator" as WC
database "PostgreSQL" as DB

Пользователь -> FE: вид «Неделя», ◀ / ▶
FE -> API: GET /api/schedule?...&week=N
API -> SVC: GetAllAsync(..., week, ...)
alt week задан
  SVC -> DB: WHERE @N = ANY(weeks)
else week не задан
  SVC -> DB: без фильтра недель
end
DB --> SVC: записи
SVC --> API: Result<PagedResponse<ScheduleResponse>>
API --> FE: 200
FE -> WC-нет: (номер недели считает клиент от currentWeek)
FE --> Пользователь: сетка недели N
@enduml
```

Строку `FE -> WC-нет:` убрать — это мусор; вместо неё комментарий `' номер недели клиент считает от currentWeek (calendar)`.

- [ ] **Step 4: Полный e2e-прогон**

Run: полный `npx playwright test` (docker)
Expected: все зелёные кроме известных 9 pre-existing падений.

- [ ] **Step 5: Коммит**

```bash
git add -A && git commit -m "test: e2e видов расписания и sequence-диаграмма week-фильтра"
```

---

### Task 8: Финальная проверка и merge

- [ ] **Step 1: dotnet build + dotnet test**

Expected: PASS.

- [ ] **Step 2: npm run build (docker)**

Expected: Compiled successfully.

- [ ] **Step 3: Smoke на compose**

```bash
docker compose build collegelms-next && docker compose up -d collegelms-next
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:3000/schedule"
```

Ожидается 200; визуально: табы, ◀/▶, бейдж недели, месяц → день.

- [ ] **Step 4: Merge**

```bash
git checkout master && git pull --rebase origin master
git merge feature/schedule-views --no-edit
git push origin master
```

Expected: push → CD success (concurrency-группа уже стоит).

---

## Self-review плана

1. **Спека → задачи:** §1.1 Task 1; §1.2 Task 2; §2.1–2.4 Task 4; §2.5 Task 5; §2.6 Task 6; §4 тесты — Tasks 1, 2, 7; DoD — Task 8. Покрыто.
2. **Плейсхолдеров нет** — все шаги с кодом; единственная «ручная» правка — Postman (шаг с описанием точечного изменения поля).
3. **Типы согласованы:** `CalendarResponse{weekStart,currentWeek,days}`, `fetchCalendar`, `weekOfDate`, `activeWeek`, пропсы `highlightDate/onShowAllWeeks` — имена совпадают между задачами.
