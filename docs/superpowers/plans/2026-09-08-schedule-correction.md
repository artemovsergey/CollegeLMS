# Корректировка расписания — Implementation Plan

> **Для агентных исполнителей:** ОБЯЗАТЕЛЬНО Sub-skill: использовать superpowers:subagent-driven-development (рекомендуется) или superpowers:executing-plans для выполнения плана задача-за-задачей. Шаги используют чек-боксы (`- [ ]`).

**Goal:** Оперативная корректировка расписания через `Корректировка.xlsx` (загрузка → превью → подтверждение), маркировка изменённых пар во фронтенде, история изменений (журнал диспетчера + «Мои изменения» в боте Max) и мгновенные уведомления затронутым группам/преподавателям.

**Architecture:** API (CollegeLMS.API) парсит XLSX, применяет операции к `ScheduleEntry` только на неделю N из даты A3, пишет метаданные в новую сущность `ScheduleHistory` и вызывает HTTP POST `/notify` в MaxBot (fail-safe). MaxBot сохраняет `schedule_revisions` в своей БД и рассылает подписчикам (student по GroupId, teacher по TeacherId). Фронтенд: страница `dispatcher/correction` (превью + журнал) и маркировка в общем расписании.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core (Npgsql, snake_case), ClosedXML, FluentValidation, xUnit+Moq+Bogus+FluentAssertions, WebApplicationFactory; CollegeLMS.MaxBot (long-polling MAX API, `EnsureCreated`); Next.js 14 + Tailwind v4 + shadcn/ui.

## Global Constraints

- Все комментарии, сообщения об ошибках, Swagger-описания — на русском.
- `Result<T>.Ok/Fail` везде, без try-catch в контроллерах/сервисах (кроме парсера файла, где try-catch уместен).
- Primary constructor DI, `CancellationToken ct` на всех async-методах.
- Entity наследуют `Entity` (Id, CreatedAt, UpdatedAt); GUID PK `ValueGeneratedNever()`.
- String props: `HasMaxLength` обязательно; enums: `HasConversion<string>()` + `HasMaxLength`.
- Модель данных корректировки: манипулируем только `ScheduleEntry` (не схема исключений).
- Валидация файла: уровни structure/data/logic; кнопка «Подтвердить» активна только при `errors == []`.
- Оповещение MaxBot — прямой HTTP POST при подтверждении, fail-safe.
- Семестр начинается 01.09.2026 (совпадает с `StudyWeek.SemesterStart` в MaxBot и `SEMESTER_START` во фронтенде). Номер недели `N = Math.Max(1, (MondayOf(date) - MondayOf(2026-09-01)).Days/7 + 1)`.
- Форматирование: CSharpier (`dotnet csharpier format .`).
- Git-префиксы: `feat:`, `docs:`, `test:`, `chore:`.
- **Replace:** снятие и ввод на РАЗНЫХ парах. Пара снятия определяется по расписанию (поиск записи по группе+дню+неделе+предмету B+преподавателю C), пара ввода = F. Если пара снятия == F → ошибка «пара снятия и ввода одинакова».

## Карта файлов

Порядок задач соответствует функциональным слоям; каждая задача самостоятельно собирается и покрыта тестами.

| Задача | Файлы |
|--------|-------|
| 1. API: enum+entity+EF+миграция | `Entities/Enums/ScheduleChangeType.cs` (new), `Entities/ScheduleHistory.cs` (new), `Data/Configurations/ScheduleHistoryConfiguration.cs` (new), `Data/AppDbContext.cs` (edit) |
| 2. API: DTO+маппер+доступ к парсеру | `Dtos/ScheduleCorrectionDtos.cs` (new), `Dtos/ScheduleHistoryDtos.cs` (new), `Mappers/ScheduleHistoryMapper.cs` (new), `Services/ScheduleImportService.cs` (edit: 3 метода → internal static) |
| 3. API: MaxBotHttpClient+DI+config | `Services/MaxBotHttpClient.cs` (new), `Extensions/ServiceCollectionExtensions.cs` (edit), `Program.cs` (edit), `appsettings.json` (edit) |
| 4. API: сервис корректировок (превью) | `Services/ScheduleCorrectionService.cs` (new), `Services/StudyWeek.cs` (new) |
| 5. API: сервис корректировок (подтверждение+история) | `Services/ScheduleCorrectionService.cs` (edit) |
| 6. API: контроллер+Swagger-examples+Postman | `Controllers/ScheduleCorrectionController.cs` (new), `SwaggerExamples/ScheduleHistoryResponseExample.cs` (new), `SwaggerExamples/CorrectionPreviewResponseExample.cs` (new), `docs/spec/CollegeLMS.postman_collection.json` (edit) |
| 7. API: changeTags в расписании | `Dtos/ScheduleDtos.cs` (edit), `Mappers/ScheduleMapper.cs` (edit), `Services/ScheduleService.cs` (edit) |
| 8. Backend unit-тесты | `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs` (new) |
| 9. Backend integration-тесты | `CollegeLMS.Tests/Integration/Controllers/ScheduleCorrectionControllerTests.cs` (new) |
| 10. MaxBot: revision entity+config+db+raw sql | `MaxBot/Models/ScheduleRevision.cs` (new), `MaxBot/Data/Configurations/ScheduleRevisionConfiguration.cs` (new), `MaxBot/Data/MaxBotDbContext.cs` (edit), `MaxBot/Program.cs` (edit) |
| 11. MaxBot: /notify + форматтер | `MaxBot/Program.cs` (edit), `MaxBot/Services/MessageFormatter.cs` (edit), `MaxBot/Clients/CollegeLmsApiDtos.cs` (edit: `NotifyChangeDto`) |
| 12. MaxBot: «Мои изменения» | `MaxBot/Bot/MaxBotService.cs` (edit), `MaxBot/Services/MessageFormatter.cs` (edit) |
| 13. MaxBot unit-тесты | `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs` (edit) |
| 14. Frontend: types+api+страница | `CollegeLMS.Next/types/correction.ts` (new), `CollegeLMS.Next/api/correction.ts` (new), `app/(authenticated)/dispatcher/correction/page.tsx` (rewrite) |
| 15. Frontend: маркировка + «снято» | `types/schedule.ts` (edit), `components/ScheduleTable.tsx` (edit), `app/(authenticated)/schedule/page.tsx` (edit), `components/SemesterView.tsx` (edit) |
| 16. Docs+DevOps | `docs/diagrams/...` (new), `docker-compose.yml` (edit), `CollegeLMS.API/Dockerfile` — не трогаем |

---

### Задача 1: API — enum, сущность ScheduleHistory, EF-конфигурация, миграция

**Files:**
- Create: `CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs`
- Create: `CollegeLMS.API/Entities/ScheduleHistory.cs`
- Create: `CollegeLMS.API/Data/Configurations/ScheduleHistoryConfiguration.cs`
- Modify: `CollegeLMS.API/Data/AppDbContext.cs`

**Interfaces:**
- Consumes: базовый `Entity` (`CollegeLMS.API/Entities/Entity.cs`), `AppDbContext.OnModelCreating` авто-применяет конфигурации.
- Produces: `ScheduleHistory` с навигациями `Group`, `Teacher`; `DbSet<ScheduleHistory> ScheduleHistory` в `AppDbContext`. Enum `ScheduleChangeType { Add, Remove, Replace }`.

