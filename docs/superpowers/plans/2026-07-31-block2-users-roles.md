# Блок 2: Пользователи и роли — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перевести управление пользователями на жёсткое удаление с переприсваиванием данных, убрать `IsActive`, добавить профиль пользователя, общий механизм валидации форм, дашборд диспетчера и единое меню админа.

**Architecture:** Backend (.NET 10, Clean Architecture, `Result<T>`, FluentValidation) — удаление `IsActive` из всей цепочки (entity → DTO → маппер → AuthService → миграция), жёсткий `DeleteAsync` с переприсваиванием News/Course/Exam системному админу и каскадным удалением профилей Teacher/Student, новый эндпоинт профиля. Frontend (Next.js 14) — обновление `/admin`, страница `/admin/users/[id]`, компоненты `FormField`/`FormErrorBanner` + `parseErrors`, `/dispatcher/dashboard`, общий модуль меню `lib/menus.ts` (фикс бага «смена drawer в Курсах»).

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core Npgsql / xUnit + Moq + Bogus + FluentAssertions; Next.js 14 / TypeScript / Tailwind v4 / shadcn-ui / sonner.

## Global Constraints

- Ветка: `feature/site-admin-news-courses-overhaul` (продолжаем, Блок 1 уже в ней)
- Все данные и комментарии в коде на русском
- `Result<T>` везде, никаких try-catch в контроллерах/сервисах; `AsNoTracking()` на чтении, `FindAsync()` по PK
- Primary constructor DI; `CancellationToken ct` на всех асинхронных методах
- Мапперы — статические расширения в `Mappers/`, интерфейсы в `Interfaces/`
- Сообщения об ошибках и Swagger-описания на русском
- Директория БД: Postgres `Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=root`
- Миграция: `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql`
- Форматирование: CSharpier (`dotnet csharpier format .`); git-префиксы: `feat:` / `fix:` / `test:`
- `git add -A` при коммите (никогда не перечислять файлы по одному)
- Локальная проверка после каждого бекенд-задания: `dotnet build` (корень решения) + `dotnet test`
- Локальная проверка после каждого фронтенд-задания: `npx tsc --noEmit` + `npm run build` (в `CollegeLMS.Next/`)
- Системный администратор = первый пользователь с `Role == Admin` по `CreatedAt` (в seed это `admin@collegelms.ru`, Id `a1000000-0000-0000-0000-000000000001`)
- Auto-созданный Teacher-профиль админа: `CyclicalCommission = "Администрация"`, `Position = "Преподаватель"` (CHECK-констрейнты `ck_teachers_department_not_empty`/`ck_teachers_position_not_empty` требуют непустые значения)
- Существующие JWT не инвалидируются при удалении/смене роли (роль запечена в токен) — вне рамок этого блока

---

### Task 1: Backend — удалить `IsActive` из всей цепочки + миграция

**Files:**
- Modify: `CollegeLMS.API/Entities/User.cs`
- Modify: `CollegeLMS.API/Dtos/UserResponse.cs`
- Modify: `CollegeLMS.API/Dtos/ProfileResponse.cs`
- Modify: `CollegeLMS.API/Mappers/UserMapper.cs`
- Modify: `CollegeLMS.API/Services/AuthService.cs:21-22`
- Modify: `CollegeLMS.API/Data/DataSeeder.cs` (все вхождения `IsActive = true` в SeedUsersAsync)
- Modify: `CollegeLMS.API/SwaggerExamples/UserResponseExample.cs`
- Modify: `CollegeLMS.API/Data/Configurations/UserConfiguration.cs:27`
- Create: миграция

**Interfaces:**
- Produces: `User` без свойства `IsActive`; `UserResponse`/`ProfileResponse` без `IsActive` — все последующие задачи используют эти типы

- [ ] **Step 1: Убрать свойство из entity и DTO**

`Entities/User.cs` — удалить строку 13 `public bool IsActive { get; set; } = true;`

`Dtos/UserResponse.cs` — удалить строку 10 `public bool IsActive { get; set; }`

`Dtos/ProfileResponse.cs` — удалить строку 10 `public bool IsActive { get; set; }`

- [ ] **Step 2: Обновить маппер**

`Mappers/UserMapper.cs`:
- `ToDto()` — удалить строку 17 `IsActive = entity.IsActive,`
- `ToProfileDto()` — удалить строку 30 `IsActive = entity.IsActive,`
- `ToEntity()` — удалить строку 64 `IsActive = true,`

- [ ] **Step 3: Убрать проверку деактивации в AuthService**

`Services/AuthService.cs` — удалить строки 21-22:
```csharp
if (!user.IsActive)
    return Result<LoginResponse>.Fail("Пользователь деактивирован", 403);
```

- [ ] **Step 4: Убрать `IsActive` из DataSeeder**

`Data/DataSeeder.cs` в `SeedUsersAsync` — удалить ВСЕ строки `IsActive = true,` (вхождения в ~12 пользователях, начинаются со строки 43).

- [ ] **Step 5: Обновить SwaggerExample**

`SwaggerExamples/UserResponseExample.cs` — заменить целиком:
```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class UserResponseExample
{
    public static UserResponse Create() =>
        new()
        {
            Id = Guid.NewGuid(),
            Login = "ivanov",
            Email = "user@collegelms.ru",
            FullName = "Иванов Иван Иванович",
            Role = "Teacher",
        };
}
```

- [ ] **Step 6: Убрать HasDefaultValue из конфигурации**

`Data/Configurations/UserConfiguration.cs` — удалить строку 27:
```csharp
builder.Property(x => x.IsActive).HasDefaultValue(true);
```

- [ ] **Step 7: Создать миграцию**

```bash
dotnet ef migrations add RemoveUserIsActive --project CollegeLMS.API -- --provider Npgsql
```
Ожидание: миграция с `migrationBuilder.DropColumn(name: "is_active", table: "users")`.

- [ ] **Step 8: Обновить тесты (red)**

`CollegeLMS.Tests/Fixtures/UserFixture.cs` — удалить строку `.RuleFor(u => u.IsActive, _ => true)`.

`CollegeLMS.Tests/Unit/Services/UserServiceTests.cs`:
- `CreateAsync_CreatesUser` (строка 89) — удалить `result.Data.IsActive.Should().BeTrue();`
- `DeleteAsync_DeactivatesUser` (строки 154-167) — заменить телом:
```csharp
[Fact]
public async Task DeleteAsync_RemovesUser()
{
    var user = UserFixture.CreateFaker().Generate();
    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    var result = await _sut.DeleteAsync(user.Id, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    (await _db.Users.FindAsync([user.Id])).Should().BeNull();
}
```

