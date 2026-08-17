# M2 «Курсы: CRUD, активность, копирование, соавторы» — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Расширить курсы: флаг активности, соавторы, дублирование курса, переключатель активности — backend + frontend.

**Architecture:** Новые сущности `CourseAuthor` (соавторы) и `Course.IsActive`. Общий доступ-хелпер `CourseAccessService` заменяет проверки «владелец ИЛИ соавтор» в LectureService/MaterialService/TestingService/DashboardService. Эндпоинты duplicate (владелец/соавтор — автор копии текущий) и PATCH active (только владелец). DELETE курса остаётся только у владельца. Удаление `Assignment` происходит в M3 — затронутые места трогаем только по необходимости.

**Tech Stack:** .NET 10, EF Core + Npgsql (миграции), xUnit + Moq + InMemory (тесты), Next.js 14 + Tailwind v4, shadcn/ui (Switch, AlertDialog, Select).

**Ветка:** `feature/m2-courses-crud` (от fresh master)

## Пользовательские истории (US) — по ТЗ

### US-C1: Преподаватель может отметить курс активным/неактивным
- [ ] Активность — отдельный переключатель на странице «Мои курсы» (владелец)
- [ ] Неактивные курсы не видны студентам и панели преподавателя
- **API:** `PATCH /api/courses/{id}/active` `{ isActive: bool }`
- **UI:** «Мои курсы» — Switch; панель преподавателя — только активные

### US-C2: Преподаватель может дублировать курс
- [ ] Кнопка «Дублировать» создаёт копию: Title « (копия)», Description, занятия, материалы (файлы копируются физически)
- [ ] Копия неактивна (черновик); группы/тесты не копируются; автор копии — текущий пользователь
- **API:** `POST /api/courses/{id}/duplicate`
- **UI:** кнопка в таблице «Мои курсы»

### US-C3: Владелец курса может добавить соавторов
- [ ] Соавтор (преподаватель) управляет занятиями/материалами/тестами/названием курса
- [ ] Соавтор НЕ удаляет курс и НЕ меняет активность
- [ ] `GET /api/courses` для роли Teacher возвращает и владения, и соавторство
- **API:** `PUT /api/courses/{id}` принимает массив `authorIds` (для владельца)
- **UI:** форма курса — мультиселект преподавателей (чекбоксы списка)

### US-C4: Администратор может создать курс «на основе» существующего
- [ ] В форме создания курса выбирается «Создать на основе курса» — списком моих курсов
- [ ] Создаётся дубликат (см. US-C2) с автором-администратором и указанным TeacherId
- **API:** `POST /api/courses/{id}/duplicate`
- **UI:** страница `/courses/new` — Select «Создать на основе курса»

## Global Constraints

- Все сообщения об ошибках, Swagger-суммарии и комментарии — на русском.
- `Result<T>` везде; без try-catch в контроллерах/сервисах.
- Guid PK: `ValueGeneratedNever()`, `HasMaxLength()` для строк, Enum → `HasConversion<string>`, индексы с `HasDatabaseName`.
- `AsNoTracking()` на чтении, `FindAsync()`/`FirstOrDefaultAsync` для поиска.
- DI-регистрации — только в `Extensions/ServiceCollectionExtensions.cs`.
- Миграция через: `dotnet ef migrations add {Name} --project CollegeLMS.API -- --provider Npgsql`.
- Форматирование: CSharpier; git-префиксы `feat:`/`fix:`/`test:`/`docs:`.
- Без локального dotnet/node: сборка/тесты — Docker (команды ниже), фронт — tsc в контейнере `collegelms-collegelms-next:latest` (mont `/app`), e2e — `node:22-bookworm` + `--network host` + volume `playwright_cache2:/root/.cache/ms-playwright` + `npx playwright install-deps chromium`.
- Файл `CollegeLMS.Next/components/Carousel.tsx` — НЕ трогать и НЕ коммитить (WIP пользователя на master).

---

### Task 1: Backend — сущности IsActive и CourseAuthor, миграция

**Files:**
- Create: `CollegeLMS.API/Entities/CourseAuthor.cs`
- Modify: `CollegeLMS.API/Entities/Course.cs`
- Modify: `CollegeLMS.API/Data/Configurations/CourseConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/CourseAuthorConfiguration.cs`
- Migration: `AddCourseIsActiveAndAuthors`

**Interfaces:**
- Produces: `Course.IsActive` (bool, default true), `Course.CourseAuthors` (`ICollection<CourseAuthor>`), `CourseAuthor { Id(Guid), CourseId, TeacherId }`, таблица `course_authors`, UNIQUE(CourseId, TeacherId).

- [ ] **Step 1: CourseAuthor + поле IsActive**

`CollegeLMS.API/Entities/CourseAuthor.cs`:
```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseAuthor : Entity
{
    public Guid CourseId { get; set; }
    public Guid TeacherId { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public Teacher Teacher { get; set; } = null!;
}
```

В `CollegeLMS.API/Entities/Course.cs` — добавить классовое поле:
```csharp
    public bool IsActive { get; set; } = true;
```
и коллекцию:
```csharp
    [JsonIgnore]
    public ICollection<CourseAuthor> CourseAuthors { get; set; } = new List<CourseAuthor>();
```

- [ ] **Step 2: Конфигурации**

`CourseConfiguration.cs` — для `CourseAuthor` navigation: Npgsql properties руками не объявляем; достаточно настроить FK через config `CourseAuthorConfiguration.cs`:

`CollegeLMS.API/Data/Configurations/CourseAuthorConfiguration.cs`:
```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseAuthorConfiguration : IEntityTypeConfiguration<CourseAuthor>
{
    public void Configure(EntityTypeBuilder<CourseAuthor> builder)
    {
        builder.ToTable("course_authors");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => new { x.CourseId, x.TeacherId })
            .IsUnique()
            .HasDatabaseName("IX_course_authors_course_id_teacher_id");

        builder.HasOne(x => x.Course)
            .WithMany(c => c.CourseAuthors)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
    }
}
```

Проверить, что `AppDbContext` авто-обнаруживает конфигурации через `ApplyConfigurationsFromAssembly` (уже так в проекте — ничего не менять).

