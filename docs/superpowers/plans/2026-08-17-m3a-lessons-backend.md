# M3a «Занятия: бэкенд» Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перестроить бэкенд курсов: полностью удалить задания (Assignment/Submission), переименовать Lecture → Lesson, добавить позиционирование/reorder, текущее занятие (IsCurrent) и документацию курса (CourseDocument).

**Architecture:** Одна миграция (`AddLessonsRemoveAssignments`) применяется при старте API на проде. Все проверки прав — через `ICourseAccessService` (владелец || соавтор || админ). Удаление заданий идёт вместе с переименованием, поэтому промежуточных состояний модели не коммитим — каждый коммит компилируется и тесты зелёные.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core + Npgsql, FluentValidation, xUnit + Moq + Bogus, WebApplicationFactory.

**Дизайн:** `docs/superpowers/specs/2026-08-17-m3-lessons-design.md`

## Global Constraints

- Все данные и комментарии в коде на русском; сообщения об ошибках на русском
- `Result<T>` везде, без try-catch в сервисах/контроллерах
- Primary constructor DI, `CancellationToken ct` на всех async-методах
- `AsNoTracking()` на чтении; Guid PK с `ValueGeneratedNever()`
- String props `HasMaxLength()`; enum props `HasConversion<string>()` + `HasMaxLength()`
- Мапперы в `Mappers/`, интерфейсы в `Interfaces/`, DI — в `Extensions/ServiceCollectionExtensions.cs`
- Enum сущности занятия: **`LessonKind`** (Lecture/Practice/SelfStudy). **НЕ** `LessonType` — он занят расписанием (`ScheduleEntry.LessonType`)
- API-поле типа занятия: `kind` (string, значения "Lecture"|"Practice"|"SelfStudy")
- **ВАЖНО для миграции:** колонки переименовываются через `RenameColumn`/`RenameIndex` (сохранение данных), НЕ через Drop+Add
- Частичный уникальный индекс IsCurrent — в `Data/DbConstraints.cs` (идемпотентный SQL)
- Docker-команды (нет локального dotnet): `docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "<CMD>"`
- Миграции: файлы создаются от root в docker → `chown user1:user1` после создания
- Фронтенд в M3a НЕ трогаем (M3b). После merge M3a на проде кратковременно сломаны страницы курса до деплоя M3b — нормально
- Тесты: `docker run ... sh -c "dotnet test CollegeLMS.Tests --filter ..."` — до 323 тестов (после M3a состав изменится)
- Git-префиксы: `feat:` / `fix:` / `docs:` / `test:` / `refactor:` / `chore:`

---

### Task 1: Удаление заданий (Assignment / AssignmentSubmission)

**Files:**
- Delete: `CollegeLMS.API/Entities/Assignment.cs`, `CollegeLMS.API/Entities/AssignmentSubmission.cs`
- Delete: `CollegeLMS.API/Data/Configurations/AssignmentConfiguration.cs`, `CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs`
- Delete: `CollegeLMS.API/Controllers/AssignmentController.cs`, `CollegeLMS.API/Controllers/SubmissionController.cs`
- Delete: `CollegeLMS.API/Services/AssignmentService.cs`, `CollegeLMS.API/Services/SubmissionService.cs`
- Delete: `CollegeLMS.API/Interfaces/IAssignmentService.cs`, `CollegeLMS.API/Interfaces/ISubmissionService.cs`
- Delete: `CollegeLMS.API/Dtos/AssignmentRequest.cs`, `AssignmentResponse.cs`, `SubmissionRequest.cs`, `SubmissionResponse.cs`
- Delete: `CollegeLMS.API/Mappers/AssignmentMapper.cs`, `SubmissionMapper.cs`
- Delete: `CollegeLMS.API/Validators/AssignmentRequestValidator.cs`, `SubmissionRequestValidator.cs`
- Delete: `CollegeLMS.API/SwaggerExamples/AssignmentResponseExample.cs`, `SubmissionResponseExample.cs`
- Delete: `CollegeLMS.Tests/Integration/Controllers/AssignmentControllerTests.cs`, `SubmissionControllerTests.cs`
- Delete: `CollegeLMS.Tests/Unit/Services/SubmissionServiceTests.cs`
- Delete: `CollegeLMS.Tests/Fixtures/AssignmentFixture.cs`, `SubmissionFixture.cs`
- Modify: `CollegeLMS.API/Entities/Course.cs`, `Student.cs`, `CourseMaterial.cs`, `Data/AppDbContext.cs`, `Data/DbConstraints.cs`, `Dtos/CourseResponse.cs`, `Mappers/CourseMapper.cs`, `Services/CourseService.cs`, `Services/DashboardService.cs`, `Services/MaterialService.cs`, `Controllers/MaterialController.cs`, `Extensions/ServiceCollectionExtensions.cs`, `CollegeLMS.Tests/Unit/Services/DashboardServiceTests.cs`, `CourseServiceTests.cs`

**Interfaces:**
- Consumes: текущая модель `AppDbContext` (DbSets `Assignments`, `AssignmentSubmissions`)
- Produces: модель без следов заданий; `CourseProgressResponse` пока остаётся с полями (поправка — в Task 6); `DashboardService` считает прогресс только по тестам

- [x] **Step 1: Удалить файлы заданий/ответов**

```bash
rm CollegeLMS.API/Entities/Assignment.cs CollegeLMS.API/Entities/AssignmentSubmission.cs \
   CollegeLMS.API/Data/Configurations/AssignmentConfiguration.cs CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs \
   CollegeLMS.API/Controllers/AssignmentController.cs CollegeLMS.API/Controllers/SubmissionController.cs \
   CollegeLMS.API/Services/AssignmentService.cs CollegeLMS.API/Services/SubmissionService.cs \
   CollegeLMS.API/Interfaces/IAssignmentService.cs CollegeLMS.API/Interfaces/ISubmissionService.cs \
   CollegeLMS.API/Dtos/AssignmentRequest.cs CollegeLMS.API/Dtos/AssignmentResponse.cs \
   CollegeLMS.API/Dtos/SubmissionRequest.cs CollegeLMS.API/Dtos/SubmissionResponse.cs \
   CollegeLMS.API/Mappers/AssignmentMapper.cs CollegeLMS.API/Mappers/SubmissionMapper.cs \
   CollegeLMS.API/Validators/AssignmentRequestValidator.cs CollegeLMS.API/Validators/SubmissionRequestValidator.cs \
   CollegeLMS.API/SwaggerExamples/AssignmentResponseExample.cs CollegeLMS.API/SwaggerExamples/SubmissionResponseExample.cs \
   CollegeLMS.Tests/Integration/Controllers/AssignmentControllerTests.cs CollegeLMS.Tests/Integration/Controllers/SubmissionControllerTests.cs \
   CollegeLMS.Tests/Unit/Services/SubmissionServiceTests.cs \
   CollegeLMS.Tests/Fixtures/AssignmentFixture.cs CollegeLMS.Tests/Fixtures/SubmissionFixture.cs
```

- [x] **Step 2: Убрать связи в сущностях**

`CollegeLMS.API/Entities/Course.cs` — удалить строки 20–21 (свойство `Assignments`):

```csharp
    [JsonIgnore]
    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
```

`CollegeLMS.API/Entities/Student.cs` — удалить свойство `Submissions` (строки 17–19), оставить `TestAttempts`.

`CollegeLMS.API/Entities/CourseMaterial.cs` — удалить свойство `AssignmentId` (строка 9):

```csharp
    public Guid CourseId { get; set; }
    public Guid? LectureId { get; set; }
    public string FileName { get; set; } = string.Empty;
```

- [x] **Step 3: Убрать DbSet из AppDbContext**

`CollegeLMS.API/Data/AppDbContext.cs` — удалить строки 19–20:

```csharp
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
```

- [x] **Step 4: Убрать CHECK-секции из DbConstraints**

`CollegeLMS.API/Data/DbConstraints.cs` — удалить блок «Assignments» (строки 74–84) и блок «Submissions» (строки 143–153) целиком.

- [x] **Step 5: Убрать регистрации DI**

`CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — удалить строки 183–184:

```csharp
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
```

- [x] **Step 6: Поправить MaterialService / MaterialController**

`CollegeLMS.API/Services/MaterialService.cs`:
- Сигнатура `UploadAsync`: убрать параметр `Guid? assignmentId` (строка 19), `AssignmentId = null` из объекта `CourseMaterial` (строка 49)
- `GetByCourseAsync` — без изменений

`CollegeLMS.API/Controllers/MaterialController.cs`:
- Убрать `[FromQuery] Guid? assignmentId` (строка 51) и передачу `assignmentId` в вызов сервиса (строка 64)

- [x] **Step 7: Поправить CourseService — убрать Assignments**

`CollegeLMS.API/Services/CourseService.cs` — во всех местах убрать `.Include(c => c.Assignments)`:
- строки 29, 77, 167, 187 (в `GetAllAsync`, `GetByIdAsync`, `CreateAsync` (re-query), `UpdateAsync`)
- строка 255 (в `DeleteAsync`: оставить только `.Include(c => c.Lectures)`)

`CollegeLMS.API/Mappers/CourseMapper.cs` — удалить строку 28 (`AssignmentCount`).
`CollegeLMS.API/Dtos/CourseResponse.cs` — удалить строку 16 (`AssignmentCount`).

- [x] **Step 8: Поправить DashboardService — прогресс по тестам**

`CollegeLMS.API/Services/DashboardService.cs` (GetStudentDashboardAsync, строки 58–88):

```csharp
        var courses = await db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Where(c => courseIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(ct);

        var result = new List<CourseWithProgressDto>();
        foreach (var course in courses)
        {
            var totalTests = await db.Tests.CountAsync(t => t.CourseId == course.Id, ct);
            var completedTests = await db.TestAttempts.CountAsync(
                a =>
                    a.StudentId == student.Id
                    && a.Test.CourseId == course.Id
                    && a.Status == Entities.Enums.AttemptStatus.Completed,
                ct
            );

            var total = totalTests;
            var completed = completedTests;
            var percent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;
```

- [x] **Step 9: Поправить тесты**

`CollegeLMS.Tests/Unit/Services/DashboardServiceTests.cs` — удалить тесты/части, использующие `DashboardFixture.CreateAssignmentFaker()` / `CreateSubmissionFaker()` и `AssignmentSubmission`; прогресс-ассерты пересчитать только по тестам. Если тест проверял «смешанный прогресс» — переписать на «прогресс по тестам» (пример: 1 тест пройден из 2 → 50%).

`CollegeLMS.Tests/Unit/Services/CourseServiceTests.cs` — тесты `GetProgress*` переписать: убрать setup с `Assignments`/`AssignmentSubmissions`, ожидать прогресс только по тестам.

- [x] **Step 10: Собрать проект и проверить отсутствие ссылок**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build CollegeLMS.API"
```
Expected: `Build succeeded` (0 ошибок).

```bash
grep -rn "Assignments\|AssignmentSubmission\|SubmissionService\|AssignmentService" CollegeLMS.API --include=*.cs | grep -v Migrations || echo "ЧИСТО"
```
Ожидается: только совпадения в `Migrations/` (история — не трогаем).

- [x] **Step 11: Прогнать тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests"
```
Expected: все тесты PASS (~305, без удалённых ~18).

- [x] **Step 12: Commit**

```bash
git add -A
git commit -m "refactor: удалены задания и ответы на задания (Assignment/Submission)"
```

---

### Task 2: Lecture → Lesson (rename по всему бэкенду)

**Files:**
- Rename: `Entities/Lecture.cs` → `Entities/Lesson.cs`; `Entities/Enums/LectureType.cs` → `Entities/Enums/LessonKind.cs`; `Data/Configurations/LectureConfiguration.cs` → `LessonConfiguration.cs`; `Services/LectureService.cs` → `LessonService.cs`; `Interfaces/ILectureService.cs` → `ILessonService.cs`; `Controllers/LectureController.cs` → `LessonController.cs`; `Dtos/LectureRequest.cs` → `LessonRequest.cs`; `Dtos/LectureResponse.cs` → `LessonResponse.cs`; `Mappers/LectureMapper.cs` → `LessonMapper.cs`; `Validators/LectureRequestValidator.cs` → `LessonRequestValidator.cs`; `SwaggerExamples/LectureResponseExample.cs` → `LessonResponseExample.cs`
- Modify: `Entities/Course.cs`, `CourseMaterial.cs`, `Data/AppDbContext.cs`, `Dtos/CourseResponse.cs`, `CourseRequest.cs`, `TestDtos.cs`, `MaterialResponse.cs`, `Mappers/CourseMapper.cs`, `MaterialMapper.cs`, `Services/CourseService.cs`, `TestingService.cs`, `MaterialService.cs`, `Controllers/MaterialController.cs`, `TestingController.cs`, `Extensions/ServiceCollectionExtensions.cs`
- Tests: rename `CollegeLMS.Tests/Unit/Services/LectureServiceTests.cs` → `LessonServiceTests.cs`; `CollegeLMS.Tests/Integration/Controllers/LectureControllerTests.cs` → `LessonControllerTests.cs`; `CollegeLMS.Tests/Fixtures/LectureFixture.cs` → `LessonFixture.cs` (классы внутри тоже переименовать)

**Interfaces:**
- Produces: `ILessonService` с методами `GetAllAsync(Guid courseId, CancellationToken ct)`, `GetByIdAsync(Guid courseId, Guid id, CancellationToken ct)`, `CreateAsync(Guid courseId, CreateLessonRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct)`, `UpdateAsync(...)`, `DeleteAsync(...)` (сигнатуры как у старого LectureService, типы DTO новые)

- [x] **Step 1: git mv файлов**

```bash
git mv CollegeLMS.API/Entities/Lecture.cs CollegeLMS.API/Entities/Lesson.cs
git mv CollegeLMS.API/Entities/Enums/LectureType.cs CollegeLMS.API/Entities/Enums/LessonKind.cs
git mv CollegeLMS.API/Data/Configurations/LectureConfiguration.cs CollegeLMS.API/Data/Configurations/LessonConfiguration.cs
git mv CollegeLMS.API/Services/LectureService.cs CollegeLMS.API/Services/LessonService.cs
git mv CollegeLMS.API/Interfaces/ILectureService.cs CollegeLMS.API/Interfaces/ILessonService.cs
git mv CollegeLMS.API/Controllers/LectureController.cs CollegeLMS.API/Controllers/LessonController.cs
git mv CollegeLMS.API/Dtos/LectureRequest.cs CollegeLMS.API/Dtos/LessonRequest.cs
git mv CollegeLMS.API/Dtos/LectureResponse.cs CollegeLMS.API/Dtos/LessonResponse.cs
git mv CollegeLMS.API/Mappers/LectureMapper.cs CollegeLMS.API/Mappers/LessonMapper.cs
git mv CollegeLMS.API/Validators/LectureRequestValidator.cs CollegeLMS.API/Validators/LessonRequestValidator.cs
git mv CollegeLMS.API/SwaggerExamples/LectureResponseExample.cs CollegeLMS.API/SwaggerExamples/LessonResponseExample.cs
git mv CollegeLMS.Tests/Unit/Services/LectureServiceTests.cs CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs
git mv CollegeLMS.Tests/Integration/Controllers/LectureControllerTests.cs CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs
git mv CollegeLMS.Tests/Fixtures/LectureFixture.cs CollegeLMS.Tests/Fixtures/LessonFixture.cs
```

- [x] **Step 2: LessonKind enum**

`CollegeLMS.API/Entities/Enums/LessonKind.cs`:

```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum LessonKind
{
    Lecture,
    Practice,
    SelfStudy,
}
```

- [x] **Step 3: Lesson entity**

`CollegeLMS.API/Entities/Lesson.cs` (полностью):

```csharp
using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Lesson : Entity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public LessonKind Kind { get; set; } = LessonKind.Lecture;
    public bool IsCurrent { get; set; }
    public Guid? TestId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public Test? Test { get; set; }
}
```

- [x] **Step 4: LessonConfiguration**

`CollegeLMS.API/Data/Configurations/LessonConfiguration.cs` (полностью):

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Content).HasMaxLength(65535);
        builder.Property(x => x.Order).HasDefaultValue(0);
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.IsCurrent).HasDefaultValue(false);
        builder
            .HasOne(x => x.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Test)
            .WithMany()
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TestId).HasDatabaseName("ix_lessons_test_id");

        builder
            .HasIndex(x => new { x.CourseId, x.Order })
            .HasDatabaseName("ix_lessons_course_id_order");
    }
}
```

> Примечание: `HasColumnName("kind")` зафиксирует имя колонки `kind`, а миграция (Task 7) переименует `lecture_type` → `kind` через `RenameColumn`, сохранив данные.

- [x] **Step 5: AppDbContext, Course, CourseMaterial**

`Data/AppDbContext.cs`: `DbSet<Lecture> Lectures` → `DbSet<Lesson> Lessons` (строка 18).
`Entities/Course.cs`: `ICollection<Lecture> Lectures` → `ICollection<Lesson> Lessons` (строки 17–18).
`Entities/CourseMaterial.cs`: `Guid? LectureId` → `Guid? LessonId` (строка 8).

- [x] **Step 6: DTO**

`CollegeLMS.API/Dtos/LessonRequest.cs` (полностью):

```csharp
namespace CollegeLMS.API.Dtos;

public class CreateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Kind { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Kind { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}
```

`CollegeLMS.API/Dtos/LessonResponse.cs` (полностью):

```csharp
namespace CollegeLMS.API.Dtos;

public class LessonResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Kind { get; set; } = "Lecture";
    public bool IsCurrent { get; set; }
    public Guid? TestId { get; set; }
    public string? TestTitle { get; set; }
}
```

`CollegeLMS.API/Dtos/CourseResponse.cs` — строка 15: `LectureCount` → `LessonCount`.
`CollegeLMS.API/Dtos/TestDtos.cs` — строка 12: `Guid? LectureId` → `Guid? LessonId` (в `CreateTestRequest`).
`CollegeLMS.API/Dtos/MaterialResponse.cs` — строка 7: `Guid? LectureId` → `Guid? LessonId`.

- [x] **Step 7: LessonMapper**

`CollegeLMS.API/Mappers/LessonMapper.cs` (полностью):

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class LessonMapper
{
    public static LessonResponse ToDto(this Lesson lesson) =>
        new()
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Content = lesson.Content,
            Order = lesson.Order,
            Kind = lesson.Kind.ToString(),
            IsCurrent = lesson.IsCurrent,
            TestId = lesson.TestId,
            TestTitle = lesson.Test?.Title,
        };
}
```

`CollegeLMS.API/Mappers/CourseMapper.cs` — строка 27: `course.Lectures?.Count` → `course.Lessons?.Count`.
`CollegeLMS.API/Mappers/MaterialMapper.cs` — строка 13: `LectureId` → `LessonId`.

- [x] **Step 8: LessonService**

`CollegeLMS.API/Services/LessonService.cs` — переименовать класс/типы, заменить:
- `db.Lectures` → `db.Lessons`
- `new Lecture` → `new Lesson`
- `LectureType` → `LessonKind`, `request.LectureType` → `request.Kind`, свойство `lecture.LectureType` → `lecture.Kind`
- сообщения «Лекция не найдена» → «Занятие не найдено», «добавление лекций» → «добавление занятий», «редактирование лекций» → «редактирование занятий», «удаление лекций» → «удаление занятий»
- `ILectureService` → `ILessonService`, `CreateLectureRequest` → `CreateLessonRequest`, `UpdateLectureRequest` → `UpdateLessonRequest`, `LectureResponse` → `LessonResponse`
- `lectures.Select(l => l.ToDto())` → `lessons.Select(l => l.ToDto())`

Правка в `CreateAsync` (Enum.Parse через LessonKind):

```csharp
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = maxOrder + 1,
            Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk)
                ? lk
                : LessonKind.Lecture,
            TestId = request.TestId,
        };
        db.Lessons.Add(lesson);