`CollegeLMS.Tests/Unit/Services/AuthServiceTests.cs` — удалить тест `LoginAsync_ReturnsFail_WhenUserDeactivated` (строки 79-95).

`CollegeLMS.Tests/Integration/Controllers/AuthControllerTests.cs` — удалить тест `Login_ReturnsForbidden_WhenUserDeactivated` (строки 86-110).

`CollegeLMS.Tests/Integration/Controllers/UserControllerTests.cs` — тест `Delete_DeactivatesUser_WhenAdmin` (строки 213-231) заменить:
```csharp
[Fact]
public async Task Delete_RemovesUser_WhenAdmin()
{
    var token = await GetAdminToken();
    var user = new Faker<User>()
        .RuleFor(u => u.Id, f => f.Random.Guid())
        .RuleFor(u => u.Login, f => f.Internet.UserName())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.FullName, f => f.Name.FullName())
        .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword("test123"))
        .RuleFor(u => u.Role, UserRole.Student)
        .Generate();

    using (var db = CreateDbContext())
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{user.Id}")
    {
        Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
    });

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using (var db = CreateDbContext())
    {
        (await db.Users.FindAsync([user.Id])).Should().BeNull();
    }
}
```

- [ ] **Step 9: Запустить тесты (green)**

```bash
dotnet test
```
Ожидание: все тесты зелёные (включая переименованные).

- [ ] **Step 10: Commit**

```bash
git add -A && git commit -m "feat: удалён IsActive из модели пользователя"
```

---

### Task 2: Backend — жёсткое удаление пользователя с переприсваиванием

**Files:**
- Modify: `CollegeLMS.API/Services/UserService.cs` (DeleteAsync, строки 68-79)
- Modify: `CollegeLMS.API/Data/Configurations/TeacherConfiguration.cs:20` (Restrict → Cascade)
- Modify: `CollegeLMS.API/Data/Configurations/StudentConfiguration.cs:19` (Restrict → Cascade)
- Modify: `CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs:26` (Restrict → Cascade)
- Modify: `CollegeLMS.API/Data/Configurations/RetakeConfiguration.cs:25`, `TestAttemptConfiguration.cs:24`, `TransferRecordConfiguration.cs:19`, `StipendListItemConfiguration.cs:23` (Restrict → Cascade)
- Modify: `CollegeLMS.API/Controllers/UserController.cs:146-165` (Swagger/XML)
- Create: миграция
- Modify: `CollegeLMS.Tests/Unit/Services/UserServiceTests.cs` (новые тесты удаления)

**Interfaces:**
- Consumes: `User` без `IsActive` (Task 1)
- Produces: `DeleteAsync(Guid id, CancellationToken)` — жёсткое удаление; FK User→Teacher/Student и Student→дочерние записи = Cascade в БД

- [ ] **Step 1: Изменить DeleteBehavior на Cascade в конфигурациях**

`TeacherConfiguration.cs` (строка ~20):
```csharp
builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
```
`StudentConfiguration.cs` (строка ~19):
```csharp
builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
```
`AssignmentSubmissionConfiguration.cs` (строка ~26):
```csharp
builder.HasOne(x => x.Student).WithMany(s => s.Submissions).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
```
`RetakeConfiguration.cs` (строка ~25): `StudentId` → `DeleteBehavior.Cascade`
`TestAttemptConfiguration.cs` (строка ~24): `StudentId` → `DeleteBehavior.Cascade`
`TransferRecordConfiguration.cs` (строка ~19): `StudentId` → `DeleteBehavior.Cascade`
`StipendListItemConfiguration.cs` (строка ~23): `StudentId` → `DeleteBehavior.Cascade`

Оставить как есть (переприсваиваются вручную в сервисе): `NewsConfiguration.CreatedById` (Restrict), `CourseConfiguration.TeacherId` (Restrict), `ExamConfiguration.TeacherId` (Restrict), `ScheduleEntryConfiguration.TeacherId` (SetNull — обнуляется автоматически).

- [ ] **Step 2: Написать тесты (red)**

Добавить в `CollegeLMS.Tests/Unit/Services/UserServiceTests.cs` (перед реализацией):

```csharp
[Fact]
public async Task DeleteAsync_ReassignsNewsToSystemAdmin()
{
    var admin = UserFixture.CreateFaker().Generate();
    admin.Role = UserRole.Admin;
    var victim = UserFixture.CreateFaker().Generate();
    victim.Role = UserRole.Teacher;
    _db.Users.AddRange(admin, victim);
    await _db.SaveChangesAsync();

    var news = new API.Entities.News
    {
        Title = "Новость",
        Slug = "novost",
        Content = "Текст",
        CreatedById = victim.Id,
        PublishedAt = DateTime.UtcNow,
    };
    _db.News.Add(news);
    await _db.SaveChangesAsync();

    var result = await _sut.DeleteAsync(victim.Id, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    (await _db.News.FindAsync([news.Id])).Should().NotBeNull();
    _db.News.AsNoTracking().Single().CreatedById.Should().Be(admin.Id);
    (await _db.Users.FindAsync([victim.Id])).Should().BeNull();
}

[Fact]
public async Task DeleteAsync_ReassignsCoursesToAdminTeacherProfile()
{
    var admin = UserFixture.CreateFaker().Generate();
    admin.Role = UserRole.Admin;
    var victim = UserFixture.CreateFaker().Generate();
    victim.Role = UserRole.Teacher;
    _db.Users.AddRange(admin, victim);
    await _db.SaveChangesAsync();

    var teacher = new API.Entities.Teacher
    {
        Id = Guid.NewGuid(),
        UserId = victim.Id,
        CyclicalCommission = "ИВТ",
        Position = "Преподаватель",
    };
    var course = new API.Entities.Course
    {
        Title = "Курс",
        Description = "Описание",
        TeacherId = teacher.Id,
        Status = CourseStatus.Active,
    };
    _db.Teachers.Add(teacher);
    _db.Courses.Add(course);
    await _db.SaveChangesAsync();

    var result = await _sut.DeleteAsync(victim.Id, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    var adminTeacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == admin.Id);
    adminTeacher.Should().NotBeNull();
    _db.Courses.AsNoTracking().Single().TeacherId.Should().Be(adminTeacher!.Id);
    (await _db.Teachers.FindAsync([teacher.Id])).Should().BeNull();
}

[Fact]
public async Task DeleteAsync_DeletesTeacherAndStudentProfiles()
{
    var admin = UserFixture.CreateFaker().Generate();
    admin.Role = UserRole.Admin;
    var victim = UserFixture.CreateFaker().Generate();
    victim.Role = UserRole.Student;
    _db.Users.AddRange(admin, victim);
    await _db.SaveChangesAsync();

    var student = new API.Entities.Student
    {
        Id = Guid.NewGuid(),
        UserId = victim.Id,
        GroupId = Guid.NewGuid(),
        RecordBookNumber = "001",
    };
    _db.Students.Add(student);
    await _db.SaveChangesAsync();

    var result = await _sut.DeleteAsync(victim.Id, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    (await _db.Students.FindAsync([student.Id])).Should().BeNull();
    (await _db.Users.FindAsync([victim.Id])).Should().BeNull();
}
```