- [ ] **Step 3: Миграция (в Docker)**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -v /tmp/opencode/dotnet-tools:/tools -e PATH=/tools:$PATH -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet ef migrations add AddCourseIsActiveAndAuthors --project CollegeLMS.API -- --provider Npgsql"
```
Expected: создан `CollegeLMS.API/Migrations/*_AddCourseIsActiveAndAuthors.cs`. Проверить глазами: `ALTER TABLE courses ADD COLUMN is_active boolean NOT NULL DEFAULT TRUE;` (или эквивалент `defaultValue: true`), `CREATE TABLE course_authors` + уникальный индекс.

- [ ] **Step 4: Сборка**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build -v q"
```
Expected: 0 ошибок.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Entities/CourseAuthor.cs CollegeLMS.API/Entities/Course.cs CollegeLMS.API/Data/Configurations/CourseAuthorConfiguration.cs CollegeLMS.API/Data/Configurations/CourseConfiguration.cs CollegeLMS.API/Migrations/
git commit -m "feat: курс — активность (IsActive) и соавторы (CourseAuthor), миграция"
```

---

### Task 2: Backend — CourseAccessService и замена проверок прав

**Files:**
- Create: `CollegeLMS.API/Interfaces/ICourseAccessService.cs`
- Create: `CollegeLMS.API/Services/CourseAccessService.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`
- Modify: `CollegeLMS.API/Services/LectureService.cs:67,121,167`
- Modify: `CollegeLMS.API/Services/MaterialService.cs:34,118`
- Modify: `CollegeLMS.API/Services/TestingService.cs:32-34,77,798`

**Interfaces:**
- Consumes: `CourseAuthor` (Task 1), `Teacher` (существует).
- Produces:
  - `Task<bool> CanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken ct)` — владелец ИЛИ соавтор
  - `Task<bool> CanManageCourseAsync(Course course, Guid teacherId, CancellationToken ct)` — перегрузка для загруженной сущности
  - `Task<List<Guid>> GetManagedCourseIdsAsync(Guid teacherId, CancellationToken ct)` — id курсов владения + соавторства

- [ ] **Step 1: Упадающий тест (unit)**

Create: `CollegeLMS.Tests/Unit/Services/CourseAccessServiceTests.cs`

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLMS.Tests.Unit.Services;

public class CourseAccessServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Access_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsTrue_ForOwner()
    {
        await using var db = CreateDb();
        var course = new Course { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid(), Title = "Курс", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(course.Id, course.TeacherId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsTrue_ForCoAuthor()
    {
        await using var db = CreateDb();
        var courseId = Guid.NewGuid();
        var coAuthor = Guid.NewGuid();
        db.Courses.Add(new Course { Id = courseId, TeacherId = Guid.NewGuid(), Title = "Курс", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true });
        db.CourseAuthors.Add(new CourseAuthor { Id = Guid.NewGuid(), CourseId = courseId, TeacherId = coAuthor });
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(courseId, coAuthor, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsFalse_ForForeignTeacher()
    {
        await using var db = CreateDb();
        var course = new Course { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid(), Title = "Курс", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(course.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetManagedCourseIdsAsync_ReturnsOwnerAndCoAuthorCourses()
    {
        await using var db = CreateDb();
        var teacherId = Guid.NewGuid();
        var ownedId = Guid.NewGuid();
        var coOwnedId = Guid.NewGuid();
        db.Courses.Add(new Course { Id = ownedId, TeacherId = teacherId, Title = "Мой", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true });
        db.Courses.Add(new Course { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid(), Title = "Владение", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true });
        var coCourse = new Course { Id = coOwnedId, TeacherId = Guid.NewGuid(), Title = "Соавтор", Description = "", Status = Entities.Enums.CourseStatus.Active, IsActive = true };
        db.Courses.Add(coCourse);
        db.CourseAuthors.Add(new CourseAuthor { Id = Guid.NewGuid(), CourseId = coOwnedId, TeacherId = teacherId });
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.GetManagedCourseIdsAsync(teacherId, CancellationToken.None);

        Assert.Contains(ownedId, result);
        Assert.Contains(coOwnedId, result);
        Assert.Equal(2, result.Count);
    }
}
```

- [ ] **Step 2: Запустить — тест падает (нет класса)**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter FullyQualifiedName~CourseAccessServiceTests 2>&1 | tail -3"
```
Expected: FAIL (CS0246: тип `CourseAccessService` не найден).

- [ ] **Step 3: Реализация CourseAccessService**

`CollegeLMS.API/Interfaces/ICourseAccessService.cs`:
```csharp
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Interfaces;

public interface ICourseAccessService
{
    Task<bool> CanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken ct);
    Task<bool> CanManageCourseAsync(Course course, Guid teacherId, CancellationToken ct);
    Task<List<Guid>> GetManagedCourseIdsAsync(Guid teacherId, CancellationToken ct);
}
```

`CollegeLMS.API/Services/CourseAccessService.cs`:
```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseAccessService(AppDbContext db) : ICourseAccessService
{
    public async Task<bool> CanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken ct)
    {
        var course = await db
            .Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return false;
        return await CanManageCourseAsync(course, teacherId, ct);
    }

    public async Task<bool> CanManageCourseAsync(Course course, Guid teacherId, CancellationToken ct)
    {
        if (course.TeacherId == teacherId)
            return true;

        return await db.CourseAuthors.AsNoTracking().AnyAsync(
            a => a.CourseId == course.Id && a.TeacherId == teacherId,
            ct
        );
    }

    public async Task<List<Guid>> GetManagedCourseIdsAsync(Guid teacherId, CancellationToken ct)
    {
        var ownedIds = db.Courses.AsNoTracking().Where(c => c.TeacherId == teacherId).Select(c => c.Id);
        var coAuthorIds = db.CourseAuthors.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.CourseId);

        return await ownedIds.Concat(coAuthorIds).Distinct().ToListAsync(ct);
    }
}
```

- [ ] **Step 4: DI**

`Extensions/ServiceCollectionExtensions.cs` — рядом с остальными scoped-регистрациями:
```csharp
        builder.Services.AddScoped<ICourseAccessService, CourseAccessService>();
```

- [ ] **Step 5: Замена проверок в сервисах**

`LectureService.cs` (3 места, строки ~67, 121, 167) — паттерн замены. Перед этим: в конструктор подмешать `ICourseAccessService`:
```csharp
public class LectureService(AppDbContext db, ICourseAccessService access) : ILectureService
```
и каждую проверку
```csharp
if (teacher is null || course.TeacherId != teacher.Id)
```
заменить на
```csharp
if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
```
(`teacher`-переменная остаётся — нужна для Teacher.Id; если кода после проверки teacher не используется, блок оставить как есть, только условие заменить).

`MaterialService.cs` (строки ~34, 118) — то же самое.

`TestingService.cs`:
- строка ~32-34 (список тестов преподавателя): заменить
```csharp
query = query.Where(t => t.Course.TeacherId == teacher.Id);
```
на
```csharp
var managedIds = await access.GetManagedCourseIdsAsync(teacher.Id, ct);
query = query.Where(t => managedIds.Contains(t.CourseId));
```
- строки ~77 и ~798: `course.TeacherId != teacher.Id` → `!await access.CanManageCourseAsync(course, teacher.Id, ct)`. Конструктор: `TestingService(AppDbContext db, ICourseAccessService access)` — проверить текущие параметры конструктора и добавить access.

Интерфейсы сервисов не меняются (сигнатуры методов те же) — `AddScoped` остаются.

- [ ] **Step 6: Тесты зелёные + сборка**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter FullyQualifiedName~CourseAccessServiceTests 2>&1 | tail -2 && dotnet build -v q 2>&1 | tail -2"
```
Expected: 4 PASS, сборка 0 ошибок.

- [ ] **Step 7: Commit**

```bash
git add CollegeLMS.API/Interfaces/ICourseAccessService.cs CollegeLMS.API/Services/CourseAccessService.cs CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs CollegeLMS.API/Services/LectureService.cs CollegeLMS.API/Services/MaterialService.cs CollegeLMS.API/Services/TestingService.cs CollegeLMS.Tests/Unit/Services/CourseAccessServiceTests.cs
git commit -m "feat: соавторы курса — единая проверка прав владельца/соавтора"
```

---

### Task 3: Backend — duplicate, PATCH active, соавторы в GET/дашборде

**Files:**
- Modify: `CollegeLMS.API/Dtos/CourseResponse.cs` (+ IsActive, AuthorIds, AuthorNames)
- Create: `CollegeLMS.API/Dtos/UpdateCourseActiveRequest.cs`
- Modify: `CollegeLMS.API/Dtos/CreateCourseRequest.cs` (+ AuthorIds)
- Modify: `CollegeLMS.API/Dtos/UpdateCourseRequest.cs` (+ AuthorIds)
- Modify: `CollegeLMS.API/Mappers/*` (маппер курса)
- Modify: `CollegeLMS.API/Services/CourseService.cs`
- Modify: `CollegeLMS.API/Services/DashboardService.cs`
- Modify: `CollegeLMS.API/Controllers/CourseController.cs`
- Modify: `CollegeLMS.API/Services/FileService.cs` (не обязательно — копирование делать в CourseService)

**Interfaces:**
- Consumes: `ICourseAccessService` (Task 2), `CourseAuthor` (Task 1).
- Produces:
  - `CourseResponse.IsActive` (bool), `CourseResponse.AuthorIds` (List<Guid>), `CourseResponse.AuthorNames` (string «, » список)
  - `UpdateCourseActiveRequest { bool IsActive }`
  - `CourseService.DuplicateAsync(Guid courseId, Guid currentUserId, string currentUserRole, CancellationToken ct)`
  - `CourseService.SetActiveAsync(Guid courseId, bool isActive, Guid currentUserId, string currentUserRole, CancellationToken ct)`
  - Endpoints: `POST /api/courses/{id:guid}/duplicate`, `PATCH /api/courses/{id:guid}/active`
  - Роли в эндпоинтах: duplicate — Teacher/Admin (внутри: Teacher → владелец/соавтор; Admin — любой курс, можно дублировать чужой), active — Teacher(владелец)/Admin.

- [ ] **Step 1: DTO**

`CollegeLMS.API/Dtos/CourseResponse.cs` — добавить свойства:
```csharp
    public bool IsActive { get; set; }
    public List<Guid> AuthorIds { get; set; } = new();
    public string AuthorNames { get; set; } = string.Empty;
```

`CollegeLMS.API/Dtos/UpdateCourseActiveRequest.cs`:
```csharp
namespace CollegeLMS.API.Dtos;

public class UpdateCourseActiveRequest
{
    public bool IsActive { get; set; }
}
```

`CreateCourseRequest` — добавить:
```csharp
    public List<Guid> AuthorIds { get; set; } = new();
```

`UpdateCourseRequest` — добавить:
```csharp
    public List<Guid> AuthorIds { get; set; } = new();
```

- [ ] **Step 2: Маппер**

Найти маппер курса (вероятно `CollegeLMS.API/Mappers/CourseMapper.cs` или `CoursesMapper.cs` — проверить `ls CollegeLMS.API/Mappers/`), в `ToDto` добавить:
```csharp
    IsActive = course.IsActive,
    AuthorIds = course.CourseAuthors.Select(a => a.TeacherId).ToList(),
    AuthorNames = string.Join(", ", course.CourseAuthors.Select(a => a.Teacher.FullName)),
```
(Навигационные свойства `CourseAuthors` и `Teacher` инклюдить в Includes запросов, где нужны — см. Step 3/4.)

- [ ] **Step 3: CourseService — соавторы в GET, Update, Create**

`GetAllAsync` — для роли Teacher заменить блок (строки ~35-41):
```csharp
        if (currentUserRole == "Teacher")
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);

            if (teacher is null)
                return Result<List<CourseResponse>>.Fail("Преподаватель не найден", 404);

            var managedIds = await access.GetManagedCourseIdsAsync(teacher.Id, ct);
            query = query.Where(c => managedIds.Contains(c.Id));
        }
```
(`access` — добавить `ICourseAccessService access` в конструктор `CourseService`.)

В includes `GetAllAsync` и `GetByIdAsync` добавить:
```csharp
            .Include(c => c.CourseAuthors)
                .ThenInclude(a => a.Teacher)
                    .ThenInclude(t => t.User)
```

`GetByIdAsync` — доступ соавтору: метод не знает пользователя; добавить параметры `(Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct)` и для Teacher — проверку `access.CanManageCourseAsync`. Обновить контроллер.

`CreateAsync` — после `db.Courses.Add(course)` принять `request.AuthorIds`:
```csharp
        foreach (var authorId in request.AuthorIds.Distinct())
        {
            if (authorId != teacherId)
            {
                db.CourseAuthors.Add(new CourseAuthor
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    TeacherId = authorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }
```
(Другие преподаватели существуют — валидировать: если не существует → Result.Fail «Преподаватель не найден: {id}», 400.)

`UpdateAsync` — заменить проверку прав на:
```csharp
            if (teacher is null || !await access.CanManageCourseAsync(course, teacher.Id, ct))
                return Result<CourseResponse>.Fail(
                    "У вас нет прав на редактирование этого курса",
                    403
                );
```
И синхронизировать соавторов: удалить отсутствующие в `request.AuthorIds`, добавить новые (ID владельца игнорировать):
```csharp
        var existingAuthorIds = course.CourseAuthors.Select(a => a.TeacherId).ToList();
        foreach (var removed in existingAuthorIds.Where(id => !request.AuthorIds.Contains(id)))
        {
            var author = course.CourseAuthors.First(a => a.TeacherId == removed);
            db.CourseAuthors.Remove(author);
        }
        var newIds = request.AuthorIds.Distinct().Where(id => id != course.TeacherId && !existingAuthorIds.Contains(id));
        foreach (var authorId in newIds)
        {
            db.CourseAuthors.Add(new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                TeacherId = authorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
```
`GetByIdAsync`/`UpdateAsync`/`DeleteAsync` включает `CourseAuthors.ThenInclude(Teacher)` необязательны — маппер допускает пустой список.

- [ ] **Step 4: DuplicateAsync + SetActiveAsync (в CourseService)**

```csharp
    public async Task<Result<CourseResponse>> DuplicateAsync(
        Guid courseId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct
    )
    {
        var source = await db
            .Courses
            .Include(c => c.Lectures)
            .Include(c => c.Materials)
            .Include(c => c.CourseAuthors)
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (source is null)
            return Result<CourseResponse>.Fail("Курс не найден", 404);

        Guid authorTeacherId;
        if (currentUserRole == "Teacher")
        {
            var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null)
                return Result<CourseResponse>.Fail("Преподаватель не найден", 404);
            authorTeacherId = teacher.Id;
            if (!await access.CanManageCourseAsync(source, teacher.Id, ct))
                return Result<CourseResponse>.Fail("У вас нет прав на дублирование этого курса", 403);
        }
        else
        {
            authorTeacherId = source.TeacherId;
        }

        var copy = new Course
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (копия)",
            Description = source.Description,
            TeacherId = authorTeacherId,
            Status = CourseStatus.Draft,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Courses.Add(copy);

        foreach (var lecture in source.Lectures)
        {
            db.Lectures.Add(new Lecture
            {
                Id = Guid.NewGuid(),
                CourseId = copy.Id,
                Title = lecture.Title,
                Content = lecture.Content,
                Order = lecture.Order,
                LectureType = lecture.LectureType,
                TestId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        foreach (var material in source.Materials)
        {
            var newPath = await CopyMaterialFileAsync(material.FilePath, copy.Id, ct);
            db.CourseMaterials.Add(new CourseMaterial
            {
                Id = Guid.NewGuid(),
                CourseId = copy.Id,
                LectureId = null,
                FileName = material.FileName,
                FilePath = newPath,
                FileSize = material.FileSize,
                MimeType = material.MimeType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        foreach (var author in source.CourseAuthors)
        {
            db.CourseAuthors.Add(new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = copy.Id,
                TeacherId = author.TeacherId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
        copy.CourseAuthors = source.CourseAuthors;
        copy.Teacher = source.Teacher;
        return Result<CourseResponse>.Ok(copy.ToDto());
    }

    private static async Task<string> CopyMaterialFileAsync(string relativePath, Guid newCourseId, CancellationToken ct)
    {
        var sourcePath = Path.Combine("uploads", relativePath);
        var fileName = Path.GetFileName(relativePath);
        var destDir = Path.Combine("uploads", "materials", newCourseId.ToString());
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, fileName);
        await using (var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
        await using (var dst = new FileStream(destPath, FileMode.Create))
        {
            await src.CopyToAsync(dst, ct);
        }
        return Path.Combine("materials", newCourseId.ToString(), fileName).Replace('\\', '/');
    }
```

```csharp
    public async Task<Result> SetActiveAsync(
        Guid courseId,
        bool isActive,
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
            var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == currentUserId, ct);
            if (teacher is null || course.TeacherId != teacher.Id)
                return Result.Fail("Только владелец курса может менять активность", 403);
        }

        course.IsActive = isActive;
        course.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
```

- [ ] **Step 5: Интерфейс + контроллер**

`ICourseService` — добавить:
```csharp
    Task<Result<CourseResponse>> DuplicateAsync(Guid courseId, Guid currentUserId, string currentUserRole, CancellationToken ct);
    Task<Result> SetActiveAsync(Guid courseId, bool isActive, Guid currentUserId, string currentUserRole, CancellationToken ct);
```

`CourseController` — обновить вызов `GetByIdAsync(id, ct)` → `GetByIdAsync(id, userId, role, ct)` (сигнатура контроллера уже содержит `IUserIdProvider`? — проверить как другие эндпоинты получают userId/role; использовать существующий паттерн `User.GetUserId()` + роль из claims) и добавить эндпоинты:

```csharp
    /// <summary>Дублировать курс (занятия и материалы, без групп и тестов). Копия — черновик (неактивна).</summary>
    /// <response code="200">Копия курса</response>
    /// <response code="403">Нет прав</response>
    /// <response code="404">Курс не найден</response>
    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Roles = "Teacher,Admin")]
    [SwaggerOperation(Summary = "Дублировать курс")]
    [SwaggerResponse(200, "Копия курса", typeof(Result<CourseResponse>))]
    [SwaggerResponse(403, "Нет прав")]
    [SwaggerResponse(404, "Курс не найден")]
    public async Task<ActionResult<Result<CourseResponse>>> Duplicate(
        Guid id,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var result = await service.DuplicateAsync(id, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Изменить активность курса. Только владелец.</summary>
    /// <response code="200">Активность обновлена</response>
    /// <response code="403">Только владелец</response>
    /// <response code="404">Курс не найден</response>
    [HttpPatch("{id:guid}/active")]
    [Authorize(Roles = "Teacher,Admin")]
    [SwaggerOperation(Summary = "Изменить активность курса")]
    [SwaggerResponse(200, "Активность обновлена")]
    [SwaggerResponse(403, "Только владелец")]
    [SwaggerResponse(404, "Курс не найден")]
    public async Task<ActionResult<Result>> SetActive(
        Guid id,
        [FromBody] UpdateCourseActiveRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var result = await service.SetActiveAsync(id, request.IsActive, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
```
(проверить существующие usings: `ClaimTypes` = `System.Security.Claims`, `SwaggerOperation`/`SwaggerResponse` = `Swashbuckle.AspNetCore.Annotations`, `Authorize` уже есть в файле.)

`UpdateCourseActiveRequest` валидатор не нужен (bool).

- [ ] **Step 6: DashboardService — только активные + соавторы**

`DashboardService.cs:24` — заменить:
```csharp
            .Where(c => c.TeacherId == teacher.Id)
```
на:
```csharp
            .Where(c => c.IsActive && (c.TeacherId == teacher.Id || c.CourseAuthors.Any(a => a.TeacherId == teacher.Id)))
```
(добавить `Include(c => c.CourseAuthors)` в этот запрос — проверить текущие Includes и добавить.)

- [ ] **Step 7: Сборка + текущие тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build -v q 2>&1 | tail -2 && dotnet test CollegeLMS.Tests 2>&1 | tail -2"
```
Expected: 0 ошибок сборки; существующие тесты PASS (при падении — обновить ожидания: CourseResponse получил новые поля только с дифаультами, тесты не должны ломаться).

- [ ] **Step 8: Commit**

```bash
git add CollegeLMS.API/Dtos/ CollegeLMS.API/Mappers/ CollegeLMS.API/Services/CourseService.cs CollegeLMS.API/Services/DashboardService.cs CollegeLMS.API/Controllers/CourseController.cs CollegeLMS.API/Interfaces/ICourseService.cs
git commit -m "feat: дублирование курса, активность (PATCH /active), соавторы в списке и дашборде"
```

---

### Task 4: Backend — интеграционные тесты новых эндпоинтов

**Files:**
- Create: `CollegeLMS.Tests/Integration/Controllers/CourseControllerTests.cs`

**Interfaces:**
- Consumes: `POST /api/courses/{id}/duplicate`, `PATCH /api/courses/{id}/active`, `GET /api/courses` (Task 3), паттерн `BaseIntegrationTest` (см. `AuthControllerTests` — токен через `ITokenService.GenerateAccessToken(user)`).

- [ ] **Step 1: Тест-файл**

```csharp
using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class CourseControllerTests : BaseIntegrationTest
{
    private static User MakeUser(string login, UserRole role) =>
        new()
        {
            Id = Guid.NewGuid(),
            Login = login,
            Email = $"{login}@test.ru",
            FullName = "Тест Тестович",
            PasswordHash = "hash",
            Role = role,
        };

    private static Teacher MakeTeacher(Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CyclicalCommission = "ИТ",
            Position = "Преподаватель",
            Category = TeacherCategory.None,
        };

    [Fact]
    public async Task Duplicate_CopiesCourseAndMakesDraft()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var user = MakeUser("dupowner", UserRole.Teacher);
        var teacher = MakeTeacher(user.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "МДК 09.01",
            Description = "Описание",
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.Add(user);
        db.Teachers.Add(teacher);
        db.Courses.Add(course);
        db.Lectures.Add(new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Занятие 1",
            Content = "Текст",
            Order = 1,
            LectureType = LectureType.Lecture,
        });
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsync($"/api/courses/{course.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<CourseResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.Contains("(копия)", body.Data.Title);
        Assert.False(body.Data.IsActive);

        var copy = await db.Courses.FirstAsync(c => c.Id == body.Data.Id);
        Assert.Equal(CourseStatus.Draft, copy.Status);
        Assert.Equal(1, await db.Lectures.CountAsync(l => l.CourseId == copy.Id));
    }

    [Fact]
    public async Task SetActive_Forbidden_ForCoAuthor()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("activeowner", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var coAuthor = MakeUser("activecoauthor", UserRole.Teacher);
        var coAuthorTeacher = MakeTeacher(coAuthor.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, coAuthor);
        db.Teachers.AddRange(ownerTeacher, coAuthorTeacher);
        db.Courses.Add(course);
        db.CourseAuthors.Add(new CourseAuthor
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            TeacherId = coAuthorTeacher.Id,
        });
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(coAuthor);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PatchAsJsonAsync(
            $"/api/courses/{course.Id}/active",
            new UpdateCourseActiveRequest { IsActive = false }
        );
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsCourses_WhereCoAuthor()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("listowner", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var teacher2 = MakeUser("listteacher", UserRole.Teacher);
        var teacher2Entity = MakeTeacher(teacher2.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Соавторский",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, teacher2);
        db.Teachers.AddRange(ownerTeacher, teacher2Entity);
        db.Courses.Add(course);
        db.CourseAuthors.Add(new CourseAuthor
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            TeacherId = teacher2Entity.Id,
        });
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(teacher2);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/courses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<List<CourseResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!);
        Assert.Equal(course.Id, body.Data![0].Id);
    }

    [Fact]
    public async Task Duplicate_Forbidden_ForForeignTeacher()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var owner = MakeUser("dupowner2", UserRole.Teacher);
        var ownerTeacher = MakeTeacher(owner.Id);
        var foreign = MakeUser("dupforeign", UserRole.Teacher);
        var foreignTeacher = MakeTeacher(foreign.Id);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой",
            Description = "",
            TeacherId = ownerTeacher.Id,
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Users.AddRange(owner, foreign);
        db.Teachers.AddRange(ownerTeacher, foreignTeacher);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(foreign);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsync($"/api/courses/{course.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```
(using: `System.Linq` не нужен; `Microsoft.EntityFrameworkCore` — для `FirstAsync`/`CountAsync` — добавить.)

- [ ] **Step 2: Прогнать интеграционные тесты**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test CollegeLMS.Tests --filter FullyQualifiedName~CourseControllerTests 2>&1 | tail -2"
```
Expected: 4 PASS.

- [ ] **Step 3: Полный прогон**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet test 2>&1 | tail -2"
```
Expected: 0 Failed.

- [ ] **Step 4: Commit**

```bash
git add CollegeLMS.Tests/Integration/Controllers/CourseControllerTests.cs
git commit -m "test: интеграционные тесты дублирования, активности и соавторов курса"
```

---

### Task 5: Docs — PlantUML ER, Postman, Swagger

**Files:**
- Create: `docs/diagrams/er/course_authors.puml`
- Modify: `docs/spec/CollegeLMS.postman_collection.json`
- Modify: `CollegeLMS.API/SwaggerExamples/CourseResponseExample.cs`

- [ ] **Step 1: ER-диаграмма**

`docs/diagrams/er/course_authors.puml`:
```plantuml
@startuml
!theme superpowers

entity "course_authors" as ca {
  * id : uuid [PK]
  --
  * course_id : uuid [FK -> courses]
  * teacher_id : uuid [FK -> teachers]
  --
  UNIQUE (course_id, teacher_id)
}

entity "courses" as c {
  * id : uuid [PK]
  --
  title : varchar(200)
  teacher_id : uuid [FK -> teachers]
  is_active : boolean
  status : varchar(20)
}

entity "teachers" as t {
  * id : uuid [PK]
}

c ||--o{ ca : "соавторы"
t ||--o{ ca
@enduml
```
(Порядок/стиль — согласовать с существующими `.puml` в `docs/diagrams/er/` — посмотреть `ls docs/diagrams/er/` перед созданием и повторить шапку/тему соседних файлов.)

- [ ] **Step 2: Swagger-пример**

`CourseResponseExample.cs` — добавить в пример:
```csharp
    IsActive = true,
    AuthorIds = new List<Guid> { Guid.NewGuid() },
    AuthorNames = "Петров Пётр Петрович",
```

- [ ] **Step 3: Postman-коллекция**

В `docs/spec/CollegeLMS.postman_collection.json` в папку Courses добавить:
1. `Duplicate course` — `POST {{baseUrl}}/api/courses/{{courseId}}/duplicate`
2. `Set course active` — `PATCH {{baseUrl}}/api/courses/{{courseId}}/active` body raw `{ "isActive": true }` (важно: `PATCH` — Postman поддерживает; в коллекции существующих PATCH нет — добавить по образцу PUT, только method «PATCH»).

- [ ] **Step 4: Сборка (SwaggerExample компилируется)**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build -v q 2>&1 | tail -2"
```
Expected: 0 ошибок.

- [ ] **Step 5: Commit**

```bash
git add docs/diagrams/er/course_authors.puml docs/spec/CollegeLMS.postman_collection.json CollegeLMS.API/SwaggerExamples/CourseResponseExample.cs
git commit -m "docs: ER соавторов, swagger-пример, postman (duplicate, active)"
```

---

### Task 6: Frontend — типы и таблица «Мои курсы» (активность, дубликат, удаление)

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts` (CourseResponse ~77-92, CreateCourseRequest ~89-92)
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/page.tsx`

**Interfaces:**
- Consumes: `CourseResponse.IsActive/AuthorIds/AuthorNames`, `PATCH /api/courses/{id}/active`, `POST /api/courses/{id}/duplicate`, `DELETE /api/courses/{id}`.
- Produces: UI-состояния таблицы; `updateCourseLocal(isActive)`.

- [ ] **Step 1: Типы**

`types/index.ts` — `CourseResponse`:
```ts
export interface CourseResponse {
  id: string
  title: string
  description: string
  teacherId: string
  teacherName: string
  groupNames: string
  status: string
  lectureCount: number
  assignmentCount: number
  isActive: boolean
  authorIds: string[]
  authorNames: string
}
```
`CreateCourseRequest`:
```ts
export interface CreateCourseRequest {
  title: string
  description: string
  authorIds: string[]
}
```

- [ ] **Step 2: Проверить наличие Switch/AlertDialog**

```bash
ls CollegeLMS.Next/components/ui/ | grep -E "switch|alert"
```
Если нет — `docker exec -u root collegelms-next sh -c "cd /app && npx --yes shadcn@latest add switch alert-dialog"` — только если проект использует shadcn CLI конвенцию (проверить `components.json`); иначе — создать примитивы по образцу существующих (см. `components/ui/input.tsx` + Radix пакеты `@radix-ui/react-switch`/`@radix-ui/react-alert-dialog` в package.json — добавить при отсутствии).

- [ ] **Step 3: courses/page.tsx — переключатель активности**

В таблице: колонку «Статус» оставить. Добавить новую колонку «Активность» (только для владельца — `user.teacherId === c.teacherId`; для остальных показать Badge «Активен/Неактивен»):

```tsx
const handleToggleActive = async (course: CourseResponse) => {
  const res = await api.patch<Result<null>>(`/api/courses/${course.id}/active`, {
    isActive: !course.isActive,
  })
  if (res.data.isSuccess) {
    setCourses(prev => prev.map(c => c.id === course.id ? { ...c, isActive: !c.isActive } : c))
  } else {
    toast.error(res.data.errorMessage ?? "Ошибка изменения активности")
  }
}
```

Ячейка:
```tsx
<TableCell>
  {user?.teacherId === c.teacherId ? (
    <Switch
      checked={c.isActive}
      onCheckedChange={() => handleToggleActive(c)}
      aria-label={`Активность курса ${c.title}`}
    />
  ) : (
    <Badge variant={c.isActive ? "default" : "outline"}>
      {c.isActive ? "Активен" : "Неактивен"}
    </Badge>
  )}
</TableCell>
```
(импорт `Switch` из `@/components/ui/switch`, `toast` из `sonner` — проверить, что `sonner`/`Toaster` уже в проекте: в profile page использовался `toast` — есть.)

- [ ] **Step 4: courses/page.tsx — кнопки Дублировать / Удалить**

Добавить колонку «Действия» (только для владельца `user?.teacherId === c.teacherId`):

```tsx
const handleDuplicate = async (course: CourseResponse) => {
  const res = await api.post<Result<CourseResponse>>(`/api/courses/${course.id}/duplicate`)
  if (res.data.isSuccess && res.data.data) {
    setCourses(prev => [...prev, res.data!.data!])
    toast.success("Курс продублирован")
  } else {
    toast.error(res.data.errorMessage ?? "Ошибка дублирования")
  }
}
```

Удаление — AlertDialog на всю страницу (state `courseToDelete: CourseResponse | null`):
```tsx
<AlertDialog open={!!courseToDelete} onOpenChange={open => !open && setCourseToDelete(null)}>
  <AlertDialogContent>
    <AlertDialogHeader>
      <AlertDialogTitle>Удалить курс?</AlertDialogTitle>
      <AlertDialogDescription>
        Курс «{courseToDelete?.title}» будет удалён безвозвратно вместе с занятиями и материалами.
      </AlertDialogDescription>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel>Отмена</AlertDialogCancel>
      <AlertDialogAction
        onClick={async () => {
          if (!courseToDelete) return
          const res = await api.delete<Result<null>>(`/api/courses/${courseToDelete.id}`)
          if (res.data.isSuccess) {
            setCourses(prev => prev.filter(c => c.id !== courseToDelete.id))
            toast.success("Курс удалён")
          } else {
            toast.error(res.data.errorMessage ?? "Ошибка удаления")
          }
          setCourseToDelete(null)
        }}
      >
        Удалить
      </AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>
</AlertDialog>
```

Действия в строке (стоп-пропагация, чтобы не открывать курс):
```tsx
<TableCell>
  <div className="flex items-center gap-1">
    <Button size="sm" variant="outline" onClick={e => { e.stopPropagation(); handleDuplicate(c) }}>
      <Copy size={14} /> <span className="sr-only sm:not-sr-only sm:ml-1">Дублировать</span>
    </Button>
    <Button size="sm" variant="destructive" onClick={e => { e.stopPropagation(); setCourseToDelete(c) }}>
      <Trash2 size={14} /> <span className="sr-only sm:not-sr-only sm:ml-1">Удалить</span>
    </Button>
  </div>
</TableCell>
```
(импорты `Copy`, `Trash2` из lucide-react.)

- [ ] **Step 5: TypeScript**

```bash
docker run --rm -v /home/user1/CollegeLMS/CollegeLMS.Next:/app -w /app -u root collegelms-collegelms-next:latest sh -c "./node_modules/.bin/tsc --noEmit"
```
Expected: 0 ошибок.

- [ ] **Step 6: Commit**

```bash
git add CollegeLMS.Next/types/index.ts CollegeLMS.Next/app/\(authenticated\)/courses/page.tsx CollegeLMS.Next/components/ui/
git commit -m "feat(frontend): мои курсы — активность, дублирование, удаление"
```

---

### Task 7: Frontend — форма курса (на основе курса + соавторы)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/new/page.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/edit/page.tsx`

**Interfaces:**
- Consumes: `GET /api/courses` (список «моих» для панели выбора), `GET /api/teachers`, `POST /api/courses` (CreateCourseRequest + authorIds), `PUT /api/courses/{id}` (UpdateCourseRequest + authorIds).

- [ ] **Step 1: Тип UpdateCourseRequest (проверить наличие)**

`types/index.ts` — если нет `UpdateCourseRequest`, добавить:
```ts
export interface UpdateCourseRequest {
  title: string
  description: string
  status: string
  authorIds: string[]
}
```
(сравнить с существующим фронтовым типом — при наличии дополнить `authorIds`.)

- [ ] **Step 2: new/page.tsx — «Создать на основе курса»**

Добавить state: `courses` (мои), `baseCourseId` (string «»), `authorIds` (string[]), `teachers` (список преподавателей для чекбоксов).

```tsx
const [courses, setCourses] = useState<CourseResponse[]>([])
const [baseCourseId, setBaseCourseId] = useState("")
const [authorIds, setAuthorIds] = useState<string[]>([])
const [teachers, setTeachers] = useState<TeacherResponse[]>([])

useEffect(() => {
  const token = localStorage.getItem("token")
  if (!token) return
  api.get<Result<CourseResponse[]>>("/api/courses").then(res => {
    if (res.data.isSuccess && res.data.data) setCourses(res.data.data)
  })
  api.get<Result<TeacherResponse[]>>("/api/teachers").then(res => {
    if (res.data.isSuccess && res.data.data) setTeachers(res.data.data)
  })
}, [])
```

`handleSubmit` — после успешного `POST /api/courses`:
```tsx
if (res.data.isSuccess && res.data.data && baseCourseId) {
  await api.post<Result<CourseResponse>>(`/api/courses/${baseCourseId}/duplicate`)
  router.push(`/courses/${res.data.data.id}`)
  return
}
```
(порядок: сначала создать пустой курс (как сейчас), затем продублировать поверх? НЕТ — упрощённый UX: если baseCourseId выбран, кнопка создаёт дубликат и сразу переходит на него: `res` = POST `/api/courses/${baseCourseId}/duplicate`, title/description игнорируются, authorIds берутся из полей формы и передаются в `PUT` после дублирования.)

Блок в форме (перед полями Title/Description):
```tsx
<div className="flex flex-col gap-2">
  <Label htmlFor="baseCourse">Создать на основе курса</Label>
  <select
    id="baseCourse"
    value={baseCourseId}
    onChange={e => setBaseCourseId(e.target.value)}
    className="w-full rounded-md border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
  >
    <option value="">— Пустой курс —</option>
    {courses.map(c => (
      <option key={c.id} value={c.id}>{c.title}</option>
    ))}
  </select>
</div>
```

Блок соавторов (чекбоксы):
```tsx
<div className="flex flex-col gap-2">
  <Label>Соавторы</Label>
  <div className="grid gap-1.5 sm:grid-cols-2">
    {teachers.map(t => (
      <label key={t.id} className="flex items-center gap-2 rounded-md border border-border px-3 py-2 text-sm hover:bg-muted">
        <input
          type="checkbox"
          checked={authorIds.includes(t.id)}
          onChange={e => {
            setAuthorIds(prev =>
              e.target.checked ? [...prev, t.id] : prev.filter(id => id !== t.id)
            )
          }}
        />
        {t.fullName}
      </label>
    ))}
  </div>
</div>
```

- [ ] **Step 3: Типы TeacherResponse**

`types/index.ts` — проверить наличие `TeacherResponse` (есть по смыслу — `teachers/page.tsx` его использует; grep). При отсутствии:
```ts
export interface TeacherResponse {
  id: string
  fullName: string
  email: string
  cyclicalCommission: string
  position: string
}
```

- [ ] **Step 4: edit/page.tsx — соавторы**

Прочитать текущий `edit/page.tsx` (если его нет — пропустить и перенести правку формулировкой: страница `courses/[id]/edit` существует). Действия:
1. Загрузить `courses` (для «на основе» — не нужно на edit) и `teachers`.
2. В `PUT /api/courses/{id}` передать `{ title, description, status, authorIds }`.
3. Добавить блок чекбоксов соавторов (как Step 2) с предзаполнением из `profile`/`course.authorIds` (загружать `GET /api/courses/{id}`).
4. Убрать поле «Статус»? — оставить как есть (Draft/Active/Archived управляется здесь; активность — отдельный Switch на списке).

- [ ] **Step 5: TypeScript + сборка**

```bash
docker run --rm -v /home/user1/CollegeLMS/CollegeLMS.Next:/app -w /app -u root collegelms-collegelms-next:latest sh -c "./node_modules/.bin/tsc --noEmit && npm run build 2>&1 | tail -3"
```
Expected: 0 ошибок, `✓ Compiled successfully`.

- [ ] **Step 6: Commit**

```bash
git add CollegeLMS.Next/types/index.ts CollegeLMS.Next/app/\(authenticated\)/courses/new/page.tsx CollegeLMS.Next/app/\(authenticated\)/courses/\[id\]/edit/page.tsx
git commit -m "feat(frontend): форма курса — создание на основе и соавторы"
```

---

### Task 8: E2E — дублирование и активность курса

**Files:**
- Modify: `CollegeLMS.Next/e2e/courses.spec.ts` (проверить существование; при отсутствии — Create)

**Interfaces:**
- Consumes: моки маршрутов курсов (паттерн auth.spec.ts: `page.route("**/api/courses**", ...)`).

