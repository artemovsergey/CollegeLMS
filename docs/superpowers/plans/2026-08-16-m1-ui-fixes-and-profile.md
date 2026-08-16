# M1: UI-фиксы + профиль + аватары + баг курсов — план реализации

> **Для агентных исполнителей:** обязательный саб-скилл: superpowers:executing-plans (или subagent-driven-development). Задачи используют чекбоксы `- [ ]`.

**Goal:** Исправить баги login/shell/курсов, добавить редактирование преподавательских полей и аватары в профиль.

**Architecture:** Backend — расширение сущностей User/Teacher, ProfileResponse, AuthService (обновление профиля + загрузка аватара через ImageSharp). Frontend — правки login, AuthenticatedShell, меню, профиль, фикс параметра teacherId.

**Tech Stack:** .NET 10 + EF Core (Npgsql), ImageSharp, Next.js 14 + TS + Tailwind v4, xUnit + Moq + Bogus, WebApplicationFactory.

## Global Constraints

- Сообщения об ошибках и Swagger-документация — на русском
- `Result<T>` везде, без try-catch в контроллерах/сервисах
- Primary constructor DI, `CancellationToken ct`, `AsNoTracking()` на чтении
- String props: `HasMaxLength()` обязателен; Enum: `HasConversion<string>()` + `HasMaxLength()`
- Мапперы в `Mappers/`, интерфейсы в `Interfaces/`, DI в `Extensions/ServiceCollectionExtensions.cs`
- Student НЕ может менять аватар (фото загружает администратор в будущем)
- Категории: `None` («Без категории») / `First` («Первая») / `Higher` («Высшая»)
- Адаптивность: проверка на 1366×768 и ~393px viewport

---

### Task 0: Подготовка репозитория

**Files:** — (только git)

- [ ] **Step 1: Синхронизация и ветка**

```bash
cd /home/user1/CollegeLMS
git fetch origin && git pull --rebase origin master
git checkout -b feature/m1-ui-fixes
```

- [ ] **Step 2: Зафиксировать дизайн-документы**

```bash
git add docs/superpowers/specs/2026-08-16-ui-courses-lessons-schedule-notifications-design.md docs/superpowers/plans/2026-08-16-m1-ui-fixes-and-profile.md
git commit -m "docs: дизайн и план M1 (UI-фиксы, профиль, аватары)"
```

---

### Task 1: Backend — сущности, enum, конфигурации, миграция

**Files:**
- Modify: `CollegeLMS.API/Entities/User.cs`
- Modify: `CollegeLMS.API/Entities/Teacher.cs`
- Create: `CollegeLMS.API/Entities/Enums/TeacherCategory.cs`
- Modify: `CollegeLMS.API/Data/Configurations/UserConfiguration.cs`
- Modify: `CollegeLMS.API/Data/Configurations/TeacherConfiguration.cs`

**Interfaces:**
- Produces: `User.AvatarPath` (string?), `Teacher.Category` (TeacherCategory), enum `TeacherCategory { None, First, Higher }`

- [ ] **Step 1: Enum**

`CollegeLMS.API/Entities/Enums/TeacherCategory.cs`:
```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum TeacherCategory
{
    None,
    First,
    Higher,
}
```

- [ ] **Step 2: User.AvatarPath**

`User.cs` — добавить свойство:
```csharp
public string? AvatarPath { get; set; }
```

- [ ] **Step 3: Teacher.Category**

`Teacher.cs` — добавить:
```csharp
using CollegeLMS.API.Entities.Enums;
// в класс:
public TeacherCategory Category { get; set; }
```

- [ ] **Step 4: EF-конфигурации**

`UserConfiguration.cs` — внутри `Configure`:
```csharp
builder.Property(x => x.AvatarPath).HasMaxLength(512);
```
`TeacherConfiguration.cs`:
```csharp
builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
```

- [ ] **Step 5: Миграция**

```bash
cd /home/user1/CollegeLMS
dotnet ef migrations add AddUserAvatarAndTeacherCategory --project CollegeLMS.API -- --provider Npgsql
```

- [ ] **Step 6: Сборка**

