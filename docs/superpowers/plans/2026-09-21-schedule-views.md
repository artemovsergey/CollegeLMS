# План: виды расписания и слои в веб и мини-приложении

> **Для агентов:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` (рекомендуется) или `superpowers:executing-plans`. Выполнять по задачам, отмечая шаги `[ ]`.
> **Спека:** `docs/superpowers/specs/2026-09-21-schedule-views-design.md`.

**Цель:** серверные виды `GET /api/schedule?view=day|week|semester|calendar` с единым слиянием слоёв (звонки, вставки, практики, нерабочие дни, бейджи), 4 режима в вебе, слои в мини-приложении Max, экспорт дня/недели с FILE-3.

**Архитектура:** новый `ScheduleViewService` собирает виды из `AppDbContext` + существующих сервисов звонков/вставок/практик; контроллер диспетчеризует по `view`; экспорт дня/недели использует тот же сервис; веб и мини-апп — тонкие клиенты `view=`.

**Стек:** .NET 10, EF Core (Npgsql), ClosedXML, QuestPDF, Next.js 14, TypeScript, Tailwind CSS 4.

**Ветка:** `feature/schedule-views` (создаётся в Task 1; миграций нет).

## Глобальные ограничения

- `Result<T>` во всех сервисах, без try-catch; сообщения об ошибках на русском.
- `AsNoTracking()` на чтении; все async-методы с `CancellationToken ct`.
- GUID PK `ValueGeneratedNever()`, `HasMaxLength` для строк, enum-ы строками — схема БД не меняется.
- Swagger: `[SwaggerOperation]`, `[ProducesResponseType]`, русские XML-комментарии; Postman обновляется.
- Гейты после каждой backend-задачи: `dotnet build`; перед merge — полный набор (см. Task 15).
- Локальный Docker не запускаем — сборка стека в CI/CD.
- CSharpier: перед коммитом `dotnet csharpier format .` (в CI — `check`).

---

## Структура файлов

**Создаются:**
- `CollegeLMS.API/Dtos/ScheduleViewDtos.cs` — DTO четырёх видов.
- `CollegeLMS.API/Interfaces/IScheduleViewService.cs` — контракт view-сервиса.
- `CollegeLMS.API/Services/ScheduleChangeTags.cs` — общий построитель бейджей (вынос из `ScheduleService`).
- `CollegeLMS.API/Services/ScheduleViewService.cs` — сборка видов.
- `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs` — unit-тесты видов.
- `CollegeLMS.Next/components/ScheduleViewSwitcher.tsx` — переключатель режимов.
- `CollegeLMS.Next/components/DayNavigation.tsx` — навигация по дате.
- `CollegeLMS.Next/components/ScheduleLayers.tsx` — блоки «Вставки»/«Практика»/«Нерабочий».
- `CollegeLMS.Next/components/ScheduleDayView.tsx` — режим «День».
- `CollegeLMS.Next/components/ScheduleWeekView.tsx` — режим «Неделя».
- `CollegeLMS.Next/components/ScheduleMonthCalendar.tsx` — режим «Календарь».
- `CollegeLMS.Next/components/ScheduleSemesterMatrix.tsx` — режим «Семестр».
- `CollegeLMS.API/SwaggerExamples/ScheduleDayViewExample.cs` — пример ответа дня.
- `docs/spec/task-schedule-views.md` — пост-фактум ТЗ итерации.

**Изменяются:**
- `CollegeLMS.API/Services/ScheduleService.cs` — вынос бейджей, удаление `GetCalendarAsync`.
- `CollegeLMS.API/Services/ScheduleExportService.cs` — `scope=day|week`, слои, FILE-3.
- `CollegeLMS.API/Controllers/ScheduleController.cs` — диспетчеризация `view`, экспорт.
- `CollegeLMS.API/Interfaces/IScheduleService.cs`, `Dtos/ScheduleDtos.cs` — удаление calendar-типов.
- `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — DI.
- `CollegeLMS.Tests/Integration/Controllers/ScheduleControllerTests.cs` — calendar-тест, новые виды, экспорт.
- `CollegeLMS.Tests/Unit/Services/ScheduleExportServiceTests.cs` — day/week кейсы.
- `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx` — 4 режима, URL, дефолт.
- `CollegeLMS.Next/api/schedule.ts`, `types/schedule.ts` — view-клиенты и типы.
- `CollegeLMS.Next/components/ScheduleTable.tsx` — переиспользование в «Дне».
- `CollegeLMS.Next/components/max/ScheduleView.tsx`, `DayFeed.tsx`, `WeekFeed.tsx` — слои.
- `docs/spec/CollegeLMS.postman_collection.json` — view/export.

**Удаляются:**
- `CollegeLMS.Next/components/DayTabs.tsx`, `SemesterView.tsx`.
- `CalendarResponse`, `CalendarDayResponse` (`ScheduleDtos.cs`), `IScheduleService.GetCalendarAsync` + реализация.

---

## Task 1: Ветка, общий построитель бейджей, DTO и интерфейс

**Файлы:**
- Create: `CollegeLMS.API/Services/ScheduleChangeTags.cs`
- Create: `CollegeLMS.API/Dtos/ScheduleViewDtos.cs`
- Create: `CollegeLMS.API/Interfaces/IScheduleViewService.cs`
- Modify: `CollegeLMS.API/Services/ScheduleService.cs:83,416-488` (вызов и удаление приватного метода)
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs` (регресс, без новых кейсов)

**Interfaces:**
- Produces: `ScheduleChangeTags.BuildAsync(AppDbContext db, List<ScheduleEntry> items, int? week, CancellationToken ct) → Task<Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>>>`
- Produces: `IScheduleViewService` (сигнатуры — ниже), `ScheduleDayViewResponse`, `ScheduleWeekViewResponse`, `ScheduleSemesterViewResponse`, `ScheduleSemesterWeekResponse`, `ScheduleMonthViewResponse`, `ScheduleMonthDayResponse`.

- [ ] **Step 1: Синхронизация и ветка**

```powershell
git fetch origin; git pull --rebase origin master
git checkout -b feature/schedule-views
```

- [ ] **Step 2: Вынести построитель бейджей**

Перенести тело `ScheduleService.GetChangeTagsAsync` (строки 416–488) в новый файл `CollegeLMS.API/Services/ScheduleChangeTags.cs` без изменения логики:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Построитель бейджей корректировок для пар расписания.</summary>
internal static class ScheduleChangeTags
{
    public static async Task<
        Dictionary<(Guid GroupId, DayOfWeek DayOfWeek, int NumberPair), List<ChangeTag>>
    > BuildAsync(
        AppDbContext db,
        List<ScheduleEntry> items,
        int? week,
        CancellationToken ct
    )
    {
        // 1:1 перенос текущей реализации ScheduleService.GetChangeTagsAsync
    }
}
```