- [ ] **Step 3: Запустить тесты — убедиться что падают (red)**

```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests.DeleteAsync"
```
Ожидание: FAIL (текущий DeleteAsync деактивирует, а не удаляет).

- [ ] **Step 4: Реализовать жёсткое удаление**

`Services/UserService.cs` — заменить `DeleteAsync` (строки 68-79) на:

```csharp
public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
{
    var user = await db.Users.FindAsync([id], ct);
    if (user is null)
        return Result.Fail("Пользователь не найден", 404);

    var admin = await db
        .Users.AsNoTracking()
        .Where(u => u.Role == UserRole.Admin)
        .OrderBy(u => u.CreatedAt)
        .FirstOrDefaultAsync(ct);

    if (admin is null)
        return Result.Fail("Не найден системный администратор", 500);

    if (admin.Id == user.Id)
    {
        var adminsCount = await db.Users.CountAsync(u => u.Role == UserRole.Admin, ct);
        if (adminsCount == 1)
            return Result.Fail("Нельзя удалить последнего администратора", 409);
    }

    // Переприсваивание новостей системному администратору
    await db
        .News.Where(n => n.CreatedById == id)
        .ExecuteUpdateAsync(s => s.SetProperty(n => n.CreatedById, admin.Id), ct);

    // Переприсваивание курсов и экзаменов преподавателя
    var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == id, ct);
    if (teacher is not null)
    {
        var adminTeacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == admin.Id, ct);
        if (adminTeacher is null)
        {
            adminTeacher = new Teacher
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                CyclicalCommission = "Администрация",
                Position = "Преподаватель",
            };
            db.Teachers.Add(adminTeacher);
            await db.SaveChangesAsync(ct);
        }

        await db
            .Courses.Where(c => c.TeacherId == teacher.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.TeacherId, adminTeacher.Id), ct);

        await db
            .Exams.Where(e => e.TeacherId == teacher.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.TeacherId, adminTeacher.Id), ct);

        db.Teachers.Remove(teacher);
    }

    // Профиль студента удаляется каскадом в БД (дочерние записи — тоже Cascade)
    var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == id, ct);
    if (student is not null)
        db.Students.Remove(student);

    db.Users.Remove(user);
    await db.SaveChangesAsync(ct);

    return Result.Ok();
}
```

Нужные usings (проверить/добавить): `CollegeLMS.API.Entities.Enums`, `Microsoft.EntityFrameworkCore` (уже есть).

- [ ] **Step 5: Обновить Swagger-документацию контроллера**

`Controllers/UserController.cs` (строки 138-165) — заменить XML-комментарии и атрибуты, тело метода не менять (оно уже `if (!result.IsSuccess) return StatusCode(...); return Ok(result);`):

```csharp
/// <summary>Удалить пользователя (жёсткое удаление).</summary>
/// <param name="id">Идентификатор пользователя</param>
/// <param name="ct">Токен отмены</param>
/// <remarks>
/// Новости и курсы пользователя переприсваиваются системному администратору,
/// профили Teacher/Student и связанные данные удаляются каскадом.
/// </remarks>
/// <response code="200">Пользователь удалён</response>
/// <response code="401">Не авторизован</response>
/// <response code="403">Доступ запрещён (требуется роль Admin)</response>
/// <response code="404">Пользователь не найден</response>
/// <response code="409">Нельзя удалить последнего администратора</response>
/// <response code="500">Ошибка сервера</response>
[HttpDelete("{id:guid}")]
[Authorize(Roles = "Admin")]
[SwaggerOperation(Summary = "Удалить пользователя (жёсткое удаление)")]
[SwaggerResponse(200, "Пользователь удалён", typeof(Result))]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(403, "Доступ запрещён")]
[SwaggerResponse(404, "Пользователь не найден")]
[SwaggerResponse(409, "Нельзя удалить последнего администратора")]
[SwaggerResponse(500, "Ошибка сервера")]
[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
{
    var result = await service.DeleteAsync(id, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

- [ ] **Step 6: Создать миграцию**

```bash
dotnet ef migrations add CascadeProfileDeletes --project CollegeLMS.API -- --provider Npgsql
```
Ожидание: миграция с DropForeignKey/CreateForeignKey для `fk_teachers_users_user_id`, `fk_students_users_user_id`, `fk_assignment_submissions_students_student_id`, `fk_retakes_students_student_id`, `fk_test_attempts_students_student_id`, `fk_transfer_records_students_student_id`, `fk_stipend_list_items_students_student_id` и `onDelete: ReferentialAction.Cascade`.

- [ ] **Step 7: Запустить тесты (green)**

```bash
dotnet test
```
Ожидание: все тесты зелёные, включая новые DeleteAsync-тесты.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: жёсткое удаление пользователя с переприсваиванием"
```

---

### Task 3: Backend — фиксы UserService (Email) + валидаторы

**Files:**
- Modify: `CollegeLMS.API/Services/UserService.cs` (CreateAsync строка 34, UpdateAsync строки 55-62)
- Modify: `CollegeLMS.API/Validators/CreateUserRequestValidator.cs`
- Modify: `CollegeLMS.API/Validators/UpdateUserRequestValidator.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/UserServiceTests.cs`

**Interfaces:**
- Consumes: `UserResponse` без `IsActive` (Task 1)
- Produces: `CreateAsync` — 409 при дубликате Login ИЛИ Email; `UpdateAsync` — обновляет Email, 409 при дубликате Email