```bash
dotnet build
```
Expected: сборка успешна (0 ошибок).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: пользовательский аватар и категория преподавателя (сущности, миграция)"
```

---

### Task 2: Backend — DTO, маппер, AuthService (обновление профиля)

**Files:**
- Modify: `CollegeLMS.API/Dtos/ProfileResponse.cs`
- Modify: `CollegeLMS.API/Dtos/UpdateProfileRequest.cs`
- Modify: `CollegeLMS.API/Dtos/UserResponse.cs` (если есть поля avatar — проверить)
- Modify: `CollegeLMS.API/Mappers/UserMapper.cs`
- Modify: `CollegeLMS.API/Services/AuthService.cs`

**Interfaces:**
- Produces: `UpdateProfileRequest.CyclicalCommission` (string?), `UpdateProfileRequest.Category` (string?)
- `TeacherProfileData.Category` (string — 'None'/'First'/'Higher')
- `ProfileResponse.AvatarUrl` (string?)
- `UserResponse.AvatarUrl` (string?)

- [ ] **Step 1: DTO — ProfileResponse**

`ProfileResponse.cs`:
```csharp
public class ProfileResponse
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public TeacherProfileData? TeacherData { get; set; }
    public StudentProfileData? StudentData { get; set; }
}

public class TeacherProfileData
{
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
```

- [ ] **Step 2: DTO — UpdateProfileRequest**

`UpdateProfileRequest.cs`:
```csharp
namespace CollegeLMS.API.Dtos;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CyclicalCommission { get; set; }
    public string? Category { get; set; }
}
```

- [ ] **Step 3: UserResponse — AvatarUrl**

Проверить `Dtos/UserResponse.cs`, добавить:
```csharp
public string? AvatarUrl { get; set; }
```

- [ ] **Step 4: Маппер**

`UserMapper.cs`:
```csharp
public static UserResponse ToDto(this User entity, Guid? teacherId = null)
{
    return new UserResponse
    {
        Id = entity.Id,
        Login = entity.Login,
        Email = entity.Email,
        FullName = entity.FullName,
        Role = entity.Role.ToString(),
        TeacherId = teacherId,
        AvatarUrl = entity.AvatarPath,
    };
}