- [ ] **Step 1: Enum**

`CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs`:

```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum ScheduleChangeType
{
    Add,
    Remove,
    Replace,
}
```

- [ ] **Step 2: Entity**

`CollegeLMS.API/Entities/ScheduleHistory.cs`:

```csharp
using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class ScheduleHistory : Entity
{
    public ScheduleChangeType ChangeType { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid AppliedByUserId { get; set; }
    public Guid GroupId { get; set; }
    public Guid? TeacherId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Room { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public int Week { get; set; }
    public string? Note { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedRoom { get; set; }
    public int? RemovedNumberPair { get; set; }

    [JsonIgnore]
    public Group? Group { get; set; }

    [JsonIgnore]
    public Teacher? Teacher { get; set; }
}
```

- [ ] **Step 3: EF config**

`CollegeLMS.API/Data/Configurations/ScheduleHistoryConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class ScheduleHistoryConfiguration : IEntityTypeConfiguration<ScheduleHistory>
{
    public void Configure(EntityTypeBuilder<ScheduleHistory> builder)
    {
        builder.ToTable("schedule_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Room).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(200);
        builder.Property(x => x.RemovedSubject).HasMaxLength(200);
        builder.Property(x => x.RemovedRoom).HasMaxLength(50);
        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.GroupId).HasDatabaseName("ix_schedule_history_group_id");
        builder.HasIndex(x => x.DayOfWeek).HasDatabaseName("ix_schedule_history_day_of_week");
        builder.HasIndex(x => x.AppliedAt).HasDatabaseName("ix_schedule_history_applied_at");

        builder
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

- [ ] **Step 4: DbContext**

`CollegeLMS.API/Data/AppDbContext.cs` — добавить после строки `public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();`:

```csharp
    public DbSet<ScheduleHistory> ScheduleHistory => Set<ScheduleHistory>();
```

- [ ] **Step 5: Сборка**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`
Expected: build success.

- [ ] **Step 6: Миграция**

Run (из корня репозитория):
```powershell
dotnet ef migrations add AddScheduleHistory --project CollegeLMS.API -- --provider Npgsql
```
Expected: создан `CollegeLMS.API/Migrations/<ts>_AddScheduleHistory.cs`.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: entity ScheduleHistory + миграция"
```

---

### Задача 2: API — DTO, маппер, доступ к парсеру из ScheduleImportService

**Files:**
- Create: `CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs`
- Create: `CollegeLMS.API/Dtos/ScheduleHistoryDtos.cs`
- Create: `CollegeLMS.API/Mappers/ScheduleHistoryMapper.cs`
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs` (методы `NormalizeSubject`, `NormalizeTeacherName`, `GetPairTime`: `private static` → `internal static`)

**Interfaces:** Produces типы, переиспользуемые в Задачах 4–9:
- `ScheduleValidationError` (расширен полем `Level: string = "data"`).
- `CorrectionPreviewEntry`, `CorrectionPreviewResponse`, `CorrectionConfirmRequest`, `CorrectionConfirmResult`, `ScheduleChangeDto`.
- `ScheduleHistoryResponse`, `ScheduleHistoryMapper.ToDto(this ScheduleHistory)`.

- [ ] **Step 1: Расширить `ScheduleValidationError`**

`CollegeLMS.API/Dtos/ScheduleImportDtos.cs` — заменить класс:

```csharp
public class ScheduleValidationError
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Level { get; set; } = "data";
    public string Message { get; set; } = string.Empty;
}
```

- [ ] **Step 2: DTO корректировок**

`CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs`:

```csharp
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class CorrectionPreviewEntry
{
    public int Row { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public ScheduleChangeType ChangeType { get; set; }
    public int DayOfWeek { get; set; }
    public int Week { get; set; }
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
}

public class CorrectionPreviewResponse
{
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public int TotalEntries { get; set; }
    public List<CorrectionPreviewEntry> Entries { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class CorrectionConfirmRequest
{
    public List<CorrectionPreviewEntry> Entries { get; set; } = [];
}

public class CorrectionConfirmResult
{
    public int Applied { get; set; }
    public List<ScheduleHistoryResponse> History { get; set; } = [];
}

/// <summary>Полезная нагрузка POST /notify в MaxBot.</summary>
public class ScheduleChangeDto
{
    public string ChangeType { get; init; } = "";
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public int DayOfWeek { get; init; }
    public int Week { get; init; }
    public int NumberPair { get; init; }
    public string Subject { get; init; } = "";
    public string? Note { get; init; }
    public string? RemovedSubject { get; init; }
    public string? RemovedTeacherName { get; init; }
    public int? RemovedNumberPair { get; init; }
}
```

- [ ] **Step 3: DTO истории**

`CollegeLMS.API/Dtos/ScheduleHistoryDtos.cs`:

```csharp
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleHistoryResponse
{
    public Guid Id { get; set; }
    public ScheduleChangeType ChangeType { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Room { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int NumberPair { get; set; }
    public int Week { get; set; }
    public string? Note { get; set; }
    public string? RemovedSubject { get; set; }
    public string? RemovedRoom { get; set; }
    public int? RemovedNumberPair { get; set; }
}
```

- [ ] **Step 4: Маппер**

`CollegeLMS.API/Mappers/ScheduleHistoryMapper.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class ScheduleHistoryMapper
{
    public static ScheduleHistoryResponse ToDto(this ScheduleHistory h) =>
        new()
        {
            Id = h.Id,
            ChangeType = h.ChangeType,
            AppliedAt = h.AppliedAt,
            AppliedByUserId = h.AppliedByUserId,
            GroupId = h.GroupId,
            GroupName = h.Group?.Name ?? string.Empty,
            TeacherId = h.TeacherId,
            TeacherName = h.Teacher?.User?.FullName,
            Subject = h.Subject,
            Room = h.Room,
            DayOfWeek = h.DayOfWeek,
            NumberPair = h.NumberPair,
            Week = h.Week,
            Note = h.Note,
            RemovedSubject = h.RemovedSubject,
            RemovedRoom = h.RemovedRoom,
            RemovedNumberPair = h.RemovedNumberPair,
        };
}
```

- [ ] **Step 5: Открыть доступ к нормализаторам**

`CollegeLMS.API/Services/ScheduleImportService.cs` — для трёх методов поменять `private static` → `internal static`: `GetPairTime` (строка 77), `NormalizeSubject` (строка 84), `NormalizeTeacherName` (строка 110).

- [ ] **Step 6: Сборка**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: DTO корректировок и истории, маппер, доступ к нормализаторам"
```

---

### Задача 3: API — MaxBotHttpClient, DI, конфигурация

**Files:**
- Create: `CollegeLMS.API/Services/MaxBotHttpClient.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (метод `AddApplicationServices` принимает `IConfiguration` и регистрирует клиент)
- Modify: `CollegeLMS.API/Program.cs`
- Modify: `CollegeLMS.API/appsettings.json`