- [ ] **Step 1: Написать тесты (red)**

Добавить в `CollegeLMS.Tests/Unit/Services/UserServiceTests.cs`:

```csharp
[Fact]
public async Task CreateAsync_ReturnsFail_WhenEmailExists_WithDifferentLogin()
{
    var existing = UserFixture.CreateFaker().Generate();
    _db.Users.Add(existing);
    await _db.SaveChangesAsync();

    var request = new CreateUserRequest
    {
        Login = "different-login",
        Email = existing.Email,
        Password = "password123",
        FullName = "Another",
        Role = UserRole.Student,
    };

    var result = await _sut.CreateAsync(request, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(409);
    result.ErrorMessage.Should().Be("Пользователь с таким email уже существует");
}

[Fact]
public async Task UpdateAsync_UpdatesEmail()
{
    var user = UserFixture.CreateFaker().Generate();
    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    var request = new UpdateUserRequest
    {
        Login = user.Login,
        Email = "new-email@test.ru",
        FullName = user.FullName,
        Role = user.Role,
    };

    var result = await _sut.UpdateAsync(user.Id, request, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Data!.Email.Should().Be("new-email@test.ru");
}
```

- [ ] **Step 2: Запустить тесты — убедиться что падают (red)**

```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests.CreateAsync_ReturnsFail_WhenEmailExists_WithDifferentLogin|FullyQualifiedName~UserServiceTests.UpdateAsync_UpdatesEmail"
```
Ожидание: FAIL.

- [ ] **Step 3: Реализовать проверки в UserService**

`Services/UserService.cs` — в `CreateAsync` (после строки 36) добавить:

```csharp
var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
if (emailExists)
    return Result<UserResponse>.Fail("Пользователь с таким email уже существует", 409);
```

В `UpdateAsync` (после проверки логина, строка 57) добавить:

```csharp
var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id, ct);
if (emailExists)
    return Result<UserResponse>.Fail("Пользователь с таким email уже существует", 409);
```
И добавить обновление Email (строка 59, перед `user.Login`):
```csharp
user.Email = request.Email;
```

- [ ] **Step 4: Обновить валидаторы (Email обязателен и корректен)**

`Validators/CreateUserRequestValidator.cs` — заменить правило Email (строки 16-18):
```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("Email обязателен")
    .MaximumLength(256)
    .WithMessage("Email не может быть длиннее 256 символов")
    .EmailAddress()
    .WithMessage("Некорректный формат email");
```
`Validators/UpdateUserRequestValidator.cs` — то же самое (заменить текущее правило Email на приведённое выше).

- [ ] **Step 5: Запустить тесты (green)**

```bash
dotnet test
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "fix: проверка уникальности email и обновление email пользователя"
```

---

### Task 4: Backend — профиль пользователя `GET /api/users/{id}/profile`

**Files:**
- Create: `CollegeLMS.API/Dtos/UserProfileResponse.cs`
- Modify: `CollegeLMS.API/Interfaces/IUserService.cs`
- Modify: `CollegeLMS.API/Services/UserService.cs`
- Modify: `CollegeLMS.API/Mappers/UserMapper.cs`
- Modify: `CollegeLMS.API/Controllers/UserController.cs`
- Create: `CollegeLMS.API/SwaggerExamples/UserProfileResponseExample.cs`
- Modify: `CollegeLMS.Tests/Unit/Services/UserServiceTests.cs`
- Modify: `CollegeLMS.Tests/Integration/Controllers/UserControllerTests.cs`

**Interfaces:**
- Consumes: `User`/`UserResponse` без `IsActive`
- Produces: `IUserService.GetProfileAsync(Guid id, CancellationToken ct) → Task<Result<UserProfileResponse>>`; DTO `UserProfileResponse { UserResponse User; List<UserCourseItem> Courses; List<UserNewsItem> News; }`

- [ ] **Step 1: Создать DTO**

`Dtos/UserProfileResponse.cs`:
```csharp
namespace CollegeLMS.API.Dtos;

public class UserProfileResponse
{
    public UserResponse User { get; set; } = null!;
    public List<UserCourseItem> Courses { get; set; } = new();
    public List<UserNewsItem> News { get; set; } = new();
}

public class UserCourseItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class UserNewsItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
```

- [ ] **Step 2: Интерфейс**

`Interfaces/IUserService.cs` — добавить:
```csharp
Task<Result<UserProfileResponse>> GetProfileAsync(Guid id, CancellationToken ct = default);
```

- [ ] **Step 3: Реализация**

`Services/UserService.cs` — добавить метод:

```csharp
public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid id, CancellationToken ct)
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
    if (user is null)
        return Result<UserProfileResponse>.Fail("Пользователь не найден", 404);

    var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == id, ct);
    var courses = new List<UserCourseItem>();
    if (teacher is not null)
    {
        courses = await db
            .Courses.AsNoTracking()
            .Where(c => c.TeacherId == teacher.Id)
            .OrderBy(c => c.Title)
            .Select(c => new UserCourseItem { Id = c.Id, Title = c.Title })
            .ToListAsync(ct);
    }

    var news = await db
        .News.AsNoTracking()
        .Where(n => n.CreatedById == id && !n.IsDeleted)
        .OrderByDescending(n => n.PublishedAt)
        .Select(n => new UserNewsItem { Id = n.Id, Title = n.Title, PublishedAt = n.PublishedAt })
        .ToListAsync(ct);

    return Result<UserProfileResponse>.Ok(
        new UserProfileResponse { User = user.ToDto(), Courses = courses, News = news }
    );
}
```

- [ ] **Step 4: Контроллер**

`Controllers/UserController.cs` — добавить перед `Delete` (или после `GetById`):