public static ProfileResponse ToProfileDto(this User entity, object? roleData = null)
{
    var dto = new ProfileResponse
    {
        Id = entity.Id,
        Login = entity.Login,
        Email = entity.Email,
        FullName = entity.FullName,
        Role = entity.Role.ToString(),
        AvatarUrl = entity.AvatarPath,
    };

    if (roleData is Teacher teacher)
    {
        dto.TeacherData = new TeacherProfileData
        {
            CyclicalCommission = teacher.CyclicalCommission,
            Position = teacher.Position,
            Category = teacher.Category.ToString(),
        };
    }
    else if (roleData is Student student)
    {
        dto.StudentData = new StudentProfileData
        {
            GroupId = student.GroupId.ToString(),
            GroupName = student.Group?.Name ?? string.Empty,
            RecordBookNumber = student.RecordBookNumber,
        };
    }

    return dto;
}
```

- [ ] **Step 5: AuthService.UpdateProfileAsync — преподавательские поля**

В `UpdateProfileAsync` после `user.UpdatedAt = DateTime.UtcNow;` добавить:
```csharp
if (user.Role == Entities.Enums.UserRole.Teacher)
{
    var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
    if (teacher is not null)
    {
        if (!string.IsNullOrWhiteSpace(request.CyclicalCommission))
            teacher.CyclicalCommission = request.CyclicalCommission;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            if (Enum.TryParse<Entities.Enums.TeacherCategory>(request.Category, out var category))
                teacher.Category = category;
        }
        teacher.UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 6: Unit-тесты (TDD-цикл)**

Файл `CollegeLMS.Tests/Unit/Services/AuthServiceTests.cs` — добавить тесты:
```csharp
[Fact]
public async Task UpdateProfileAsync_UpdatesTeacherCommissionAndCategory()
{
    var user = UserFixture.CreateFaker().Generate();
    user.Role = API.Entities.Enums.UserRole.Teacher;
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("x");
    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    var teacher = TeacherFixture.CreateFaker().Generate();
    teacher.UserId = user.Id;
    teacher.CyclicalCommission = "Информационных технологий";
    teacher.Category = API.Entities.Enums.TeacherCategory.None;
    _db.Teachers.Add(teacher);
    await _db.SaveChangesAsync();

    var result = await _sut.UpdateProfileAsync(
        user.Id,
        new UpdateProfileRequest
        {
            FullName = user.FullName,
            Email = user.Email,
            CyclicalCommission = "Программирования",
            Category = "Higher",
        },
        CancellationToken.None
    );

    result.IsSuccess.Should().BeTrue();
    result.Data!.TeacherData!.CyclicalCommission.Should().Be("Программирования");
    result.Data.TeacherData.Category.Should().Be("Higher");
}
```

- [ ] **Step 7: Прогнать тесты**

```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```
Expected: 1 новый тест PASS, остальные PASS.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: преподавательские поля в профиле (комиссия, категория) и аватар в DTO"
```

---

### Task 3: Backend — загрузка аватара (AuthService + контроллер)

**Files:**
- Modify: `CollegeLMS.API/Interfaces/IAuthService.cs`
- Modify: `CollegeLMS.API/Services/AuthService.cs`
- Modify: `CollegeLMS.API/Controllers/AuthController.cs`

**Interfaces:**
- Produces: `Task<Result<ProfileResponse>> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken ct)`; endpoint `POST /api/auth/avatar`

- [ ] **Step 1: Интерфейс**

`IAuthService.cs` — добавить:
```csharp
Task<Result<ProfileResponse>> UploadAvatarAsync(
    Guid userId,
    IFormFile file,
    CancellationToken ct = default
);
```

- [ ] **Step 2: Реализация**

`AuthService.cs` — добавить usings и метод:
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Http;

public async Task<Result<ProfileResponse>> UploadAvatarAsync(
    Guid userId,
    IFormFile file,
    CancellationToken ct
)
{
    var user = await db.Users.FindAsync([userId], ct);
    if (user is null)
        return Result<ProfileResponse>.Fail("Пользователь не найден", 404);

    if (user.Role == Entities.Enums.UserRole.Student)
        return Result<ProfileResponse>.Fail("Студенты не могут менять аватар", 403);

    if (file is null || file.Length == 0)
        return Result<ProfileResponse>.Fail("Файл не выбран", 400);

    if (file.Length > 5 * 1024 * 1024)
        return Result<ProfileResponse>.Fail("Файл больше 5 МБ", 400);

    if (file.ContentType is not ("image/jpeg" or "image/png"))
        return Result<ProfileResponse>.Fail("Разрешены только JPEG и PNG", 400);

    var uploadsDir = Path.Combine("uploads", "avatars");
    Directory.CreateDirectory(uploadsDir);
    var outputPath = Path.Combine(uploadsDir, $"{userId}.jpg");

    await using var inputStream = file.OpenReadStream();
    using var image = await Image.LoadAsync(inputStream, ct);
    image.Mutate(x =>
        x.Resize(new ResizeOptions
        {
            Size = new Size(256, 256),
            Mode = ResizeMode.Crop,
        })
    );
    var encoder = new JpegEncoder { Quality = 85 };
    await image.SaveAsync(outputPath, encoder, ct);

    user.AvatarPath = $"/uploads/avatars/{userId}.jpg";
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);

    object? roleData = null;
    if (user.Role == Entities.Enums.UserRole.Teacher)
        roleData = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == userId, ct);
    else if (user.Role == Entities.Enums.UserRole.Student)
        roleData = await db.Students.AsNoTracking().Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == userId, ct);

    return Result<ProfileResponse>.Ok(user.ToProfileDto(roleData));
}
```

- [ ] **Step 3: Контроллер**