- [ ] **Step 1: Проверить/написать e2e**

Прочитать `CollegeLMS.Next/e2e/courses.spec.ts`; обновить под новую таблицу: колонка «Активность» со Switch для владельца (мок `user` с `teacherId` и курсом с `teacherId` тем же) и кнопки «Дублировать»/«Удалить». Мок `POST **/api/courses/{id}/duplicate` — вернуть копию. Тест «пользователь может дублировать курс»: клик по кнопке «Дублировать» → в таблице появился курс «(копия)». Если спека большая — добавить один `test.describe("Course actions (duplicate, active)")` с моками:

```ts
test("duplicates a course", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("token", "test-jwt-token")
    localStorage.setItem("user", JSON.stringify({ id: "t1", email: "t@t.ru", fullName: "Преподаватель", role: "Teacher", teacherId: "th1" }))
  })
  const courses = [
    { id: "c1", title: "Математика", description: "", teacherId: "th1", teacherName: "Преподаватель", groupNames: "", status: "Active", lectureCount: 2, assignmentCount: 0, isActive: true, authorIds: [], authorNames: "" },
  ]
  await page.route("**/api/courses**", async (route) => {
    const req = route.request()
    if (req.method() === "GET") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: courses, errorMessage: null, statusCode: 200 }) })
    } else if (req.method() === "POST" && req.url().includes("/duplicate")) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: { ...courses[0], id: "c2", title: "Математика (копия)", isActive: false, status: "Draft" }, errorMessage: null, statusCode: 200 }) })
    } else {
      await route.continue()
    }
  })
  await page.goto("/courses", { waitUntil: "networkidle" })
  await page.getByRole("button", { name: "Дублировать" }).first().click()
  await expect(page.getByText("Математика (копия)")).toBeVisible()
})
```
(актуальный `user`-объект и моки сверить с существующим `courses.spec.ts` при обновлении.)