```csharp
/// <summary>Получить профиль пользователя с курсами и новостями.</summary>
/// <response code="200">Профиль получен</response>
/// <response code="401">Не авторизован</response>
/// <response code="404">Пользователь не найден</response>
/// <response code="500">Ошибка сервера</response>
[HttpGet("{id:guid}/profile")]
[Authorize]
[SwaggerOperation(Summary = "Получить профиль пользователя с курсами и новостями")]
[SwaggerResponse(200, "Профиль получен", typeof(Result<UserProfileResponse>))]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(404, "Пользователь не найден")]
[SwaggerResponse(500, "Ошибка сервера")]
[ProducesResponseType(typeof(Result<UserProfileResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<Result<UserProfileResponse>>> GetProfile(Guid id, CancellationToken ct)
{
    var result = await service.GetProfileAsync(id, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

- [ ] **Step 5: SwaggerExample**

`SwaggerExamples/UserProfileResponseExample.cs`:
```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class UserProfileResponseExample
{
    public static UserProfileResponse Create() =>
        new()
        {
            User = UserResponseExample.Create(),
            Courses = new List<UserCourseItem>
            {
                new() { Id = Guid.NewGuid(), Title = "МДК 09.01 Проектирование и разработка веб-приложений" },
            },
            News = new List<UserNewsItem>
            {
                new() { Id = Guid.NewGuid(), Title = "Новость", PublishedAt = DateTime.UtcNow },
            },
        };
}
```
Использовать пример в SwaggerResponse-атрибуте эндпоинта, если в проекте используется `SwaggerResponseExampleAttribute` (проверить паттерн других контроллеров — NewsController).

- [ ] **Step 6: Тесты**

`Unit/Services/UserServiceTests.cs` — добавить:
```csharp
[Fact]
public async Task GetProfileAsync_ReturnsUserWithCoursesAndNews()
{
    var admin = UserFixture.CreateFaker().Generate();
    admin.Role = UserRole.Admin;
    var teacherUser = UserFixture.CreateFaker().Generate();
    teacherUser.Role = UserRole.Teacher;
    _db.Users.AddRange(admin, teacherUser);
    await _db.SaveChangesAsync();

    var teacher = new API.Entities.Teacher
    {
        Id = Guid.NewGuid(),
        UserId = teacherUser.Id,
        CyclicalCommission = "ИВТ",
        Position = "Преподаватель",
    };
    var course = new API.Entities.Course
    {
        Title = "Курс 1",
        Description = "Описание",
        TeacherId = teacher.Id,
        Status = CourseStatus.Active,
    };
    var news = new API.Entities.News
    {
        Title = "Новость 1",
        Slug = "novost-1",
        Content = "Текст",
        CreatedById = teacherUser.Id,
        PublishedAt = DateTime.UtcNow,
    };
    _db.Teachers.Add(teacher);
    _db.Courses.Add(course);
    _db.News.Add(news);
    await _db.SaveChangesAsync();

    var result = await _sut.GetProfileAsync(teacherUser.Id, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Data!.User.Id.Should().Be(teacherUser.Id);
    result.Data.Courses.Should().ContainSingle(c => c.Title == "Курс 1");
    result.Data.News.Should().ContainSingle(n => n.Title == "Новость 1");
}

[Fact]
public async Task GetProfileAsync_ReturnsFail_WhenNotFound()
{
    var result = await _sut.GetProfileAsync(Guid.NewGuid(), CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
}
```

`Integration/Controllers/UserControllerTests.cs` — добавить:
```csharp
[Fact]
public async Task GetProfile_ReturnsProfile_WhenAdmin()
{
    var token = await GetAdminToken();
    var admin = new Faker<User>()
        .RuleFor(u => u.Id, f => f.Random.Guid())
        .RuleFor(u => u.Login, f => f.Internet.UserName())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.FullName, f => f.Name.FullName())
        .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword("test123"))
        .RuleFor(u => u.Role, UserRole.Student)
        .Generate();

    using (var db = CreateDbContext())
    {
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/users/{admin.Id}/profile")
    {
        Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
    });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await DeserializeAsync<Result<UserProfileResponse>>(response);
    result!.IsSuccess.Should().BeTrue();
    result.Data!.User.Id.Should().Be(admin.Id);
    result.Data.Courses.Should().BeEmpty();
    result.Data.News.Should().BeEmpty();
}
```
(Проверить существующие using в файле теста и добавить `CollegeLMS.API.Dtos` при необходимости.)

- [ ] **Step 7: Запустить тесты + build**

```bash
dotnet build && dotnet test
```

- [ ] **Step 8: Обновить Postman-коллекцию**

`docs/spec/CollegeLMS.postman_collection.json` — добавить `GET /api/users/{id}/profile` в папку Users (по образцу существующих эндпоинтов).

- [ ] **Step 9: Commit**

```bash
git add -A && git commit -m "feat: эндпоинт профиля пользователя с курсами и новостями"
```

---

### Task 5: Frontend — типы и страница `/admin`: статус, удаление, клик по строке

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts` (User, ProfileResponse, новые типы)
- Modify: `CollegeLMS.Next/app/admin/page.tsx`

**Interfaces:**
- Consumes: API без `IsActive` (Task 1-2), `GET /api/users/{id}/profile` (Task 4)
- Produces: клик по строке → `/admin/users/{id}`; `UserProfileResponse` типы для Task 7

- [ ] **Step 1: Обновить типы**

`types/index.ts`:
- `User` (строки 1-8) — удалить `isActive: boolean`
- `ProfileResponse` (строки ~487-496) — удалить `isActive: boolean`
- Добавить в конец:
```ts
export interface UserProfileResponse {
  user: User
  courses: { id: string; title: string }[]
  news: { id: string; title: string; publishedAt: string }[]
}
```

- [ ] **Step 2: Обновить страницу `/admin`**

`app/admin/page.tsx`:
1. Импорт: заменить `Ban` на `Trash2` (строка 11); добавить `useRouter` из `next/navigation`; импорт `toast` из `"sonner"`.
2. Убрать столбец «Статус»: `<TableHead>Статус</TableHead>` (строка 251) и `<TableCell>` со статусом (строки 277-283).
3. Убрать `className={!u.isActive ? "opacity-50" : ""}` (строка 257).
4. Сделать строку кликабельной — заменить `<TableRow key={u.id} ...>` (строка 256) на:
```tsx
<TableRow
  key={u.id}
  className="cursor-pointer hover:bg-muted/50"
  onClick={() => router.push(`/admin/users/${u.id}`)}
>
```
5. Кнопка деактивации (строки 331-335) → кнопка удаления:
```tsx
<Button
  variant="ghost"
  size="sm"
  onClick={(e) => { e.stopPropagation(); setDeleteConfirmId(u.id) }}
  className="text-destructive hover:text-destructive"
  aria-label="Удалить пользователя"
>
  <Trash2 size={16} />
</Button>
```
6. `handleDelete` (строки 160-170) — добавить обработку 409 и тост:
```tsx
const handleDelete = async () => {
  if (!deleteConfirmId) return
  try {
    await api.delete(`/api/users/${deleteConfirmId}`)
    toast.success("Пользователь удалён")
    await fetchUsers()
  } catch (err) {
    const data = (err as { response?: { data?: Result<null> } })?.response?.data
    toast.error(data?.errorMessage ?? "Ошибка удаления")
  } finally {
    setDeleteConfirmId(null)
  }
}
```
7. AlertDialog (строки 358-373) — текст:
```tsx
<AlertDialogTitle>Удалить пользователя?</AlertDialogTitle>
<AlertDialogDescription>
  Все связанные данные будут переприсвоены или удалены. Это действие нельзя отменить.
</AlertDialogDescription>
```
и кнопка подтверждения — текст `Удалить`.