`AuthController.cs` — внутри класса добавить:
```csharp
/// <summary>Загрузить аватар текущего пользователя.</summary>
/// <remarks>Доступно преподавателям, администраторам и диспетчерам. Студенты аватар менять не могут — фото загружает администратор.</remarks>
/// <param name="file">Изображение JPEG или PNG (до 5 МБ)</param>
/// <param name="ct">Токен отмены</param>
/// <response code="200">Аватар обновлён — возвращает профиль</response>
/// <response code="400">Некорректный файл</response>
/// <response code="401">Не авторизован</response>
/// <response code="403">Студент не может менять аватар</response>
/// <response code="404">Пользователь не найден</response>
/// <response code="500">Внутренняя ошибка сервера</response>
[HttpPost("avatar")]
[Authorize(Roles = "Teacher,Admin,Dispatcher")]
[Consumes("multipart/form-data")]
[RequestSizeLimit(5 * 1024 * 1024)]
[SwaggerOperation(Summary = "Загрузить аватар")]
[SwaggerResponse(200, "Аватар обновлён", typeof(Result<ProfileResponse>))]
[SwaggerResponse(400, "Некорректный файл")]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(403, "Студент не может менять аватар")]
[SwaggerResponse(404, "Пользователь не найден")]
[SwaggerResponse(500, "Ошибка сервера")]
[ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<Result<ProfileResponse>>> UploadAvatar(
    [FromForm] IFormFile file,
    CancellationToken ct
)
{
    var userId = User.GetUserId();
    var result = await authService.UploadAvatarAsync(userId, file, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

- [ ] **Step 4: Сборка**

```bash
dotnet build
```
Expected: 0 ошибок.

- [ ] **Step 5: Интеграционные тесты**

`CollegeLMS.Tests/Integration/Controllers/AuthControllerTests.cs` — добавить два теста (паттерн авторизации берём из существующих тестов файла):
```csharp
[Fact]
public async Task UploadAvatar_ReturnsForbidden_ForStudent()
{
    // Arrange: вход как студент (login student / password student из сидов), создать multipart-контент
    using var content = new MultipartFormDataContent
    {
        { new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]), "file", "avatar.jpg" }
    };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await client.PostAsync("/api/auth/avatar", content);

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task UploadAvatar_ReturnsOk_ForTeacher()
{
    // Arrange: вход как преподаватель, multipart с валидным PNG/JPEG-буфером
    var response = await client.PostAsync("/api/auth/avatar", content);
    var body = await Deserialize<Result<ProfileResponse>>(response);
    body.IsSuccess.Should().BeTrue();
    body.Data!.AvatarUrl.Should().Contain("/uploads/avatars/");
}
```

⚠️ Перед написанием — прочитать реальную структуру `AuthControllerTests.cs` (как получается токен, хелперы) и использовать существующие паттерны; для файла с минимальным валидным JPEG можно использовать байтовый буфер 1x1 JPEG.

- [ ] **Step 6: Прогнать тесты**

```bash
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
```
Expected: новые тесты PASS.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: загрузка аватара профиля (POST /api/auth/avatar)"
```

---

### Task 4: Backend — сиды и консистентность

**Files:**
- Modify: `CollegeLMS.API/Data/DataSeeder.cs` (создание Teacher)
- Modify: `CollegeLMS.API/Services/UserService.cs` (EnsureProfileAsync — Category по умолчанию)

- [ ] **Step 1: DataSeeder — категория преподавателя**

В `DataSeeder.cs` при создании Teacher Иванова (Position = «Преподаватель высшей категории») добавить:
```csharp
Category = Entities.Enums.TeacherCategory.Higher,
```

- [ ] **Step 2: EnsureProfileAsync**

`UserService.cs` строка ~127, в `new Teacher { ... }` добавить:
```csharp
Category = Entities.Enums.TeacherCategory.None,
```

- [ ] **Step 3: Сборка**

```bash
dotnet build && dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: категория преподавателя в сидах и при создании профиля"
```

---

### Task 5: Frontend — типы, FormField, страница логина

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts` (UserResponse ~ line 100+, ProfileResponse ~393, TeacherProfileData ~382, UpdateProfileRequest ~403)
- Modify: `CollegeLMS.Next/components/FormField.tsx`
- Modify: `CollegeLMS.Next/app/login/page.tsx`

- [ ] **Step 1: Типы**

В `types/index.ts`:
```ts
// UserResponse — добавить
avatarUrl?: string | null

// TeacherProfileData (line ~382) — добавить
category?: string

// ProfileResponse (line ~393) — добавить
avatarUrl?: string | null

// UpdateProfileRequest (line ~403)
export interface UpdateProfileRequest {
  fullName: string
  email: string
  cyclicalCommission?: string
  category?: string
}