```

Аналогично в `UpdateAsync`:

```csharp
        lesson.Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk)
            ? lk
            : LessonKind.Lecture;
```

`CollegeLMS.API/Interfaces/ILessonService.cs` — переименовать `ILectureService` → `ILessonService`, типы в сигнатурах заменить на `Lesson*`.

- [x] **Step 9: LessonController**

`CollegeLMS.API/Controllers/LessonController.cs` — переименовать класс/типы, `[Route("api/courses/{courseId:guid}/lectures")]` → `[Route("api/courses/{courseId:guid}/lessons")]`, summary «Получить список лекций курса» → «Получить список занятий курса», «Получить лекцию по ID» → «Получить занятие по ID», «Создать лекцию» → «Создать занятие», «Обновить лекцию» → «Обновить занятие», «Удалить лекцию» → «Удалить занятие», «Лекция найдена/не найдена/создана/обновлена/удалена» → «Занятие …», 404 «Лекция не найдена» → «Занятие не найдено».

- [x] **Step 10: Validator и SwaggerExample**

`CollegeLMS.API/Validators/LessonRequestValidator.cs` — переименовать классы `CreateLectureRequestValidator` → `CreateLessonRequestValidator`, `UpdateLectureRequestValidator` → `UpdateLessonRequestValidator`, типы `CreateLectureRequest` → `CreateLessonRequest`, поле `x.LectureType` → `x.Kind`, сообщения «Название лекции обязательно» → «Название занятия обязательно», «Недопустимый тип занятия» остаётся.

`CollegeLMS.API/SwaggerExamples/LessonResponseExample.cs` (полностью):

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class LessonResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            courseId = Guid.NewGuid(),
            title = "Введение в C#",
            content = "Лекция по основам языка C#",
            order = 1,
            kind = "Lecture",
            isCurrent = false,
            testId = (Guid?)null,
            testTitle = (string?)null,
        };
}
```

- [x] **Step 11: Правки зависимых сервисов/контроллеров**

`CollegeLMS.API/Services/TestingService.cs`:
- строка 85: `Lecture? lecture = null;` → `Lesson? lesson = null;`
- строки 86–98: `request.LectureId` → `request.LessonId`, `db.Lectures` → `db.Lessons`, переменные `lecture` → `lesson`, сообщения «Лекция не найдена» → «Занятие не найдено», «Лекция не принадлежит этому курсу» → «Занятие не принадлежит этому курсу», «У лекции уже есть тест» → «У занятия уже есть тест»
- строки 112–113: `if (lecture is not null) lecture.TestId = test.Id;` → `if (lesson is not null) lesson.TestId = test.Id;`
- строки 461–469: `var lecture = await db.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.TestId == testId, ct);` → `var lesson = await db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.TestId == testId, ct);` + переменные

`CollegeLMS.API/Controllers/TestingController.cs` — найти `LectureId` (query-параметр в `Create`) и переименовать в `LessonId`.

`CollegeLMS.API/Services/MaterialService.cs` — `lectureId` → `lessonId` (параметр, строка 18; `LectureId = lectureId` → `LessonId = lessonId`, строка 48).
`CollegeLMS.API/Controllers/MaterialController.cs` — `[FromQuery] Guid? lectureId` → `[FromQuery] Guid? lessonId` + вызов сервиса (строки 51, 60–68), XML-док «лекции» → «занятия».
`CollegeLMS.API/Services/CourseService.cs` — все `.Include(c => c.Lectures)` → `.Include(c => c.Lessons)` (строки 28, 76, 166, 186, 254, 450), блок duplicate (строки 490–506):

```csharp
        foreach (var lesson in source.Lessons)
        {
            db.Lessons.Add(
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = copy.Id,
                    Title = lesson.Title,
                    Content = lesson.Content,
                    Order = lesson.Order,
                    Kind = lesson.Kind,
                    TestId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }
```

строка 516: `LectureId = null,` → `LessonId = null,`; строка 500: `LectureType = lecture.LectureType,` → `Kind = lesson.Kind,`.

`CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — строка 182: `ILectureService, LectureService` → `ILessonService, LessonService`.

- [x] **Step 12: Переименовать тесты**

`CollegeLMS.Tests/Fixtures/LessonFixture.cs` — класс `LectureFixture` → `LessonFixture`, `Faker<Lecture>` → `Faker<Lesson>`, все упоминания Lecture → Lesson внутри.
`CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs` — `LectureService` → `LessonService`, `ILectureService` → `ILessonService`, `LectureFixture` → `LessonFixture`, `CreateLectureRequest` → `CreateLessonRequest`, `new Lecture` → `new Lesson`, `LectureResponse` → `LessonResponse`, поле `LectureType` → `Kind`.
`CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs` — аналогичный rename, URL `lectures` → `lessons`, тело `CreateLectureRequest` → `CreateLessonRequest` + поле `kind`.

- [x] **Step 13: Собрать и прогнать тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build CollegeLMS.API && dotnet test CollegeLMS.Tests"
```
Expected: Build succeeded + все тесты PASS.

```bash
grep -rn "Lecture" CollegeLMS.API CollegeLMS.Tests --include=*.cs | grep -v Migrations || echo "ЧИСТО"
```
Ожидается: пусто (кроме `Migrations/` и строк, где "Lecture" входит в `LessonKind`-значения enum — их не должно быть, значение enum — `Lecture`).

- [x] **Step 14: Commit**

```bash
git add -A
git commit -m "refactor: переименование Lecture в Lesson (сущность, API /lessons, enum LessonKind)"
```

---

### Task 3: Позиция занятия и переупорядочивание (Order / reorder)

**Files:**
- Modify: `Dtos/LessonRequest.cs` (+ `AfterLessonId`, + `ReorderLessonsRequest`), `Validators/LessonRequestValidator.cs` (+ `ReorderLessonsRequestValidator`), `Services/LessonService.cs` (CreateAsync — вставка в позицию, DeleteAsync — уплотнение, + `ReorderAsync`), `Interfaces/ILessonService.cs` (+ `ReorderAsync`), `Controllers/LessonController.cs` (+ `PUT reorder`)
- Test: `CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs` (+ тесты reorder), `CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs` (+ тест reorder)

