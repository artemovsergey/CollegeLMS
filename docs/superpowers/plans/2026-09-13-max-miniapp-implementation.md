# MAX mini-app Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Реализовать MAX mini-app и связанные срезы по спец. `docs/superpowers/specs/2026-09-13-max-miniapp-design.md`: API-фундамент (даты, поиск, избранное, уведомления, журнал, dispatcher), новый mini-app shell, deep links бота и синхронизация веба.

**Architecture:** Разделение каналов: mini-app — рабочий интерфейс, бот — вход/уведомления/deep links, CollegeLMS API — единый источник правил и read-моделей. Реализуется в 4 среза: A) API; B) mini-app Next.js; C) MaxBot; D) веб-синхронизация + E2E.

**Tech Stack:** .NET 10 / ASP.NET Core Web API (EF Core + Npgsql, Result<T>, FluentValidation, JWT), Next.js 14 + TS + Tailwind v4 + `@maxhub/max-ui` 0.2.0, Playwright, xUnit + WebApplicationFactory + Moq + Bogus.

## Global Constraints

- Все сервисы/контроллеры/мапперы в `CollegeLMS.API/` — primary constructor DI, `Result<T>` без try-catch в контроллерах/сервисах, `CancellationToken ct` на async-методах, `AsNoTracking()` на чтении, `FindAsync()` по PK.
- Комментарии и сообщения об ошибках — на русском. Форматирование: CSharpier.
- DI регистрации только в `Extensions/ServiceCollectionExtensions.cs` (не в Program.cs). Миграции: `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql`.
- EF Config: `ValueGeneratedNever` для GUID PK, `HasMaxLength` на строках, `HasConversion<string>` на enum, именованные индексы `ix_{table}_{column}` (snake_case), CHECK constraints — в `Data/DbConstraints.cs` (идемпотентный SQL).
- Семестр: `SemesterStart = 2026-09-01`, `TotalWeeks = 16`. Просмотр расписания — `[AllowAnonymous]`; избранное/настройки/журнал/dispatcher — `[Authorize]`.
- JWT: HS256, ключ ≥ 32 байт; роли из `[Flags] UserRole` через `GetRoles()`.
- Дефолты D1–D8 из спец. §8 в силе. Все данные/объяснения на русском.

---

# Срез A — API-фундамент

## Task A1: StudyCalendar в API

**Files:**
- Create: `CollegeLMS.API/Services/StudyCalendar.cs`
- Test: `CollegeLMS.Tests/Unit/Services/StudyCalendarTests.cs`

**Interfaces:**
- Consumes: ничего.
- Produces: `StudyCalendar.SemesterStart` (DateTime), `StudyCalendar.TotalWeeks` (int = 16), `StudyCalendar.MondayOf(DateTime)` → DateTime, `StudyCalendar.WeekOf(DateTime)` → int, `StudyCalendar.IsInSemester(DateTime)` → bool.

> Копия правил из `CollegeLMS.MaxBot/Services/StudyWeek.cs`, чтобы API был источником истины для даты→недели.

- [ ] **Step 1: Написать падающий тест**

```csharp
using CollegeLMS.API.Services;

namespace CollegeLMS.Tests.Unit.Services;

public class StudyCalendarTests
{
    [Theory]
    [InlineData("2026-09-01", "2026-08-31")] // Пн
    [InlineData("2026-09-06", "2026-08-31")] // Вс
    public void MondayOf_ReturnsMonday(string date, string expected)
    {
        var result = StudyCalendar.MondayOf(DateTime.Parse(date));
        Assert.Equal(DateTime.Parse(expected), result.Date);
    }

    [Theory]
    [InlineData("2026-09-01", 1)]
    [InlineData("2026-09-07", 2)]
    [InlineData("2026-12-14", 16)]
    public void WeekOf_ReturnsExpected(string date, int expected)
    {
        Assert.Equal(expected, StudyCalendar.WeekOf(DateTime.Parse(date)));
    }

    [Theory]
    [InlineData("2026-09-01", true)]
    [InlineData("2026-08-31", false)]
    [InlineData("2026-12-21", false)]
    public void IsInSemester_ReturnsExpected(string date, bool expected)
    {
        Assert.Equal(expected, StudyCalendar.IsInSemester(DateTime.Parse(date)));
    }
}
```

- [ ] **Step 2: Запустить тест и убедиться, что падает**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~StudyCalendarTests"`
Expected: FAIL — `StudyCalendar` не найден.

- [ ] **Step 3: Реализовать `StudyCalendar`**

```csharp
namespace CollegeLMS.API.Services;

public static class StudyCalendar
{
    public static DateTime SemesterStart { get; } = new(2026, 9, 1);

    public static int TotalWeeks { get; } = 16;

    public static DateTime MondayOf(DateTime date)
    {
        var day = (int)date.DayOfWeek; // 0 = Воскресенье
        var offset = day == 0 ? 6 : day - 1;
        return date.Date.AddDays(-offset);
    }

    public static int WeekOf(DateTime date)
    {
        var diffWeeks = (int)((MondayOf(date) - MondayOf(SemesterStart)).TotalDays / 7);
        return Math.Max(1, diffWeeks + 1);
    }

    public static bool IsInSemester(DateTime date)
    {
        var week = WeekOf(date);
        return week >= 1 && week <= TotalWeeks;
    }
}
```

- [ ] **Step 4: Убедиться, что тесты проходят**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~StudyCalendarTests"`
Expected: PASS (3 теста).

- [ ] **Step 5: Коммит**

```bash
git add CollegeLMS.API/Services/StudyCalendar.cs CollegeLMS.Tests/Unit/Services/StudyCalendarTests.cs
git commit -m "feat: добавить StudyCalendar в API"
```

## Task A2: Расписание по конкретной дате + meta-эндпоинт

**Files:**
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs:27-58` (GET /api/schedule — параметр `date`)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs:15-71` (`GetAllAsync` + новый метод)
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs`
- Create: `CollegeLMS.API/Dtos/ScheduleMetaDto.cs`, `CollegeLMS.API/Dtos/ScheduleSearchDtos.cs`
- Test: `CollegeLMS.Tests/Integration/Controllers/ScheduleDateApiTests.cs`

**Interfaces:**
- Consumes: `StudyCalendar` (A1).
- Produces: `ScheduleMetaResponse { SemesterStart, TotalWeeks, CurrentWeek, CurrentDate }`; `IScheduleService.GetAllAsync(..., DateTime? date, ...)`; `IScheduleService.GetMetaAsync(CancellationToken)`; `ScheduleSearchResponse { Groups: List<ScheduleSearchGroup>, Teachers: List<ScheduleSearchTeacher>, TotalGroups, TotalTeachers }`; `ScheduleSearchGroup { Id, Name, Course }`, `ScheduleSearchTeacher { Id, FullName, Position }`.

- [ ] **Step 1: DTO**

`CollegeLMS.API/Dtos/ScheduleMetaDto.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class ScheduleMetaResponse
{
    public DateTime SemesterStart { get; set; }
    public int TotalWeeks { get; set; }
    public int CurrentWeek { get; set; }
    public DateTime CurrentDate { get; set; }
}
```

`CollegeLMS.API/Dtos/ScheduleSearchDtos.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class ScheduleSearchGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}

public class ScheduleSearchTeacher
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
}

public class ScheduleSearchResponse
{
    public List<ScheduleSearchGroup> Groups { get; set; } = new();
    public List<ScheduleSearchTeacher> Teachers { get; set; } = new();
    public int TotalGroups { get; set; }
    public int TotalTeachers { get; set; }
}
```

- [ ] **Step 2: Интерфейс**

`CollegeLMS.API/Interfaces/IScheduleService.cs` — добавить:

```csharp
Task<Result<ScheduleMetaResponse>> GetMetaAsync(CancellationToken ct);

Task<Result<ScheduleSearchResponse>> SearchAsync(string? query, int page, int pageSize, CancellationToken ct);

Task<Result<JournalResponse>> GetJournalAsync(Guid teacherId, CancellationToken ct);
```

(`GetAllAsync` дополнить параметром `DateTime? date` в сигнатуре; `JournalResponse` появится в A11 — чтобы компилятор не ломался на этом шаге, сначала определить его пустым в A11; здесь изменить только сигнатуру `GetAllAsync` и добавить `GetMetaAsync` + `SearchAsync`.)

- [ ] **Step 3: Реализация в ScheduleService**

В начало файла добавить `using CollegeLMS.API.Services;` (класс в той же папке — using не нужен, но для StudyCalendar точно нужен, если файл в `Services/`, then same namespace). Методы:

```csharp
public async Task<Result<PagedResponse<ScheduleResponse>>> GetAllAsync(
    Guid? groupId,
    Guid? teacherId,
    string? room,
    DayOfWeek? dayOfWeek,
    string? period,
    int? week,
    DateTime? date,
    string? view,
    int? page,
    int? pageSize,
    CancellationToken ct
)
{
    if (date.HasValue)
    {
        week = StudyCalendar.WeekOf(date.Value);
        dayOfWeek = date.Value.DayOfWeek;
    }

    // ... без изменений далее (существующее тело)
}
```

```csharp
public async Task<Result<ScheduleMetaResponse>> GetMetaAsync(CancellationToken ct)
{
    var now = DateTime.UtcNow;
    return Result<ScheduleMetaResponse>.Ok(
        new ScheduleMetaResponse
        {
            SemesterStart = StudyCalendar.SemesterStart,
            TotalWeeks = StudyCalendar.TotalWeeks,
            CurrentWeek = StudyCalendar.WeekOf(now),
            CurrentDate = now,
        }
    );
}