// Новый тип категорий
export type TeacherCategory = "None" | "First" | "Higher"
```

- [ ] **Step 2: FormField — showAsterisk**

```tsx
interface FormFieldProps {
  id: string
  label: string
  error?: string
  hint?: string
  required?: boolean
  showAsterisk?: boolean   // new: скрыть маркер «*» при required
  children: ReactNode
}

export default function FormField({ id, label, error, hint, required, showAsterisk = true, children }: FormFieldProps) {
  ...
  {required && showAsterisk && <span className="text-destructive"> *</span>}
  ...
}
```

- [ ] **Step 3: Логин — без звёздочек**

`app/login/page.tsx`:
- `FormField id="login"` → убрать `required`, добавить `showAsterisk={false}`; то же для пароля (вместо `required` — `showAsterisk={false}`; звёздочка зависела только от `required`, поэтому просто убрать `required`)
- Мобильный логотип: `style={{ maxHeight: "6rem", width: 'auto', height: '100%' }}`
- `QUICK_LOGINS` — лейблы уже без подсказок, оставить как есть (проверить, что у опций нет суффиксов вида «(admin)»)

- [ ] **Step 4: Проверка сборки**

```bash
cd /home/user1/CollegeLMS/CollegeLMS.Next && npx tsc --noEmit
```
Expected: 0 ошибок.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(frontend): логин без звёздочек, крупный мобильный логотип, типы профиля"
```

---

### Task 6: Frontend — шапка, меню, правое драйвер-меню

**Files:**
- Modify: `CollegeLMS.Next/components/AuthenticatedShell.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/layout.tsx`
- Modify: `CollegeLMS.Next/lib/constants.ts` (roleLabels — проверить; добавить categoryLabels)

- [ ] **Step 1: Логотип и навигация**

`AuthenticatedShell.tsx`:
- href логотипа:
```tsx
const homeByRole: Record<string, string> = {
  Admin: "/admin",
  Dispatcher: "/schedule",
  Teacher: "/teacher/dashboard",
  Student: "/my/dashboard",
}
// <Link href={homeByRole[user?.role ?? ""] ?? "/my/dashboard"}>
```
- Тексты: `<span>Колледж связи</span>` (убрать вторую строку «имени В.А. Петрова» и блок `flex flex-col leading-tight` можно оставить однострочным).
- Логотип: выровнять по центру вертикали — контейнер уже `flex items-center`, обновить стиль картинки: `style={{ maxHeight: "40px", width: "auto", height: "auto" }} className="object-contain"`.

- [ ] **Step 2: Аватар в шапке и правом меню**

Шапка (кнопка профиля) — вместо инициалов, если есть аватар:
```tsx
{user?.avatarUrl ? (
  <Image src={user.avatarUrl} alt="Аватар" width={32} height={32} unoptimized className="h-8 w-8 rounded-full object-cover" />
) : (
  <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent">{initials}</span>
)}
```
Правое меню — та же логика с размером 64 (`h-16 w-16`), и заменить `{user?.login}` на `{user?.email}`.

Учесть: `user.avatarUrl` приходит из LoginResponse.User.AvatarUrl; права на изображение через nginx `/uploads/`.

- [ ] **Step 3: Левое меню**

`(authenticated)/layout.tsx`:
- Из `studentMenu`, `teacherMenu`, `dispatcherMenu` удалить секцию «Профиль» целиком
- `teacherMenu`: пункт «Курсы» → «Мои курсы» (href `/courses` без изменений)
- Убрать неиспользуемый импорт `Settings`

- [ ] **Step 4: TypeScript**