**Interfaces:**
- Consumes: `CreateLessonRequest` (Task 2)
- Produces: `ReorderLessonsRequest { List<Guid> LessonIds }`; `ILessonService.ReorderAsync(Guid courseId, ReorderLessonsRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct) → Task<Result>`

- [x] **Step 1: TDD — написать падающие юнит-тесты**

В `CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task Create_InsertsAfterGivenLesson_WhenAfterLessonIdSet()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var first = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "Первое", Order = 1 };
        var second = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "Второе", Order = 2 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(first, second);
        _db.SaveChanges();

        var result = await _sut.CreateAsync(
            course.Id,
            new CreateLessonRequest { Title = "Между", Content = "", Kind = "Lecture", AfterLessonId = first.Id },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess);
        var lessons = _db.Lessons.Where(l => l.CourseId == course.Id).OrderBy(l => l.Order).ToList();
        Assert.Equal(3, lessons.Count);
        Assert.Equal(first.Id, lessons[0].Id);
        Assert.Equal(result.Data!.Id, lessons[1].Id);
        Assert.Equal(1, lessons[0].Order);
        Assert.Equal(2, lessons[1].Order);
        Assert.Equal(3, lessons[2].Order);
    }

    [Fact]
    public async Task Create_InsertsAtStart_WhenAfterLessonIdNull()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var first = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "Первое", Order = 1 };
        _db.Courses.Add(course);
        _db.Lessons.Add(first);
        _db.SaveChanges();

        var result = await _sut.CreateAsync(
            course.Id,
            new CreateLessonRequest { Title = "В начало", Content = "", Kind = "Lecture", AfterLessonId = null },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess);
        var lessons = _db.Lessons.Where(l => l.CourseId == course.Id).OrderBy(l => l.Order).ToList();
        Assert.Equal(result.Data!.Id, lessons[0].Id);
        Assert.Equal(2, first.Order);
    }

    [Fact]
    public async Task Reorder_ReassignsOrders_WhenValid()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        var c = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "C", Order = 3 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(a, b, c);
        _db.SaveChanges();

        var result = await _sut.ReorderAsync(
            course.Id,
            new ReorderLessonsRequest { LessonIds = [c.Id, a.Id, b.Id] },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _db.Lessons.Single(l => l.Id == c.Id).Order);
        Assert.Equal(2, _db.Lessons.Single(l => l.Id == a.Id).Order);
        Assert.Equal(3, _db.Lessons.Single(l => l.Id == b.Id).Order);
    }

    [Fact]
    public async Task Reorder_ReturnsBadRequest_WhenLessonFromAnotherCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var other = new Lesson { Id = Guid.NewGuid(), CourseId = Guid.NewGuid(), Title = "Чужое", Order = 1 };
        _db.Courses.Add(course);
        _db.Lessons.Add(other);
        _db.SaveChanges();

        var result = await _sut.ReorderAsync(
            course.Id,
            new ReorderLessonsRequest { LessonIds = [other.Id] },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }
```

- [x] **Step 2: Прогнать — убедиться, что падают**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter LessonServiceTests"
```
Expected: 4 FAIL (нет `AfterLessonId`, `ReorderAsync`, `ReorderLessonsRequest`).

- [x] **Step 3: DTO + валидатор**

`CollegeLMS.API/Dtos/LessonRequest.cs` — добавить в `CreateLessonRequest`:

```csharp
    public Guid? AfterLessonId { get; set; }
```

и в конец файла:

```csharp
public class ReorderLessonsRequest
{
    public List<Guid> LessonIds { get; set; } = new();
}
```

`CollegeLMS.API/Validators/LessonRequestValidator.cs` — добавить в конец:

```csharp
public class ReorderLessonsRequestValidator : AbstractValidator<ReorderLessonsRequest>
{
    public ReorderLessonsRequestValidator()
    {
        RuleFor(x => x.LessonIds)
            .NotEmpty()
            .WithMessage("Список занятий обязателен")
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Список занятий не должен содержать дубликаты");
    }
}
```

- [x] **Step 4: Реализация в LessonService**

В `CollegeLMS.API/Services/LessonService.cs` `CreateAsync` заменить вычисление позиции (блоки строк 74–93):

```csharp
        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).ToListAsync(ct);

        int newOrder;
        if (request.AfterLessonId.HasValue)
        {
            var after = lessons.FirstOrDefault(l => l.Id == request.AfterLessonId.Value);
            if (after is null)
                return Result<LessonResponse>.Fail("Занятие, после которого нужно вставить, не найдено", 400);

            foreach (var l in lessons.Where(l => l.Order > after.Order))
            {
                l.Order += 1;
                l.UpdatedAt = DateTime.UtcNow;
            }
            newOrder = after.Order + 1;
        }
        else
        {
            foreach (var l in lessons)
            {
                l.Order += 1;
                l.UpdatedAt = DateTime.UtcNow;
            }
            newOrder = 1;
        }

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title,
            Content = request.Content,
            Order = newOrder,
            Kind = Enum.TryParse<LessonKind>(request.Kind, out var lk)
                ? lk
                : LessonKind.Lecture,
            TestId = request.TestId,
        };
        db.Lessons.Add(lesson);
```

В `DeleteAsync` — после `db.Lessons.Remove(lesson);` добавить уплотнение (заменить блок удаления, строки 171–172):

```csharp
        var remaining = await db.Lessons.Where(l => l.CourseId == courseId && l.Order > lecture.Order).ToListAsync(ct);
        foreach (var l in remaining)
        {
            l.Order -= 1;
            l.UpdatedAt = DateTime.UtcNow;
        }

        db.Lessons.Remove(lecture);
        await db.SaveChangesAsync(ct);
```

(имя переменной — в терминологии Task 2 уже `lesson`; в блоке выше оставлен текст-подсказка — фактически использовать `lesson`)

Добавить метод в конец класса:

```csharp
    public async Task<Result> ReorderAsync(
        Guid courseId,
        ReorderLessonsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result.Fail("У вас нет прав на изменение порядка занятий", 403);
        }

        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).ToListAsync(ct);
        if (request.LessonIds.Count != lessons.Count)
            return Result.Fail("Список занятий не соответствует курсу", 400);

        foreach (var lessonId in request.LessonIds)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson is null)
                return Result.Fail("Одно из занятий не принадлежит этому курсу", 400);
        }

        for (var i = 0; i < request.LessonIds.Count; i++)
        {
            var lesson = lessons.First(l => l.Id == request.LessonIds[i]);
            lesson.Order = i + 1;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
```

- [x] **Step 5: Интерфейс**

`CollegeLMS.API/Interfaces/ILessonService.cs` — добавить:

```csharp
    Task<Result> ReorderAsync(
        Guid courseId,
        ReorderLessonsRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
```

- [x] **Step 6: Контроллер — PUT reorder**

`CollegeLMS.API/Controllers/LessonController.cs` — добавить метод (перед `Delete`):

```csharp
    [HttpPut("reorder")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Изменить порядок занятий курса")]
    [SwaggerResponse(200, "Порядок занятий обновлён", typeof(Result))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Reorder(
        Guid courseId,
        ReorderLessonsRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.ReorderAsync(courseId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
```

- [x] **Step 7: Прогнать юнит-тесты — зелёные**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter LessonServiceTests"
```
Expected: PASS.

- [x] **Step 8: Интеграционный тест reorder**

В `CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs` добавить (по образцу существующих тестов этого файла — хелперы `AuthenticateAs*`, `DeserializeAsync` из `BaseIntegrationTest`):

```csharp
    [Fact]
    public async Task Reorder_ReturnsOk_WhenTeacherOwner()
    {
        var (userId, token) = await AuthenticateAsTeacherAsync();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = userId,
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        await InsertAsync(course);
        await InsertAsync(a);
        await InsertAsync(b);

        var response = await PutAsync(
            $"/api/courses/{course.Id}/lessons/reorder",
            new ReorderLessonsRequest { LessonIds = [b.Id, a.Id] },
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reorder_ReturnsForbidden_WhenNotOwner()
    {
        var (_, token) = await AuthenticateAsTeacherAsync();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        await InsertAsync(course);
        await InsertAsync(a);

        var response = await PutAsync(
            $"/api/courses/{course.Id}/lessons/reorder",
            new ReorderLessonsRequest { LessonIds = [a.Id] },
            token
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

(Названия хелперов сверить с существующими в файле — использовать реально существующие методы файла `LessonControllerTests.cs`.)

- [x] **Step 9: Прогнать все тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests"
```
Expected: PASS.

- [x] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: позиция занятия (вставка в начало/после N) и переупорядочивание reorder"
```

---

### Task 4: Текущее занятие (IsCurrent)

**Files:**
- Modify: `Dtos/LessonRequest.cs` (+ `UpdateLessonCurrentRequest`), `Validators/LessonRequestValidator.cs` (+ валидатор), `Services/LessonService.cs` (+ `SetCurrentAsync`), `Interfaces/ILessonService.cs` (+ метод), `Controllers/LessonController.cs` (+ `PATCH {id}/current`), `Data/DbConstraints.cs` (+ частичный уникальный индекс)
- Test: `CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs` (+ тесты), `CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs` (+ тесты)

**Interfaces:**
- Consumes: `Lesson.IsCurrent` (Task 2, шаг 3)
- Produces: `ILessonService.SetCurrentAsync(Guid courseId, Guid id, UpdateLessonCurrentRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct) → Task<Result>`

- [x] **Step 1: TDD — падающие юнит-тесты**

В `CollegeLMS.Tests/Unit/Services/LessonServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task SetCurrent_True_SetsLessonAndResetsOthers()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1, IsCurrent = true };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(a, b);
        _db.SaveChanges();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            b.Id,
            new UpdateLessonCurrentRequest { IsCurrent = true },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess);
        Assert.False(_db.Lessons.Single(l => l.Id == a.Id).IsCurrent);
        Assert.True(_db.Lessons.Single(l => l.Id == b.Id).IsCurrent);
    }

    [Fact]
    public async Task SetCurrent_False_UnsetsLesson()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1, IsCurrent = true };
        _db.Courses.Add(course);
        _db.Lessons.Add(a);
        _db.SaveChanges();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            a.Id,
            new UpdateLessonCurrentRequest { IsCurrent = false },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.True(result.IsSuccess);
        Assert.False(_db.Lessons.Single(l => l.Id == a.Id).IsCurrent);
    }

    [Fact]
    public async Task SetCurrent_ReturnsNotFound_WhenLessonMissing()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        _db.SaveChanges();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            Guid.NewGuid(),
            new UpdateLessonCurrentRequest { IsCurrent = true },
            Guid.NewGuid(),
            "Admin",
            default
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }
```

- [x] **Step 2: Прогнать — падают**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter LessonServiceTests"
```
Expected: 3 новых FAIL.