public async Task<Result<ScheduleSearchResponse>> SearchAsync(
    string? query,
    int page,
    int pageSize,
    CancellationToken ct
)
{
    var q = (query ?? "").Trim().ToLowerInvariant();
    var take = Math.Clamp(pageSize, 1, 50);

    var groups = db
        .Groups.AsNoTracking()
        .Where(g => q.Length == 0 || g.Name.ToLower().Contains(q))
        .OrderBy(g => g.Name)
        .Take(take)
        .Select(g => new ScheduleSearchGroup { Id = g.Id, Name = g.Name, Course = g.Course })
        .ToList();

    var teachers = db
        .Teachers.AsNoTracking()
        .Include(t => t.User)
        .Where(t => q.Length == 0 || t.User.FullName.ToLower().Contains(q))
        .OrderBy(t => t.User.FullName)
        .Take(take)
        .Select(t => new ScheduleSearchTeacher
        {
            Id = t.Id,
            FullName = t.User.FullName,
            Position = t.Position,
        })
        .ToList();

    var totalGroups = db.Groups.Count(g => q.Length == 0 || g.Name.ToLower().Contains(q));
    var totalTeachers = db.Teachers.Count(t => t.User.FullName.ToLower().Contains(q));

    return Result<ScheduleSearchResponse>.Ok(
        new ScheduleSearchResponse
        {
            Groups = groups,
            Teachers = teachers,
            TotalGroups = totalGroups,
            TotalTeachers = totalTeachers,
        }
    );
}
```

> Пустой запрос возвращает первые `pageSize` записей каждого типа — используется для «просмотр всех» без ошибки.

- [ ] **Step 4: Контроллер**

`ScheduleController.cs`:
- строка 29: `[FromQuery] DateTime? date,` между `week` и `view`;
- вызов: `service.GetAllAsync(groupId, teacherId, room, dayOfWeek, period, week, date, view, page, pageSize, ct)`;
- добавить эндпоинты:

```csharp
[HttpGet("meta")]
[AllowAnonymous]
[SwaggerOperation(Summary = "Календарь учебного семестра и текущая неделя")]
[SwaggerResponse(200, "Метаданные получены", typeof(Result<ScheduleMetaResponse>))]
[ProducesResponseType(typeof(Result<ScheduleMetaResponse>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetMeta(CancellationToken ct)
{
    var result = await service.GetMetaAsync(ct);
    return Ok(result);
}

[HttpGet("search")]
[AllowAnonymous]
[EnableRateLimiting("SearchPolicy")]
[SwaggerOperation(Summary = "Поиск групп и преподавателей для расписания")]
[SwaggerResponse(200, "Результаты получены", typeof(Result<ScheduleSearchResponse>))]
[SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
[ProducesResponseType(typeof(Result<ScheduleSearchResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> Search(
    [FromQuery] string? q = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default
)
{
    var result = await service.SearchAsync(q, page, pageSize, ct);
    return Ok(result);
}
```

> Внимание: маршрут `/api/schedule/search` и `/api/schedule/meta` должны регистрироваться ДО `{id:guid}`, иначе Guid-роут перехватит. Порядок действий: meta/search — перед `[HttpGet("{id:guid}")]`. Признак-lite: добавить их сразу после главного `[HttpGet]`.

- [ ] **Step 5: Интеграционный тест (дата + meta)**

`CollegeLMS.Tests/Integration/Controllers/ScheduleDateApiTests.cs` (паттерн — существующий InMemory WebApplicationFactory: см. `CollegeLMS.Tests/Integration/Controllers/*`):

```csharp
using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Response;
using CollegeLMS.API.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleDateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ScheduleDateApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMeta_ReturnsCurrentWeek()
    {
        var resp = await _client.GetAsync("/api/schedule/meta");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Result<ScheduleMetaResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.Equal(16, body.Data!.TotalWeeks);
        Assert.True(body.Data.CurrentWeek >= 1);
    }

    [Fact]
    public async Task GetAll_WithDate_ReturnsEntries()
    {
        // Без данных в InMemory пусто, но ответ 200 и корректный Result
        var resp = await _client.GetAsync("/api/schedule?date=2026-09-07");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Result<PagedResponse<ScheduleResponse>>>();
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task Search_ReturnsGroupAndTeacherBlocks()
    {
        var resp = await _client.GetAsync("/api/schedule/search?q=группа");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Result<ScheduleSearchResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.NotNull(body.Data!.Groups);
        Assert.NotNull(body.Data.Teachers);
    }
}
```

- [ ] **Step 6: Запустить API-тесты**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleDateApiTests"`
Expected: PASS.

> Если `Program` недоступен в тестовом проекте — использовать существующий зарекомендовавший себя класс fixture из соседних интеграционных тестов (скопировать его паттерн; `InternalsVisibleTo` уже настроен).

- [ ] **Step 7: Коммит**

```bash
git add -A
git commit -m "feat: расписание по дате, meta и единый поиск групп/преподавателей"
```

## Task A3: Сущность Favorite + миграция

**Files:**
- Create: `CollegeLMS.API/Entities/Favorite.cs`, `CollegeLMS.API/Entities/Enums/FavoriteTargetType.cs`, `CollegeLMS.API/Data/Configurations/FavoriteConfiguration.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs` (DbSet)
- Migration: `dotnet ef migrations add AddFavorites --project CollegeLMS.API -- --provider Npgsql`

**Interfaces:**
- Produces: `Favorite : Entity { UserId, TargetType, TargetId }`, `enum FavoriteTargetType { Group, Teacher }`.

- [ ] **Step 1: Enum**

`CollegeLMS.API/Entities/Enums/FavoriteTargetType.cs`:

```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum FavoriteTargetType
{
    Group,
    Teacher,
}
```

- [ ] **Step 2: Entity**

`CollegeLMS.API/Entities/Favorite.cs`:

```csharp
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Favorite : Entity
{
    public Guid UserId { get; set; }
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
}
```

- [ ] **Step 3: Конфигурация**

`CollegeLMS.API/Data/Configurations/FavoriteConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);

        builder
            .HasIndex(x => new { x.UserId, x.TargetType, x.TargetId })
            .IsUnique()
            .HasDatabaseName("ux_favorites_user_target");
    }
}
```

- [ ] **Step 4: DbContext**

В `AppDbContext.cs` добавить:

```csharp
public DbSet<Favorite> Favorites => Set<Favorite>();
```

- [ ] **Step 5: Миграция**

Run: `dotnet ef migrations add AddFavorites --project CollegeLMS.API -- --provider Npgsql`
Expected: создан файл миграции `*_AddFavorites.cs`, `dotnet build` проходит.

- [ ] **Step 6: Коммит**

```bash
git add -A
git commit -m "feat: сущность Favorite и миграция"
```

## Task A4: Favorites API (сервис + контроллер)

**Files:**
- Create: `CollegeLMS.API/Dtos/FavoriteDtos.cs`, `CollegeLMS.API/Interfaces/IFavoritesService.cs`, `CollegeLMS.API/Services/FavoritesService.cs`, `CollegeLMS.API/Controllers/FavoritesController.cs`, `CollegeLMS.API/Mappers/FavoriteMapper.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (регистрация)
- Test: `CollegeLMS.Tests/Integration/Controllers/FavoritesApiTests.cs`

**Interfaces:**
- Consumes: `Favorite`, `FavoriteTargetType`, `User.GetUserId()`.
- Produces: `IFavoritesService.GetAllAsync(Guid userId, CancellationToken) → Result<List<FavoriteResponse>>`; `AddAsync(Guid userId, AddFavoriteRequest, ct) → Result<FavoriteResponse>`; `DeleteAsync(Guid userId, Guid id, ct) → Result`.
- DTOs: `FavoriteResponse { Id, TargetType, TargetId, GroupName?, TeacherName? }`; `AddFavoriteRequest { TargetType, TargetId }`.

- [ ] **Step 1: DTOs**

`CollegeLMS.API/Dtos/FavoriteDtos.cs`:

```csharp
using CollegeLMS.API.Entities.Enums;
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Dtos;

public class FavoriteResponse
{
    public Guid Id { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string? GroupName { get; set; }
    public string? TeacherName { get; set; }
}

public class AddFavoriteRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
}
```

- [ ] **Step 2: Интерфейс + реализация**

`CollegeLMS.API/Interfaces/IFavoritesService.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IFavoritesService
{
    Task<Result<List<FavoriteResponse>>> GetAllAsync(Guid userId, CancellationToken ct);
    Task<Result<FavoriteResponse>> AddAsync(Guid userId, AddFavoriteRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}
```

`CollegeLMS.API/Services/FavoritesService.cs`:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class FavoritesService(AppDbContext db) : IFavoritesService
{
    public async Task<Result<List<FavoriteResponse>>> GetAllAsync(Guid userId, CancellationToken ct)
    {
        var items = await db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        var result = new List<FavoriteResponse>();
        foreach (var f in items)
        {
            var dto = f.ToDto();
            if (f.TargetType == FavoriteTargetType.Group)
            {
                var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == f.TargetId, ct);
                dto.GroupName = group?.Name;
            }
            else
            {
                var teacher = await db.Teachers
                    .AsNoTracking()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == f.TargetId, ct);
                dto.TeacherName = teacher?.User.FullName;
            }
            result.Add(dto);
        }
        return Result<List<FavoriteResponse>>.Ok(result);
    }

    public async Task<Result<FavoriteResponse>> AddAsync(Guid userId, AddFavoriteRequest request, CancellationToken ct)
    {
        if (request.TargetType == FavoriteTargetType.Group)
        {
            if (!await db.Groups.AnyAsync(g => g.Id == request.TargetId, ct))
                return Result<FavoriteResponse>.Fail("Группа не найдена", 404);
        }
        else
        {
            if (!await db.Teachers.AnyAsync(t => t.Id == request.TargetId, ct))
                return Result<FavoriteResponse>.Fail("Преподаватель не найден", 404);
        }

        var existing = await db.Favorites.FirstOrDefaultAsync(
            f => f.UserId == userId && f.TargetType == request.TargetType && f.TargetId == request.TargetId,
            ct
        );
        if (existing is not null)
        {
            var dto = existing.ToDto();
            dto.TargetType = request.TargetType;
            await EnrichAsync(dto, ct);
            return Result<FavoriteResponse>.Ok(dto);
        }

        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Favorites.Add(favorite);
        await db.SaveChangesAsync(ct);

        var created = favorite.ToDto();
        await EnrichAsync(created, ct);
        return Result<FavoriteResponse>.Ok(created);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            ct
        );
        if (favorite is null)
            return Result.Fail("Избранное не найдено", 404);

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task EnrichAsync(FavoriteResponse dto, CancellationToken ct)
    {
        if (dto.TargetType == FavoriteTargetType.Group)
        {
            var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == dto.TargetId, ct);
            dto.GroupName = group?.Name;
        }
        else
        {
            var teacher = await db.Teachers.AsNoTracking().Include(t => t.User).FirstOrDefaultAsync(t => t.Id == dto.TargetId, ct);
            dto.TeacherName = teacher?.User.FullName;
        }
    }
}
```

- [ ] **Step 3: Маппер**

`CollegeLMS.API/Mappers/FavoriteMapper.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class FavoriteMapper
{
    public static FavoriteResponse ToDto(this Favorite favorite) =>
        new()
        {
            Id = favorite.Id,
            TargetType = favorite.TargetType,
            TargetId = favorite.TargetId,
        };
}
```

- [ ] **Step 4: Контроллер**

`CollegeLMS.API/Controllers/FavoritesController.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Produces("application/json")]
[Authorize]
public class FavoritesController(IFavoritesService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Список избранного текущего пользователя")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<FavoriteResponse>>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<List<FavoriteResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Добавить группу или преподавателя в избранное")]
    [SwaggerResponse(200, "Добавлено", typeof(Result<FavoriteResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Объект не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<FavoriteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(AddFavoriteRequest request, CancellationToken ct)
    {
        var result = await service.AddAsync(User.GetUserId(), request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить запись избранного")]
    [SwaggerResponse(200, "Удалено", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Не найдено", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(User.GetUserId(), id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

- [ ] **Step 5: DI**

В `ServiceCollectionExtensions.AddApplicationServices` добавить:

```csharp
services.AddScoped<IFavoritesService, FavoritesService>();
```

- [ ] **Step 6: Интеграционный тест**

`CollegeLMS.Tests/Integration/Controllers/FavoritesApiTests.cs` — использовать ту же fixture, что и соседние файлы:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CollegeLMS.Tests.Integration.Controllers;

public class FavoritesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FavoritesApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private string? _token;
    private async Task<string> GetTokenAsync()
    {
        if (_token is not null) return _token;
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { login = "admin", password = "admin" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<Result<LoginResponse>>();
        _token = body!.Data!.Token;
        return _token!;
    }

    [Fact]
    public async Task Favorite_RequiresAuth()
    {
        var resp = await _client.GetAsync("/api/favorites");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task AddThenListThenDelete()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstGroup = await _client.GetFromJsonAsync<Result<List<GroupResponse>>>("/api/groups");
        var groupId = firstGroup!.Data!.First().Id;

        var add = await _client.PostAsJsonAsync("/api/favorites", new { targetType = "Group", targetId = groupId });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var addBody = await add.Content.ReadFromJsonAsync<Result<FavoriteResponse>>();
        Assert.True(addBody!.IsSuccess);
        Assert.NotNull(addBody.Data!.GroupName);

        var list = await _client.GetAsync("/api/favorites");
        var listBody = await list.Content.ReadFromJsonAsync<Result<List<FavoriteResponse>>>();
        Assert.True(listBody!.IsSuccess);
        Assert.Contains(listBody.Data!, f => f.Id == addBody.Data!.Id);

        var delete = await _client.DeleteAsync($"/api/favorites/{addBody.Data!.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        var list2 = await _client.GetAsync("/api/favorites");
        var list2Body = await list2.Content.ReadFromJsonAsync<Result<List<FavoriteResponse>>>();
        Assert.DoesNotContain(list2Body!.Data!, f => f.Id == addBody.Data!.Id);
    }
}
```

> `LoginResponse`/`GroupResponse` — существующие DTO во фронтенд-DTO API (`CollegeLMS.API.Dtos`). Заменить на реальные типы при компиляции, если имена отличаются.

- [ ] **Step 7: Прогнать тесты**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~FavoritesApiTests"`
Expected: PASS.

- [ ] **Step 8: Коммит**

```bash
git add -A
git commit -m "feat: API избранного (список, добавление, удаление)"
```

## Task A5: Сущность NotificationSettings + миграция

**Files:**
- Create: `CollegeLMS.API/Entities/NotificationSettings.cs`, `CollegeLMS.API/Data/Configurations/NotificationSettingsConfiguration.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs`
- Migration: `dotnet ef migrations add AddNotificationSettings --project CollegeLMS.API -- --provider Npgsql`

**Interfaces:**
- Produces: `NotificationSettings : Entity { UserId, Enabled, Time (TimeSpan), Days (List<int>) }`.

- [ ] **Step 1: Entity**

```csharp
namespace CollegeLMS.API.Entities;

public class NotificationSettings : Entity
{
    public Guid UserId { get; set; }
    public bool Enabled { get; set; } = true;
    public TimeSpan Time { get; set; } = new(7, 30, 0);
    public List<int> Days { get; set; } = new() { 1, 2, 3, 4, 5 };
}
```

- [ ] **Step 2: Конфигурация**

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("notification_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Time).HasColumnType("interval");
        builder.Property(x => x.Days).HasColumnType("integer[]");

        builder
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("ux_notification_settings_user_id");
    }
}
```

- [ ] **Step 3: DbContext + миграция**

Добавить `public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();` и выполнить:
Run: `dotnet ef migrations add AddNotificationSettings --project CollegeLMS.API -- --provider Npgsql`

- [ ] **Step 4: CHECK-констрейнт времени в DbConstraints.cs**

Добавить в `EnsureAsync`:

```csharp
await db.Database.ExecuteSqlRawAsync(
    """
    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_notification_settings_time_range') THEN
            ALTER TABLE notification_settings
            ADD CONSTRAINT ck_notification_settings_time_range
            CHECK (time >= INTERVAL '7 hours 30 minutes' AND time <= INTERVAL '8 hours 30 minutes');
        END IF;
    END $$;
    """
);
```

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: сущность NotificationSettings и миграция"
```

## Task A6: NotificationSettings API

**Files:**
- Create: `CollegeLMS.API/Dtos/NotificationSettingsDtos.cs`, `CollegeLMS.API/Interfaces/INotificationSettingsService.cs`, `CollegeLMS.API/Services/NotificationSettingsService.cs`, `CollegeLMS.API/Controllers/NotificationSettingsController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`
- Test: `CollegeLMS.Tests/Integration/Controllers/NotificationSettingsApiTests.cs`

**Interfaces:**
- Produces: `INotificationSettingsService.GetAsync(Guid userId, ct) → Result<NotificationSettingsResponse>`; `UpdateAsync(Guid userId, UpdateNotificationSettingsRequest, ct) → Result<NotificationSettingsResponse>`.
- `NotificationSettingsResponse { Enabled, Time (string "HH:mm"), Days, NextNotifyAt (DateTime?) }`; `UpdateNotificationSettingsRequest { Enabled, Time, Days }`.
- Правила: 07:30 ≤ Time ≤ 08:30, `minutes % 5 == 0`, дни из 1..7 (Пн..Вс).

- [ ] **Step 1: DTO**

```csharp
namespace CollegeLMS.API.Dtos;

public class NotificationSettingsResponse
{
    public bool Enabled { get; set; }
    public string Time { get; set; } = "07:30";
    public List<int> Days { get; set; } = new();
    public DateTime? NextNotifyAt { get; set; }
}

public class UpdateNotificationSettingsRequest
{
    public bool Enabled { get; set; }
    public string Time { get; set; } = "07:30";
    public List<int> Days { get; set; } = new();
}
```

- [ ] **Step 2: Интерфейс**

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface INotificationSettingsService
{
    Task<Result<NotificationSettingsResponse>> GetAsync(Guid userId, CancellationToken ct);
    Task<Result<NotificationSettingsResponse>> UpdateAsync(
        Guid userId,
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    );
}
```

- [ ] **Step 3: Реализация**

```csharp
using System.Globalization;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class NotificationSettingsService(AppDbContext db) : INotificationSettingsService
{
    private static readonly TimeSpan MinTime = new(7, 30, 0);
    private static readonly TimeSpan MaxTime = new(8, 30, 0);

    public async Task<Result<NotificationSettingsResponse>> GetAsync(Guid userId, CancellationToken ct)
    {
        var settings = await EnsureAsync(userId, ct);
        var next = NextNotifyAt(settings.Time, settings.Days);
        return Result<NotificationSettingsResponse>.Ok(
            new NotificationSettingsResponse
            {
                Enabled = settings.Enabled,
                Time = settings.Time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                Days = settings.Days.OrderBy(d => d).ToList(),
                NextNotifyAt = settings.Enabled ? next : null,
            }
        );
    }

    public async Task<Result<NotificationSettingsResponse>> UpdateAsync(
        Guid userId,
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    )
    {
        if (!TimeSpan.TryParseExact(request.Time, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
            return Result<NotificationSettingsResponse>.Fail("Время должно быть в формате ЧЧ:ММ", 400);

        var totalMinutes = (int)time.TotalMinutes;
        if (time < MinTime || time > MaxTime)
            return Result<NotificationSettingsResponse>.Fail(
                "Время дайджеста должно быть от 07:30 до 08:30",
                400
            );
        if (totalMinutes % 5 != 0)
            return Result<NotificationSettingsResponse>.Fail(
                "Время дайджеста выбирается с шагом 5 минут",
                400
            );
        if (request.Days.Count == 0)
            return Result<NotificationSettingsResponse>.Fail("Выберите хотя бы один день недели", 400);
        if (request.Days.Any(d => d is < 1 or > 7))
            return Result<NotificationSettingsResponse>.Fail("Дни недели должны быть от 1 (Пн) до 7 (Вс)", 400);

        var settings = await EnsureAsync(userId, ct);
        settings.Enabled = request.Enabled;
        settings.Time = time;
        settings.Days = request.Days.Distinct().OrderBy(d => d).ToList();
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<NotificationSettingsResponse>.Ok(
            new NotificationSettingsResponse
            {
                Enabled = settings.Enabled,
                Time = settings.Time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                Days = settings.Days,
                NextNotifyAt = settings.Enabled ? NextNotifyAt(settings.Time, settings.Days) : null,
            }
        );
    }

    private async Task<NotificationSettings> EnsureAsync(Guid userId, CancellationToken ct)
    {
        var existing = await db.NotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (existing is not null)
            return existing;

        var created = new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Enabled = true,
            Time = new TimeSpan(7, 30, 0),
            Days = new List<int> { 1, 2, 3, 4, 5 },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.NotificationSettings.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    private static DateTime? NextNotifyAt(TimeSpan time, List<int> days)
    {
        if (days.Count == 0)
            return null;

        var now = DateTime.UtcNow;
        for (var offset = 0; offset <= 14; offset++)
        {
            var candidate = now.Date.AddDays(offset).Add(time);
            var dayIndex = ((int)candidate.DayOfWeek == 0) ? 7 : (int)candidate.DayOfWeek;
            if (days.Contains(dayIndex) && candidate > now)
                return candidate;
        }
        return null;
    }
}
```

- [ ] **Step 4: Контроллер**

```csharp
[ApiController]
[Route("api/notifications/settings")]
[Produces("application/json")]
[Authorize]
public class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Настройки уведомлений текущего пользователя")]
    [SwaggerResponse(200, "Настройки получены", typeof(Result<NotificationSettingsResponse>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NotificationSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await service.GetAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Обновить настройки уведомлений")]
    [SwaggerResponse(200, "Настройки обновлены", typeof(Result<NotificationSettingsResponse>))]
    [SwaggerResponse(400, "Некорректные настройки", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NotificationSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(User.GetUserId(), request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

- [ ] **Step 5: DI + тест**

Добавить `services.AddScoped<INotificationSettingsService, NotificationSettingsService>();`.

Тест `CollegeLMS.Tests/Integration/Controllers/NotificationSettingsApiTests.cs`:

```csharp
[Fact]
public async Task Update_ValidatesTimeBounds()
{
    var token = await GetTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var bad = await _client.PutAsJsonAsync("/api/notifications/settings",
        new { enabled = true, time = "09:00", days = new[] { 1 } });
    var badBody = await bad.Content.ReadFromJsonAsync<Result<NotificationSettingsResponse>>();
    Assert.False(badBody!.IsSuccess);

    var notStep = await _client.PutAsJsonAsync("/api/notifications/settings",
        new { enabled = true, time = "07:33", days = new[] { 1 } });
    var notStepBody = await notStep.Content.ReadFromJsonAsync<Result<NotificationSettingsResponse>>();
    Assert.False(notStepBody!.IsSuccess);
}

[Fact]
public async Task Update_SetsNextNotifyAt()
{
    var token = await GetTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var ok = await _client.PutAsJsonAsync("/api/notifications/settings",
        new { enabled = true, time = "07:45", days = new[] { 1, 3, 5 } });
    var okBody = await ok.Content.ReadFromJsonAsync<Result<NotificationSettingsResponse>>();
    Assert.True(okBody!.IsSuccess);
    Assert.Equal("07:45", okBody.Data!.Time);
    Assert.Equal(new[] { 1, 3, 5 }, okBody.Data!.Days);
    Assert.NotNull(okBody.Data.NextNotifyAt);
}
```

- [ ] **Step 6: Прогнать тесты и закоммитить**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~NotificationSettingsApiTests"`
Expected: PASS.

```bash
git add -A
git commit -m "feat: API настроек уведомлений (границы времени, nextNotifyAt)"
```

## Task A7: ProfileResponse.TeacherId + эндпоинт контекста расписания

**Files:**
- Modify: `CollegeLMS.API/Dtos/ProfileResponse.cs` (добавить `TeacherId`)
- Modify: `CollegeLMS.API/Services/AuthService.cs` (`GetProfileAsync`)
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs` (GET /api/schedule/context)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs`, `CollegeLMS.API/Interfaces/IScheduleService.cs`
- Test: `CollegeLMS.Tests/Integration/Controllers/ScheduleContextApiTests.cs`

**Interfaces:**
- Produces: `ProfileResponse.TeacherData.TeacherId` (Guid?); `GET /api/schedule/context` → `Result<ScheduleContextResponse { TeacherId?, TeacherName?, GroupId?, GroupName?, Role }>`.

- [ ] **Step 1: DTO и AuthService**

`ProfileResponse.cs` — в `TeacherProfileData` добавить `public Guid? TeacherId { get; set; }`.

`AuthService.GetProfileAsync` — перед построением `TeacherData` доставить id:

```csharp
if (user.Role.HasRole(UserRole.Teacher) || user.Role.HasRole(UserRole.Admin) || user.Role.HasRole(UserRole.Dispatcher))
{
    var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == user.Id, ct);
    profile.TeacherData = new TeacherProfileData
    {
        TeacherId = teacher?.Id,
        CyclicalCommission = teacher?.CyclicalCommission ?? profile.TeacherData?.CyclicalCommission ?? string.Empty,
        Position = teacher?.Position ?? profile.TeacherData?.Position ?? string.Empty,
    };
}
```

> Учесть текущую логику построения `TeacherData`/`StudentData` в AuthService и дополнить её, не ломая старое поведение.

- [ ] **Step 2: ScheduleContextResponse + сервис**

В `CollegeLMS.API/Dtos/ScheduleMetaDto.cs` добавить:

```csharp
public class ScheduleContextResponse
{
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Role { get; set; } = string.Empty;
}
```

`IScheduleService` добавить `Task<Result<ScheduleContextResponse>> GetContextAsync(Guid userId, CancellationToken ct);`.

`ScheduleService`:

```csharp
public async Task<Result<ScheduleContextResponse>> GetContextAsync(Guid userId, CancellationToken ct)
{
    var student = await db.Students
        .AsNoTracking()
        .Include(s => s.Group)
        .FirstOrDefaultAsync(s => s.UserId == userId, ct);
    if (student is not null)
    {
        return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse
        {
            GroupId = student.GroupId,
            GroupName = student.Group?.Name ?? string.Empty,
            Role = "Student",
        });
    }

    var teacher = await db.Teachers
        .AsNoTracking()
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.UserId == userId, ct);
    if (teacher is not null)
    {
        return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.User.FullName,
            Role = "Teacher",
        });
    }

    return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse { Role = "Other" });
}
```

- [ ] **Step 3: Контроллер (после GET /api/schedule, до {id:guid})**

```csharp
[HttpGet("context")]
[Authorize]
[SwaggerOperation(Summary = "Личный контекст расписания текущего пользователя")]
[SwaggerResponse(200, "Контекст получен", typeof(Result<ScheduleContextResponse>))]
[SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
[ProducesResponseType(typeof(Result<ScheduleContextResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetContext(CancellationToken ct)
{
    var result = await service.GetContextAsync(User.GetUserId(), ct);
    return Ok(result);
}
```

- [ ] **Step 4: Тест**

`ScheduleContextApiTests.cs` — авторизованный запрос `/api/schedule/context` вернёт 200 и `isSuccess == true`; неавторизованный — 401.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: teacherId в профиле и эндпоинт контекста расписания"
```

## Task A8: Dispatcher password + короткая dispatcher-сессия

**Files:**
- Create: `CollegeLMS.API/Entities/DispatcherCredential.cs`, `CollegeLMS.API/Data/Configurations/DispatcherCredentialConfiguration.cs`, `CollegeLMS.API/Dtos/DispatcherDtos.cs`, `CollegeLMS.API/Interfaces/IDispatcherAuthService.cs`, `CollegeLMS.API/Services/DispatcherAuthService.cs`, `CollegeLMS.API/Controllers/DispatcherAuthController.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs`, `CollegeLMS.API/Services/JwtTokenService.cs` (+ интерфейс `ITokenService`), `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`, `CollegeLMS.API/Data/DataSeeder.cs` (seed)
- Migration: `dotnet ef migrations add AddDispatcherCredential --project CollegeLMS.API -- --provider Npgsql`
- Test: `CollegeLMS.Tests/Unit/Services/DispatcherAuthServiceTests.cs`

**Interfaces:**
- Produces: `DispatcherCredential : Entity { PasswordHash }`; `IDispatcherAuthService.LoginAsync(string password, string clientIp, CancellationToken) → Result<DispatcherLoginResponse{ Token, ExpiresAt }>`.
- `ITokenService` расширяется методом `string GenerateCustomToken(IReadOnlyCollection<string> roles, int lifetimeMinutes, string nameIdentifier)`.
- Rate limit: 5 неудачных подряд → lockout 15 мин (in-memory по IP). TTL токена 30 мин.
- Seed: один `DispatcherCredential` с BCrypt-хэшем пароля из конфигурации `Dispatcher:Password` (fallback "dispatcher").

- [ ] **Step 1: Entity + конфигурация + DbContext + миграция**

```csharp
namespace CollegeLMS.API.Entities;

public class DispatcherCredential : Entity
{
    public string PasswordHash { get; set; } = string.Empty;
}
```

Конфигурация: `ToTable("dispatcher_credentials")`, `HasKey`, `ValueGeneratedNever`, `PasswordHash.HasMaxLength(200)`.

Добавить `public DbSet<DispatcherCredential> DispatcherCredentials => Set<DispatcherCredential>();`.

Run: `dotnet ef migrations add AddDispatcherCredential --project CollegeLMS.API -- --provider Npgsql`

- [ ] **Step 2: Расширить ITokenService и JwtTokenService**

`ITokenService` добавить:

```csharp
string GenerateCustomToken(IReadOnlyCollection<string> roles, int lifetimeMinutes, string nameIdentifier);
```

`JwtTokenService`:

```csharp
public string GenerateCustomToken(IReadOnlyCollection<string> roles, int lifetimeMinutes, string nameIdentifier)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, nameIdentifier),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };
    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
    var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"] ?? "CollegeLMS",
        audience: config["Jwt:Audience"] ?? "CollegeLMS.Clients",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
        signingCredentials: creds
    );
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

- [ ] **Step 3: DTO**

```csharp
namespace CollegeLMS.API.Dtos;

public class DispatcherLoginRequest
{
    public string Password { get; set; } = string.Empty;
}

public class DispatcherLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
```

- [ ] **Step 4: Сервис**

```csharp
using System.Collections.Concurrent;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DispatcherAuthService(AppDbContext db, ITokenService tokens, IConfiguration config)
    : IDispatcherAuthService
{
    private static readonly ConcurrentDictionary<string, AttemptState> Attempts = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    private sealed record AttemptState(int Count, DateTime LockedUntil);

    public async Task<Result<DispatcherLoginResponse>> LoginAsync(
        string password,
        string clientIp,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        var state = Attempts.GetOrAdd(clientIp, new AttemptState(0, DateTime.MinValue));

        var updated = state with { LockedUntil = state.LockedUntil > now ? state.LockedUntil : DateTime.MinValue };
        if (updated is not { } current)
            current = updated;
        if (current.LockedUntil > now)
        {
            var wait = (current.LockedUntil - now).TotalMinutes;
            return Result<DispatcherLoginResponse>.Fail(
                $"Слишком много попыток. Повторите через {Math.Ceiling(wait)} мин",
                429
            );
        }

        var credential = await db.DispatcherCredentials.AsNoTracking().FirstOrDefaultAsync(ct);
        if (credential is null || !BCrypt.Net.BCrypt.Verify(password, credential.PasswordHash))
        {
            var incremented = current with { Count = current.Count + 1 };
            if (incremented.Count >= MaxAttempts)
                incremented = incremented with { Count = 0, LockedUntil = now.Add(Lockout) };
            Attempts[clientIp] = incremented;
            return Result<DispatcherLoginResponse>.Fail("Неверный пароль диспетчера", 401);
        }

        Attempts[clientIp] = current with { Count = 0 };

        var token = tokens.GenerateCustomToken(["Dispatcher"], 30, $"dispatcher-{Guid.NewGuid():N}");
        return Result<DispatcherLoginResponse>.Ok(
            new DispatcherLoginResponse { Token = token, ExpiresAt = DateTime.UtcNow.AddMinutes(30) }
        );
    }
}
```

- [ ] **Step 5: Контроллер**

```csharp
[ApiController]
[Route("api/dispatcher")]
[Produces("application/json")]
public class DispatcherAuthController(IDispatcherAuthService service) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [SwaggerOperation(Summary = "Вход диспетчера по паролю — короткий dispatcher-токен")]
    [SwaggerResponse(200, "Токен выдан", typeof(Result<DispatcherLoginResponse>))]
    [SwaggerResponse(401, "Неверный пароль", typeof(ErrorResponse))]
    [SwaggerResponse(429, "Слишком много попыток", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<DispatcherLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(DispatcherLoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await service.LoginAsync(request.Password, ip, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

- [ ] **Step 6: DI + Seed**

`AddApplicationServices`: `services.AddScoped<IDispatcherAuthService, DispatcherAuthService>();`

`DataSeeder`:

```csharp
private static async Task SeedDispatcherCredentialAsync(AppDbContext db, IConfiguration config)
{
    var password = config["Dispatcher:Password"] ?? "dispatcher";
    if (await db.DispatcherCredentials.AnyAsync())
        return;
    db.DispatcherCredentials.Add(new DispatcherCredential
    {
        Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    });
    await db.SaveChangesAsync();
}
```

Вызвать в `SeedAsync` (передав конфиг, либо считывая `config` внутри — согласовать с текущей сигнатурой `SeedAsync(db)`; проще внедрить `IConfiguration` в `DataSeeder` или передать `default` через `new ConfigurationBuilder` — использовать существующий стиль файла).

- [ ] **Step 7: Юнит-тест rate limit**

`CollegeLMS.Tests/Unit/Services/DispatcherAuthServiceTests.cs` (Moq: `ITokenService`, InMemory `AppDbContext`, stub `IConfiguration`). Логика: 5 неверных паролей → 6-я попытка возвращает 429; с верным паролем после сброса — токен. Хэш в БД: `BCrypt.HashPassword("secret")`.

- [ ] **Step 8: Прогнать тесты и закоммитить**

Run: `dotnet test CollegeLMS.Tests`
Expected: PASS.

```bash
git add -A
git commit -m "feat: dispatcher password flow с rate limit и короткой сессией"
```

## Task A9: Идемпотентное подтверждение корректировок

**Files:**
- Create: `CollegeLMS.API/Entities/CorrectionConfirmation.cs`, `CollegeLMS.API/Data/Configurations/CorrectionConfirmationConfiguration.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs`, `CollegeLMS.API/Interfaces/IScheduleCorrectionService.cs`, `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (`ConfirmAsync`), `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs`
- Migration: `dotnet ef migrations add AddCorrectionConfirmations --project CollegeLMS.API -- --provider Npgsql`
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionIdempotencyTests.cs`

**Interfaces:**
- Produces: `IScheduleCorrectionService.ConfirmAsync(CorrectionConfirmRequest request, Guid appliedByUserId, string idempotencyKey, CancellationToken)` — при повторном ключе возвращает сохранённый результат без переприменения.
- `CorrectionConfirmation : Entity { IdempotencyKey (string), HistoryCount (int) }`.

- [ ] **Step 1: Entity + конфигурация + миграция**

```csharp
namespace CollegeLMS.API.Entities;

public class CorrectionConfirmation : Entity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public int HistoryCount { get; set; }
}
```

Конфигурация: таблица `correction_confirmations`, `IdempotencyKey.HasMaxLength(64)`, unique-индекс `ux_correction_confirmations_idempotency_key`; `HistoryCount` required.

`DbSet<CorrectionConfirmation> CorrectionConfirmations => Set<CorrectionConfirmation>();`

Run: `dotnet ef migrations add AddCorrectionConfirmations --project CollegeLMS.API -- --provider Npgsql`

- [ ] **Step 2: Сигнатура + реализация**

`IScheduleCorrectionService`:

```csharp
Task<Result<CorrectionConfirmResult>> ConfirmAsync(
    CorrectionConfirmRequest request,
    Guid appliedByUserId,
    string idempotencyKey,
    CancellationToken ct
);
```

`ScheduleCorrectionService.ConfirmAsync` — в начале метода:

```csharp
if (string.IsNullOrWhiteSpace(idempotencyKey))
    return Result<CorrectionConfirmResult>.Fail("Заголовок Idempotency-Key обязателен", 400);

var existing = await db.CorrectionConfirmations.AsNoTracking()
    .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, ct);
if (existing is not null)
    return Result<CorrectionConfirmResult>.Fail(
        $"Корректировка уже применена (ключ {idempotencyKey})",
        409
    );

if (request.Entries.Count == 0)
    return Result<CorrectionConfirmResult>.Ok(new CorrectionConfirmResult { Applied = 0, History = [] });
```

После `CommitAsync` (до уведомления MaxBot):

```csharp
db.CorrectionConfirmations.Add(new CorrectionConfirmation
{
    Id = Guid.NewGuid(),
    IdempotencyKey = idempotencyKey,
    HistoryCount = history.Count,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
});
await db.SaveChangesAsync(ct);
```

> 409 вторичной подачи — сервер остаётся источником истины, повторное применение исключено.

- [ ] **Step 3: Контроллер**

`ScheduleCorrectionController.ConfirmCorrection` — прочитать заголовок и передать:

```csharp
var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
var result = await service.ConfirmAsync(request, appliedByUserId, idempotencyKey, ct);
```

Обновить Swagger: добавить `<response code="409">Ключ уже использован — корректировка была применена ранее</response>` и `[SwaggerResponse(409, ...)]`, `[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]`.

- [ ] **Step 4: Юнит-тест**

InMemory-контекст с одной `ScheduleEntry` (можно без неё — проверить именно ключ): вызвать `ConfirmAsync` дважды с одним ключом и пустым `Entries`; второй вызов должен вернуть 409. Для этого даже с пустыми Entries ключ сохраняется — наше условие `if (existing is not null) return ...409;` идёт до кратного `Entries.Count == 0`, значит сохранение ключа нужно для случая `Entries.Count == 0` тоже. Поправить: проводить проверку и сохранять ключ в любом случае. Реализовать так:

```csharp
var existing = ...;
if (existing is not null) return ...409;

if (request.Entries.Count == 0)
{
    db.CorrectionConfirmations.Add(new CorrectionConfirmation { IdempotencyKey = idempotencyKey, HistoryCount = 0, ... });
    await db.SaveChangesAsync(ct);
    return Result<CorrectionConfirmResult>.Ok(new CorrectionConfirmResult { Applied = 0, History = [] });
}

// ... основной цикл
```

- [ ] **Step 5: Прогнать тесты и закоммитить**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleCorrectionIdempotencyTests"`
Expected: PASS.

```bash
git add -A
git commit -m "feat: идемпотентное подтверждение корректировок (Idempotency-Key)"
```

## Task A10: Журнал преподавателя

**Files:**
- Create: `CollegeLMS.API/Dtos/JournalDtos.cs` (если не создан в A2 — создать здесь), `CollegeLMS.API/Services/Journal`-методы в `ScheduleService.cs`
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs` (`GetJournalAsync`), `CollegeLMS.API/Controllers/ScheduleController.cs`
- Test: `CollegeLMS.Tests/Unit/Services/JournalServiceTests.cs`

**Interfaces:**
- Produces: `GetJournalAsync(Guid teacherId, ct) → Result<JournalResponse>`.
- `JournalResponse { TeacherId, TeacherName, Subjects: List<JournalSubjectGroup>, TotalPairCount }`; `JournalSubjectGroup { Subject, Items: List<JournalEntryItem>, PairCount }`; `JournalEntryItem { Week, Date, NumberPairs: List<int> }`.

- [ ] **Step 1: DTO**

```csharp
namespace CollegeLMS.API.Dtos;

public class JournalResponse
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public List<JournalSubjectGroup> Subjects { get; set; } = new();
    public int TotalPairCount { get; set; }
}

public class JournalSubjectGroup
{
    public string Subject { get; set; } = string.Empty;
    public List<JournalEntryItem> Items { get; set; } = new();
    public int PairCount { get; set; }
}

public class JournalEntryItem
{
    public int Week { get; set; }
    public DateTime Date { get; set; }
    public List<int> NumberPairs { get; set; } = new();
}
```

- [ ] **Step 2: Реализация в ScheduleService**

```csharp
public async Task<Result<JournalResponse>> GetJournalAsync(Guid teacherId, CancellationToken ct)
{
    var teacher = await db.Teachers.AsNoTracking().Include(t => t.User).FirstOrDefaultAsync(t => t.Id == teacherId, ct);
    if (teacher is null)
        return Result<JournalResponse>.Fail("Преподаватель не найден", 404);

    var entries = await db.ScheduleEntries
        .AsNoTracking()
        .Where(e => e.TeacherId == teacherId)
        .ToListAsync(ct);

    var itemMap = new Dictionary<(string Subject, int Week), List<int>>();
    foreach (var e in entries)
    {
        foreach (var week in e.Weeks.Where(w => w >= 1 && w <= StudyCalendar.TotalWeeks))
        {
            var key = (e.Subject, week);
            if (!itemMap.TryGetValue(key, out var pairs))
            {
                pairs = new List<int>();
                itemMap[key] = pairs;
            }
            pairs.Add(e.NumberPair);
        }
    }

    var subjects = itemMap
        .OrderBy(x => x.Key.Subject)
        .GroupBy(x => x.Key.Subject)
        .Select(g =>
        {
            var items = g
                .OrderBy(x => x.Key.Week)
                .Select(x => new JournalEntryItem
                {
                    Week = x.Key.Week,
                    Date = StudyCalendar.MondayOf(StudyCalendar.SemesterStart)
                        .AddDays((x.Key.Week - 1) * 7 + ((int)firstDayOfEntry(x.Key) - 1)),
                    NumberPairs = x.Value.Distinct().OrderBy(v => v).ToList(),
                })
                .ToList();
            return new JournalSubjectGroup
            {
                Subject = g.Key,
                Items = items,
                PairCount = items.Sum(i => i.NumberPairs.Count),
            };
        })
        .ToList();

    return Result<JournalResponse>.Ok(new JournalResponse
    {
        TeacherId = teacherId,
        TeacherName = teacher.User.FullName,
        Subjects = subjects,
        TotalPairCount = subjects.Sum(s => s.PairCount),
    });

    static DayOfWeek firstDayOfEntry((string Subject, int Week) key) => DayOfWeek.Monday;
}
```

> Примечание: в MVP «дата» вычисляется как понедельник недели (реальное зафиксировать по `DayOfWeek` занятия — дорабатывается при обновлении DTO); для журнала по предмету достаточно группировки по неделе. Если нужен точный день — в `JournalEntryItem` передавать день занятия из первой пары по неделе. Для шага «по расписанию» день недели опускается: UI показывает неделю и количество пар.

> Код выше содержит неиспользуемую вспомогательную функцию — заменить реализацию вычисления даты на чистую: построить `date = MondayOf(SemesterStart).AddDays((week-1)*7)`, а номер пары/день взять из первой записи группы. При исполнении написать аккуратный код без лишних хелперов.

- [ ] **Step 3: Контроллер**

```csharp
[HttpGet("journal")]
[Authorize(Roles = "Teacher,Admin,Dispatcher")]
[SwaggerOperation(Summary = "Журнал проведённых занятий преподавателя (по расписанию)")]
[SwaggerResponse(200, "Журнал получен", typeof(Result<JournalResponse>))]
[SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
[SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
[SwaggerResponse(404, "Не найдено", typeof(ErrorResponse))]
[ProducesResponseType(typeof(Result<JournalResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetJournal(
    [FromQuery] Guid? teacherId,
    CancellationToken ct
)
{
    if (teacherId.HasValue && !User.IsInRole("Admin") && !User.IsInRole("Dispatcher"))
    {
        var context = await service.GetContextAsync(User.GetUserId(), ct);
        if (!context.IsSuccess || context.Data!.TeacherId != teacherId)
            return Forbid();
    }
    var effectiveTeacherId = teacherId ?? (await service.GetContextAsync(User.GetUserId(), ct)).Data?.TeacherId;
    if (!effectiveTeacherId.HasValue)
        return BadRequest(Result<JournalResponse>.Fail("Не указан преподаватель", 400));

    var result = await service.GetJournalAsync(effectiveTeacherId.Value, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

- [ ] **Step 4: Юнит-тест**

InMemory-контекст: преподаватель + записи расписания с `Weeks = [1, 2]` → `Subjects.Count == 1`, `Items.Count == 2`, `TotalPairCount == 2`.

- [ ] **Step 5: Прогнать тесты и закоммитить**

Run: `dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~JournalServiceTests"`
Expected: PASS.

```bash
git add -A
git commit -m "feat: журнал преподавателя по расписанию (без подтверждения факта)"
```

## Task A11: Сборка API и полный прогон тестов

- [ ] **Step 1: Сборка**

Run: `dotnet csharpier format . && dotnet build CollegeLMS.API`
Expected: build OK, форматирование без изменений.

- [ ] **Step 2: Полный тестовый прогон**

Run: `dotnet test`
Expected: все тесты PASS (включая существующие).

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "chore: стабилизация API после среза A"
```

---

# Срез B — Mini-app Next.js

> Все новые страницы — клиентские компоненты в `app/max/`. Используют `@maxhub/max-ui` (MaxUI, CellList, CellSimple, Button, Spinner, Typography) и существующий `api` axios-клиент с JWT из localStorage.

## Task B1: Deep-link парсер и контекст mini-app

**Files:**
- Create: `CollegeLMS.Next/lib/max-deeplink.ts`, `CollegeLMS.Next/lib/max-context.tsx`
- Test: `CollegeLMS.Next/lib/max-deeplink.test.ts` (опционально, если настроен jest — иначе проверить через `npm run build`)

**Interfaces:**
- Produces: `parseMaxDeepLink(search: string): MaxDeepLink` где `MaxDeepLink { route: "today"|"day"|"week"|"changes"|"correction"|"dispatcher"|"schedule"; date?: string; id?: string; groupId?: string; teacherId?: string }`.
- `useMaxContext(): { isAuthed, profile, viewContext, setViewContext, loading, reload }` — `viewContext: { groupId?: string; groupName?: string; teacherId?: string; teacherName?: string }`.

- [ ] **Step 1: Парсер**

`lib/max-deeplink.ts`:

```ts
export type MaxRoute = "today" | "day" | "week" | "changes" | "correction" | "dispatcher" | "schedule"

export interface MaxDeepLink {
  route: MaxRoute
  date?: string
  id?: string
  groupId?: string
  teacherId?: string
  view?: "day" | "week"
}

const VALID_ROUTES: MaxRoute[] = ["today", "day", "week", "changes", "correction", "dispatcher", "schedule"]

export function parseMaxDeepLink(search: string): MaxDeepLink {
  const params = new URLSearchParams(search)
  const rawRoute = params.get("route") ?? "today"
  const route = (VALID_ROUTES.includes(rawRoute as MaxRoute) ? rawRoute : "today") as MaxRoute
  const result: MaxDeepLink = { route }
  const date = params.get("date")
  if (date) result.date = date
  const id = params.get("id")
  if (id) result.id = id
  const groupId = params.get("groupId")
  if (groupId) result.groupId = groupId
  const teacherId = params.get("teacherId")
  if (teacherId) result.teacherId = teacherId
  const view = params.get("view")
  if (view === "day" || view === "week") result.view = view
  return result
}
```

- [ ] **Step 2: Контекст**

`lib/max-context.tsx`:

```tsx
"use client"

import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react"
import api from "@/lib/api"
import type { ProfileResponse } from "@/types"

export interface ViewContext {
  groupId?: string
  groupName?: string
  teacherId?: string
  teacherName?: string
}

interface MaxContextValue {
  isAuthed: boolean
  profile: ProfileResponse | null
  viewContext: ViewContext
  setViewContext: (ctx: ViewContext) => void
  loading: boolean
  reload: () => void
}

const EMPTY: ViewContext = {}

const MaxContext = createContext<MaxContextValue>({
  isAuthed: false,
  profile: null,
  viewContext: EMPTY,
  setViewContext: () => {},
  loading: true,
  reload: () => {},
})

export function MaxContextProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [isAuthed, setIsAuthed] = useState(false)
  const [loading, setLoading] = useState(true)
  const [viewContext, setViewContext] = useState<ViewContext>(EMPTY)

  const reload = useCallback(() => {
    setLoading(true)
    const token = typeof window !== "undefined" ? localStorage.getItem("token") : null
    if (!token) {
      setIsAuthed(false)
      setProfile(null)
      setLoading(false)
      return
    }
    api
      .get<{ data: ProfileResponse | null }>("/api/schedule/context")
      .then((res) => {
        const ctx = res.data?.data
        if (ctx) {
          setIsAuthed(true)
          setProfile(ctx as unknown as ProfileResponse)
          const own: ViewContext = {}
          if (ctx.groupId) own.groupId = ctx.groupId
          if (ctx.groupName) own.groupName = ctx.groupName
          if (ctx.teacherId) own.teacherId = ctx.teacherId
          if (ctx.teacherName) own.teacherName = ctx.teacherName
          setViewContext((prev) =>
            Object.keys(prev).length === 0 ? own : prev,
          )
        } else {
          setIsAuthed(false)
        }
      })
      .catch(() => setIsAuthed(false))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    reload()
  }, [reload])

  return (
    <MaxContext.Provider value={{ isAuthed, profile, viewContext, setViewContext, loading, reload }}>
      {children}
    </MaxContext.Provider>
  )
}

export function useMaxContext() {
  return useContext(MaxContext)
}
```

> `profile` типизируется через `ProfileResponse`, а `context`-ответ не совпадает с ним точно — привести типы к единому в API (расширить `/api/schedule/context` полями профиля не нужно: на фронте профиль отдельно берётся из `/api/auth/profile`, если требуется). Здесь контекст хранится локально; `setViewContext` вызывается из поиска/избранного.

- [ ] **Step 3: Сборка**

Run: `npm run build` (в `CollegeLMS.Next`)
Expected: build OK.

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "feat: deep-link парсер и контекст mini-app (без доверия URL)"
```

## Task B2: Shell mini-app + навигация

**Files:**
- Create: `CollegeLMS.Next/app/max/layout.tsx`, `CollegeLMS.Next/components/max/MaxShell.tsx`
- Modify: `CollegeLMS.Next/app/layout.tsx` (не требуется — провайдер вешаем локально в `/max/layout.tsx`)

**Interfaces:**
- Produces: layout `/max` с `MaxContextProvider` + `MaxShell` (bottom bar: Главная, Расписание, Избранное, Изменения, Настройки; пункт «Диспетчер» — только при `dispatcherSession`, хранимом в `sessionStorage`).

- [ ] **Step 1: `MaxShell.tsx`**

```tsx
"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  Home,
  CalendarDays,
  Star,
  History,
  Settings,
  ShieldCheck,
} from "lucide-react"
import { MaxUI, Typography } from "@maxhub/max-ui"
import type { ReactNode } from "react"

const TABS = [
  { href: "/max", label: "Главная", icon: Home },
  { href: "/max/schedule", label: "Расписание", icon: CalendarDays },
  { href: "/max/favorites", label: "Избранное", icon: Star },
  { href: "/max/changes", label: "Изменения", icon: History },
  { href: "/max/settings", label: "Настройки", icon: Settings },
]

export default function MaxShell({ children }: { children: ReactNode }) {
  const pathname = usePathname()
  const [dispatcher] =
    typeof window !== "undefined"
      ? [sessionStorage.getItem("dispatcherToken") !== null]
      : [false]

  const isActive = (href: string) =>
    href === "/max" ? pathname === "/max" : pathname.startsWith(href)

  return (
    <MaxUI className="max-app">
      <div className="max-app__content pb-20">{children}</div>
      <nav className="max-app__tabbar" aria-label="Разделы">
        {TABS.map((tab) => (
          <Link
            key={tab.href}
            href={tab.href}
            className={`max-app__tab ${isActive(tab.href) ? "max-app__tab--active" : ""}`}
            aria-current={isActive(tab.href) ? "page" : undefined}
          >
            <tab.icon size={20} />
            <Typography.Label>{tab.label}</Typography.Label>
          </Link>
        ))}
        {dispatcher && (
          <Link
            href="/max/dispatcher"
            className={`max-app__tab ${pathname.startsWith("/max/dispatcher") ? "max-app__tab--active" : ""}`}
          >
            <ShieldCheck size={20} />
            <Typography.Label>Диспетчер</Typography.Label>
          </Link>
        )}
      </nav>
    </MaxUI>
  )
}
```

- [ ] **Step 2: Layout**

`app/max/layout.tsx`:

```tsx
import { MaxContextProvider } from "@/lib/max-context"
import MaxShell from "@/components/max/MaxShell"
import "./max.css"

export default function MaxLayout({ children }: { children: React.ReactNode }) {
  return (
    <MaxContextProvider>
      <MaxShell>{children}</MaxShell>
    </MaxContextProvider>
  )
}
```

- [ ] **Step 3: Стили tabbar**

`app/max/max.css` — фиксированный bottom-bar, тумблеры ≥ 44px, активная вкладка акцентирована (с рефакторингом под токены переменных, см. `globals.css`). Минимальный набор:

```css
.max-app__tabbar {
  position: fixed;
  inset-inline: 0;
  bottom: 0;
  display: flex;
  justify-content: space-around;
  background: var(--color-card, #fff);
  border-top: 1px solid var(--border, #e5e7eb);
  padding: 0 env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left);
  z-index: 40;
}
.max-app__tab {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  min-height: 56px;
  min-width: 44px;
  padding: 6px 10px;
  color: var(--color-muted-foreground, #6b7280);
  transition: color 0.15s ease;
}
.max-app__tab--active { color: var(--color-primary, #2563eb); }
```

- [ ] **Step 4: Сборка + проверка рендера**

Run: `npm run build`
Expected: OK. Открыть `http://localhost:3000/max` (через `npm run dev`) — пустая Главная с bottom bar.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: shell mini-app с нижней навигацией"
```

## Task B3: Главная (сегодня)

**Files:**
- Create: `CollegeLMS.Next/app/max/page.tsx`, `CollegeLMS.Next/components/max/ScheduleEmpty.tsx`, `CollegeLMS.Next/components/max/ScheduleError.tsx`, `CollegeLMS.Next/components/max/CurrentPairCard.tsx`
- Modify: `CollegeLMS.Next/api/schedule.ts` (добавить `fetchScheduleMeta`, `fetchDaySchedule`)

**Interfaces:**
- Produces: `api/schedule.ts`:
  - `fetchScheduleMeta(): Promise<Result<ScheduleMeta>>` (GET `/api/schedule/meta`);
  - `fetchDaySchedule(params: { date: string; groupId?: string; teacherId?: string }): Promise<Result<PagedResponse<ScheduleResponse>>>` (GET `/api/schedule?date=...&groupId=...&teacherId=...&pageSize=200`).
- `ScheduleMeta { semesterStart: string; totalWeeks: number; currentWeek: number; currentDate: string }`.

- [ ] **Step 1: API-функции**

В `api/schedule.ts` добавить типы и функции:

```ts
export interface ScheduleMeta {
  semesterStart: string
  totalWeeks: number
  currentWeek: number
  currentDate: string
}

export async function fetchScheduleMeta(): Promise<Result<ScheduleMeta>> {
  const { data } = await api.get<Result<ScheduleMeta>>("/api/schedule/meta")
  return data
}

export async function fetchDaySchedule(params: {
  date: string
  groupId?: string
  teacherId?: string
}): Promise<Result<PagedResponse<ScheduleResponse>>> {
  const qs = new URLSearchParams({ date: params.date })
  if (params.groupId) qs.set("groupId", params.groupId)
  if (params.teacherId) qs.set("teacherId", params.teacherId)
  qs.set("pageSize", "200")
  const { data } = await api.get<Result<PagedResponse<ScheduleResponse>>>(
    `/api/schedule?${qs.toString()}`,
  )
  return data
}
```

- [ ] **Step 2: Главная**

`app/max/page.tsx` — использует `useMaxContext`, читает deep link (для `route=today`), загружает `fetchScheduleMeta` + `fetchDaySchedule` для сегодня, рендерит: заголовок «Сегодня, <дата>», индикатор недели, карточку текущей/следующей пары, «ленту дня», сводку изменений (первые 3 из `/api/schedule/correction/history`), пустые/error состояния. Код строится по образцу `MaxScheduleView` (см. `components/MaxScheduleView.tsx`): `MaxUI`, `CellList`/`CellSimple`, `Button`/`Spinner`/`Typography`. Ключевая логика текущей пары:

```ts
function currentPair(entries: ScheduleResponse[], now: Date) {
  const sorted = [...entries].sort((a, b) => a.numberPair - b.numberPair)
  const current = sorted.find(
    (e) => now >= toDate(e.startTime) && now <= toDate(e.endTime),
  )
  const next = sorted.find((e) => now < toDate(e.startTime))
  return { current, next }
}
```

> `toDate(time: string)` — `new Date()` с той же датой и временем `HH:mm`.

- [ ] **Step 3: Компоненты состояний**

`ScheduleEmpty.tsx`: иконка, «Пар нет», подпись «На этот день занятий нет», `Typography`.
`ScheduleError.tsx`: `Typography.Body` с `message` + `Button` «Повторить» (`onRetry`).

- [ ] **Step 4: Проверка**

Run: `npm run build`
Expected: OK.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: главная mini-app — сегодня, текущая пара, неделя"
```

## Task B4: Расписание (день/неделя/дата)

**Files:**
- Modify: `CollegeLMS.Next/app/max/schedule/page.tsx` (переписать), `CollegeLMS.Next/components/max/DayFeed.tsx` (new)
- Create: `CollegeLMS.Next/components/max/DayFeed.tsx`, `CollegeLMS.Next/components/max/WeekFeed.tsx`

**Interfaces:**
- Consumes: `parseMaxDeepLink` (route `day`/`week` + `date`), `fetchDaySchedule`, `fetchSchedule`.
- Produces: переключатель `День / Неделя`, навигация ‹ › по дням/неделям, кнопка «Сегодня», выбор даты (`<input type="date">`), поиск/смена контекста (переход на `/max?search=1`), карточки занятий.

- [ ] **Step 1: `DayFeed.tsx`**

Клиент: принимает `{ date: string; entry: ScheduleResponse[] }`, рендерит `CellList mode="island"` с `CellSimple` — паттерн уже в `MaxScheduleView`; добавляется маркер изменения рядом с типом занятия (из `entry.changeTags`), `aria-label` для доступности, `touch ≥ 44px`.

- [ ] **Step 2: `WeekFeed.tsx`**

Обёртка над `DayFeed`: группирует записи недели по `dayOfWeek` (6 блоков Пн–Сб), общий шапка «Неделя N, даты». Данные: `fetchSchedule({ week, groupId, teacherId, pageSize: 200 })`.

- [ ] **Step 3: Страница**

`app/max/schedule/page.tsx` (Client Component). Логика:
- `const { viewContext } = useMaxContext()` — источник группового/преподавательского контекста (не из URL).
- Deep link: `const link = parseMaxDeepLink(window.location.search)` → если `route="day"`/`date` — установить дату; если `route="week"` — режим недели.
- Fallback для старых ссылок: если есть `link.groupId`/`link.teacherId` и пустой `viewContext` — использовать как подсказку просмотра с пометкой «Открыто (по ссылке из чата)».
- Смена контекста: кнопка «Сменить просмотр» открывает `SearchSheet`.
- Данные по дате/неделе; `loading`/`error`/`empty` состояния обязательны; при дате вне семестра — объяснение «Вне учебного семестра» + кнопка перехода к ближайшей доступной неделе.

- [ ] **Step 4: Проверка + коммит**

Run: `npm run build`. Затем:

```bash
git add -A
git commit -m "feat: экран расписания день/неделя/дата с сменой контекста"
```

## Task B5: Поиск + Избранное

**Files:**
- Create: `CollegeLMS.Next/api/favorites.ts`, `CollegeLMS.Next/components/max/SearchSheet.tsx`, `CollegeLMS.Next/app/max/favorites/page.tsx`
- Modify: `CollegeLMS.Next/app/max/schedule/page.tsx` (вызов `SearchSheet`), типы `CollegeLMS.Next/types/max.ts` (new)

**Interfaces:**
- Produces: `api/favorites.ts` — `listFavorites(): Promise<Favorite[]>`, `addFavorite(targetType, targetId)`, `removeFavorite(id)`. `Favorite { id, targetType: "Group"|"Teacher", targetId, groupName?, teacherName? }`.
- `api/schedule.ts` add `searchSchedule(q, page): Promise<Result<ScheduleSearchResponse>>`; тип `ScheduleSearchResponse { groups: ScheduleSearchGroup[]; teachers: ScheduleSearchTeacher[]; totalGroups; totalTeachers }`.

- [ ] **Step 1: API-клиенты**

```ts
// api/favorites.ts
import api from "@/lib/api"
import type { Result } from "@/types"

export type FavoriteTargetType = "Group" | "Teacher"

export interface Favorite {
  id: string
  targetType: FavoriteTargetType
  targetId: string
  groupName?: string | null
  teacherName?: string | null
}

export async function listFavorites(): Promise<Favorite[]> {
  const { data } = await api.get<Result<Favorite[]>>("/api/favorites")
  if (!data.isSuccess || !data.data) throw new Error(data.errorMessage ?? "Ошибка загрузки избранного")
  return data.data
}

export async function addFavorite(targetType: FavoriteTargetType, targetId: string): Promise<Favorite> {
  const { data } = await api.post<Result<Favorite>>("/api/favorites", { targetType, targetId })
  if (!data.isSuccess || !data.data) throw new Error(data.errorMessage ?? "Ошибка добавления")
  return data.data
}

export async function removeFavorite(id: string): Promise<void> {
  const { data } = await api.delete<Result<null>>(`/api/favorites/${id}`)
  if (!data.isSuccess) throw new Error(data.errorMessage ?? "Ошибка удаления")
}
```

```ts
// api/schedule.ts — добавить
export interface ScheduleSearchGroup { id: string; name: string; course: number }
export interface ScheduleSearchTeacher { id: string; fullName: string; position: string | null }
export interface ScheduleSearchResponse {
  groups: ScheduleSearchGroup[]
  teachers: ScheduleSearchTeacher[]
  totalGroups: number
  totalTeachers: number
}

export async function searchSchedule(q: string, page = 1, pageSize = 20): Promise<Result<ScheduleSearchResponse>> {
  const qs = new URLSearchParams({ q, page: String(page), pageSize: String(pageSize) })
  const { data } = await api.get<Result<ScheduleSearchResponse>>(`/api/schedule/search?${qs.toString()}`)
  return data
}
```

- [ ] **Step 2: `SearchSheet.tsx`**

Полноэкранная панель (на MAX-стиле): поле ввода с debounce 300 мс → `searchSchedule`; секции «Группы» / «Преподаватели»; каждый элемент — кнопка «Открыть расписание» (устанавливает `viewContext`) и «⭐» (добавить в избранное, если авторизован). Пустой-без-результата ≠ ошибка сети (показывается «Ничего не найдено» отдельно от «Не удалось загрузить. Повторить»).

- [ ] **Step 3: `favorites/page.tsx`**

Список избранного: две группы (Преподаватели / Группы), звезда удаления, переход к расписанию («Открыть»). Требует авторизации: `useMaxContext().isAuthed === false` → заглушка «Войдите, чтобы сохранять избранное» + кнопка на `/login`.

- [ ] **Step 4: Обновить страницы (вызов SearchSheet из Главной и Расписания), сборка, коммит**

```bash
npm run build
git add -A
git commit -m "feat: поиск групп/преподавателей и избранное"
```

## Task B6: Изменения (лента + deep link)

**Files:**
- Create: `CollegeLMS.Next/app/max/changes/page.tsx`, `CollegeLMS.Next/components/max/ChangeCard.tsx`
- Modify: `CollegeLMS.Next/api/correction.ts` (типы совпадают — уже есть `getHistory` и `ScheduleHistoryItem`)

**Interfaces:**
- Consumes: `getHistory({ groupId?, teacherId?, week?, page, pageSize })`, `parseMaxDeepLink` (route `changes`, `correction` + `id`).
- Produces: лента изменений с фильтрами (период `week`, контекст), deep-link акцент на записи `id`, переход «Открыть на дату» (ссылка `/max/schedule?route=day&date=...`).

- [ ] **Step 1: `ChangeCard.tsx`**

Карточка: тип изменения (бейдж Add/Remove/Replace/Move), группа, предмет (снятый + новый), день/пара, автор/время (`appliedAt` через `toLocaleString("ru-RU")`), примечание, кнопка «Открыть на дату» → `href={`/max/schedule?route=day&date=${toIso(monday + week)}`}`. Использует `CHANGE_TYPE_META` по образцу `dispatcher/correction/page.tsx`.

- [ ] **Step 2: Страница**

Лента с `getHistory` (пагинация), фильтр по неделе (`week`) и по контексту (из `viewContext`). Если `deepLink.route === "correction"` и `id` найден — подсветить запись (`className="ring-2"` + auto-scroll). Error/empty/loading состояния обязательны.

- [ ] **Step 3: Сборка + коммит**

```bash
npm run build
git add -A
git commit -m "feat: лента изменений с фильтрами и подсветкой deep link"
```

## Task B7: Настройки (уведомления)

**Files:**
- Create: `CollegeLMS.Next/api/notifications.ts`, `CollegeLMS.Next/app/max/settings/page.tsx`
- Modify: `CollegeLMS.Next/app/max/{layout}.tsx` (не нужно)

**Interfaces:**
- Produces: `api/notifications.ts` — `getNotificationSettings()`, `updateNotificationSettings(body)` с типами `{ enabled, time, days }`; ответ `{ enabled, time, days, nextNotifyAt }`.

- [ ] **Step 1: API-клиент**

```ts
import api from "@/lib/api"
import type { Result } from "@/types"

export interface NotificationSettingsDto {
  enabled: boolean
  time: string
  days: number[]
  nextNotifyAt?: string | null
}

export type NotificationSettingsInput = Pick<NotificationSettingsDto, "enabled" | "time" | "days">

function unwrap(res: { data: Result<NotificationSettingsDto> }): NotificationSettingsDto {
  if (!res.data.isSuccess || !res.data.data) throw new Error(res.data.errorMessage ?? "Ошибка настроек")
  return res.data.data
}

export async function getNotificationSettings(): Promise<NotificationSettingsDto> {
  const res = await api.get<Result<NotificationSettingsDto>>("/api/notifications/settings")
  return unwrap(res)
}

export async function updateNotificationSettings(body: NotificationSettingsInput): Promise<NotificationSettingsDto> {
  const res = await api.put<Result<NotificationSettingsDto>>("/api/notifications/settings", body)
  return unwrap(res)
}
```

- [ ] **Step 2: Страница `settings/page.tsx`**

- Switch «Ежедневный дайджест» (`enabled`).
- Время: `<select>` с шагом 5 минут в 07:30–08:30 (варианты «07:30»…«08:30»).
- Дни: 7 чекбоксов Пн–Вс.
- Сохранение → `updateNotificationSettings` → показать `toast`/блок: «Сохранено. Время 07:45, следующие — 16.09 в 07:45 (МСК)» (из `nextNotifyAt`, дата/время через `toLocaleString("ru-RU")`).
- Подсказка: «Уведомления об изменениях приходят сразу, независимо от времени дайджеста».
- Авторизация: `useMaxContext().isAuthed === false` → заглушка + ссылка на `/login`.
- Ошибки: поле «Время» — подсказка про диапазон/шаг (серверная валидация), «Повторить» на `error`.

- [ ] **Step 3: Сборка + коммит**

```bash
npm run build
git add -A
git commit -m "feat: настройки уведомлений (дайджест, время, дни, nextNotifyAt)"
```

## Task B8: Журнал

**Files:**
- Create: `CollegeLMS.Next/app/max/journal/page.tsx`, `CollegeLMS.Next/api/schedule.ts` add `fetchJournal(teacherId?): Promise<Result<JournalResponse>>`

**Interfaces:**
- Produces: `JournalResponse { teacherId, teacherName, subjects: LookupItem[], totalPairCount }`.

- [ ] **Step 1: API + тип**

В `api/schedule.ts`:

```ts
export interface JournalEntryItem { week: number; date: string; numberPairs: number[] }
export interface JournalSubjectGroup { subject: string; items: JournalEntryItem[]; pairCount: number }
export interface JournalResponse {
  teacherId: string
  teacherName: string
  subjects: JournalSubjectGroup[]
  totalPairCount: number
}

export async function fetchJournal(teacherId?: string): Promise<Result<JournalResponse>> {
  const qs = teacherId ? `?teacherId=${teacherId}` : ""
  const { data } = await api.get<Result<JournalResponse>>(`/api/schedule/journal${qs}`)
  return data
}
```

- [ ] **Step 2: Страница**

- Селектор предмета (из `subjects`), счётчик пар на предмете.
- Список: неделя, дата, «Пар: N». Маркер «по расписанию» — без утверждения «проведено».
- Роль: только преподаватель; иначе заглушка «Журнал доступен преподавателю». Если преподаватель без расписания — empty state «Занятий по расписанию нет».
- Авторизация: `[Authorize]` на API; фронт при 401 уходит на `/login` (interceptor уже это делает).

- [ ] **Step 3: Сборка + коммит**

```bash
npm run build
git add -A
git commit -m "feat: журнал преподавателя (по расписанию)"
```

## Task B9: Диспетчер (пароль, импорт, превью, подтверждение, история)

**Files:**
- Create: `CollegeLMS.Next/api/dispatcher.ts`, `CollegeLMS.Next/app/max/dispatcher/page.tsx`, `CollegeLMS.Next/components/max/DispatcherGate.tsx`, `CollegeLMS.Next/components/max/DispatcherImport.tsx`, `CollegeLMS.Next/components/max/DispatcherResult.tsx`
- Modify: `CollegeLMS.Next/api/correction.ts` (confirm с заголовком `Idempotency-Key`)

**Interfaces:**
- Produces: `api/dispatcher.ts` — `dispatcherLogin(password): Promise<{ token, expiresAt }>` (POST `/api/dispatcher/login`); токен кладётся в `sessionStorage.dispatcherToken`.
- `confirmCorrection(entries, idempotencyKey)` — передаёт заголовок `Idempotency-Key`.

- [ ] **Step 1: API dispatcher + заголовок идемпотентности**

```ts
// api/dispatcher.ts
import api from "@/lib/api"
import type { Result } from "@/types"

export interface DispatcherLoginResponse { token: string; expiresAt: string }

export async function dispatcherLogin(password: string): Promise<DispatcherLoginResponse> {
  const { data } = await api.post<Result<DispatcherLoginResponse>>("/api/dispatcher/login", {
    password,
  })
  if (!data.isSuccess || !data.data) throw new Error(data.errorMessage ?? "Ошибка входа")
  sessionStorage.setItem("dispatcherToken", data.data.token)
  return data.data
}

export function dispatcherToken(): string | null {
  return typeof window !== "undefined" ? sessionStorage.getItem("dispatcherToken") : null
}

export function dispatcherLogout(): void {
  sessionStorage.removeItem("dispatcherToken")
}
```

`api/correction.ts` — `confirmCorrection`:

```ts
export async function confirmCorrection(
  entries: CorrectionPreviewEntry[],
  idempotencyKey: string,
): Promise<ConfirmResult> {
  return unwrap(
    await api.post<Result<ConfirmResult>>(
      "/api/schedule/correction/confirm",
      { entries },
      { headers: { "Idempotency-Key": idempotencyKey } },
    ),
  )
}
```

- [ ] **Step 2: `DispatcherGate.tsx`**

Пин-экран: поле пароля + «Войти» → `dispatcherLogin`; при `429` показывать «Слишком много попыток, повторите позже»; при успехе — `onSuccess()`. Заглушки про `useMaxContext().isAuthed === false`.

- [ ] **Step 3: `DispatcherImport.tsx`**

Импорт XLSX: выбор файла → `previewCorrection(file)` → таблица записей + ошибки (переиспользовать разметку из `app/(authenticated)/dispatcher/correction/page.tsx`, но в MAX-стиле и компактно). При критических ошибках кнопка «Применить» неактивна. Сгенерировать `idempotencyKey = crypto.randomUUID()` при закрытии превью; «Применить» → `confirmCorrection(entries, key)` → `DispatcherResult`.

- [ ] **Step 4: `DispatcherResult.tsx` + история**

- Результат: `Applied` и переход в историю (`getHistory`), кнопка «Скачать XLSX» → `exportSchedule(groupId, "xlsx")`.
- История: таблица/список записей с пагинацией.

- [ ] **Step 5: Страница `dispatcher/page.tsx`**

`if (!dispatcherToken()) → <DispatcherGate onSuccess={reload} />; else → {DispatcherImport, DispatcherResult, История}`.

- [ ] **Step 6: Сборка + коммит**

```bash
npm run build
git add -A
git commit -m "feat: диспетчерский раздел mini-app"
```

---

# Срез C — MaxBot

## Task C1: Deep links бота с route/date

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs` (`BuildMiniAppUrl`), `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs` (не требуется)
- Test: `CollegeLMS.MaxBot.Tests/CallbackPayloadTests.cs` (добавить тесты на новые построители)

**Interfaces:**
- Produces: `BuildMiniAppUrl(UserSettings settings, string route, DateTime? date = null, int? historyPage = null)` → `https://stvcc.tech/max/...?route=day&date=2026-09-13` (+ legacy `groupId/teacherId` как hint).

- [ ] **Step 1: Реализация**

`MaxBotService.BuildMiniAppUrl` — заменить на:

```csharp
private string BuildMiniAppUrl(UserSettings settings, string route, DateTime? date = null)
{
    var query = new List<string> { $"route={route}" };
    if (date.HasValue)
        query.Add($"date={date:yyyy-MM-dd}");
    if (settings.GroupId.HasValue)
        query.Add($"groupId={Uri.EscapeDataString(settings.GroupId.Value.ToString())}");
    if (settings.TeacherId.HasValue)
        query.Add($"teacherId={Uri.EscapeDataString(settings.TeacherId.Value.ToString())}");
    return query.Count == 0
        ? _options.MiniAppUrl
        : $"{_options.MiniAppUrl}?{string.Join("&", query)}";
}
```

> `MiniAppUrl` в `MaxBotOptions` заменить на `https://stvcc.tech/max` (база mini-app) — при необходимости через конфигурацию.

- [ ] **Step 2: Обновить вызовы**

`ShowMainMenuAsync` — кнопка «Открыть расписание»: `Url = BuildMiniAppUrl(settings, settings.Role == "student" ? "today" : "today")` (т.е. `route=today`). В `ShowDayAsync`/`ShowWeekAsync` кнопка «Открыть в mini-app» с `route=day&date=`/`route=week&date=`. В `ShowMyChangesAsync` — кнопка «Открыть изменения» с `route=changes`.

- [ ] **Step 3: Тесты**

`CallbackPayloadTests.cs` — добавить тест, что новый мини-app URL содержит `route=today` и `date`, а legacy-параметры остаются (проверка через приватный метод не нужна — проверить формат на уровне построителя-помощника, вынести в `static` если нужно; объект `UserSettings` создаётся в тесте).

- [ ] **Step 4: Сборка + тесты + коммит**

Run: `dotnet build CollegeLMS.MaxBot && dotnet test CollegeLMS.MaxBot.Tests`

```bash
git add -A
git commit -m "feat: deep links бота с route/date"
```

## Task C2: Время дайджеста per-user в боте

**Files:**
- Modify: `CollegeLMS.MaxBot/Models/UserSettings.cs` (добавить `NotifyTime`), `CollegeLMS.MaxBot/Data/Configurations/UserSettingsConfiguration.cs`, `CollegeLMS.MaxBot/Data/MaxBotDbContext.cs` (миграция не нужна — `EnsureCreated` + raw SQL ALTER), `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs`, `CollegeLMS.MaxBot/Bot/MaxBotService.cs` (кнопки времени)
- Test: `CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs` / новый `ScheduleNotifierTimeTests`

**Interfaces:**
- Produces: `UserSettings.NotifyTime (TimeSpan, default 07:30)`, константа `NotificationTimeRules.Min`/`Max`/`Step` (в `CollegeLMS.MaxBot/Services/NotificationTimeRules.cs`) с теми же значениями, что в API.

- [ ] **Step 1: Правила + миграция колонки**

`NotificationTimeRules.cs`:

```csharp
namespace CollegeLMS.MaxBot.Services;

public static class NotificationTimeRules
{
    public static readonly TimeSpan Min = TimeSpan.FromHours(7) + TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Max = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(30);
    public static readonly int StepMinutes = 5;

    public static bool IsValid(TimeSpan time) =>
        time >= Min && time <= Max && (int)time.TotalMinutes % StepMinutes == 0;
}
```

`UserSettings` добавить `public TimeSpan NotifyTime { get; set; } = new(7, 30, 0);`. В `Program.cs` сырой SQL `CREATE TABLE IF NOT EXISTS user_settings` — его нет, таблица через EF `EnsureCreated`; добавить идемпотентный `ALTER TABLE ... ADD COLUMN IF NOT EXISTS notify_time interval NOT NULL DEFAULT INTERVAL '7 hours 30 minutes'` рядом с create-скриптом. Конфигурация: `builder.Property(x => x.NotifyTime).HasColumnType("interval")`.

- [ ] **Step 2: ScheduleNotifier**

В `SendNotificationsAsync`:

```csharp
var subscribers = await db
    .UserSettings.Where(x => x.NotifyEnabled && x.NotifyDays.Contains(dayOfWeek))
    .ToListAsync(ct);
```

оставить, но в цикле слать не раньше, чем наступит время `user.NotifyTime` конкретного дня (для этого `ExecuteAsync` должен брать не одно `target`, а по-пользовательски). Проще: в `ExecuteAsync` после срабатывания окна из `_options.NotifyHour/Minute` вызывать `SendNotificationsAsync` для тех, чей `NotifyTime` уже наступил и не был отправлен сегодня. Добавить таблицу `LastSent` не нужно — отправка идемпотентна по дню, которую хранить в `_lastSentDay` глобально уже есть. Для per-user времени:

```csharp
private async Task SendNotificationsAsync(CancellationToken ct)
{
    var now = GetNow();
    var today = DateOnly.FromDateTime(now);
    var dayOfWeek = (int)now.DayOfWeek;
    if (dayOfWeek is 0 or 6) return;

    var week = StudyWeek.Current(_tz);
    var candidates = await db.UserSettings
        .Where(x => x.NotifyEnabled && x.NotifyDays.Contains(dayOfWeek))
        .ToListAsync(ct);

    // _lastSentPerUser: Dictionary<long, DateOnly>
    foreach (var user in candidates)
    {
        if (_lastSentPerUser.TryGetValue(user.MaxUserId, out var last) && last == today)
            continue;
        if (now.TimeOfDay < user.NotifyTime) // окно: от времени до +15 мин
            continue;
        if (now.TimeOfDay > user.NotifyTime.Add(TimeSpan.FromMinutes(15)))
        {
            _lastSentPerUser[user.MaxUserId] = today;
            continue;
        }
        await SendToUserAsync(user, week, dayOfWeek, ct);
        _lastSentPerUser[user.MaxUserId] = today;
    }
}
```

`ScheduleNotifier` получает поле `private readonly Dictionary<long, DateOnly> _lastSentPerUser = new();`. `ExecuteAsync` — пока есть первая строка совпадения окна, держаться; упрощением: «окно дня» 07:30–08:45, отправка в момент когда `now.TimeOfDay >= NotifyTime` после каждого полушага цикла. Логика «жду до минимального NotifyTime следующего дня»:

```csharp
var minTime = subscribers.Min(s => s.NotifyTime);     // минимум среди активных сегодня
var target = today.ToDateTime(minTime, DateTimeKind.Unspecified); // с учётом МСК
```

> Реализовать цикл ожидания аккуратно: дождаться `today.ToDateTime(minTime)` (или следующего дня при пропуске), затем перебирать пользователей в окне. Валидность `NotifyTime` гарантируется правилами; лог предупреждает при `!NotificationTimeRules.IsValid(user.NotifyTime)`.

- [ ] **Step 3: Кнопки времени в боте**

В `ShowSettingsAsync`/`HandleNotifyToggleAsync` добавить строку с тумблером времени (±5 мин): `payload = "notifytime:-5"/"notifytime:+5"`, обработчик `notifysave` сохраняет. Обработчики в `HandleCallbackAsync`:

```csharp
case "notifytime":
    var delta = int.Parse(p.Param1!);
    settings.NotifyTime = settings.NotifyTime.Add(TimeSpan.FromMinutes(delta));
    if (!NotificationTimeRules.IsValid(settings.NotifyTime))
    {
        await _max.SendMessageAsync(chatId, $"⏰ Время дайджеста — от {NotificationTimeRules.Min:hh\\:mm} до {NotificationTimeRules.Max:hh\\:mm} с шагом 5 минут.", ct: ct);
        settings.NotifyTime = settings.NotifyTime.Add(TimeSpan.FromMinutes(-delta));
    }
    settings.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    break;
```

- [ ] **Step 4: Тесты**

`NotificationTimeRulesTests` (новый файл): границы 07:30/08:30 валидны, 07:33 невалиден, шаг. 

- [ ] **Step 5: Сборка + тесты + коммит**

Run: `dotnet build CollegeLMS.MaxBot && dotnet test CollegeLMS.MaxBot.Tests`

```bash
git add -A
git commit -m "feat: per-user время дайджеста в боте с общими границами"
```

## Task C3: Dispatcher-флоу в боте

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`, `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs` (метод `DispatcherLoginAsync` + `GetFullScheduleXlsxAsync`), `CollegeLMS.MaxBot/MaxBotOptions.cs` (`DispatchChatIds` whitelist)
- Test: `CollegeLMS.MaxBot.Tests/*`

**Interfaces:**
- Produces: `CollegeLmsApiClient.DispatcherLoginAsync(string password, ct) → string? token`; `CollegeLmsApiClient.GetScheduleXlsxAsync(Guid? groupId, ct) → byte[]?` (GET `/api/schedule/export?format=xlsx`).
- `MaxBotOptions.DispatchChatIds` (List<string>) — whitelist MAX-чатов для отправки XLSX.

- [ ] **Step 1: Клиент**

```csharp
public async Task<string?> DispatcherLoginAsync(string password, CancellationToken ct)
{
    var resp = await _http.PostAsJsonAsync(
        "/api/dispatcher/login",
        new { password },
        JsonOpts,
        ct
    );
    if (!resp.IsSuccessStatusCode)
        return null;
    var wrapper = await resp.Content.ReadFromJsonAsync<ResultWrapper<DispatcherLoginResponse>>(JsonOpts, ct);
    return wrapper?.Data?.Token;
}

public async Task<byte[]?> GetScheduleXlsxAsync(Guid? groupId, CancellationToken ct)
{
    var url = groupId.HasValue
        ? $"/api/schedule/export?format=xlsx&groupId={groupId}"
        : "/api/schedule/export?format=xlsx";
    var resp = await _http.GetAsync(url, ct);
    if (!resp.IsSuccessStatusCode)
        return null;
    return await resp.Content.ReadAsByteArrayAsync(ct);
}
```

Добавить record `DispatcherLoginResponse { Token }` в `CollegeLmsApiDtos.cs`.

- [ ] **Step 2: Обработчики команд**

В `SetCommandsAsync` добавить `/dispatcher` (или кнопку в меню). В `HandleCallbackAsync` — `case "dispatcher"` → спрашивает пароль текстом (состояние ожидания через локальный словарь `_pendingDispatcherPasswords[userId] = true`); при следующем сообщении текста с паролем вызывает `DispatcherLoginAsync`, при успехе — меню: «Создать корректировку» (`open_app` → `BuildMiniAppUrl(settings, "dispatcher")`), «Отправить последний XLSX» → выбор чата из `_options.DispatchChatIds` (inline-кнопки), отправка `SendMessageAsync` с подписью + `SendXlsx` (Max SDK-файл недоступен → слать текст: `Расписание XLSX: {MiniAppUrl}/api/schedule/export?format=xlsx&groupId=...` со ссылкой на скачивание). Заглушка «не настроено» при пустом whitelist.

> Файловая отправка в MAX SDK не реализована (см. D7); текущий дефолт — доставка ссылки на результат с описанием. При появлении upload-API в SDK добавить вложение.

- [ ] **Step 3: Тесты**

`MaxBotDispatcherFlowTests`: формирование ссылки XLSX из groupId; `DispatcherLoginAsync` возвращает null при 401.

- [ ] **Step 4: Сборка + тесты + коммит**

Run: `dotnet build CollegeLMS.MaxBot && dotnet test CollegeLMS.MaxBot.Tests`

```bash
git add -A
git commit -m "feat: dispatcher-флоу в боте (вход, создание корректировки, отправка XLSX-ссылки)"
```

## Task C4: Уведомления об изменениях с deep link на дату

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs` (`FormatChangeNotification`), `CollegeLMS.MaxBot/Services/ChangeNotifier.cs`
- Test: `CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs`

- [ ] **Step 1: Сообщение с deep link**

`FormatChangeNotification(ScheduleRevision revision, string miniAppUrl)` → текст «🔔 Изменение: {тип} · {предмет} · {дата}» + строка-ссылка `📅 Открыть на дату: {miniAppUrl}?route=day&date={date}`. Дата недели: `StudyWeek.MondayOf(SemesterStart).AddDays((revision.Week - 1) * 7 + (int)revision.DayOfWeek ... )` — вычислить дату по `DayOfWeek` из `revision` (в ревизии хранится строка дня). Добавить в `ChangeNotifier.NotifyAsync` параметр `miniAppUrl` из `IOptions<MaxBotOptions>` (DI дополняется).

- [ ] **Step 2: Тесты**

`MessageFormatter.FormatChangeNotification` содержит `route=day` и `date=`; получатель отмечен.

- [ ] **Step 3: Сборка + тесты + коммит**

Run: `dotnet build CollegeLMS.MaxBot && dotnet test CollegeLMS.MaxBot.Tests`

```bash
git add -A
git commit -m "feat: deep link на дату в уведомлениях об изменениях"
```

---

# Срез D — Веб-синхронизация + E2E

## Task D1: Веб-расписание использует дату (синхронизация правил)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx` (не обязательно; при желании использовать `fetchDaySchedule` для «карточек»)

**Rationale:** Правила уже общие (один API). Единственное изменение, требуемое спец. — единый календарь: заменить дублирующиеся `getCurrentWeek/getMondayOfWeek` в вебе на `fetchScheduleMeta` там, где это снижает риск расхождений.

- [ ] **Step 1: Вынести неделю в meta**

В `schedule/page.tsx` заменить `const [selectedWeek, setSelectedWeek] = useState(getCurrentWeek())` на инициализацию из `fetchScheduleMeta()` (при успехе), сохранив fallback `getCurrentWeek()`. Удалить `SEMESTER_START/getCurrentWeek` или оставить как fallback.

- [ ] **Step 2: Сборка**

Run: `npm run build`

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "refactor: веб-расписание берёт неделю из API meta"
```

## Task D2: E2E мини-app потоки

**Files:**
- Create: `CollegeLMS.Next/e2e/max-miniapp.spec.ts`
- Playwright config уже настроен (`webServer: npm run dev`, chromium desktop). Добавить отдельный проект viewport 393px в `playwright.config.ts`.

**Interfaces:**
- Consumes: запущенный API + фронт (локально, `npm run dev`; API на localhost:5000).

- [ ] **Step 1: Конфиг viewport**

`playwright.config.ts` — добавить проект:

```ts
{
  name: "mobile",
  use: { ...devices["Pixel 5"], viewport: { width: 393, height: 1366 } },
},
```

(плюс `fullyParallel: false`, `workers: 1` уже есть).

- [ ] **Step 2: Спек**

`e2e/max-miniapp.spec.ts`:

```ts
import { expect, test } from "@playwright/test"

test("Главная открывается без ошибок", async ({ page }) => {
  await page.goto("/max?route=today")
  await expect(page).toHaveTitle(/Расписание|Главная/)
  // заголовок недели присутствует в skeleton или после загрузки
  await expect(page.locator(".max-app__tabbar")).toBeVisible()
})

test("Deep link на дату открывает день расписания", async ({ page }) => {
  await page.goto("/max/schedule?route=day&date=2026-09-07")
  await expect(page.locator("text=Понедельник")).toBeVisible()
})

test("Поиск возвращает группы и преподавателей (аннонимный просмотр)", async ({ page }) => {
  await page.goto("/max/schedule?route=week&date=2026-09-07")
  await page.getByRole("button", { name: "Сменить просмотр" }).click()
  await page.getByRole("textbox", { name: /поиск/i }).fill("11")
  await expect(page.locator("text=Группы")).toBeVisible()
  await expect(page.locator("text=Преподаватели")).toBeVisible()
})

test("Измения открываются по deep link", async ({ page }) => {
  await page.goto("/max/changes")
  await expect(page.locator("text=Изменения") ).toBeVisible()
})
```

> Тесты зависят от наличия seed-данных в API. Если пусто — использовать независимые от содержимого проверки (состояния empty/error, а не конкретные записи). Адаптировать строки локаторов к реальному рендеру (`getByRole`, классам `.max-app__*`).

- [ ] **Step 3: Локальный прогон**

Run (в `CollegeLMS.Next`): `npm run test:e2e`
Expected: PASS (или явно задокументированные empty-assertions).

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "test: E2E мини-app (deep link, поиск, изменения, viewport 393px)"
```

## Task D3: Финальная сборка и Definition of Done

- [ ] **Step 1: Backend**

Run: `dotnet build CollegeLMS.API && dotnet test`

- [ ] **Step 2: MaxBot**

Run: `dotnet build CollegeLMS.MaxBot && dotnet test CollegeLMS.MaxBot.Tests`

- [ ] **Step 3: Frontend**

Run: `npm run build` (в `CollegeLMS.Next`)

- [ ] **Step 4: CSharpier**

Run: `dotnet csharpier format . --check`
Expected: без изменений (если есть — `dotnet csharpier format .`).

- [ ] **Step 5: Обновить Postman-коллекцию**

Добавить в `docs/spec/CollegeLMS.postman_collection.json`: `/api/schedule/meta`, `/api/schedule/search`, `/api/favorites` (GET/POST/DELETE), `/api/notifications/settings` (GET/PUT), `/api/dispatcher/login`, `/api/schedule/journal`, `/api/schedule/context`.

- [ ] **Step 6: Коммит**

```bash
git add -A
git commit -m "chore: DoD max-miniapp — стабилизация, Postman, CSharpier"
```