- [ ] **Step 3: Проверка**

```bash
npx tsc --noEmit && npm run build
```
Ожидание: без ошибок. Внимание: если где-то ещё используется `u.isActive` (например `app/admin/news` нет — проверить grep'ом `isActive` по `CollegeLMS.Next/` и удалить все вхождения из кода; в `lib/constants.ts` его нет).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: админ-страница пользователей — жёсткое удаление и клик по строке"
```

---

### Task 6: Frontend — FormField, FormErrorBanner, parseErrors + применение

**Files:**
- Create: `CollegeLMS.Next/components/FormField.tsx`
- Create: `CollegeLMS.Next/components/FormErrorBanner.tsx`
- Create: `CollegeLMS.Next/lib/errors.ts`
- Modify: `CollegeLMS.Next/app/admin/page.tsx` (диалоги create/edit)
- Modify: `CollegeLMS.Next/app/login/page.tsx`

**Interfaces:**
- Consumes: бекенд-валидаторы (Task 3)
- Produces: `FormField` (label + children + error + hint), `FormErrorBanner` (banner для формы), `parseErrors(err)` → `{ fieldErrors, message }` — используемые в последующих блоках (новости, курсы, feedback)

- [ ] **Step 1: Создать `components/FormField.tsx`**

```tsx
"use client"

import { type ReactNode } from "react"
import { Label } from "@/components/ui/label"

interface FormFieldProps {
  id: string
  label: string
  error?: string
  hint?: string
  required?: boolean
  children: ReactNode
}

export default function FormField({ id, label, error, hint, required, children }: FormFieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>
        {label}
        {required && <span className="text-destructive"> *</span>}
      </Label>
      {children}
      {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
      {error && (
        <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Создать `components/FormErrorBanner.tsx`**

```tsx
import { AlertCircle } from "lucide-react"

export default function FormErrorBanner({ message }: { message: string }) {
  return (
    <div role="alert" className="flex items-start gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
      <AlertCircle size={16} className="mt-0.5 shrink-0" />
      <span>{message}</span>
    </div>
  )
}
```

- [ ] **Step 3: Создать `lib/errors.ts`**

```ts
export interface ParsedErrors {
  fieldErrors: Record<string, string[]>
  message: string | null
}

export function parseErrors(err: unknown): ParsedErrors {
  const anyErr = err as {
    response?: {
      data?: {
        errors?: Record<string, string[]>
        errorMessage?: string
      }
    }
  }
  const data = anyErr?.response?.data
  if (data?.errors && Object.keys(data.errors).length > 0) {
    return { fieldErrors: data.errors, message: null }
  }
  return { fieldErrors: {}, message: data?.errorMessage ?? "Ошибка сети. Попробуйте позже" }
}
```

- [ ] **Step 4: Применить к `/admin` диалогам**

`app/admin/page.tsx`:
- Импорты: `FormField`, `FormErrorBanner`, `parseErrors`
- Добавить state: `const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})`
- В `resetForm` добавить `setFieldErrors({})`
- В `handleCreate`/`handleUpdate` заменить блок обработки ошибки:
```tsx
} catch (err) {
  const parsed = parseErrors(err)
  setFieldErrors(parsed.fieldErrors)
  setFormError(parsed.message)
}
```
- В форме create каждый блок заменить на FormField, например логин:
```tsx
<FormField
  id="create-login"
  label="Логин"
  required
  error={fieldErrors.login?.[0]}
  hint="Используется для входа в систему"
>
  <Input id="create-login" type="text" value={formLogin} onChange={e => setFormLogin(e.target.value)} />
</FormField>
```
и аналогично email (`hint="Рабочая почта пользователя"`), пароль, ФИО. `{formError && <ErrorBanner message={formError} />}` (строка 196/298) заменить на `{formError && <FormErrorBanner message={formError} />}`.

- [ ] **Step 5: Применить к `/login`**

`app/login/page.tsx`:
- Импорт `FormField` и `parseErrors`
- Заменить `error` state на `{ fieldErrors, formError }`:
```tsx
const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
const [formError, setFormError] = useState<string | null>(null)
```
- В `handleSubmit` catch:
```tsx
} catch (err) {
  const parsed = parseErrors(err)
  setFieldErrors(parsed.fieldErrors)
  setFormError(parsed.message ?? "Неверный логин или пароль")
}
```
- Поля формы заменить на FormField (id `login`, `password`), ошибки полей из `fieldErrors.login?.[0]` / `fieldErrors.password?.[0]`
- Блок ошибки (строки 108-112) — использовать `FormErrorBanner`

- [ ] **Step 6: Проверка**

```bash
npx tsc --noEmit && npm run build
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: единый механизм валидации форм (FormField, parseErrors)"
```

---

### Task 7: Frontend — страница профиля пользователя `/admin/users/[id]`

**Files:**
- Create: `CollegeLMS.Next/app/admin/users/[id]/page.tsx`

**Interfaces:**
- Consumes: `GET /api/users/{id}/profile` (Task 4), типы `UserProfileResponse` (Task 5)
- Produces: страница профиля

- [ ] **Step 1: Создать страницу**

`app/admin/users/[id]/page.tsx`:
```tsx
"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { useParams, useRouter } from "next/navigation"
import { ChevronLeft, BookOpen, Newspaper } from "lucide-react"
import type { Result, UserProfileResponse } from "@/types"
import api from "@/lib/api"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import ErrorBanner from "@/components/ErrorBanner"
import { Skeleton } from "@/components/ui/skeleton"
import { roleLabels, roleVariants } from "@/lib/constants"

export default function UserProfilePage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const [profile, setProfile] = useState<UserProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<UserProfileResponse>>(`/api/users/${id}/profile`)
      .then(res => {
        const body = res.data
        if (body.isSuccess && body.data) setProfile(body.data)
        else setError(body.errorMessage ?? "Ошибка загрузки")
      })
      .catch(() => setError("Ошибка загрузки профиля"))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <Skeleton className="h-8 w-40" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-32 w-full" />
      </div>
    )
  }

  if (error || !profile) {
    return (
      <div className="p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error ?? "Профиль не найден"} />
      </div>
    )
  }

  const { user, courses, news } = profile

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
      <button
        onClick={() => router.push("/admin")}
        className="flex items-center gap-1 text-sm text-muted-foreground hover:text-fg w-fit"
      >
        <ChevronLeft size={16} /> К списку пользователей
      </button>

      <Card>
        <CardHeader>
          <CardTitle>Пользователь</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 text-sm">
          <div className="flex items-center justify-between">
            <span className="font-medium">{user.fullName}</span>
            <Badge variant={roleVariants[user.role] ?? "secondary"}>
              {roleLabels[user.role] ?? user.role}
            </Badge>
          </div>
          <p>Логин: {user.login}</p>
          <p>Email: {user.email}</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <BookOpen size={16} /> Курсы преподавателя
          </CardTitle>
        </CardHeader>
        <CardContent>
          {courses.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет курсов</p>
          ) : (
            <ul className="flex flex-col divide-y divide-border">
              {courses.map(c => (
                <li key={c.id}>
                  <Link href={`/courses/${c.id}`} className="block py-2 text-sm hover:text-primary">
                    {c.title}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Newspaper size={16} /> Новости автора
          </CardTitle>
        </CardHeader>
        <CardContent>
          {news.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет новостей</p>
          ) : (
            <ul className="flex flex-col divide-y divide-border">
              {news.map(n => (
                <li key={n.id}>
                  <Link href={`/news/${n.id}`} className="flex items-center justify-between py-2 text-sm hover:text-primary">
                    <span>{n.title}</span>
                    <span className="text-xs text-muted-foreground">
                      {new Date(n.publishedAt).toLocaleDateString("ru-RU")}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
```
Проверить: компоненты `Card`, `CardContent`, `CardHeader`, `CardTitle`, `Skeleton` существуют в `components/ui/` (Card — стандартный shadcn; Skeleton — проверить, иначе заменить на `<div className="animate-pulse rounded-md bg-muted h-8 w-40" />`).

- [ ] **Step 2: Проверка**

```bash
npx tsc --noEmit && npm run build
```
Ожидание: сборка с новой страницей `/admin/users/[id]`.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: страница профиля пользователя"
```

---

### Task 8: Frontend — дашборд диспетчера `/dispatcher/dashboard`

**Files:**
- Create: `CollegeLMS.Next/app/(authenticated)/dispatcher/dashboard/page.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/layout.tsx` (dispatcherMenu)
- Modify: `CollegeLMS.Next/types/index.ts` (CalendarResponse)

**Interfaces:**
- Consumes: `GET /api/schedule?view=calendar` → `Result<CalendarResponse>`; `GET /api/groups` → `Result<List<GroupResponse>>`
- Produces: страница `/dispatcher/dashboard`

- [ ] **Step 1: Добавить типы**

`types/index.ts` — добавить:
```ts
export interface ScheduleEntryResponse {
  id: string
  groupId: string
  groupName: string
  teacherId: string | null
  teacherName: string | null
  subject: string
  room: string
  dayOfWeek: number
  numberPair: number
  startTime: string
  endTime: string
  weeks: number[]
  lessonType: string
}

export interface CalendarDayResponse {
  day: string
  dayOfWeek: number
  entries: ScheduleEntryResponse[]
}

export interface CalendarResponse {
  weekStart: string
  days: CalendarDayResponse[]
}
```
(Проверить, нет ли уже этих типов в файле — если есть, не дублировать.)

- [ ] **Step 2: Создать страницу**

`app/(authenticated)/dispatcher/dashboard/page.tsx`:
```tsx
"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { CalendarDays, UsersRound } from "lucide-react"
import type { Result, CalendarResponse, GroupResponse } from "@/types"
import api from "@/lib/api"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import ErrorBanner from "@/components/ErrorBanner"
import { Skeleton } from "@/components/ui/skeleton"

const DAY_LABELS = ["", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"]

export default function DispatcherDashboardPage() {
  const [calendar, setCalendar] = useState<CalendarResponse | null>(null)
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      api.get<Result<CalendarResponse>>("/api/schedule", { params: { view: "calendar" } }),
      api.get<Result<GroupResponse[]>>("/api/groups"),
    ])
      .then(([calRes, groupsRes]) => {
        if (calRes.data.isSuccess && calRes.data.data) setCalendar(calRes.data.data)
        if (groupsRes.data.isSuccess && groupsRes.data.data) setGroups(groupsRes.data.data)
      })
      .catch(() => setError("Ошибка загрузки данных"))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-64 w-full" />
        <Skeleton className="h-32 w-full" />
      </div>
    )
  }

  const today = new Date().getDay()

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Панель диспетчера</h2>

      {error && <ErrorBanner message={error} />}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <CalendarDays size={16} /> Расписание на неделю
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!calendar || calendar.days.length === 0 ? (
            <p className="text-sm text-muted-foreground">Расписание пусто</p>
          ) : (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {calendar.days.map(day => (
                <div
                  key={day.dayOfWeek}
                  className={`rounded-md border p-3 ${day.dayOfWeek === today ? "border-primary bg-primary/5" : "border-border"}`}
                >
                  <p className="mb-2 text-sm font-medium">{day.day}</p>
                  {day.entries.length === 0 ? (
                    <p className="text-xs text-muted-foreground">Нет занятий</p>
                  ) : (
                    <ul className="flex flex-col gap-1.5">
                      {day.entries.map(e => (
                        <li key={e.id} className="text-xs">
                          <span className="font-medium">{e.numberPair} пара</span> — {e.subject}
                          <span className="text-muted-foreground"> · {e.groupName}</span>
                          <span className="text-muted-foreground"> · {e.room}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <UsersRound size={16} /> Группы
          </CardTitle>
        </CardHeader>
        <CardContent>
          {groups.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет групп</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {groups.map(g => (
                <Link
                  key={g.id}
                  href={`/schedule?groupId=${g.id}`}
                  className="rounded-md border border-border px-3 py-1.5 text-sm hover:border-primary hover:text-primary"
                >
                  {g.name} ({g.studentCount})
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
```
(Проверить реальную форму ответа `/api/schedule?view=calendar`: `Result<CalendarResponse>`; если CalendarDayResponse содержит другие поля — адаптировать.)

- [ ] **Step 3: Обновить меню диспетчера**

`app/(authenticated)/layout.tsx` — `dispatcherMenu` (строки 33-40):
```tsx
const dispatcherMenu = [
  { label: "Расписание", items: [
    { href: "/dispatcher/dashboard", label: "Дашборд", icon: LayoutDashboard },
    { href: "/schedule", label: "Расписание", icon: CalendarDays },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки", icon: Settings },
  ]},
]
```

- [ ] **Step 4: Проверка**

```bash
npx tsc --noEmit && npm run build
```

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: дашборд диспетчера — расписание и группы"
```

---

### Task 9: Frontend — единое меню админа (фикс бага drawer)

**Files:**
- Create: `CollegeLMS.Next/lib/menus.ts`
- Modify: `CollegeLMS.Next/app/admin/layout.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/layout.tsx`

**Interfaces:**
- Produces: `adminMenuSections` (export из `lib/menus.ts`) — единый источник меню админа

- [ ] **Step 1: Создать `lib/menus.ts`**

Перенести `menuSections` и `roleMap` из `app/admin/layout.tsx` (строки 10-44, 62-77) в новый файл:
```ts
import { Users, Newspaper, MessageSquare, BookOpen, UsersRound, GraduationCap, CalendarDays, BookType, BadgeInfo, Banknote, type LucideIcon } from "lucide-react"

export interface MenuItem {
  href: string
  label: string
  icon?: LucideIcon
}

export interface MenuSection {
  label: string
  items: MenuItem[]
}

export const adminMenuSections: MenuSection[] = [
  {
    label: "Система",
    items: [
      { href: "/admin", label: "Пользователи", icon: Users },
      { href: "/admin/news", label: "Новости", icon: Newspaper },
      { href: "/admin/feedback", label: "Обратная связь", icon: MessageSquare },
    ],
  },
  {
    label: "Обучение",
    items: [
      { href: "/courses", label: "Курсы", icon: BookOpen },
      { href: "/groups", label: "Группы", icon: UsersRound },
      { href: "/teachers", label: "Преподаватели", icon: GraduationCap },
      { href: "/students", label: "Студенты", icon: Users },
      { href: "/admin/semesters", label: "Семестры", icon: CalendarDays },
      { href: "/admin/specialties", label: "Специальности", icon: BadgeInfo },
      { href: "/admin/exams", label: "Экзамены", icon: ClipboardCheck },
      { href: "/admin/testing", label: "Тесты", icon: BookType },
    ],
  },
  {
    label: "Финансы",
    items: [
      { href: "/admin/stipends", label: "Стипендии", icon: Banknote },
    ],
  },
  {
    label: "Расписание",
    items: [
      { href: "/schedule", label: "Расписание", icon: CalendarDays },
    ],
  },
]

export const adminRoleMap: Record<string, string[]> = {
  "/admin": ["Admin"],
  "/admin/news": ["Admin", "Dispatcher"],
  "/admin/feedback": ["Admin"],
  "/admin/import": ["Admin"],
  "/courses": ["Admin", "Teacher"],
  "/groups": ["Admin"],
  "/teachers": ["Admin"],
  "/students": ["Admin"],
  "/admin/semesters": ["Admin"],
  "/admin/specialties": ["Admin"],
  "/admin/exams": ["Admin"],
  "/admin/testing": ["Admin"],
  "/admin/stipends": ["Admin"],
  "/schedule": ["Admin", "Dispatcher", "Teacher"],
}
```
(иконка `ClipboardCheck` — добавить в импорт из lucide-react; не забыть удалить импорты иконок из layout.tsx, ставшие неиспользуемыми)

- [ ] **Step 2: Обновить `app/admin/layout.tsx`**

- Удалить локальные `menuSections` (строки 10-44) и `roleMap` (строки 62-77)
- Импорт: `import { adminMenuSections, adminRoleMap } from "@/lib/menus"`
- Заменить `menuSections` на `adminMenuSections` в фильтрации (строки 58-81) и `roleMap` на `adminRoleMap`
- Очистить импорт lucide-иконок (оставить только используемые)

- [ ] **Step 3: Обновить `app/(authenticated)/layout.tsx` — фикс бага**

- Импорт: `import { adminMenuSections } from "@/lib/menus"`
- `menuByRole` (строки 42-46):
```tsx
const menuByRole: Record<string, typeof studentMenu> = {
  Student: studentMenu,
  Teacher: teacherMenu,
  Dispatcher: dispatcherMenu,
  Admin: adminMenuSections,
}
```
(тип `typeof studentMenu` совместим с `adminMenuSections` — оба `MenuSection[]` с icon: `LucideIcon | undefined`; если TS ругается на `undefined` в `icon`, поправить тип в `AuthenticatedShell`/`menus.ts` — `icon?: LucideIcon` уже optional)
- Это чинит баг: админ на `/courses` и любых разделах `(authenticated)` теперь видит полное админ-меню

- [ ] **Step 4: Проверка**

```bash
npx tsc --noEmit && npm run build
```

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "fix: единое меню админа во всех разделах"
```

---

## Self-Review (Block 2)

| Требование дизайна (§3) | Task |
|---|---|
| 3.1 Удалить `IsActive` (backend+frontend+миграция+тесты) | 1, 5 |
| 3.1 Жёсткое удаление с переприсваиванием News/Course → админ | 2 |
| 3.1 Профили Teacher/Student удаляются, сдачи каскадом | 2 |
| 3.1 DTO без IsActive, тумблер убран, AlertDialog «Удалить» | 1, 5 |
| 3.2 Профиль по клику `/admin/users/[id]` + API | 4, 7 |
| 3.3 Уникальность Email (Create/Update), обновление Email | 3 |
| 3.3 FormField + FormErrorBanner + parseErrors + применение | 6 |
| 3.4 Dispatcher dashboard (расписание + группы) + пункт меню | 8 |
| 3.5 Единое меню админа, фикс drawer в Курсах | 9 |
| Валидаторы: русские сообщения + hint | 6 (hint), 3 (Email) |