- [x] **Step 3: DTO + валидатор**

`CollegeLMS.API/Dtos/LessonRequest.cs` — в конец файла:

```csharp
public class UpdateLessonCurrentRequest
{
    public bool IsCurrent { get; set; }
}
```

`CollegeLMS.API/Validators/LessonRequestValidator.cs` — в конец:

```csharp
public class UpdateLessonCurrentRequestValidator : AbstractValidator<UpdateLessonCurrentRequest>
{
    public UpdateLessonCurrentRequestValidator()
    {
        RuleFor(x => x.IsCurrent).NotNull().WithMessage("Поле isCurrent обязательно");
    }
}
```

- [x] **Step 4: Реализация в LessonService**

Добавить метод (в конец класса `LessonService`):

```csharp
    public async Task<Result> SetCurrentAsync(
        Guid courseId,
        Guid id,
        UpdateLessonCurrentRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(
            l => l.Id == id && l.CourseId == courseId,
            ct
        );

        if (lesson is null)
            return Result.Fail("Занятие не найдено", 404);

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (currentUserRole == "Teacher" && course is not null)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result.Fail(
                    "У вас нет прав на изменение текущего занятия",
                    403
                );
        }

        if (request.IsCurrent)
        {
            var others = await db
                .Lessons.Where(l => l.CourseId == courseId && l.Id != lesson.Id && l.IsCurrent)
                .ToListAsync(ct);
            foreach (var other in others)
            {
                other.IsCurrent = false;
                other.UpdatedAt = DateTime.UtcNow;
            }
            lesson.IsCurrent = true;
        }
        else
        {
            lesson.IsCurrent = false;
        }
        lesson.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
```

- [x] **Step 5: Интерфейс**

`CollegeLMS.API/Interfaces/ILessonService.cs` — добавить:

```csharp
    Task<Result> SetCurrentAsync(
        Guid courseId,
        Guid id,
        UpdateLessonCurrentRequest request,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
```

- [x] **Step 6: Контроллер — PATCH {id}/current**

`CollegeLMS.API/Controllers/LessonController.cs` — добавить (после `Update`):

```csharp
    [HttpPatch("{id:guid}/current")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Пометить занятие как текущее")]
    [SwaggerResponse(200, "Текущее занятие обновлено", typeof(Result))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Занятие не найдено")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> SetCurrent(
        Guid courseId,
        Guid id,
        UpdateLessonCurrentRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.SetCurrentAsync(courseId, id, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
```

- [x] **Step 7: Частичный уникальный индекс в DbConstraints**

`CollegeLMS.API/Data/DbConstraints.cs` — добавить блок (после секции Courses):

```csharp
        // Lessons (текущее занятие — одно на курс)
        await db.Database.ExecuteSqlRawAsync(
            """
                CREATE UNIQUE INDEX IF NOT EXISTS ux_lessons_course_id_is_current
                ON lessons (course_id)
                WHERE is_current;
            """
        );
```

- [x] **Step 8: Юнит-тесты зелёные + интеграционные**

Прогнать юнит-тесты (`--filter LessonServiceTests`) — PASS.

В `CollegeLMS.Tests/Integration/Controllers/LessonControllerTests.cs` добавить (образец хелперов — из существующих тестов файла):

```csharp
    [Fact]
    public async Task SetCurrent_ReturnsOk_WhenOwner()
    {
        var (userId, token) = await AuthenticateAsTeacherAsync();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = userId,
            Status = CourseStatus.Draft,
        };
        var lesson = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        await InsertAsync(course);
        await InsertAsync(lesson);

        var response = await PatchAsync(
            $"/api/courses/{course.Id}/lessons/{lesson.Id}/current",
            new UpdateLessonCurrentRequest { IsCurrent = true },
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetCurrent_ReturnsForbidden_WhenNotOwner()
    {
        var (_, token) = await AuthenticateAsTeacherAsync();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var lesson = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        await InsertAsync(course);
        await InsertAsync(lesson);

        var response = await PatchAsync(
            $"/api/courses/{course.Id}/lessons/{lesson.Id}/current",
            new UpdateLessonCurrentRequest { IsCurrent = true },
            token
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

Прогнать все тесты — PASS.

- [x] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: текущее занятие (isCurrent, частичный уникальный индекс на курс)"
```

---

### Task 5: CourseDocument — документация курса

**Files:**
- Create: `CollegeLMS.API/Entities/CourseDocument.cs`, `Data/Configurations/CourseDocumentConfiguration.cs`, `Dtos/CourseDocumentResponse.cs`, `Mappers/CourseDocumentMapper.cs`, `Interfaces/ICourseDocumentService.cs`, `Services/CourseDocumentService.cs`, `Controllers/CourseDocumentController.cs`, `SwaggerExamples/CourseDocumentResponseExample.cs`, `Validators` (не нужен — нет DTO-валидации)
- Modify: `Data/AppDbContext.cs` (+ DbSet), `Extensions/ServiceCollectionExtensions.cs` (+ DI), `Services/CourseService.cs` (DuplicateAsync — копирование документов)
- Test: `CollegeLMS.Tests/Unit/Services/CourseDocumentServiceTests.cs`, `CollegeLMS.Tests/Integration/Controllers/CourseDocumentControllerTests.cs`

**Interfaces:**
- Produces: `ICourseDocumentService`:
  - `GetAllAsync(Guid courseId, CancellationToken ct) → Task<Result<List<CourseDocumentResponse>>>`
  - `UploadAsync(Guid courseId, IFormFile file, Guid currentUserId, string currentUserRole, CancellationToken ct) → Task<Result<CourseDocumentResponse>>`
  - `DownloadAsync(Guid id, CancellationToken ct) → Task<Result<(Stream Stream, string FileName, string MimeType)>>`
  - `DeleteAsync(Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct) → Task<Result>`

- [x] **Step 1: TDD — падающие юнит-тесты**