```bash
npx tsc --noEmit
```
Expected: 0 ошибок.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(frontend): шапка «Колледж связи», роль-навигация логотипа, аватар, меню без «Настроек»"
```

---

### Task 7: Frontend — профиль (преподавательские поля, аватар, без смены пароля)

**Files:**
- Modify: `CollegeLMS.Next/app/my/profile/page.tsx`
- Modify: `CollegeLMS.Next/lib/auth.tsx` (проверить наличие `setUser`/обновления пользователя; при отсутствии — добавить `updateUser`)

- [ ] **Step 1: lib/auth — обновление пользователя**

Проверить `CollegeLMS.Next/lib/auth.tsx`: контекст хранит `user` в localStorage. Добавить в провайдер:
```ts
const updateUser = (patch: Partial<UserResponse>) => {
  setUser(prev => {
    const next = prev ? { ...prev, ...patch } : prev
    if (next) localStorage.setItem("user", JSON.stringify(next))
    return next
  })
}
```
(и выставить в значение контекста; тип контекста дополнить)

- [ ] **Step 2: Профиль — преподавательские поля**

Состояние: `const [cyclicalCommission, setCyclicalCommission] = useState("")`, `const [category, setCategory] = useState<TeacherCategory | "">("")`.
`fetchProfile`: заполнять из `body.data.teacherData`.
`handleSave`:
```ts
const request: UpdateProfileRequest = {
  fullName,
  email,
  cyclicalCommission: profile?.teacherData ? cyclicalCommission : undefined,
  category: profile?.teacherData && category ? category : undefined,
}
```
Карточка «Данные преподавателя» — поля ввода:
```tsx
<div>
  <Label htmlFor="cyclicalCommission">Цикловая комиссия</Label>
  <Input id="cyclicalCommission" value={cyclicalCommission} onChange={e => setCyclicalCommission(e.target.value)} />
</div>
<div>
  <Label htmlFor="category">Категория</Label>
  <select id="category" value={category} onChange={e => setCategory(e.target.value as TeacherCategory | "")}
    className="w-full rounded-md border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary">
    <option value="None">Без категории</option>
    <option value="First">Первая</option>
    <option value="Higher">Высшая</option>
  </select>
</div>
```
«Должность» (position) — оставить read-only строкой.

- [ ] **Step 3: Убрать «Смену пароля»**

Удалить блок разметки «Смена пароля» + state `oldPassword/newPassword/changingPassword` + `handleChangePassword` + импорт `Lock` и `ChangePasswordRequest` (проверить использование).

- [ ] **Step 4: Блок аватара** (в «Основные данные», только если роль ≠ Student):

```tsx
{user?.role !== "Student" && (
  <div className="flex items-center gap-4">
    {profile.avatarUrl ? (
      // eslint-disable-next-line @next/next/no-img-element
      <img src={profile.avatarUrl} alt="Аватар" className="h-16 w-16 rounded-full object-cover" />
    ) : (
      <span className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/20 text-xl font-bold text-accent">
        {profile.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)}
      </span>
    )}
    <div className="flex flex-col gap-1">
      <input
        type="file"
        accept="image/jpeg,image/png"
        onChange={handleAvatarChange}
        className="text-sm file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:text-white"
      />
      <p className="text-xs text-muted-foreground">JPEG или PNG, до 5 МБ</p>
    </div>
  </div>
)}
```
Обработчик:
```ts
const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
  const file = e.target.files?.[0]
  if (!file) return
  const formData = new FormData()
  formData.append("file", file)
  setSaving(true)
  try {
    const res = await api.post<Result<ProfileResponse>>("/api/auth/avatar", formData)
    const body = res.data
    if (body.isSuccess && body.data) {
      setProfile(body.data)
      updateUser({ avatarUrl: body.data.avatarUrl })
      toast.success("Аватар обновлён")
    } else {
      toast.error(body.errorMessage ?? "Ошибка загрузки")
    }
  } catch {
    toast.error("Ошибка сети")
  } finally {
    setSaving(false)
  }
}
```

- [ ] **Step 5: TypeScript**

```bash
npx tsc --noEmit
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(frontend): профиль — комиссия/категория, аватар, без смены пароля"
```

---

### Task 8: Frontend — фикс «Мои курсы» (баг teacherId)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/page.tsx`

- [ ] **Step 1: Фикс параметра и заголовок**

```tsx
const params = ""   // параметр teacherId не нужен: бэкенд фильтрует по роли Teacher
```
Точнее — в строке 50 заменить на `const params = ""` (или просто не подставлять). Заголовок:
```tsx
<h2 className="text-xl font-semibold">{isTeacher ? "Мои курсы" : "Курсы"}</h2>
```

- [ ] **Step 2: Проверка**