В `ScheduleService`: `var changeTagsBySlot = await ScheduleChangeTags.BuildAsync(db, items, week, ct);` и удалить приватный метод.

- [ ] **Step 3: Проверить регресс**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleServiceTests`
Expected: PASS (логика бейджей не изменилась).

- [ ] **Step 4: Добавить DTO видов**

`CollegeLMS.API/Dtos/ScheduleViewDtos.cs`:

```csharp
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleDayViewResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsSunday { get; set; }
    public bool IsNonWorking { get; set; }
    public string? NonWorkingTitle { get; set; }
    public List<PracticeResponse> Practices { get; set; } = [];
    public List<ScheduleInsertResponse> Inserts { get; set; } = [];
    public List<ScheduleResponse> Entries { get; set; } = [];
}

public class ScheduleWeekViewResponse
{
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public List<ScheduleDayViewResponse> Days { get; set; } = [];
}

public class ScheduleSemesterWeekResponse
{
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public List<ScheduleDayViewResponse> Days { get; set; } = [];
}

public class ScheduleSemesterViewResponse
{
    public int TotalWeeks { get; set; }
    public List<ScheduleSemesterWeekResponse> Weeks { get; set; } = [];
}

public class ScheduleMonthDayResponse
{
    public DateTime Date { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsSunday { get; set; }
    public bool IsNonWorking { get; set; }
    public string? NonWorkingTitle { get; set; }
    public List<PracticeKind> PracticeKinds { get; set; } = [];
    public bool IsOutOfSemester { get; set; }
    public int PairCount { get; set; }
}

public class ScheduleMonthViewResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<ScheduleMonthDayResponse> Days { get; set; } = [];
}
```

- [ ] **Step 5: Добавить интерфейс**

`CollegeLMS.API/Interfaces/IScheduleViewService.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleViewService
{
    Task<Result<ScheduleDayViewResponse>> GetDayAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime? date,
        CancellationToken ct
    );

    Task<Result<ScheduleWeekViewResponse>> GetWeekAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        int? week,
        DateTime? date,
        CancellationToken ct
    );

    Task<Result<ScheduleSemesterViewResponse>> GetSemesterAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    );

    Task<Result<ScheduleMonthViewResponse>> GetMonthAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? month,
        CancellationToken ct
    );
}
```

- [ ] **Step 6: Сборка и коммит**

```powershell
dotnet build
dotnet csharpier format .
git add -A; git commit -m "feat: DTO и интерфейс серверных видов расписания, общий построитель бейджей"
```

---

## Task 2: `ScheduleViewService` — день

**Файлы:**
- Create: `CollegeLMS.API/Services/ScheduleViewService.cs`
- Create: `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (DI)

**Interfaces:**
- Consumes: `IScheduleViewService`, `ScheduleDayViewResponse` (Task 1); `StudyWeek` (`SemesterStart`, `TotalWeeks`, `WeekOf`); `IBellScheduleService.GetTimeMapAsync`; `IPracticeService.GetAllAsync`; `IScheduleInsertService.GetAllAsync`; `ScheduleChangeTags.BuildAsync`; `ScheduleMapper.ToDto`.
- Produces: `ScheduleViewService(AppDbContext db, IBellScheduleService bells, IPracticeService practices, IScheduleInsertService inserts)` с реализацией `GetDayAsync`.

**Канонические правила дня (спека §2.2):** воскресенье → `IsSunday`, всё пусто; нерабочий → `NonWorkingTitle`, всё пусто; практика (группы/преподавателя) → `Practices`, пары/вставки пусты; иначе вставки (активные, по дню, при группе `Course == null || Course == курса группы`) + пары недели с временем звонков и бейджами.

- [ ] **Step 1: Написать падающие тесты**

`CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs` (конвенции — как в `ScheduleInsertServiceTests`: `TestDbContextFactory.Create()`, FluentAssertions):

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleViewServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BellScheduleServiceStub _bells = new();
    private readonly ScheduleViewService _sut;

    private static readonly DateTime Monday1 =
        StudyWeek.MondayOf(StudyWeek.SemesterStart); // понедельник недели 1

    public ScheduleViewServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleViewService(
            _db,
            _bells,
            new PracticeService(_db),
            new ScheduleInsertService(_db)
        );
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Group Group, Teacher Teacher)> SeedGroupAndTeacherAsync(int course = 2)
    {
        var group = GroupFixture.CreateFaker().Generate();
        group.Course = course;
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return (group, teacher);
    }

    private async Task SeedEntryAsync(
        Guid groupId, Guid? teacherId, DayOfWeek day, int numberPair, params int[] weeks
    )
    {
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        entry.GroupId = groupId;
        entry.TeacherId = teacherId;
        entry.Group = null;
        entry.Teacher = null;
        entry.DayOfWeek = day;
        entry.NumberPair = numberPair;
        entry.Weeks = weeks.ToList();
        entry.StartTime = new TimeSpan(8, 30, 0);
        entry.EndTime = new TimeSpan(9, 50, 0);
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    private async Task SeedInsertAsync(DayOfWeek day, int? course, bool active = true)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleInserts.Add(new ScheduleInsert
        {
            Id = Guid.NewGuid(), Title = "Разговор о важном", DayOfWeek = day,
            StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(9, 0, 0),
            Course = course, IsActive = active, CreatedAt = utcNow, UpdatedAt = utcNow,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedPracticeAsync(Guid groupId, Guid teacherId, DateTime date, PracticeKind kind = PracticeKind.Pp)
    {
        var utcNow = DateTime.UtcNow;
        _db.Practices.Add(new Practice
        {
            Id = Guid.NewGuid(), Kind = kind, GroupId = groupId, TeacherId = teacherId,
            DateFrom = date.Date, DateTo = date.Date, CreatedAt = utcNow, UpdatedAt = utcNow,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedNonWorkingAsync(DateTime from, DateTime to, string title)
    {
        var utcNow = DateTime.UtcNow;
        _db.NonWorkingDays.Add(new NonWorkingDay
        {
            Id = Guid.NewGuid(), DateFrom = from.Date, DateTo = to.Date, Title = title,
            CreatedAt = utcNow, UpdatedAt = utcNow,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedHistoryAsync(Guid groupId, DayOfWeek day, int numberPair, int week, ScheduleChangeType type)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleHistory.Add(new ScheduleHistory
        {
            Id = Guid.NewGuid(), ChangeType = type, AppliedAt = utcNow,
            AppliedByUserId = Guid.NewGuid(), GroupId = groupId, DayOfWeek = day,
            NumberPair = numberPair, Week = week, Subject = "Математика",
            CreatedAt = utcNow, UpdatedAt = utcNow,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDayAsync_Sunday_ReturnsIsSundayAndEmpty()
    {
        var result = await _sut.GetDayAsync(null, null, null, Monday1.AddDays(6), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsSunday.Should().BeTrue();
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
        result.Data.Practices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_NonWorking_ReturnsTitleAndEmpty()
    {
        var date = Monday1.AddDays(2); // среда недели 1
        await SeedNonWorkingAsync(date, date, "День народного единства");
        await SeedEntryAsync(Guid.NewGuid(), null, date.DayOfWeek, 1, 1);
        await SeedInsertAsync(date.DayOfWeek, null);

        var result = await _sut.GetDayAsync(null, null, null, date, CancellationToken.None);

        result.Data!.IsNonWorking.Should().BeTrue();
        result.Data.NonWorkingTitle.Should().Be("День народного единства");
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
        result.Data.Practices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_Practice_SuppressesEntriesAndInserts()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        var date = Monday1.AddDays(1);
        await SeedPracticeAsync(group.Id, teacher.Id, date);
        await SeedEntryAsync(group.Id, teacher.Id, date.DayOfWeek, 1, 1);
        await SeedInsertAsync(date.DayOfWeek, null);

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Practices.Should().HaveCount(1);
        result.Data.Practices[0].Kind.Should().Be(PracticeKind.Pp);
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_Inserts_FiltersByCourseAndActive()
    {
        var (group, _) = await SeedGroupAndTeacherAsync(course: 2);
        var date = Monday1.AddDays(1);
        await SeedInsertAsync(date.DayOfWeek, null, active: true);   // общая — попадает
        await SeedInsertAsync(date.DayOfWeek, 2, active: true);      // курс группы — попадает
        await SeedInsertAsync(date.DayOfWeek, 3, active: true);      // другой курс — нет
        await SeedInsertAsync(date.DayOfWeek, 2, active: false);     // неактивная — нет

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Inserts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDayAsync_Entries_ApplyBellTimesAndTags()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        var date = Monday1.AddDays(1);
        await SeedEntryAsync(group.Id, teacher.Id, date.DayOfWeek, 1, 1, 2);
        await SeedHistoryAsync(group.Id, date.DayOfWeek, 1, 1, ScheduleChangeType.Add);
        _bells.TimeMap[1] = (new TimeSpan(10, 0, 0), new TimeSpan(11, 20, 0));

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Entries.Should().HaveCount(1);
        result.Data.Entries[0].StartTime.Should().Be(new TimeSpan(10, 0, 0));
        result.Data.Entries[0].EndTime.Should().Be(new TimeSpan(11, 20, 0));
        result.Data.Entries[0].ChangeTags.Should().ContainSingle(t => t.ChangeType == ScheduleChangeType.Add);
    }

    [Fact]
    public async Task GetDayAsync_Week_ComputedFromDate()
    {
        var result = await _sut.GetDayAsync(null, null, null, Monday1.AddDays(7), CancellationToken.None);

        result.Data!.Week.Should().Be(2);
    }
}
```

Даты — от `Monday1 = StudyWeek.MondayOf(StudyWeek.SemesterStart)`; тесты не зависят от текущей даты.

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: FAIL (класс `ScheduleViewService` не существует / компиляция).

- [ ] **Step 3: Реализовать день**

`CollegeLMS.API/Services/ScheduleViewService.cs` — скелет с `GetDayAsync`; остальные методы — `NotImplementedException` (заменяются в Task 3–4):

```csharp
public class ScheduleViewService(
    AppDbContext db,
    IBellScheduleService bells,
    IPracticeService practices,
    IScheduleInsertService inserts
) : IScheduleViewService
{
    public async Task<Result<ScheduleDayViewResponse>> GetDayAsync(
        Guid? groupId, Guid? teacherId, string? room, DateTime? date, CancellationToken ct)
    {
        var target = (date ?? DateTime.UtcNow).Date;
        var week = Math.Clamp(StudyWeek.WeekOf(target), 1, StudyWeek.TotalWeeks);

        if (target.DayOfWeek == DayOfWeek.Sunday)
            return Ok(Empty(target, week, isSunday: true));

        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom <= target && d.DateTo >= target, ct);
        if (nonWorking is not null)
            return Ok(new ScheduleDayViewResponse { Date = target, Week = week, DayOfWeek = (int)target.DayOfWeek,
                IsNonWorking = true, NonWorkingTitle = nonWorking.Title });

        var practiceResult = await practices.GetAllAsync(groupId, teacherId, null, target, target, 1, 50, ct);
        var practiceList = practiceResult.Data?.Items ?? [];
        if (practiceList.Count > 0)
            return Ok(new ScheduleDayViewResponse { Date = target, Week = week, DayOfWeek = (int)target.DayOfWeek,
                Practices = practiceList });

        return Ok(new ScheduleDayViewResponse {
            Date = target, Week = week, DayOfWeek = (int)target.DayOfWeek,
            Inserts = await GetInsertsAsync(target.DayOfWeek, groupId, ct),
            Entries = await GetEntriesAsync(groupId, teacherId, room, target, week, ct),
        });
    }

    private async Task<List<ScheduleInsertResponse>> GetInsertsAsync(DayOfWeek day, Guid? groupId, CancellationToken ct)
    {
        int? course = null;
        if (groupId.HasValue)
            course = await db.Groups.AsNoTracking()
                .Where(g => g.Id == groupId.Value).Select(g => (int?)g.Course).FirstOrDefaultAsync(ct);
        var result = await inserts.GetAllAsync(day, course, activeOnly: true, ct);
        return result.Data ?? [];
    }

    private async Task<List<ScheduleResponse>> GetEntriesAsync(
        Guid? groupId, Guid? teacherId, string? room, DateTime date, int week, CancellationToken ct)
    {
        var query = db.ScheduleEntries.AsNoTracking()
            .Include(s => s.Group).Include(s => s.Teacher!).ThenInclude(t => t.User)
            .Where(s => s.DayOfWeek == date.DayOfWeek && s.Weeks.Contains(week)
                && date >= StudyWeek.SemesterStart
                && date < StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(StudyWeek.TotalWeeks * 7));
        if (groupId.HasValue) query = query.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue) query = query.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room)) query = query.Where(s => s.Room == room);

        var items = await query.OrderBy(s => s.NumberPair).ToListAsync(ct);
        var tags = await ScheduleChangeTags.BuildAsync(db, items, week, ct);
        var bellTimes = await bells.GetTimeMapAsync(ct);
        return items.Select(s => {
            var dto = s.ToDto(tags.GetValueOrDefault((s.GroupId, s.DayOfWeek, s.NumberPair)));
            if (bellTimes.TryGetValue(s.NumberPair, out var time)) { dto.StartTime = time.Start; dto.EndTime = time.End; }
            return dto;
        }).ToList();
    }
}
```

Добавить в `ServiceCollectionExtensions` (рядом с другими сервисами расписания):
`services.AddScoped<IScheduleViewService, ScheduleViewService>();`

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: PASS.

- [ ] **Step 5: Коммит**

```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat: серверный вид расписания на день со слоями"
```

---

## Task 3: `ScheduleViewService` — неделя и календарь месяца

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleViewService.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs`

**Interfaces:**
- Consumes: `GetDayAsync`-логика (выделить `BuildDayAsync(groupId, teacherId, room, date, week, ct)` без `Result`-обёртки — используется неделей/семестром).
- Produces: рабочие `GetWeekAsync`, `GetMonthAsync`.

- [ ] **Step 1: Тесты недели и месяца**

Добавить в `ScheduleViewServiceTests`:

```csharp
[Fact]
public async Task GetWeekAsync_ReturnsSixDaysMondayToSaturday()
{
    var (group, teacher) = await SeedGroupAndTeacherAsync();
    await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Wednesday, 2, 1);

    var result = await _sut.GetWeekAsync(group.Id, null, null, 1, null, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Data!.Week.Should().Be(1);
    result.Data.WeekStart.Should().Be(Monday1);
    result.Data.Days.Should().HaveCount(6);                // Пн–Сб
    result.Data.Days[0].Date.Should().Be(Monday1);
    result.Data.Days[2].Entries.Should().ContainSingle();  // среда
    result.Data.Days[5].Date.DayOfWeek.Should().Be(DayOfWeek.Saturday);
}

[Fact]
public async Task GetWeekAsync_ClampsWeekToSemester()
{
    var result = await _sut.GetWeekAsync(null, null, null, 99, null, CancellationToken.None);

    result.Data!.Week.Should().Be(StudyWeek.TotalWeeks);
}

[Fact]
public async Task GetMonthAsync_MarksSundayAndNonWorkingWithPairCount()
{
    var (group, teacher) = await SeedGroupAndTeacherAsync();
    await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Tuesday, 1, 1); // 01.09.2026 — вторник недели 1
    await SeedNonWorkingAsync(new DateTime(2026, 9, 7), new DateTime(2026, 9, 7), "Праздник");

    var result = await _sut.GetMonthAsync(group.Id, null, null, "2026-09", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    var days = result.Data!.Days;
    days.Should().HaveCount(30);
    var sunday = days.Single(d => d.Date == new DateTime(2026, 9, 6));
    sunday.IsSunday.Should().BeTrue();
    sunday.PairCount.Should().Be(0);
    var holiday = days.Single(d => d.Date == new DateTime(2026, 9, 7));
    holiday.IsNonWorking.Should().BeTrue();
    holiday.NonWorkingTitle.Should().Be("Праздник");
    days.Single(d => d.Date == new DateTime(2026, 9, 1)).PairCount.Should().Be(1);
    days.Should().OnlyContain(d => !d.IsOutOfSemester);
}

[Fact]
public async Task GetMonthAsync_PracticeKindAndOutOfSemester()
{
    var (group, teacher) = await SeedGroupAndTeacherAsync();
    await SeedPracticeAsync(group.Id, teacher.Id, new DateTime(2026, 12, 15), PracticeKind.Up);

    var result = await _sut.GetMonthAsync(group.Id, null, null, "2026-12", CancellationToken.None);

    var practiceDay = result.Data!.Days.Single(d => d.Date == new DateTime(2026, 12, 15));
    practiceDay.PracticeKinds.Should().ContainSingle().Which.Should().Be(PracticeKind.Up);
    practiceDay.PairCount.Should().Be(0);
    // семестр: 31.08.2026 + 16 недель → конец 20.12.2026
    result.Data.Days.Single(d => d.Date == new DateTime(2026, 12, 21)).IsOutOfSemester.Should().BeTrue();
}

[Fact]
public async Task GetMonthAsync_InvalidMonth_Returns400()
{
    var result = await _sut.GetMonthAsync(null, null, null, "2026-13", CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
}
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: FAIL (`NotImplementedException`).

- [ ] **Step 3: Реализовать неделю**

```csharp
public async Task<Result<ScheduleWeekViewResponse>> GetWeekAsync(
    Guid? groupId, Guid? teacherId, string? room, int? week, DateTime? date, CancellationToken ct)
{
    var derived = week ?? StudyWeek.WeekOf(date ?? DateTime.UtcNow);
    var effectiveWeek = Math.Clamp(derived, 1, StudyWeek.TotalWeeks);
    var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays((effectiveWeek - 1) * 7);
    var days = new List<ScheduleDayViewResponse>();
    for (var i = 0; i < 6; i++)  // Пн–Сб
        days.Add(await BuildDayAsync(groupId, teacherId, room, monday.AddDays(i), effectiveWeek, ct));
    return Result<ScheduleWeekViewResponse>.Ok(
        new ScheduleWeekViewResponse { Week = effectiveWeek, WeekStart = monday, Days = days });
}
```

`BuildDayAsync` — тело текущего `GetDayAsync` (без `Result`), возвращает `ScheduleDayViewResponse`; `GetDayAsync` вызывает его и оборачивает в `Result.Ok`.

- [ ] **Step 4: Реализовать месяц**

```csharp
public async Task<Result<ScheduleMonthViewResponse>> GetMonthAsync(
    Guid? groupId, Guid? teacherId, string? room, string? month, CancellationToken ct)
{
    DateTime first;
    if (string.IsNullOrWhiteSpace(month))
        first = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    else if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out first))
        return Result<ScheduleMonthViewResponse>.Fail("Неверный формат месяца.", 400);

    var last = first.AddMonths(1).AddDays(-1);
    var nonWorking = await db.NonWorkingDays.AsNoTracking()
        .Where(d => d.DateFrom <= last && d.DateTo >= first).ToListAsync(ct);
    var practiceResult = await practices.GetAllAsync(groupId, teacherId, null, first, last, 1, 100, ct);
    var practiceList = practiceResult.Data?.Items ?? [];
    var entries = await GetMonthEntriesAsync(groupId, teacherId, room, ct); // все записи фильтра, без недельного среза
    var semesterEnd = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(StudyWeek.TotalWeeks * 7 - 1);

    var days = new List<ScheduleMonthDayResponse>();
    for (var date = first; date <= last; date = date.AddDays(1))
    {
        var week = StudyWeek.WeekOf(date);
        var nwd = nonWorking.FirstOrDefault(d => d.DateFrom <= date && d.DateTo >= date);
        var coversPractice = practiceList.Where(p => p.DateFrom <= date && p.DateTo >= date).ToList();
        var isSunday = date.DayOfWeek == DayOfWeek.Sunday;
        var pairCount = (isSunday || nwd is not null || coversPractice.Count > 0)
            ? 0
            : entries.Count(e => e.DayOfWeek == date.DayOfWeek && e.Weeks.Contains(week));
        days.Add(new ScheduleMonthDayResponse {
            Date = date, DayOfWeek = (int)date.DayOfWeek, IsSunday = isSunday,
            IsNonWorking = nwd is not null, NonWorkingTitle = nwd?.Title,
            PracticeKinds = coversPractice.Select(p => p.Kind).Distinct().ToList(),
            IsOutOfSemester = date < StudyWeek.SemesterStart.Date || date > semesterEnd,
            PairCount = pairCount,
        });
    }
    return Result<ScheduleMonthViewResponse>.Ok(
        new ScheduleMonthViewResponse { Year = first.Year, Month = first.Month, Days = days });
}
```

`GetMonthEntriesAsync` — та же выборка, что `GetEntriesAsync`, но без `Weeks.Contains(week)` и без подстановки звонков/бейджей (нужны только для подсчёта).

- [ ] **Step 5: Тесты зелёные, коммит**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: PASS.

```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat: серверные виды расписания — неделя и календарь месяца"
```

---

## Task 4: `ScheduleViewService` — семестр

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleViewService.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleViewServiceTests.cs`

**Interfaces:**
- Produces: рабочий `GetSemesterAsync`; ошибка `400` «Укажите одну группу или одного преподавателя.» при `groupId.HasValue == teacherId.HasValue`.

- [ ] **Step 1: Тесты**

```csharp
[Fact]
public async Task GetSemesterAsync_RequiresExactlyOneFilter()
{
    var none = await _sut.GetSemesterAsync(null, null, CancellationToken.None);
    none.IsSuccess.Should().BeFalse();
    none.StatusCode.Should().Be(400);
    none.ErrorMessage.Should().Contain("одну группу или одного преподавателя");

    var (group, teacher) = await SeedGroupAndTeacherAsync();
    var both = await _sut.GetSemesterAsync(group.Id, teacher.Id, CancellationToken.None);
    both.StatusCode.Should().Be(400);
}

[Fact]
public async Task GetSemesterAsync_ReturnsTotalWeeksRowsWithSixDays()
{
    var (group, teacher) = await SeedGroupAndTeacherAsync();
    await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);

    var result = await _sut.GetSemesterAsync(group.Id, null, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Data!.TotalWeeks.Should().Be(StudyWeek.TotalWeeks);
    result.Data.Weeks.Should().HaveCount(StudyWeek.TotalWeeks);
    result.Data.Weeks.Should().OnlyContain(w => w.Days.Count == 6);
    result.Data.Weeks[0].Days[0].Entries.Should().ContainSingle();
}
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: FAIL.

- [ ] **Step 3: Реализация**

```csharp
public async Task<Result<ScheduleSemesterViewResponse>> GetSemesterAsync(
    Guid? groupId, Guid? teacherId, CancellationToken ct)
{
    if (groupId.HasValue == teacherId.HasValue)
        return Result<ScheduleSemesterViewResponse>.Fail(
            "Укажите одну группу или одного преподавателя.", 400);

    var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart);
    var weeks = new List<ScheduleSemesterWeekResponse>();
    for (var week = 1; week <= StudyWeek.TotalWeeks; week++)
    {
        var weekStart = monday.AddDays((week - 1) * 7);
        var days = new List<ScheduleDayViewResponse>();
        for (var i = 0; i < 6; i++)
            days.Add(await BuildDayAsync(groupId, teacherId, null, weekStart.AddDays(i), week, ct));
        weeks.Add(new ScheduleSemesterWeekResponse { Week = week, WeekStart = weekStart, Days = days });
    }
    return Result<ScheduleSemesterViewResponse>.Ok(
        new ScheduleSemesterViewResponse { TotalWeeks = StudyWeek.TotalWeeks, Weeks = weeks });
}
```

- [ ] **Step 4: Тесты, коммит**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleViewServiceTests`
Expected: PASS.

```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat: серверный вид расписания на семестр"
```

---

## Task 5: Контроллер, Swagger, удаление старого calendar

**Файлы:**
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs:29-63` (диспетчеризация), `:258-294` (экспорт — параметры из Task 6)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs` (удалить `GetCalendarAsync`, `GetRussianDayName`), `Interfaces/IScheduleService.cs`, `Dtos/ScheduleDtos.cs` (удалить `CalendarResponse`, `CalendarDayResponse`)
- Create: `CollegeLMS.API/SwaggerExamples/ScheduleDayViewExample.cs`
- Modify: `CollegeLMS.Tests/Integration/Controllers/ScheduleControllerTests.cs:358`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (если регистрация ещё не добавлена)

**Interfaces:**
- Consumes: `IScheduleViewService` (Task 2–4).
- Produces: `GET /api/schedule?view=day|week|semester|calendar` → соответствующий `Result<...>`.

- [ ] **Step 1: Интеграционный тест (падающий)**

В `ScheduleControllerTests` заменить calendar-тест на проверки видов (пример):

```csharp
[Fact]
public async Task GetAll_DayView_ReturnsStructuredDay()
{
    var response = await Client.GetAsync($"/api/schedule?view=day&date=2026-09-01&groupId={_groupId}");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var json = await response.Content.ReadAsStringAsync();
    json.Should().Contain("\"isSunday\":false").And.Contain("\"entries\"");
}

[Fact]
public async Task GetAll_SemesterView_WithoutFilter_Returns400()
{
    var response = await Client.GetAsync("/api/schedule?view=semester");
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

- [ ] **Step 2: Тест падает**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleControllerTests`
Expected: FAIL (day-view уходит в paged-список).

- [ ] **Step 3: Диспетчеризация в контроллере**

В `GetAll` перед вызовом `GetAllAsync`:

```csharp
switch (view)
{
    case "day":
        return Ok(await viewService.GetDayAsync(groupId, teacherId, room, date, ct));
    case "week":
        return Ok(await viewService.GetWeekAsync(groupId, teacherId, room, week, date, ct));
    case "semester":
        return Ok(await viewService.GetSemesterAsync(groupId, teacherId, ct));
    case "calendar":
        return Ok(await viewService.GetMonthAsync(groupId, teacherId, room, month, ct));
}
```

Конструктор: `ScheduleController(IScheduleService service, ScheduleImportService importService, IScheduleViewService viewService)`. Добавить `[FromQuery] string? month` в сигнатуру `GetAll`. Swagger: `[SwaggerResponse(...)]` на каждый вариант + XML-комментарий с описанием видов; удалить старую ветку `if (view == "calendar")`.

- [ ] **Step 4: Удалить старый calendar**

Удалить `CalendarResponse`/`CalendarDayResponse`, `IScheduleService.GetCalendarAsync`, реализацию `GetCalendarAsync` и `GetRussianDayName` в `ScheduleService`.

- [ ] **Step 5: Пример Swagger**

`SwaggerExamples/ScheduleDayViewExample.cs` — `Result<ScheduleDayViewResponse>` с заполненным днём (2 пары, 1 вставка, бейдж `Add`) по образцу существующих примеров; сослаться в атрибутах контроллера.

- [ ] **Step 6: Тесты зелёные, коммит**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleControllerTests`
Expected: PASS.

```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat: endpoint видов расписания, удаление calendar-шаблона недели"
```

---

## Task 6: Экспорт дня и недели

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleExportService.cs:42-106` (сигнатура и ветки), рендер-хелперы
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs` (параметры `date`, `week`; убрать `period`)
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleExportServiceTests.cs`

**Interfaces:**
- Consumes: `IScheduleViewService.GetDayAsync/GetWeekAsync`.
- Produces: `ScheduleExportService.ExportAsync(Guid? groupId, Guid? teacherId, string? room, string? scope, DateTime? date, int? week, ExportFormat format, ExportLayout layout, CancellationToken ct)`.

- [ ] **Step 1: Тесты**

Обновить конструктор тестового класса (сервис получает view-сервис):

```csharp
private readonly BellScheduleServiceStub _bells = new();

public ScheduleExportServiceTests()
{
    _db = TestDbContextFactory.Create();
    _sut = new ScheduleExportService(
        _db,
        _bells,
        new ScheduleViewService(_db, _bells, new PracticeService(_db), new ScheduleInsertService(_db))
    );
}
```

Добавить кейсы (используют существующие хелперы `SeedGroupAsync`/`SeedTeacherAsync`/`SeedEntryAsync` файла):

```csharp
[Fact]
public async Task ExportAsync_Day_NonWorking_Returns400WithTitle()
{
    var utcNow = DateTime.UtcNow;
    _db.NonWorkingDays.Add(new NonWorkingDay
    {
        Id = Guid.NewGuid(), DateFrom = new DateTime(2026, 9, 7), DateTo = new DateTime(2026, 9, 7),
        Title = "Праздник", CreatedAt = utcNow, UpdatedAt = utcNow,
    });
    await _db.SaveChangesAsync();

    var result = await _sut.ExportAsync(
        null, null, null, "day", new DateTime(2026, 9, 7), null,
        ExportFormat.Xlsx, ExportLayout.Grid, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    result.ErrorMessage.Should().Be("Нерабочий день: Праздник");
}

[Fact]
public async Task ExportAsync_Day_Empty_Returns404()
{
    var group = await SeedGroupAsync();

    var result = await _sut.ExportAsync(
        group.Id, null, null, "day", new DateTime(2026, 9, 1), null,
        ExportFormat.Xlsx, ExportLayout.Grid, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
}

[Fact]
public async Task ExportAsync_Day_FileNameHasTimestampPrefix()
{
    var group = await SeedGroupAsync();
    var teacher = await SeedTeacherAsync();
    await SeedEntryAsync(group, teacher, DayOfWeek.Tuesday, 1);

    var result = await _sut.ExportAsync(
        group.Id, null, null, "day", new DateTime(2026, 9, 1), null,
        ExportFormat.Xlsx, ExportLayout.Grid, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Data!.FileName.Should()
        .MatchRegex(@"^Расписание_день_\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}\.xlsx$");
}

[Fact]
public async Task ExportAsync_Week_IncludesInsertsAndPractices()
{
    var utcNow = DateTime.UtcNow;
    var group = await SeedGroupAsync();
    var teacher = await SeedTeacherAsync();
    _db.ScheduleInserts.Add(new ScheduleInsert
    {
        Id = Guid.NewGuid(), Title = "Разговор о важном", DayOfWeek = DayOfWeek.Tuesday,
        StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(9, 0, 0),
        IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow,
    });
    _db.Practices.Add(new Practice
    {
        Id = Guid.NewGuid(), Kind = PracticeKind.Up, GroupId = group.Id, TeacherId = teacher.Id,
        DateFrom = new DateTime(2026, 9, 2), DateTo = new DateTime(2026, 9, 2),
        CreatedAt = utcNow, UpdatedAt = utcNow,
    });
    await _db.SaveChangesAsync();

    var result = await _sut.ExportAsync(
        group.Id, null, null, "week", null, new DateTime(2026, 9, 1),
        ExportFormat.Xlsx, ExportLayout.DayCards, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    using var stream = new MemoryStream(result.Data!.FileContent);
    using var workbook = new XLWorkbook(stream);
    var text = string.Join(
        "\n",
        workbook.Worksheets.SelectMany(ws => ws.CellsUsed().Select(c => c.GetString()))
    );
    text.Should().Contain("УП").And.Contain("Разговор о важном");
}
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleExportServiceTests`
Expected: FAIL.

- [ ] **Step 3: Реализация**

В `ExportAsync` — ветки:

```csharp
if (scope == "semester") return await ExportSemesterAsync(...);
if (scope == "day") return await ExportDayViewAsync(groupId, teacherId, room, date, format, layout, ct);
if (scope == "week") return await ExportWeekViewAsync(groupId, teacherId, room, week, date, format, layout, ct);
return Result<ExportResult>.Fail("Укажите scope: day, week или semester.", 400);
```

- `ExportDayViewAsync`: `viewService.GetDayAsync(...)`; `IsNonWorking` → `Fail($"Нерабочий день: {title}", 400)`; пусто (`Entries`/`Inserts`/`Practices` пусты или `IsSunday`) → `Fail("Нет данных для экспорта", 404)`; иначе рендер одним днём через новые хелперы `ExportDayViewXlsx/Pdf(day, layout)`.
- `ExportWeekViewAsync`: `GetWeekAsync(...)`; все дни пусты → `404`; рендер `ExportWeekViewXlsx/Pdf(days, layout)`.
- Рендер: строки дня — вставки (`HH:mm–HH:mm Название`), пары (`N. Предмет · ауд. · преподаватель · бейджи`), карточка практики («УП/ПП: группа · преподаватель · организация»), «Не работает: {title}»; `grid` — таблица «№ пары × дни», `daycards` — блоки дней. Звонки/бейджи приходят из DTO.
- Имена файлов (FILE-3):

```csharp
private static string BuildFileName(string prefix, string extension) =>
    $"{prefix}_{DateTime.UtcNow:dd.MM.yyyy_HH-mm-ss}.{extension}";
// день: "Расписание_день", неделя: "Расписание_неделя", семестр: "Расписание_семестр"
```

Семестровый экспорт перевести на `BuildFileName("Расписание_семестр", ...)` (сохранить текущий вид имени из `task-schedule-reference-data.md` — там уже FILE-3).

- [ ] **Step 4: Контроллер**

В `Export`: `[FromQuery] DateTime? date, [FromQuery] int? week`, убрать `period`; передать в сервис. Обновить XML-комментарии/Swagger.

- [ ] **Step 5: Тесты зелёные, коммит**

Run: `dotnet test CollegeLMS.Tests --filter FullyQualifiedName~ScheduleExportServiceTests`
Expected: PASS.

```powershell
dotnet build; dotnet csharpier format .
git add -A; git commit -m "feat: экспорт дня и недели со слоями, FILE-3"
```

---

## Task 7: Документация backend (Postman)

**Файлы:**
- Modify: `docs/spec/CollegeLMS.postman_collection.json`

- [ ] **Step 1: Postman**

В папке расписания: запросы `Get schedule (view=day|week|semester|calendar)` с параметрами и примерами ответов; экспорт — `scope=day|week` с `date`/`week` и примером имени FILE-3; удалить упоминание старого calendar-шаблона.

- [ ] **Step 3: Коммит**


---

## Task 8: Веб-клиент и типы

**Файлы:**
- Modify: `CollegeLMS.Next/types/schedule.ts`, `CollegeLMS.Next/api/schedule.ts`
- Consumes (типы): `PracticeResponse` из `@/api/practices`, `ScheduleInsert` из `@/api/inserts` (проверить фактические экспорты; при необходимости добавить типы).

- [ ] **Step 1: Типы**

```ts
export interface ScheduleDayView {
  date: string
  week: number
  dayOfWeek: number
  isSunday: boolean
  isNonWorking: boolean
  nonWorkingTitle: string | null
  practices: Practice[]
  inserts: ScheduleInsert[]
  entries: ScheduleResponse[]
}
export interface ScheduleWeekView { week: number; weekStart: string; days: ScheduleDayView[] }
export interface ScheduleSemesterWeek { week: number; weekStart: string; days: ScheduleDayView[] }
export interface ScheduleSemesterView { totalWeeks: number; weeks: ScheduleSemesterWeek[] }
export interface ScheduleMonthDay {
  date: string; dayOfWeek: number; isSunday: boolean; isNonWorking: boolean
  nonWorkingTitle: string | null; practiceKinds: ("Up" | "Pp")[]; isOutOfSemester: boolean; pairCount: number
}
export interface ScheduleMonthView { year: number; month: number; days: ScheduleMonthDay[] }
```

- [ ] **Step 2: API-функции**

```ts
export async function fetchDayView(params: { date: string; groupId?: string; teacherId?: string; room?: string }) { /* /api/schedule?view=day... */ }
export async function fetchWeekView(params: { week?: number; date?: string; groupId?: string; teacherId?: string }) { /* view=week */ }
export async function fetchSemesterView(params: { groupId?: string; teacherId?: string }) { /* view=semester */ }
export async function fetchMonthView(params: { month: string; groupId?: string; teacherId?: string }) { /* view=calendar&month=... */ }
```

`exportSchedule(filters, format, layout, scope, opts: { date?: string; week?: number })` — добавить `date`/`week`, убрать `period`; удалить `fetchScheduleCalendar`.

- [ ] **Step 3: Сборка и коммит**

Run (в `CollegeLMS.Next/`): `npm run build`
Expected: PASS (страница пока использует удалённую функцию — временно заменить вызов на `fetchSemesterView` заглушкой логики в `page.tsx`, финально — Task 9).

```powershell
git add -A; git commit -m "feat(frontend): клиент и типы серверных видов расписания"
```

---

## Task 9: Страница — переключатель, URL-состояние, дефолт

**Файлы:**
- Modify: `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx`
- Create: `CollegeLMS.Next/components/ScheduleViewSwitcher.tsx`, `DayNavigation.tsx`

- [ ] **Step 1: Переключатель**

`ScheduleViewSwitcher` — 4 кнопки («День», «Неделя», «Календарь», «Семестр»), `value/onChange`, активная — `variant="default"`, доступность (`aria-pressed`, 44×44).

- [ ] **Step 2: `DayNavigation`**

Кнопки ‹ / ›, пикер (`<input type="date">`), «Сегодня»; `aria-label` на иконках.

- [ ] **Step 3: Состояние в URL и дефолт**

В `page.tsx`:
- парсинг `view|date|week|month` на маунте; `view` по умолчанию `day`; миграция старых `?week=&day=` → `date` (понедельник недели + смещение);
- синхронизация `window.history.replaceState` при смене режима/навигации;
- `fetchScheduleContext()` — студент/преподаватель → `selectedGroupId`/`selectedTeacherId`; «Сбросить» — к дефолту;
- `fetchScheduleMeta()` — `semesterStart`/`totalWeeks` в состояние; удалить `SEMESTER_START`, `getCurrentWeek`;
- рендер: `ScheduleDayView | ScheduleWeekView | ScheduleMonthCalendar | ScheduleSemesterMatrix` по `view`;
- экспорт-меню: День → `scope=day&date`, Неделя → `scope=week&week`, Семестр → `scope=semester`; в «Календаре» скрыто.

- [ ] **Step 4: Сборка и коммит**

Run: `npm run build`
Expected: PASS.

```powershell
git add -A; git commit -m "feat(frontend): режимы расписания, состояние в URL, дефолт пользователя"
```

---

## Task 10: Режим «День» со слоями

**Файлы:**
- Create: `CollegeLMS.Next/components/ScheduleDayView.tsx`, `ScheduleLayers.tsx`
- Modify: `CollegeLMS.Next/components/ScheduleTable.tsx`, `page.tsx`
- Delete: `CollegeLMS.Next/components/DayTabs.tsx`

- [ ] **Step 1: `ScheduleLayers`**

Пропсы `{ nonWorkingTitle: string | null; isSunday: boolean; practices: Practice[]; inserts: ScheduleInsert[] }`:
- нерабочий → сообщение «Нерабочий день: {название}» (иконка, `role="status"`);
- воскресенье → «Выходной»;
- практика → карточка «УП/ПП: группа · преподаватель · организация» (цвета `emerald`/`amber`, бейдж вида);
- вставки → строки «HH:mm–HH:mm Название» с иконкой, без номера пары.

- [ ] **Step 2: `ScheduleDayView`**

Загрузка `fetchDayView({date, groupId, teacherId})`; состояния UI-1…UI-3; `DayNavigation`; заголовок «{день недели}, {dd.MM} · неделя N»; `ScheduleLayers` + `ScheduleTable` (карточки пар с бейджами, «Сейчас идёт»); диспетчерские «Добавить»/редактирование/удаление — пропсами.

- [ ] **Step 3: `page.tsx`**

Подключить `ScheduleDayView`, удалить использование `DayTabs` и `SemesterView`.

- [ ] **Step 4: Проверка и коммит**

Run: `npm run dev` → `/schedule`: день, нерабочий, воскресенье, практика, вставки, бейджи; 393/1366/1920.
Run: `npm run build`
Expected: PASS.

```powershell
git add -A; git commit -m "feat(frontend): режим «День» со слоями"
```

---

## Task 11: Режим «Неделя»

**Файлы:**
- Create: `CollegeLMS.Next/components/ScheduleWeekView.tsx`
- Modify: `page.tsx`

- [ ] **Step 1: Компонент**

Загрузка `fetchWeekView({week, groupId, teacherId})`; заголовок «Неделя N · dd.MM–dd.MM»; ≥lg — сетка 6 колонок (`grid-cols-6`), <lg — вертикальные дни; в дне: дата, вставки, пары (номер/время/предмет/аудитория/преподаватель/бейджи), практика, «Не работает: {название}», пусто — «Нет пар»; клик по дню → `view=day` с датой.

- [ ] **Step 2: Проверка и коммит**

Run: `npm run dev` → неделя на 393/1366/1920; `npm run build` → PASS.

```powershell
git add -A; git commit -m "feat(frontend): режим «Неделя»"
```

---

## Task 12: Режим «Календарь»

**Файлы:**
- Create: `CollegeLMS.Next/components/ScheduleMonthCalendar.tsx`
- Modify: `page.tsx`

- [ ] **Step 1: Компонент**

Загрузка `fetchMonthView({month, groupId, teacherId})`; `DayNavigation` для месяцев (‹ месяц ›, «Текущий»); сетка Пн–Вс 7 колонок: число, `pairCount` («N пар»), маркеры — праздник (`title` подсказка), практика (`УП`/`ПП` цветом); воскресенья и `isOutOfSemester` — `text-muted-foreground/opacity-50`; клик → `view=day&date=...`; состояния UI-1…UI-3.

- [ ] **Step 2: Проверка и коммит**

Run: `npm run build` → PASS; проверка клика и маркеров.

```powershell
git add -A; git commit -m "feat(frontend): режим «Календарь» месяца"
```

---

## Task 13: Режим «Семестр» (матрица)

**Файлы:**
- Create: `CollegeLMS.Next/components/ScheduleSemesterMatrix.tsx`
- Modify: `page.tsx`
- Delete: `CollegeLMS.Next/components/SemesterView.tsx`

- [ ] **Step 1: Компонент**

Загрузка `fetchSemesterView({groupId, teacherId})`; без фильтра — приглашение «Выберите группу или преподавателя» (запрос не выполняется); таблица: sticky первый столбец «Неделя», колонки Пн–Сб с датами; ячейка: пары (номер, предмет, аудитория, бейджи), вставки, практика, «Не работает: …», пусто — «—»; `overflow-x-auto`; клик по ячейке → `view=day&date=...`; состояния UI-1…UI-3.

- [ ] **Step 2: Проверка и коммит**

Run: `npm run build` → PASS; проверка на 393 (скролл), 1366, 1920.

```powershell
git add -A; git commit -m "feat(frontend): режим «Семестр» — матрица недели × дни"
```

---

## Task 14: Мини-приложение Max

**Файлы:**
- Modify: `CollegeLMS.Next/components/max/ScheduleView.tsx`, `DayFeed.tsx`, `WeekFeed.tsx`

- [ ] **Step 1: Данные**

`ScheduleView`: для дня — `fetchDayView`, для недели — `fetchWeekView`; типы `ScheduleDayView`/`ScheduleWeekView`; deep-link'и и `meta` сохранить.

- [ ] **Step 2: `DayFeed`**

Добавить блоки: «Вставки» (строки «HH:mm–HH:mm Название»), карточка практики (заменяет пары), «Нерабочий день: {название}», «Выходной»; touch-target ≥ 44×44; тёмная тема.

- [ ] **Step 3: `WeekFeed`**

Маркеры по дням: бейдж практики (УП/ПП), «Не работает», вставки; пустые дни — «Нет пар».

- [ ] **Step 4: Проверка и коммит**

Run: `npm run build` → PASS; проверка `/max/schedule` day/week в браузере (viewport 393).

```powershell
git add -A; git commit -m "feat(max): слои расписания в мини-приложении"
```

---

## Task 15: Пост-фактум ТЗ, финальные гейты, merge

**Файлы:**
- Create: `docs/spec/task-schedule-views.md`
- Modify: `docs/spec/task-schedule-management.md` (отметить покрытые критерии UC при необходимости)

- [ ] **Step 1: Пост-фактум ТЗ**

По образцу `docs/spec/task-schedule-reference-data.md`: цель, роли, модель (без изменений), API-контракты `view=…` и экспорта, правила слоёв, фронтенд, требования, документация.

- [ ] **Step 2: Полные гейты**

```powershell
dotnet build
dotnet csharpier check .
dotnet test CollegeLMS.Tests
dotnet test CollegeLMS.MaxBot.Tests
npm run build --prefix CollegeLMS.Next
```
Expected: всё PASS.

- [ ] **Step 3: Ручная проверка**

`npm run dev`: 4 режима, слои, дефолт студента/преподавателя, экспорт дня/недели (файл скачивается, имя FILE-3), мини-апп; viewport'ы 393 / 1366 / 1920.

- [ ] **Step 4: Коммит, merge, push**

```powershell
git add -A; git commit -m "docs: пост-фактум ТЗ видов расписания"
git checkout master
git merge feature/schedule-views
git push origin master   # → CI quality + CD на VPS
```

- [ ] **Step 5: Проверить CI/CD**

`gh run list --limit 3` (или вкладка Actions): quality и deploy зелёные; при падении — `gh-fix-ci`.

---

## Самопроверка плана

- **Покрытие спеки:** §2.1 — Task 1–5; §2.2 — Task 2–4; §3 — Task 2–5; §4 — Task 6; §5 — Task 8–13; §6 — Task 14; §7 — тесты в Task 2–6, финальные гейты Task 15; §8 — Task 7, 15; §9–10 (DoD, трассируемость) — Task 15.
- **Плейсхолдеров нет:** каждая задача содержит код или точную структуру; шаги проверяемы.
- **Согласованность имён:** `ScheduleChangeTags.BuildAsync`, `IScheduleViewService.GetDayAsync/GetWeekAsync/GetSemesterAsync/GetMonthAsync`, `fetchDayView/fetchWeekView/fetchSemesterView/fetchMonthView`, `ExportAsync(... scope, date, week, format, layout ...)` — совпадают во всех задачах.