**Interfaces:**
- Consumes: `ScheduleChangeDto` из Задачи 2.
- Produces: `MaxBotHttpClient.SendChangesAsync(IReadOnlyList<ScheduleChangeDto>, CancellationToken)` для Задачи 5.

- [ ] **Step 1: Клиент**

`CollegeLMS.API/Services/MaxBotHttpClient.cs`:

```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.Services;

/// <summary>
/// HTTP-клиент к боту Max. Отправляет изменения расписания на POST /notify.
/// Fail-safe: недоступность бота не роняет подтверждение корректировки.
/// </summary>
public class MaxBotHttpClient(HttpClient http, ILogger<MaxBotHttpClient> logger)
{
    public async Task SendChangesAsync(
        IReadOnlyList<ScheduleChangeDto> changes,
        CancellationToken ct
    )
    {
        if (changes.Count == 0)
            return;

        try
        {
            var resp = await http.PostAsJsonAsync("/notify", changes.ToList(), ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "MaxBot /notify вернул {Code}: {Body}",
                    resp.StatusCode,
                    body.Length > 200 ? body[..200] : body
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MaxBot недоступен — уведомления об изменениях не отправлены");
        }
    }
}
```

- [ ] **Step 2: DI**

`CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — изменить сигнатуру и телocode метода:

```csharp
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<ICourseDocumentService, CourseDocumentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IWordPressImportService, WordPressImportService>();
        services.AddScoped<IStvccHealthService, StvccHealthService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ITestingService, TestingService>();
        services.AddScoped<ICourseAccessService, CourseAccessService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<ScheduleExportService>();
        services.AddScoped<ScheduleImportService>();
        services.AddScoped<ScheduleCorrectionService>();
        services.AddHttpClient<MaxBotHttpClient>(c =>
        {
            c.BaseAddress = new Uri(config["MaxBot:BaseUrl"] ?? "http://localhost:8080");
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
```

`CollegeLMS.API/Program.cs` — заменить `.AddApplicationServices()` на `.AddApplicationServices(builder.Configuration)`.

- [ ] **Step 3: Конфиг**

`CollegeLMS.API/appsettings.json` — добавить после блока `"WordPress"`:

```json
  "MaxBot": {
    "BaseUrl": "http://localhost:8080"
  },
```

- [ ] **Step 4: Сборка**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: MaxBotHttpClient + DI + конфигурация MaxBot:BaseUrl"
```

---

### Задача 4: API — StudyWeek + ScheduleCorrectionService (превью)

**Files:**
- Create: `CollegeLMS.API/Services/StudyWeek.cs`
- Create: `CollegeLMS.API/Services/ScheduleCorrectionService.cs`

**Interfaces:**
- Consumes: `ScheduleImportService.NormalizeSubject/NormalizeTeacherName/GetPairTime` (internal static, Задача 2), `MaxBotHttpClient` (Задача 3), `ScheduleValidationError` с `Level`.
- Produces: `ScheduleCorrectionService.PreviewAsync(Stream, CancellationToken) → Result<CorrectionPreviewResponse>`.

- [ ] **Step 1: StudyWeek в API**

`CollegeLMS.API/Services/StudyWeek.cs` (синхронизирован с `CollegeLMS.MaxBot/Services/StudyWeek.cs`):

```csharp
namespace CollegeLMS.API.Services;

public static class StudyWeek
{
    /// <summary>Начало семестра. Совпадает с MaxBot StudyWeek.SemesterStart и фронтендом.</summary>
    public static DateTime SemesterStart { get; } = new(2026, 9, 1);

    public static DateTime MondayOf(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        var offset = day == 0 ? 6 : day - 1;
        return date.Date.AddDays(-offset);
    }

    public static int ForDate(DateTime date)
    {
        var diffWeeks = (int)((MondayOf(date) - MondayOf(SemesterStart)).TotalDays / 7);
        return Math.Max(1, diffWeeks + 1);
    }
}
```

- [ ] **Step 2: Сервис корректировок (парсер + превью)**

`CollegeLMS.API/Services/ScheduleCorrectionService.cs`:

```csharp
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleCorrectionService(AppDbContext db, MaxBotHttpClient maxBot)
{
    private static readonly Regex DatePattern =
        new(@"на\s+(\d{1,2})\.(\d{1,2})\.(\d{4})", RegexOptions.Compiled);

    private static readonly string[] DayNames =
        ["", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

    private static string DayName(DayOfWeek day) => DayNames[(int)day];

    public async Task<Result<CorrectionPreviewResponse>> PreviewAsync(
        Stream fileStream,
        CancellationToken ct
    )
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return Result<CorrectionPreviewResponse>.Fail(
                "Файл не является корректным XLSX. Сохраните файл в формате .xlsx.",
                400
            );
        }

        using (workbook)
        {
            var parsed = await ParseWorkbookAsync(workbook, ct);

            return Result<CorrectionPreviewResponse>.Ok(
                new CorrectionPreviewResponse
                {
                    CorrectionDate = parsed.Date,
                    Week = parsed.Week,
                    DayOfWeek = (int)parsed.Date.DayOfWeek,
                    TotalEntries = parsed.Entries.Count,
                    Entries = parsed.Entries,
                    Errors = parsed.Errors,
                }
            );
        }
    }

    private static ScheduleValidationError Error(
        int row,
        int column,
        string level,
        string message
    ) =>
        new()
        {
            Row = row,
            Column = column,
            Level = level,
            Message = message,
        };

    private async Task<(DateTime Date, int Week, List<CorrectionPreviewEntry> Entries, List<ScheduleValidationError> Errors)>
        ParseWorkbookAsync(XLWorkbook workbook, CancellationToken ct)
    {
        var ws = workbook.Worksheet(1);
        var errors = new List<ScheduleValidationError>();
        var entries = new List<CorrectionPreviewEntry>();

        // --- Уровень 1: структура файла ---
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 7)
        {
            errors.Add(Error(0, 0, "structure", "В файле нет данных."));
            return (default, 0, entries, errors);
        }

        var a3 = ws.Cell(3, 1).GetString().Trim();
        var dateMatch = DatePattern.Match(a3);
        if (!dateMatch.Success)
        {
            errors.Add(
                Error(
                    3,
                    1,
                    "structure",
                    string.IsNullOrWhiteSpace(a3)
                        ? "Не найдена дата корректировки в ячейке A3 (например, «на 01.09.2026 г.»)."
                        : "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.»."
                )
            );
            return (default, 0, entries, errors);
        }

        DateTime date;
        try
        {
            date = new DateTime(
                int.Parse(dateMatch.Groups[3].Value),
                int.Parse(dateMatch.Groups[2].Value),
                int.Parse(dateMatch.Groups[1].Value)
            );
        }
        catch
        {
            errors.Add(Error(3, 1, "structure", "Дата в A3 не распознана. Формат: «на ДД.ММ.ГГГГ г.»."));
            return (default, 0, entries, errors);
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors.Add(
                Error(3, 1, "structure", "Указана дата на выходной день. Корректировка применяется к учебным дням.")
            );
            return (default, 0, entries, errors);
        }

        var a5 = ws.Cell(5, 1).GetString().Trim();
        if (!string.Equals(a5, "Группа", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error(5, 1, "structure", "Не найден заголовок «Группа» в ячейке A5."));
            return (default, 0, entries, errors);
        }

        var b5 = ws.Cell(5, 2).GetString();
        var d5 = ws.Cell(5, 4).GetString();
        var hasRemoveHeader = b5.Contains("Снимается", StringComparison.OrdinalIgnoreCase);
        var hasAddHeader = d5.Contains("Вводится", StringComparison.OrdinalIgnoreCase);
        if (!hasRemoveHeader && !hasAddHeader)
        {
            errors.Add(
                Error(
                    5,
                    2,
                    "structure",
                    "Не найдены колонки «Снимается по расписанию» или «Вводится в расписание»."
                )
            );
            return (default, 0, entries, errors);
        }

        var week = StudyWeek.ForDate(date);

        for (int row = 7; row <= lastRow; row++)
        {
            var groupName = ws.Cell(row, 1).GetString().Trim();
            var removeSubject = ws.Cell(row, 2).GetString().Trim();
            var removeTeacher = ws.Cell(row, 3).GetString().Trim();
            var addSubject = ws.Cell(row, 4).GetString().Trim();
            var addTeacher = ws.Cell(row, 5).GetString().Trim();
            var pairValue = ws.Cell(row, 6).Value;
            var note = ws.Cell(row, 7).GetString().Trim();

            var pairNumber = ParsePair(pairValue);

            var anyContent =
                groupName.Length > 0
                || removeSubject.Length > 0
                || removeTeacher.Length > 0
                || addSubject.Length > 0
                || addTeacher.Length > 0
                || pairNumber is not null;

            if (!anyContent)
                continue;

            // --- Уровень 2: данные строки ---
            if (groupName.Length == 0)
            {
                errors.Add(Error(row, 1, "data", $"Строка {row}: не указана группа."));
                continue;
            }

            if (pairNumber is not { } pair || pair < 1 || pair > 8)
            {
                errors.Add(
                    Error(row, 6, "data", $"Строка {row}: не указан/некорректен № пары (F). Ожидается число 1–8.")
                );
                continue;
            }

            var hasRemove = removeSubject.Length > 0 || removeTeacher.Length > 0;
            var hasAdd = addSubject.Length > 0 || addTeacher.Length > 0;

            if ((removeSubject.Length > 0) != (removeTeacher.Length > 0))
            {
                errors.Add(
                    Error(row, 3, "data", $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот).")
                );
                continue;
            }

            if (hasAdd && (addSubject.Length > 0) != (addTeacher.Length > 0))
            {
                errors.Add(
                    Error(row, 5, "data", $"Строка {row}: заполнен предмет, но не указан преподаватель (или наоборот).")
                );
                continue;
            }

            if (!hasRemove && !hasAdd)
            {
                errors.Add(
                    Error(row, 4, "data", $"Строка {row}: не заполнены ни «Снимается», ни «Вводится».")
                );
                continue;
            }

            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == groupName, ct);
            if (group is null)
            {
                errors.Add(Error(row, 1, "data", $"Строка {row}: группа «{groupName}» не найдена в системе."));
                continue;
            }

            Guid? removeTeacherId = null;
            if (removeTeacher.Length > 0)
            {
                var t = await FindTeacherAsync(removeTeacher, ct);
                if (t is null)
                {
                    errors.Add(
                        Error(row, 3, "data", $"Строка {row}: преподаватель «{removeTeacher}» не найден.")
                    );
                    continue;
                }
                removeTeacherId = t;
            }

            Guid? addTeacherId = null;
            if (addTeacher.Length > 0)
            {
                var t = await FindTeacherAsync(addTeacher, ct);
                if (t is null)
                {
                    errors.Add(
                        Error(row, 5, "data", $"Строка {row}: преподаватель «{addTeacher}» не найден.")
                    );
                    continue;
                }
                addTeacherId = t;
            }

            // --- Уровень 3: бизнес-логика ---
            var changeType = (hasRemove, hasAdd) switch
            {
                (true, false) => ScheduleChangeType.Remove,
                (false, true) => ScheduleChangeType.Add,
                _ => ScheduleChangeType.Replace,
            };

            if (changeType == ScheduleChangeType.Add)
            {
                if (await IsPairBusyAsync(group.Id, date.DayOfWeek, pair, week, null, ct))
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} уже занята."
                        )
                    );
                    continue;
                }

                var subject = addSubject.Length > 0
                    ? ScheduleImportService.NormalizeSubject(addSubject)
                    : string.Empty;

                entries.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupId = group.Id,
                        GroupName = group.Name,
                        ChangeType = changeType,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = pair,
                        Subject = subject,
                        TeacherId = addTeacherId,
                        TeacherName = Normalize(addTeacher),
                        Note = note,
                    }
                );
            }
            else if (changeType == ScheduleChangeType.Remove)
            {
                var target = await db
                    .ScheduleEntries.AsNoTracking()
                    .Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(
                        e =>
                            e.GroupId == group.Id
                            && e.DayOfWeek == date.DayOfWeek
                            && e.NumberPair == pair
                            && e.Weeks.Contains(week),
                        ct
                    );

                if (target is null)
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: занятие на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} не найдено."
                        )
                    );
                    continue;
                }

                entries.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupId = group.Id,
                        GroupName = group.Name,
                        ChangeType = changeType,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = pair,
                        RemovedSubject = target.Subject,
                        RemovedTeacherId = target.TeacherId,
                        RemovedTeacherName = target.Teacher?.User?.FullName,
                        RemovedNumberPair = target.NumberPair,
                        Note = note,
                    }
                );
            }
            else // Replace
            {
                var now = DateTime.UtcNow;
                var maxWeeksInPast = 1;
                _ = maxWeeksInPast;

                var removed = await db
                    .ScheduleEntries.AsNoTracking()
                    .Include(e => e.Teacher!)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(
                        e =>
                            e.GroupId == group.Id
                            && e.DayOfWeek == date.DayOfWeek
                            && e.Weeks.Contains(week)
                            && e.Subject == ScheduleImportService.NormalizeSubject(removeSubject)
                            && (
                                removeTeacherId.HasValue
                                    ? e.TeacherId == removeTeacherId.Value
                                    : e.TeacherId == null
                            ),
                        ct
                    );

                if (removed is null)
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: занятие на {DayName(date.DayOfWeek)} {week}-й неделе не найдено."
                        )
                    );
                    continue;
                }

                if (removed.NumberPair == pair)
                {
                    errors.Add(
                        Error(row, 4, "logic", $"Строка {row}: пара снятия и ввода одинакова.")
                    );
                    continue;
                }

                if (await IsPairBusyAsync(group.Id, date.DayOfWeek, pair, week, removed.Id, ct))
                {
                    errors.Add(
                        Error(
                            row,
                            4,
                            "logic",
                            $"Строка {row}: на {DayName(date.DayOfWeek)} {week}-й неделе, пара {pair} уже занята."
                        )
                    );
                    continue;
                }

                entries.Add(
                    new CorrectionPreviewEntry
                    {
                        Row = row,
                        GroupId = group.Id,
                        GroupName = group.Name,
                        ChangeType = changeType,
                        DayOfWeek = (int)date.DayOfWeek,
                        Week = week,
                        NumberPair = pair,
                        Subject = ScheduleImportService.NormalizeSubject(addSubject),
                        TeacherId = addTeacherId,
                        TeacherName = Normalize(addTeacher),
                        RemovedSubject = removed.Subject,
                        RemovedTeacherId = removed.TeacherId,
                        RemovedTeacherName = removed.Teacher?.User?.FullName,
                        RemovedNumberPair = removed.NumberPair,
                        Note = note,
                    }
                );
            }
        }

        return (date, week, entries, errors);
    }

    private static int? ParsePair(XLCellValue pairValue)
    {
        if (pairValue.IsNumber)
            return (int)pairValue.GetNumber();

        var text = pairValue.GetText().Trim();
        return int.TryParse(text, out var n) ? n : null;
    }

    private async Task<Guid?> FindTeacherAsync(string name, CancellationToken ct)
    {
        var normalized = ScheduleImportService.NormalizeTeacherName(name);
        var teacher = await db
            .Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.User.FullName == normalized, ct);
        return teacher?.Id;
    }

    private static string Normalize(string name) =>
        ScheduleImportService.NormalizeTeacherName(name);

    private async Task<bool> IsPairBusyAsync(
        Guid groupId,
        DayOfWeek day,
        int pair,
        int week,
        Guid? excludeEntryId,
        CancellationToken ct
    )
    {
        var query = db.ScheduleEntries.Where(e =>
            e.GroupId == groupId
            && e.DayOfWeek == day
            && e.NumberPair == pair
            && e.Weeks.Contains(week)
        );

        if (excludeEntryId.HasValue)
            query = query.Where(e => e.Id != excludeEntryId.Value);

        return await query.AnyAsync(ct);
    }
}
```

- [ ] **Step 3: Сборка**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: превью корректировки расписания (StudyWeek + парсер)"
```

---

### Задача 5: API — ScheduleCorrectionService (подтверждение + история)

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs`

**Interfaces:**
- Consumes: `ScheduleCorrectionService.PreviewAsync` (Задача 4), `MaxBotHttpClient` (Задача 3), `ClaimsPrincipalExtensions.GetUserId` из `CollegeLMS.API/Extensions/ClaimsPrincipalExtensions.cs`.
- Produces: `ConfirmAsync(CorrectionConfirmRequest, Guid appliedByUserId, CancellationToken) → Result<CorrectionConfirmResult>` и `GetHistoryAsync(Guid?, Guid?, int?, int, int, CancellationToken) → Result<PagedResponse<ScheduleHistoryResponse>>` для Задач 6–7.

- [ ] **Step 1: Добавить методы в сервис**

Добавить в `ScheduleCorrectionService` (после `PreviewAsync`):

```csharp
    public async Task<Result<CorrectionConfirmResult>> ConfirmAsync(
        CorrectionConfirmRequest request,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        if (request.Entries.Count == 0)
            return Result<CorrectionConfirmResult>.Ok(
                new CorrectionConfirmResult { Applied = 0, History = [] }
            );

        var history = new List<ScheduleHistory>();
        var notifChanges = new List<ScheduleChangeDto>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var entry in request.Entries)
            {
                var applied = await ApplyEntryAsync(entry, appliedByUserId, ct);
                history.Add(applied.History);
                if (applied.Change is not null)
                    notifChanges.Add(applied.Change);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // Оповещение MaxBot — fail-safe, после фиксации транзакции
        await maxBot.SendChangesAsync(notifChanges, ct);

        var ids = history.Select(h => h.Id).ToList();
        var saved = await db
            .ScheduleHistory.AsNoTracking()
            .Include(h => h.Group)
            .Include(h => h.Teacher!)
            .ThenInclude(t => t.User)
            .Where(h => ids.Contains(h.Id))
            .OrderBy(h => h.AppliedAt)
            .ToListAsync(ct);

        return Result<CorrectionConfirmResult>.Ok(
            new CorrectionConfirmResult
            {
                Applied = history.Count,
                History = saved.Select(h => h.ToDto()).ToList(),
            }
        );
    }

    public async Task<Result<PagedResponse<ScheduleHistoryResponse>>> GetHistoryAsync(
        Guid? groupId,
        Guid? teacherId,
        int? week,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db.ScheduleHistory.AsNoTracking()
            .Include(h => h.Group)
            .Include(h => h.Teacher!)
            .ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(h => h.GroupId == groupId.Value);

        if (teacherId.HasValue)
            query = query.Where(
                h => h.TeacherId == teacherId.Value || h.RemovedTeacherId == teacherId.Value
            );

        if (week.HasValue)
            query = query.Where(h => h.Week == week.Value);

        query = query.OrderByDescending(h => h.AppliedAt).ThenByDescending(h => h.CreatedAt);

        var total = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 200);
        var items = await query
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        return Result<PagedResponse<ScheduleHistoryResponse>>.Ok(
            new PagedResponse<ScheduleHistoryResponse>(
                items.Select(h => h.ToDto()).ToList(),
                total,
                p,
                ps
            )
        );
    }

    private record AppliedEntry(ScheduleHistory History, ScheduleChangeDto? Change);

    private async Task<AppliedEntry> ApplyEntryAsync(
        CorrectionPreviewEntry entry,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == entry.GroupId, ct);
        if (group is null)
            throw new InvalidOperationException($"Группа {entry.GroupId} не найдена.");

        var day = (DayOfWeek)entry.DayOfWeek;
        var utcNow = DateTime.UtcNow;

        switch (entry.ChangeType)
        {
            case ScheduleChangeType.Add:
            {
                var entity = CreateEntry(group.Id, entry, day, utcNow);
                db.ScheduleEntries.Add(entity);

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Add,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = entry.TeacherId,
                    Subject = entry.Subject ?? string.Empty,
                    Room = entity.Room,
                    DayOfWeek = day,
                    NumberPair = entry.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    ChangeType = "Add",
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = entry.TeacherId,
                    TeacherName = entry.TeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = entry.NumberPair,
                    Subject = entry.Subject ?? string.Empty,
                    Note = entry.Note,
                };

                return new AppliedEntry(history, change);
            }

            case ScheduleChangeType.Remove:
            {
                var target = await db.ScheduleEntries.FirstOrDefaultAsync(
                    e =>
                        e.GroupId == group.Id
                        && e.DayOfWeek == day
                        && e.NumberPair == entry.NumberPair
                        && e.Weeks.Contains(entry.Week),
                    ct
                );
                if (target is null)
                    throw new InvalidOperationException(
                        $"Занятие на {day} {entry.Week}-й неделе, пара {entry.NumberPair} не найдено."
                    );

                target.Weeks = target.Weeks.Where(w => w != entry.Week).ToList();
                if (target.Weeks.Count == 0)
                    db.ScheduleEntries.Remove(target);
                else
                    target.UpdatedAt = utcNow;

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Remove,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = target.TeacherId,
                    Subject = target.Subject,
                    Room = target.Room,
                    DayOfWeek = day,
                    NumberPair = target.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    ChangeType = "Remove",
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = target.TeacherId,
                    TeacherName = entry.RemovedTeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = target.NumberPair,
                    Subject = target.Subject,
                    Note = entry.Note,
                };

                return new AppliedEntry(history, change);
            }

            default: // Replace
            {
                var removed = await db.ScheduleEntries.FirstOrDefaultAsync(
                    e =>
                        e.GroupId == group.Id
                        && e.DayOfWeek == day
                        && e.Weeks.Contains(entry.Week)
                        && (
                            entry.RemovedTeacherId.HasValue
                                ? e.TeacherId == entry.RemovedTeacherId.Value
                                : e.TeacherId == null
                        )
                        && e.Subject == (entry.RemovedSubject ?? string.Empty),
                    ct
                );
                if (removed is null)
                    throw new InvalidOperationException(
                        $"Занятие на {day} {entry.Week}-й неделе не найдено."
                    );

                if (removed.NumberPair == entry.NumberPair)
                    throw new InvalidOperationException("Пара снятия и ввода одинакова.");

                removed.Weeks = removed.Weeks.Where(w => w != entry.Week).ToList();
                if (removed.Weeks.Count == 0)
                    db.ScheduleEntries.Remove(removed);
                else
                    removed.UpdatedAt = utcNow;

                var entity = CreateEntry(group.Id, entry, day, utcNow);
                db.ScheduleEntries.Add(entity);

                var history = new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Replace,
                    AppliedAt = utcNow,
                    AppliedByUserId = appliedByUserId,
                    GroupId = group.Id,
                    TeacherId = entry.TeacherId,
                    Subject = entry.Subject ?? string.Empty,
                    Room = entity.Room,
                    DayOfWeek = day,
                    NumberPair = entry.NumberPair,
                    Week = entry.Week,
                    Note = entry.Note,
                    RemovedSubject = removed.Subject,
                    RemovedTeacherId = removed.TeacherId,
                    RemovedRoom = removed.Room,
                    RemovedNumberPair = removed.NumberPair,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                };
                db.ScheduleHistory.Add(history);

                var change = new ScheduleChangeDto
                {
                    ChangeType = "Replace",
                    GroupId = group.Id,
                    GroupName = group.Name,
                    TeacherId = entry.TeacherId,
                    TeacherName = entry.TeacherName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = entry.NumberPair,
                    Subject = entry.Subject ?? string.Empty,
                    Note = entry.Note,
                    RemovedSubject = removed.Subject,
                    RemovedTeacherName = entry.RemovedTeacherName,
                    RemovedNumberPair = removed.NumberPair,
                };

                return new AppliedEntry(history, change);
            }
        }
    }

    private static ScheduleEntry CreateEntry(
        Guid groupId,
        CorrectionPreviewEntry entry,
        DayOfWeek day,
        DateTime utcNow)
    {
        var (start, end) = ScheduleImportService.GetPairTime(day, entry.NumberPair);

        var entity = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = entry.TeacherId,
            Subject = ScheduleImportService.NormalizeSubject(entry.Subject ?? string.Empty),
            Room = string.Empty,
            DayOfWeek = day,
            NumberPair = entry.NumberPair,
            StartTime = start,
            EndTime = end,
            Weeks = new List<int> { entry.Week },
            LessonType = LessonType.Practice,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        return entity;
    }
}

- [ ] **Final: Git**
git add -A && git commit -m "feat: подтверждение корректировки и история (транзакция, /notify)"

---

### Задача 6: API — контроллер, Swagger-примеры, Postman

**Files:**
- Create: `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs`
- Create: `CollegeLMS.API/SwaggerExamples/CorrectionPreviewResponseExample.cs`
- Create: `CollegeLMS.API/SwaggerExamples/ScheduleHistoryResponseExample.cs`
- Modify: `docs/spec/CollegeLMS.postman_collection.json`

**Interfaces:**
- Consumes: `IScheduleCorrectionService` (Задачи 4–5), `[Authorize(Roles = "Dispatcher,Admin")]`, `ClaimsPrincipalExtensions`.
- Produces: 3 REST endpoint'а под маршрутом `api/schedule`.

- [ ] **Step 1: Controller**

`CollegeLMS.API/Controllers/ScheduleCorrectionController.cs`, route `[Route("api/schedule")]`:

```csharp
[Authorize(Roles = "Dispatcher,Admin")]
[ApiController]
[Route("api/schedule")]
public class ScheduleCorrectionController(IScheduleCorrectionService service) : ControllerBase
{
    // POST api/schedule/correction/preview  — IFormFile file
    // POST api/schedule/correction/confirm   — CorrectionConfirmRequest
    // GET  api/schedule/history?page&pageSize&groupId&week&changeType
}
```

- `POST correction/preview`: `[Consumes("multipart/form-data")]`, вызывает `service.PreviewAsync(file, ct)`, 200 `CorrectionPreviewResponse`, 400 — невалидная структура файла.
- `POST correction/confirm`: `service.ConfirmAsync(request, User, ct)`, 200 `CorrectionConfirmResult`, 400 — невалидная логика, 404 — группа не найдена.
- `GET history`: `service.GetHistoryAsync(...)`, 200 `PagedResponse<ScheduleHistoryResponse>`.

- [ ] **Step 2: Swagger-примеры**

`CorrectionPreviewResponseExample` и `ScheduleHistoryResponseExample` — по паттерну существующих `SwaggerExamples/*ResponseExample.cs`. `[SwaggerResponse]` для 200/400/401/403/404 + `ErrorResponseExample`. `<summary>/<remarks>/<response code>` на русском.

- [ ] **Step 3: Postman**

`docs/spec/CollegeLMS.postman_collection.json` — добавить 3 запроса (preview multipart, confirm, history) в коллекцию с переменной `token`.

- [ ] **Step 4: Build**
```
dotnet build
```

- [ ] **Final: Git**
git add -A && git commit -m "docs: swagger-примеры корректировок, Postman-запросы"

---

### Задача 7: API — changeTags в расписании

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleDtos.cs`, `CollegeLMS.API/Mappers/ScheduleMapper.cs`, `CollegeLMS.API/Services/ScheduleService.cs`

**Interfaces:**
- Consumes: таблицу `schedule_history` (Задача 1), `ScheduleEntry`, маппер.
- Produces: `ChangeTag` + `ChangeTags` в `ScheduleResponse`.

- [ ] **Step 1: DTO**

`ScheduleDtos`: `ChangeTag { ScheduleChangeType ChangeType; int Week; }`, поле `List<ChangeTag> ChangeTags` в `ScheduleResponse` (default `[]`).

- [ ] **Step 2: Сервис**

`ScheduleService.GetAllAsync`: после выборки записей одним запросом к `schedule_history` для выбранной недели (фильтр `Week`-параметра) собрать словарь `(GroupId, DayOfWeek, NumberPair) → List<ChangeTag>` и проставить в DTO. Remove-операции: пара отмечена тегом, при наличии `AsNoTracking` чтения группировку делать на `List`.

- [ ] **Step 3: Маппер**

`ScheduleMapper.ToDto` — принять `ChangeTags` и заполнить в `ScheduleResponse`.

- [ ] **Step 4: Build**
```
dotnet build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: changeTags в расписании (метки изменённых пар)"

---

### Задача 8: Backend unit-тесты

**Files:**
- Create: `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs`
- Modify (при необходимости): `CollegeLMS.Tests/Fixtures/`

**Interfaces:**
- Consumes: `IScheduleCorrectionService`, EF Core InMemory, Moq (`ILogger`, `HttpClient`), Bogus, `ScheduleImportService`.
- Produces: unit-покрытие превью и применения (паттерны из `dotnet-test` skill).

- [ ] **Step 1: Превью**
  - Невалидный xlsx → structure-ошибка `Level = "structure"`; пустые обязательные поля → data; неизвестные группа/преподаватель → data.
  - logic: «пара занята» (Add/Replace на занятой паре недели N), повтор операции на той же паре, Replace с совпадающими парами → «пара снятия и ввода одинакова».
- [ ] **Step 2: Подтверждение**
  - Remove: убирает неделю из `Weeks`; при пустом списке удаляет строку.
  - Add: создаёт `ScheduleEntry` с `Weeks = [N]`, времена из `GetPairTime`.
  - Replace: снятие + ввод, история с `Removed*`; ошибка при same pair.
  - Транзакция: сбой на одной операции → откат всех (проверить пустые таблицы).
- [ ] **Step 3: Fail-safe MaxBot**
  - При недоступности `/notify` (HttpClient throw) `ConfirmAsync` всё равно возвращает Ok и история пишется (заглушка `HttpMessageHandler`).
- [ ] **Step 4: Test**
```
dotnet test
```

- [ ] **Final: Git**
git add -A && git commit -m "test: unit-тесты корректировки расписания"

---

### Задача 9: Backend integration-тесты

**Files:**
- Create: `CollegeLMS.Tests/Integration/Controllers/ScheduleCorrectionControllerTests.cs`

**Interfaces:**
- Consumes: `WebApplicationFactory` (паттерн `ScheduleControllerTests`), `ITokenService` для токена Dispatcher/Admin, фикстуры группы/преподавателя/занятий.
- Produces: сквозной сценарий preview → confirm → проверка в БД.

- [ ] **Step 1: preview**
  - Валидный `Корректировка.xlsx` (создать тестовый файл) → 200, `Errors == []`, `TotalEntries > 0`.
  - Битый файл → 400 с `Level = "structure"`.
- [ ] **Step 2: confirm**
  - preview → confirm; проверить `schedule_entries` (изменения недели N) и `schedule_history` (строки на каждую операцию).
  - Защита ролей: без токена → 401, токен Student → 403.
- [ ] **Step 3: history**
  - Пагинация и фильтры `groupId`, `week`, `changeType`.
- [ ] **Step 4: Test**
```
dotnet test
```

- [ ] **Final: Git**
git add -A && git commit -m "test: integration-тесты корректировки расписания"

---

### Задача 10: MaxBot — сущность ScheduleRevision, конфиг, raw SQL

**Files:**
- Create: `CollegeLMS.MaxBot/Models/ScheduleRevision.cs`
- Create: `CollegeLMS.MaxBot/Data/Configurations/ScheduleRevisionConfiguration.cs`
- Modify: `CollegeLMS.MaxBot/Data/MaxBotDbContext.cs`, `CollegeLMS.MaxBot/Program.cs`

**Interfaces:**
- Consumes: `MaxBotDbContext` (EnsureCreated!), `MaxBotOptions`.
- Produces: таблица `schedule_revisions`, `DbSet<ScheduleRevision>`.

- [ ] **Step 1: Модель**

`ScheduleRevision`: `long Id`, `Guid ForeignId`, `string ChangeType`, `string GroupName`, `string? TeacherName`, `string Subject`, `string Room`, `string DayOfWeek`, `int Week`, `int NumberPair`, `string? Note`, `string? RemovedSubject`, `string? RemovedTeacherName`, `int? RemovedNumberPair`, `DateTime CreatedAt`.

- [ ] **Step 2: EF-конфиг**

`ScheduleRevisionConfiguration`: `ToTable("schedule_revisions")`, `HasMaxLength` на строковых, индексы `(GroupName)`, `(TeacherName)`, `(CreatedAt)` — с кастомными именами (`HasDatabaseName`).

- [ ] **Step 3: DbContext**

`MaxBotDbContext`: `DbSet<ScheduleRevision> ScheduleRevisions { get; set; }`.

- [ ] **Step 4: Raw SQL для существующей БД**

`EnsureCreated` не создаст новую таблицу в существующей БМ → в `Program.cs` выполнить идемпотентный `CREATE TABLE IF NOT EXISTS schedule_revisions (...)` (snake_case, колонки под `MaxBotDbContext`).

- [ ] **Step 5: Build**
```
dotnet build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: MaxBot ScheduleRevision + raw SQL"

---

### Задача 11: MaxBot — эндпоинт /notify, форматтер, рассылка

**Files:**
- Modify: `CollegeLMS.MaxBot/Program.cs`, `CollegeLMS.MaxBot/Services/MessageFormatter.cs`, `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs`

**Interfaces:**
- Consumes: `ScheduleRevision`, `UserSettings` (Group/Teacher `Name`), отправка через MAX API, `MessageFormatter`.
- Produces: `POST /notify` с телом `NotifyChangeDto[]`, уведомления подписчикам.

- [ ] **Step 1: DTO**

`CollegeLmsApiDtos.cs`: `NotifyChangeDto` (camelCase) — поля из API `ScheduleChangeDto`: `ChangeType, GroupId, GroupName, TeacherId?, TeacherName?, DayOfWeek, Week, NumberPair, Subject, Note?, RemovedSubject?, RemovedTeacherName?, RemovedNumberPair?`.

- [ ] **Step 2: Endpoint**

`Program.cs` MaxBot: `MapPost("/notify")` — сохранить `ScheduleRevision` для каждого элемента, вызвать рассылку, вернуть `200`. Fail-safe: исключения логируются, не пробрасываются.

- [ ] **Step 3: Рассылка подписчикам**

Сервис-нотификатор: по ревизиям выбрать `UserSettings` — Роль «Группа»: `Group.Name == GroupName`; Роль «Преподаватель»: `Teacher.Name == TeacherName`; `NotifyEnabled`, день недели в `NotifyDays`. Отправка `sendText` через MAX API.

- [ ] **Step 4: Форматтер**

`MessageFormatter.FormatChangeNotification(ScheduleRevision)` — формат из спеки:

```
🔔 Изменение в расписании / ПО262 · Вторник / Нед. 1 · Пара 2
📖 История (замена)
Преподаватель: Петренко В.Б.
Примечание: вм.4 п
```

`MessageFormatter.FormatChangeNotificationTitle` — зависит от `ChangeType` (добавлена/снята/замена).

- [ ] **Step 5: Build**
```
dotnet build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: MaxBot /notify + форматтер уведомлений"

---

### Задача 12: MaxBot — кнопка «Мои изменения»

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`, `CollegeLMS.MaxBot/Services/MessageFormatter.cs`

**Interfaces:**
- Consumes: `UserSettings`, `ScheduleRevisions`, существующий callback-свитч (`CallbackPayload`).
- Produces: callback-action `changes` / `changes_page`; сообщение-список.

- [ ] **Step 1: Callback**
  - В `MaxBotService` обработчик добавить ветки `changes`, `changes_page`.
  - Группа: `WHERE GroupName = Group.Name`; Преподаватель: `WHERE TeacherName = Teacher.Name`. Сортировка `CreatedAt desc`.
- [ ] **Step 2: Формат**

`MessageFormatter.FormatMyChanges(revisions, page, pageSize=20)` — нумерованный список, кнопки «←/→» по паттерну `page` для групп.

- [ ] **Step 3: Меню**

В главное меню добавить пункт «Мои изменения» с payload `changes`.

- [ ] **Step 4: Build**
```
dotnet build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: MaxBot «Мои изменения»"

---

### Задача 13: MaxBot unit-тесты

**Files:**
- Modify: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`
- Create: `CollegeLMS.MaxBot.Tests/ScheduleNotifierTests.cs` (фильтр подписчиков)

- [ ] **Step 1: Форматтер**
  - `FormatChangeNotification` — эмодзи, поля, заголовок по типу.
  - `FormatMyChanges` — пагинация 20/стр., пустой список.
- [ ] **Step 2: Рассилка**
  - Выборка получателей по роли/`Name`/`NotifyDays`; `NotifyEnabled=false` исключён.
- [ ] **Step 3: Test**
```
dotnet test
```

- [ ] **Final: Git**
git add -A && git commit -m "test: MaxBot тесты форматтера и рассылки"

---

### Задача 14: Frontend — типы, API-клиент, страница «Корректировка»

**Files:**
- Create: `CollegeLMS.Next/types/correction.ts`
- Create: `CollegeLMS.Next/api/correction.ts`
- Modify: `CollegeLMS.Next/app/(authenticated)/dispatcher/correction/page.tsx` (rewrite)

**Interfaces:**
- Consumes: `api/client` (axios + `unwrap()`), `types/schedule.ts`, тосты (как в `ScheduleImportDialog`).
- Produces: страница `/dispatcher/correction` — загрузка → превью → подтверждение → журнал.

- [ ] **Step 1: Типы**

`types/correction.ts`: `CorrectionPreviewResponse`, `CorrectionPreviewEntry`, `ScheduleValidationError {Row, Column, Level, Message}`, `ConfirmResult`, `ChangeTags`.

- [ ] **Step 2: API-клиент**

`api/correction.ts`: `previewCorrection(file: File)`, `confirmCorrection(entries)`, `getHistory(params)` — все через общий `unwrap()`.

- [ ] **Step 3: Страница** (паттерн `ScheduleImportDialog`)
  - Вкладки «Импорт» / «Журнал».
  - Дроп-зона загрузки → вызов `previewCorrection` → таблица операций: тип (Add/Remove/Replace), группа, день, пара, предмет, преподаватель, примечание.
  - Ошибки подсвечены (красная иконка + тултип `Message`); кнопка «Применить» `disabled` при `errors.length > 0`.
  - Подтверждение → тост «Применено N изменений», обновление журнала.
- [ ] **Step 4: Build**
```
npm run build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: страница «Корректировка» (превью, подтверждение, журнал)"

---

### Задача 15: Frontend — маркировка изменённых пар

**Files:**
- Modify: `CollegeLMS.Next/types/schedule.ts`, `CollegeLMS.Next/components/ScheduleTable.tsx`, `CollegeLMS.Next/app/(authenticated)/schedule/page.tsx`, `CollegeLMS.Next/components/SemesterView.tsx`

**Interfaces:**
- Consumes: `ScheduleResponse.ChangeTags` (Задача 7/14), Lucide (`Plus`, `Minus`, `Repeat`, `BadgeAlert`), тултипы.
- Produces: визуальная маркировка изменённой пары.

- [ ] **Step 1: Тип**

`types/schedule.ts`: `ChangeTag { ChangeType: 'add' | 'remove' | 'replace'; Week: number }` + `ChangeTags` в `ScheduleResponse`.

- [ ] **Step 2: ScheduleTable/SemesterView**

Для пары с `ChangeTags`: бейдж (Add — зелёный «+», Remove — красный «−», Replace — синий «↻») + тултип «Замена: Иванов → Марченко (вм. 4 п)». Remove-пара отображается помеченной как «снята», с просмотром старой подписи через тултип.

- [ ] **Step 3: Проверка адаптивности**

1366×768 и ~393px (в т.ч. на телефоне). Empty state «Изменений нет» — необязателен здесь (есть в журнале).

- [ ] **Step 4: Build**
```
npm run build
```

- [ ] **Final: Git**
git add -A && git commit -m "feat: маркировка изменённых пар в расписании"

---

### Задача 16: Docs + DevOps

**Files:**
- Create: `docs/diagrams/er/schedule_history.puml`, `docs/diagrams/sequence/correction.puml`, `docs/diagrams/class/schedule-correction-service.puml`
- Modify: `docker-compose.yml` (env `MaxBot__BaseUrl` для api)

- [ ] **Step 1: PlantUML**

ER (ScheduleHistory, ScheduleEntry, ScheduleRevision), Sequence (импорт → превью → confirm → POST /notify → бот → подписчик), Class (`ScheduleCorrectionService`, `MaxBotHttpClient`, `ScheduleImportService`).

- [ ] **Step 2: docker-compose**

Проверить имя сервиса MaxBot в `docker-compose.yml` (профиль `max-bot`); для контейнера API добавить `MaxBot__BaseUrl=http://maxbot:8080` (порт из Dockerfile).

- [ ] **Step 3: Проверка стека**
```
docker compose up --build -d --profile max-bot
docker compose ps
```

- [ ] **Final: Git**
git add -A && git commit -m "docs: PlantUML корректировок; chore: MaxBot__BaseUrl в compose"

---

## Вехи и готовность (Milestones / DoD)

| Веха | Задачи | Гейт |
|------|--------|------|
| M1 | 1–7 | `dotnet build` |
| M2 | 8–9 | `dotnet test` |
| M3 | 10–13 | `dotnet build` + `dotnet test` (MaxBot) |
| M4 | 14–15 | `npm run build` (Next) |
| M5 | 16 | `docker compose up --build -d --profile max-bot` |

**Definition of Done:**
- [ ] `dotnet build` и `dotnet test` проходят (API + MaxBot)
- [ ] `npm run build` фронтенда
- [ ] Swagger-примеры и Postman-коллекция обновлены
- [ ] PlantUML-диаграммы сгенерированы
- [ ] `docker compose --profile max-bot up --build` поднимается
- [ ] На push в master — CD разворачивает на VPS

## Риски

- **Fail-safe /notify**: сбой MaxBot не должен откатывать транзакцию подтверждения — в `ConfirmAsync` вызов после `CommitAsync`, обёрнут try-catch + лог.
- **EnsureCreated + raw SQL**: порядок создания таблицы `schedule_revisions` — выполнять после `EnsureCreated`, идемпотентно.
- **Пара снятия и ввода одинакова**: по решению пользователя — ошибка (уровень 3); строка 9 эталонного `Корректировка.xlsx` невалидна и в примерах используется как негативный случай.
- **Времена пар**: единый источник — `ScheduleImportService.GetPairTime` (сделать `internal static`), чтобы фронт/бот и API не расходились.