```bash
npx tsc --noEmit
```

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "fix(frontend): курсы преподавателя — фильтрация по TeacherId (баг 0 курсов)"
```

---

### Task 9: Frontend — центрирование лоадеров

**Files:**
- Modify: `CollegeLMS.Next/components/LoadingSpinner.tsx` (оставить как есть — контейнер уже центрирует)
- Modify: страницы, где лоадер используется как основной (проверить `courses/page.tsx`, `my/courses/page.tsx`, `teacher/dashboard/page.tsx`, `my/dashboard/page.tsx`, `schedule/page.tsx`, `courses/[id]/page.tsx`)

- [ ] **Step 1: Проверить паттерны**

Найти использования `<LoadingSpinner` с `className="py-20"` (или подобным) и заменить каждое на обёртку:
```tsx
<div className="flex min-h-[60vh] items-center justify-center">
  <LoadingSpinner size="lg" />
</div>
```
Для `(authenticated)/layout.tsx` — лоадер уже `min-h-screen` (центрирован) — оставить.

- [ ] **Step 2: Commit**

```bash
git add -A && git commit -m "fix(frontend): лоадеры по центру экрана"
```

---

### Task 10: Документация (Swagger-examples, Postman) + e2e

**Files:**
- Modify: `CollegeLMS.API/SwaggerExamples/ProfileResponseExample.cs` (если есть — дополнить AvatarUrl; иначе пропустить)
- Modify: `docs/spec/CollegeLMS.postman_collection.json` — добавить `POST /api/auth/avatar`
- Modify: `CollegeLMS.Next/e2e/auth.spec.ts` — обновить под новые ожидания (без звёздочек; redirect по роли)

- [ ] **Step 1: Swagger/Postman**

Проверить папку `SwaggerExamples/`; при наличии `ProfileResponseExample` — добавить `avatarUrl`; в Postman-коллекцию добавить запись avatar (multipart).

- [ ] **Step 2: e2e-тест**

Обновить `e2e/auth.spec.ts`: ожидание отсутствия звёздочек/подписи; teacher login → redirect `/teacher/dashboard` (с моками маршрута `/api/teacher/dashboard`).

- [ ] **Step 3: Прогнать e2e**

```bash
cd /home/user1/CollegeLMS/CollegeLMS.Next && npx playwright test e2e/auth.spec.ts
```
Expected: PASS (моки маршрутов актуальны).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "docs: swagger-примеры, postman; test(e2e): логин и редиректы"
```

---

### Task 11: Финальная верификация (verification-before-completion)

- [ ] **Step 1: Backend**

```bash
dotnet build && dotnet test
```
Expected: build 0 ошибок, все тесты PASS.

- [ ] **Step 2: Frontend**

```bash
cd CollegeLMS.Next && npx tsc --noEmit && npm run build
```
Expected: 0 ошибок.

- [ ] **Step 3: Визуальная проверка**

`docker compose up --build -d` (или локально `npm run dev` + API), проверить: логин (без звёздочек, быстрый вход), педагог → дашборд через логотип, правое меню (email, аватар), профиль (комиссия/категория/аватар, без смены пароля), «Мои курсы» показывает курсы.

- [ ] **Step 4: Merge**

```bash
git checkout master && git merge feature/m1-ui-fixes
git push
```

---

## Саморевизия (по чек-листу writing-plans)

- **Покрытие спеки M1:** логин ✓ (Task 5), шапка/логотип ✓ (Task 6), левое меню ✓ (Task 6), правое меню ✓ (Task 6), лоадеры ✓ (Task 9), профиль ✓ (Task 7), аватар ✓ (Tasks 3, 7), баг курсов ✓ (Task 8), категория/комиссия ✓ (Tasks 1-2, 7), e2e/docs ✓ (Task 10).
- **Плейсхолдеры:** в Task 3 и 10 есть пометки «прочитать существующий файл и повторить паттерн» — допустимо, т.к. точный код зависит от существующих хелперов тестов (нельзя предсказать без чтения); исполнитель обязан прочитать файл перед написанием.
- **Консистентность типов:** `TeacherCategory` enum ↔ строка 'None'/'First'/'Higher' ↔ TS `TeacherCategory` — согласованы; `AvatarPath` (БД) vs `AvatarUrl` (DTO, всегда с префиксом `/uploads/...`) — согласовано в Task 3 Step 2.