`CollegeLMS.Tests/Unit/Services/CourseDocumentServiceTests.cs` (создать, образец структуры — `MaterialServiceTests.cs`):

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class CourseDocumentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CourseDocumentService _sut;

    public CourseDocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        var fileService = new Mock<IFileService>();
        var access = new Mock<ICourseAccessService>();
        _sut = new CourseDocumentService(_db, fileService.Object, access.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsDocuments_WhenExist()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        _db.CourseDocuments.AddRange(
            new CourseDocument { Id = Guid.NewGuid(), CourseId = course.Id, FileName = "a.pdf", FilePath = "documents/1/a.pdf", ContentType = "application/pdf", SizeBytes = 100 },
            new CourseDocument { Id = Guid.NewGuid(), CourseId = course.Id, FileName = "b.pdf", FilePath = "documents/1/b.pdf", ContentType = "application/pdf", SizeBytes = 200 }
        );
        _db.SaveChanges();

        var result = await _sut.GetAllAsync(course.Id, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.GetAllAsync(Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Upload_SavesFile_WhenTeacherCanManage()
    {
        var teacher = new Teacher { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FullName = "Иван", CyclicalCommission = "ЦК", Position = "преподаватель" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = teacher.Id,
            Status = CourseStatus.Draft,
        };
        _db.Teachers.Add(teacher);
        _db.Courses.Add(course);
        _db.SaveChanges();

        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("док.pdf");
        file.Setup(f => f.Length).Returns(42);
        file.Setup(f => f.ContentType).Returns("application/pdf");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

        var result = await _sut.UploadAsync(
            course.Id,
            file.Object,
            teacher.UserId,
            "Teacher",
            default
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("док.pdf", result.Data!.FileName);
        Assert.Equal(42, result.Data!.SizeBytes);
    }

    [Fact]
    public async Task Delete_ReturnsForbidden_WhenNotOwner()
    {
        var teacher = new Teacher { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FullName = "Иван", CyclicalCommission = "ЦК", Position = "преподаватель" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Teachers.Add(teacher);
        _db.Courses.Add(course);
        var doc = new CourseDocument { Id = Guid.NewGuid(), CourseId = course.Id, FileName = "a.pdf", FilePath = "documents/1/a.pdf", ContentType = "application/pdf", SizeBytes = 1 };
        _db.CourseDocuments.Add(doc);
        _db.SaveChanges();

        var result = await _sut.DeleteAsync(doc.Id, teacher.UserId, "Teacher", default);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }
}
```

> Примечание: если `IFileService`/`ICourseAccessService` — public интерфейсы в `CollegeLMS.API.Interfaces` (да, судя по `MaterialServiceTests.cs`), `Mock<IFileService>` работает. Имена свойств `Teacher` (FullName/CyclicalCommission/Position) сверить с `TeacherFixture`/реальной сущностью `Teacher.cs` — поправить под фактические.

- [x] **Step 2: Прогнать — падают**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter CourseDocumentServiceTests"
```
Expected: FAIL (нет типов).

- [x] **Step 3: Entity + Configuration + DbSet**

`CollegeLMS.API/Entities/CourseDocument.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseDocument : Entity
{
    public Guid CourseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;
}
```

`CollegeLMS.API/Data/Configurations/CourseDocumentConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseDocumentConfiguration : IEntityTypeConfiguration<CourseDocument>
{
    public void Configure(EntityTypeBuilder<CourseDocument> builder)
    {
        builder.ToTable("course_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FileName).HasMaxLength(255);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder
            .HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CourseId).HasDatabaseName("ix_course_documents_course_id");
    }
}
```

`CollegeLMS.API/Data/AppDbContext.cs` — добавить DbSet (после `CourseMaterials`):

```csharp
    public DbSet<CourseDocument> CourseDocuments => Set<CourseDocument>();
```

- [x] **Step 4: DTO + Mapper**

`CollegeLMS.API/Dtos/CourseDocumentResponse.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class CourseDocumentResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

`CollegeLMS.API/Mappers/CourseDocumentMapper.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class CourseDocumentMapper
{
    public static CourseDocumentResponse ToDto(this CourseDocument doc) =>
        new()
        {
            Id = doc.Id,
            CourseId = doc.CourseId,
            FileName = doc.FileName,
            ContentType = doc.ContentType,
            SizeBytes = doc.SizeBytes,
            CreatedAt = doc.CreatedAt,
        };
}
```

- [x] **Step 5: Интерфейс + сервис**

`CollegeLMS.API/Interfaces/ICourseDocumentService.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;

namespace CollegeLMS.API.Interfaces;

public interface ICourseDocumentService
{
    Task<Result<List<CourseDocumentResponse>>> GetAllAsync(Guid courseId, CancellationToken ct);
    Task<Result<CourseDocumentResponse>> UploadAsync(
        Guid courseId,
        IFormFile file,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
    Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(
        Guid id,
        CancellationToken ct
    );
    Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    );
}
```

`CollegeLMS.API/Services/CourseDocumentService.cs`:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseDocumentService(
    AppDbContext db,
    IFileService fileService,
    ICourseAccessService access
) : ICourseDocumentService
{
    public async Task<Result<List<CourseDocumentResponse>>> GetAllAsync(
        Guid courseId,
        CancellationToken ct
    )
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            return Result<List<CourseDocumentResponse>>.Fail("Курс не найден", 404);

        var documents = await db
            .CourseDocuments.AsNoTracking()
            .Where(d => d.CourseId == courseId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return Result<List<CourseDocumentResponse>>.Ok(
            documents.Select(d => d.ToDto()).ToList()
        );
    }

    public async Task<Result<CourseDocumentResponse>> UploadAsync(
        Guid courseId,
        IFormFile file,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<CourseDocumentResponse>.Fail("Курс не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<CourseDocumentResponse>.Fail(
                    "У вас нет прав на добавление документов в этот курс",
                    403
                );
        }

        var filePath = await fileService.SaveFileAsync("documents", courseId, file, ct);

        var document = new CourseDocument
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
        };
        db.CourseDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return Result<CourseDocumentResponse>.Ok(document.ToDto());
    }

    public async Task<Result<(Stream Stream, string FileName, string MimeType)>> DownloadAsync(
        Guid id,
        CancellationToken ct
    )
    {
        var document = await db
            .CourseDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return Result<(Stream, string, string)>.Fail("Документ не найден", 404);

        var fullPath = Path.Combine("uploads", document.FilePath);
        if (!File.Exists(fullPath))
            return Result<(Stream, string, string)>.Fail("Файл не найден на сервере", 404);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Result<(Stream, string, string)>.Ok(
            (stream, document.FileName, document.ContentType)
        );
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var document = await db
            .CourseDocuments.Include(d => d.Course)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return Result.Fail("Документ не найден", 404);

        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null || !await access.CanManageCourseAsync(document.Course, teacher.Id, ct))
                return Result.Fail("У вас нет прав на удаление этого документа", 403);
        }

        db.CourseDocuments.Remove(document);
        await db.SaveChangesAsync(ct);

        await fileService.DeleteFileAsync(document.FilePath, ct);

        return Result.Ok();
    }
}
```

- [x] **Step 6: DI**

`CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — добавить после `IMaterialService`:

```csharp
        services.AddScoped<ICourseDocumentService, CourseDocumentService>();
```

- [x] **Step 7: Контроллер**

`CollegeLMS.API/Controllers/CourseDocumentController.cs` (образец — `MaterialController.cs`):

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
[Route("api/courses/{courseId:guid}/documents")]
[Authorize]
[Produces("application/json")]
public class CourseDocumentController(ICourseDocumentService service) : ControllerBase
{
    /// <summary>Загрузить документ в курс.</summary>
    /// <remarks>Преподаватель может загрузить документ в курс.
    /// Файл сохраняется на сервере, а информация о нём — в базе данных.</remarks>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="file">Файл для загрузки</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Документ загружен</response>
    /// <response code="400">Файл не выбран</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Загрузить документ в курс")]
    [SwaggerResponse(200, "Документ загружен", typeof(Result<CourseDocumentResponse>))]
    [SwaggerResponse(400, "Файл не выбран")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<CourseDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50L * 1024 * 1024)]
    public async Task<ActionResult<Result<CourseDocumentResponse>>> Upload(
        Guid courseId,
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result<CourseDocumentResponse>.Fail("Файл не выбран", 400));

        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UploadAsync(courseId, file, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить список документов курса.</summary>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Список документов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список документов курса")]
    [SwaggerResponse(200, "Список документов получен", typeof(Result<List<CourseDocumentResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<CourseDocumentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<CourseDocumentResponse>>>> GetAll(
        Guid courseId,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(courseId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Скачать файл документа.</summary>
    /// <param name="id">Идентификатор документа</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Файл скачан</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Документ не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("{id:guid}/download")]
    [SwaggerOperation(Summary = "Скачать файл документа")]
    [SwaggerResponse(200, "Файл скачан")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Документ не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        var (stream, fileName, mimeType) = result.Data!;
        return File(stream, mimeType, fileName);
    }

    /// <summary>Удалить документ.</summary>
    /// <param name="id">Идентификатор документа</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Документ удалён</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Документ не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Удалить документ")]
    [SwaggerResponse(200, "Документ удалён", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Документ не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.DeleteAsync(id, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

- [x] **Step 8: SwaggerExample**

`CollegeLMS.API/SwaggerExamples/CourseDocumentResponseExample.cs`:

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class CourseDocumentResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            courseId = Guid.NewGuid(),
            fileName = "учебный-план.pdf",
            contentType = "application/pdf",
            sizeBytes = 1048576,
            createdAt = DateTime.UtcNow,
        };
}
```

- [x] **Step 9: Duplicate — копирование документов**

`CollegeLMS.API/Services/CourseService.cs`:
- В `DuplicateAsync` цепочку `.Include` дополнить: `.Include(c => c.CourseDocuments)` (после `.Include(c => c.Materials)`, строка 451)
- После цикла материалов добавить цикл документов:

```csharp
        foreach (var doc in source.CourseDocuments)
        {
            var newPath = await CopyDocumentFileAsync(doc.FilePath, copy.Id, ct);
            db.CourseDocuments.Add(
                new CourseDocument
                {
                    Id = Guid.NewGuid(),
                    CourseId = copy.Id,
                    FileName = doc.FileName,
                    FilePath = newPath,
                    ContentType = doc.ContentType,
                    SizeBytes = doc.SizeBytes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }
```

- В конец класса добавить приватный метод (обобщение `CopyMaterialFileAsync`):

```csharp
    private static async Task<string> CopyDocumentFileAsync(
        string relativePath,
        Guid newCourseId,
        CancellationToken ct
    )
    {
        var sourcePath = Path.Combine("uploads", relativePath);
        var fileName = Path.GetFileName(relativePath);
        var destDir = Path.Combine("uploads", "documents", newCourseId.ToString());
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, fileName);
        await using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        await using var dst = new FileStream(destPath, FileMode.Create);
        await src.CopyToAsync(dst, ct);
        return Path.Combine("documents", newCourseId.ToString(), fileName).Replace('\\', '/');
    }
```

- [x] **Step 10: Юнит-тесты зелёные + интеграционные**

Прогнать `--filter CourseDocumentServiceTests` — PASS.

`CollegeLMS.Tests/Integration/Controllers/CourseDocumentControllerTests.cs` (создать, образец — `MaterialControllerTests.cs`; хелперы — из `BaseIntegrationTest`):

```csharp
[Fact]
public async Task Upload_ReturnsOk_WhenTeacher()
{
    var (userId, token) = await AuthenticateAsTeacherAsync();
    var course = new Course { Id = Guid.NewGuid(), Title = "Курс", Description = "", TeacherId = userId, Status = CourseStatus.Draft };
    await InsertAsync(course);

    var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "док.pdf")
    {
        Headers = new HeaderDictionary { { "Content-Type", "application/pdf" } },
    };
    var response = await PostFileAsync($"/api/courses/{course.Id}/documents", file, token);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task GetByCourse_ReturnsOk_WhenAuthenticated()
{
    var (_, token) = await AuthenticateAsStudentAsync();
    var course = new Course { Id = Guid.NewGuid(), Title = "Курс", Description = "", TeacherId = Guid.NewGuid(), Status = CourseStatus.Draft };
    await InsertAsync(course);

    var response = await GetAsync($"/api/courses/{course.Id}/documents", token);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

> Хелперы `PostFileAsync`/`GetAsync`/`AuthenticateAsStudentAsync` — если их нет в `BaseIntegrationTest`, использовать существующие аналоги (например, `PostAsync` с `MultipartFormDataContent` или хелперы из `MaterialControllerTests`). Сверить перед написанием.

Прогнать все тесты — PASS.

- [x] **Step 11: Commit**

```bash
git add -A
git commit -m "feat: документация курса CourseDocument (upload/download/delete, копирование в duplicate)"
```

---

### Task 6: Прогресс — тесты-only (убрать поля заданий)

**Files:**
- Modify: `Dtos/LearningDtos.cs` (`CourseProgressResponse`), `Services/CourseService.cs` (`GetProgressAsync`), `CollegeLMS.Tests/Unit/Services/CourseServiceTests.cs`
- (DashboardService уже поправлен в Task 1)

**Interfaces:**
- Consumes: `CourseProgressResponse` (существующее)
- Produces: `CourseProgressResponse { CourseId, CourseTitle, TotalTests, CompletedTests, CompletionPercent }` (без `TotalAssignments`, `CompletedAssignments`, `AverageScore`)

- [x] **Step 1: DTO**

`CollegeLMS.API/Dtos/LearningDtos.cs` — класс `CourseProgressResponse` (строки 39–48) заменить:

```csharp
public class CourseProgressResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int CompletedTests { get; set; }
    public double CompletionPercent { get; set; }
}
```

- [x] **Step 2: GetProgressAsync**

`CollegeLMS.API/Services/CourseService.cs` — метод `GetProgressAsync` (строки 369–439) заменить:

```csharp
    public async Task<Result<CourseProgressResponse>> GetProgressAsync(
        Guid courseId,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return Result<CourseProgressResponse>.Fail("Курс не найден", 404);

        var student = await db
            .Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<CourseProgressResponse>.Fail("Студент не найден", 404);

        var inGroup = await db.CourseGroups.AnyAsync(
            cg => cg.CourseId == courseId && cg.GroupId == student.GroupId,
            ct
        );
        if (!inGroup)
            return Result<CourseProgressResponse>.Fail("Вы не привязаны к этому курсу", 403);

        var totalTests = await db.Tests.CountAsync(t => t.CourseId == courseId, ct);
        var completedTests = await db.TestAttempts.CountAsync(
            a =>
                a.StudentId == student.Id
                && a.Test.CourseId == courseId
                && a.Status == Entities.Enums.AttemptStatus.Completed,
            ct
        );

        return Result<CourseProgressResponse>.Ok(
            new CourseProgressResponse
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                TotalTests = totalTests,
                CompletedTests = completedTests,
                CompletionPercent = totalTests > 0
                    ? Math.Round((double)completedTests / totalTests * 100, 1)
                    : 0,
            }
        );
    }
```

- [x] **Step 3: Тесты**

`CollegeLMS.Tests/Unit/Services/CourseServiceTests.cs` — тесты `GetProgress*`: убрать setup `Assignments`/`AssignmentSubmissions`/`Submissions`, использовать только `Tests`/`TestAttempts`; ассерты на `TotalTests`/`CompletedTests`/`CompletionPercent` (пример: 2 теста, 1 пройден → `TotalTests == 2`, `CompletedTests == 1`, `CompletionPercent == 50.0`).

- [x] **Step 4: Собрать + прогнать**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build CollegeLMS.API && dotnet test CollegeLMS.Tests"
```
Expected: Build succeeded + PASS.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: прогресс студента считается только по тестам"
```

---

### Task 7: Миграция AddLessonsRemoveAssignments

**Files:**
- Create: `CollegeLMS.API/Migrations/20260817xxxxxx_AddLessonsRemoveAssignments.cs` (+ Designer)

**Interfaces:**
- Consumes: модель из Task 1–6
- Produces: миграция, применяемая автоматически при старте API (см. `Program.cs`/`ApplicationBuilderExtensions` — миграции применяются при старте)

- [x] **Step 1: Сгенерировать миграцию**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet ef migrations add AddLessonsRemoveAssignments --project CollegeLMS.API -- --provider Npgsql"
```
(если dotnet-ef недоступен: `dotnet tool install --global dotnet-ef` внутри контейнера сначала)

```bash
sudo chown -R user1:user1 CollegeLMS.API/Migrations
```

- [x] **Step 2: Проверить и поправить миграцию вручную**

Открыть `CollegeLMS.API/Migrations/<timestamp>_AddLessonsRemoveAssignments.cs`. Ожидаемые операции:

1. `DropTable(name: "assignment_submissions")`, `DropTable(name: "assignments")`
2. `RenameTable(name: "lectures", newName: "lessons")`
3. `RenameColumn(name: "lecture_type", table: "lessons", newName: "kind")` — ЕСЛИ EF сгенерировал `AddColumn` + `DropColumn`, заменить на `RenameColumn` (сохранить данные!)
4. `RenameColumn(name: "lecture_id", table: "course_materials", newName: "lesson_id")` — если EF сделал Drop+Add — заменить на `RenameColumn`
5. `DropColumn(name: "assignment_id", table: "course_materials")`
6. `AddColumn<bool>("is_current", "lessons", nullable: false, defaultValue: false)`
7. `CreateTable("course_documents", ...)` с `FK course_id → courses` (cascade)
8. Индексы: `RenameIndex` для `ix_lectures_course_id_order` → `ix_lessons_course_id_order` и `ix_lectures_test_id` → `ix_lessons_test_id` (если сгенерировались Drop+Create — заменить на `RenameIndex`; `ix_course_materials` индексов нет)

> Критично: НЕ терять данные при переименованиях. Проверить, что EF не делает `DropColumn("lecture_type")` — иначе данные типа занятий пропадут (значения "Lecture"/"Practice"/"SelfStudy" нужны).

- [x] **Step 3: Собрать**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build CollegeLMS.API"
```
Expected: Build succeeded.

- [x] **Step 4: Применить миграцию к локальной БД и проверить данные**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet ef database update --project CollegeLMS.API -- --provider Npgsql"
```

Проверить схему и данные:

```bash
docker exec -i collegelms-postgres psql -U postgres -d collegelms -c "\dt lessons; \dt course_documents; \dt assignments;"
docker exec -i collegelms-postgres psql -U postgres -d collegelms -c "SELECT count(*) FROM lessons; SELECT count(*) FROM course_documents;"
```

(имя контейнера postgres сверить с `docker compose ps` — может быть `collegelms-db` или аналогичное)

Expected: таблицы `lessons`, `course_documents` есть; `assignments` нет; `lessons.kind` содержит значения без потерь; `course_materials.lesson_id` переименован, `assignment_id` отсутствует.

- [x] **Step 5: Проверить DbConstraints на чистой БД (свежая)**

Создать временную БД и проверить, что API стартует и индексы создаются:

```bash
docker exec -i collegelms-postgres psql -U postgres -c "CREATE DATABASE collegelms_m3a_check;"
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms_m3a_check;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet ef database update --project CollegeLMS.API -- --provider Npgsql && cd CollegeLMS.API && dotnet run --no-build & sleep 25 && curl -s http://localhost:5000/health | head -c 200"
```

> API стартует, применяет миграции и `DbConstraints.EnsureAsync` (включая уникальный индекс `ux_lessons_course_id_is_current`). Проверить, что старт без ошибок. Остановить контейнер; удалить временную БД:

```bash
docker exec -i collegelms-postgres psql -U postgres -c "DROP DATABASE collegelms_m3a_check;"
```

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: миграция AddLessonsRemoveAssignments (lessons, is_current, course_documents; drop заданий)"
```

---

### Task 8: Swagger, Postman, PlantUML

**Files:**
- Modify: `docs/spec/CollegeLMS.postman_collection.json`, `docs/diagrams/er/*.puml` (lessons, course_documents), `docs/diagrams/class/*.puml`, `docs/diagrams/sequence/*.puml`
- Create: при необходимости новые `.puml`

- [x] **Step 1: Проверить Swagger-комментарии**

`CollegeLMS.API/SwaggerExamples/LessonResponseExample.cs` и `CourseDocumentResponseExample.cs` созданы (Task 2/5). Убедиться, что в `CourseResponseExample.cs` поле `lessonCount` (было `lectureCount`), `assignmentCount` удалён:

```bash
grep -n "lectureCount\|assignmentCount\|lessonCount" CollegeLMS.API/SwaggerExamples/CourseResponseExample.cs
```
Ожидается: `lessonCount` присутствует, остального нет. Если `assignmentCount` есть — удалить строку, `lectureCount` → `lessonCount`.

- [x] **Step 2: Postman-коллекция**

`docs/spec/CollegeLMS.postman_collection.json`:
- В папке Tests: запрос «Create test with lecture» → переименовать в «Create test with lesson», в теле запроса поле `lectureId` → `lessonId`, URL `/api/courses/.../tests` (сверить фактический URL)
- В папке Courses: добавить подпапку «Lessons» с запросами (URLы — `{{baseUrl}}/api/courses/{{courseId}}/lessons`):
  - GET `/lessons` — список занятий
  - GET `/lessons/{{lessonId}}` — занятие по ID
  - POST `/lessons` — создать занятие (body: `{ "title": "Введение", "content": "...", "kind": "Lecture", "testId": null, "afterLessonId": null }`)
  - PUT `/lessons/{{lessonId}}` — обновить
  - PUT `/lessons/reorder` — переупорядочивание (`{ "lessonIds": [] }`)
  - PATCH `/lessons/{{lessonId}}/current` — текущее занятие (`{ "isCurrent": true }`)
  - DELETE `/lessons/{{lessonId}}` — удалить
- В папке Courses: добавить подпапку «Documents»:
  - POST `/documents` (form-data, file)
  - GET `/documents` — список
  - GET `/documents/{{documentId}}/download`
  - DELETE `/documents/{{documentId}}`
- В папке Courses: если есть запросы с `/assignments` или `/submissions` — удалить
- Убедиться, что в переменных коллекции есть `courseId`, `lessonId`, `documentId`

- [x] **Step 3: PlantUML**

`docs/diagrams/er/` — обновить ER-диаграммы:
- `lectures` → `lessons` (поля: id, course_id, title, content, order, kind, is_current, test_id), отметить unique partial index `ux_lessons_course_id_is_current` на (course_id)
- Добавить `course_documents` (id, course_id FK, file_name, file_path, content_type, size_bytes, timestamps)
- Убрать `assignments`, `assignment_submissions` из всех диаграмм; `course_materials.lesson_id` (вместо lecture_id), без `assignment_id`
- Проверить остальные `.puml` (sequence/class) на упоминания Lecture/Assignment — переименовать/удалить

- [x] **Step 4: Проверить отсутствие старых названий в доках**

```bash
grep -rn "assignments\|Assignments\|lecture" docs/diagrams --include=*.puml | grep -iv "lecture\b.*введение\|Lesson" || echo "ЧИСТО"
```
Правила: в диаграммах не должно быть `assignments`; `lectures` не должно быть (только `lessons`).

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "docs: swagger-примеры, Postman (Lessons/Documents), PlantUML ER/Class/Sequence"
```

---

### Task 9: Полная проверка и финальный прогон

- [x] **Step 1: dotnet build + все тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  -e Jwt__Key="dev-key-m2-0123456789abcdef0123456789abcdef" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build CollegeLMS.API && dotnet test CollegeLMS.Tests"
```
Expected: Build succeeded, все тесты PASS.

- [x] **Step 2: Проверка старых маршрутов**

Убедиться, что в коде нет `/lectures` и `/assignments` (кроме Migrations):

```bash
grep -rn "api/courses/{courseId:guid}/lectures\|/assignments\|/submissions\|lectures" CollegeLMS.API --include=*.cs | grep -v Migrations || echo "ЧИСТО"
```

- [x] **Step 3: Проверка грита (формат CSharpier)**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet csharpier format . --check"
```
Если есть ошибки форматирования: `dotnet csharpier format .` (тот же контейнер), затем пересобрать и перепрогнать тесты.

- [x] **Step 4: Финальный коммит (если были правки)**

```bash
git add -A
git commit -m "chore: финальные правки M3a"   # только если были изменения
```

---

### Task 10: Merge и деплой

- [x] **Step 1: Ветка и синхронизация**

Ветка `feature/m3a-lessons-backend` уже создана от актуального master (всё коммитится в неё с Task 1). Перед merge:

```bash
git fetch origin
git pull --rebase origin master
```

- [x] **Step 2: Merge + push (CD деплоит на VPS)**

```bash
git checkout master
git merge feature/m3a-lessons-backend
git push origin master
```

> После деплоя на VPS миграция применится автоматически при старте API. Фронтенд в этот момент ещё обращается к `/lectures` и `/assignments` — страницы курсов будут частично нерабочими до деплоя M3b. Это ожидаемо (решение пользователя — полный переход без redirect).

- [x] **Step 3: Smoke на проде**

Проверить, что CD прошёл (gh run list / действия на GitHub), и:

```bash
curl -s https://<VPS>/health
curl -s -H "Authorization: Bearer $TOKEN" https://<VPS>/api/courses/<courseId>/lessons | head -c 500
```

Ожидается: health OK, список занятий (`lessons`) возвращается; `/api/courses/<courseId>/lectures` → 404.

## Self-Review

**1. Spec coverage:**
- Удаление заданий целиком — Task 1 ✅
- Lecture→Lesson (таблица, сущность, маршруты /lessons) — Task 2 ✅
- Позиция «в начало/после занятия N» + reorder — Task 3 ✅
- Lesson.IsCurrent + уникальность на курс — Task 4 ✅
- CourseDocument + копирование в duplicate — Task 5 ✅
- Прогресс тесты-only — Task 6 ✅
- Миграция (drop assignments, rename lectures→lessons, add is_current/order, create course_documents) — Task 7 ✅
- Swagger/Postman/PlantUML — Task 8 ✅
- G1 (dotnet build) + тесты + merge/deploy — Task 9/10 ✅
- `CourseResponse.LessonCount` вместо `LectureCount` — Task 2/8 ✅

**2. Placeholder scan:** Все новые файлы имеют полный код; правки существующих — с точными путями/строками. Единственные условные места — имена хелперов в интеграционных тестах (помечено «сверить с существующими»), допустимо.

**3. Type consistency:**
- `LessonKind` везде (не `LessonType` — занят расписанием) ✅
- `ILessonService` с `CreateAsync/UpdateAsync/DeleteAsync/ReorderAsync/SetCurrentAsync` ✅
- `CreateLessonRequest.AfterLessonId`, `ReorderLessonsRequest.LessonIds`, `UpdateLessonCurrentRequest.IsCurrent` — единообразно в DTO/валидаторах/сервисе/контроллере ✅
- `CourseDocumentResponse` поля совпадают в DTO/Mapper/Example ✅
- `CourseProgressResponse` без полей заданий в DTO и GetProgressAsync ✅