- [ ] **Step 2: Прогнать e2e**

```bash
docker stop collegelms-next
docker run --rm --network host -v /home/user1/CollegeLMS/CollegeLMS.Next:/app -w /app -u root -v playwright_cache2:/root/.cache/ms-playwright node:22-bookworm sh -c "npx playwright install-deps chromium > /dev/null 2>&1; npx playwright test e2e/courses.spec.ts 2>&1 | tail -5"
docker start collegelms-next
```
Expected: PASS (для всего courses.spec.ts — при падениях не по нашей фиче — зафиксировать отдельно, не чинить вне скоупа).

- [ ] **Step 3: Commit**

```bash
git add CollegeLMS.Next/e2e/courses.spec.ts
git commit -m "test(e2e): дублирование курса и активность"
```

---

### Task 9: Локальная верификация (G1–G3) и merge

**Files:** — (без изменений кода)

**Interfaces:** — результаты всех задач.

- [ ] **Step 1: Backend**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src -v nuget_packages:/root/.nuget/packages -e Jwt__Key="dev-key-m2-0123456789abcdef" -e ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet build -v q | tail -2 && dotnet test 2>&1 | tail -2"
```
Expected: 0 ошибок, все тесты PASS (включая новые ~4 курс + 4 access).

- [ ] **Step 2: Frontend**

```bash
docker run --rm -v /home/user1/CollegeLMS/CollegeLMS.Next:/app -w /app -u root collegelms-collegelms-next:latest sh -c "./node_modules/.bin/tsc --noEmit && npm run build 2>&1 | tail -3"
```
Expected: 0 ошибок, `✓ Compiled successfully`.

- [ ] **Step 3: Docker-compose**

```bash
docker compose build api collegelms-next && docker compose up -d --force-recreate api collegelms-next loadbalancer
sleep 20 && curl -s -o /dev/null -w "frontend:%{http_code}\n" http://localhost/ && curl -s -X POST http://localhost/api/auth/login -H "Content-Type: application/json" -d '{"login":"teacher","password":"teacher"}' | head -c 60
```
Expected: frontend 200, login даёт токен. Live smoke: `PATCH /api/courses/{id}/active` с владельцем и «чужим» преподавателем (403), `duplicate` создаёт копию `(копия)`.

- [ ] **Step 4: Merge (verification-before-completion, requesting-code-review)**

```bash
git checkout master && git pull origin master && git merge feature/m2-courses-crud --no-edit
```
Прогнать Step 1 заново на master, затем:
```bash
git branch -d feature/m2-courses-crud
GIT_TERMINAL_PROMPT=0 GIT_SSH_COMMAND="ssh -o BatchMode=yes" git push origin master
```
Expected: push → GitHub Actions CD деплой на VPS (мониторить Actions UI).

- [ ] **Step 5: Проверка Carousel WIP пользователя**

```bash
git status --short
```
Expected: `M CollegeLMS.Next/components/Carousel.tsx` (если пользователь снова правил) — НЕ коммитить, оставить как есть.