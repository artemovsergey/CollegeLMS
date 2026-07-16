# Lecture↔Test, очистка UI, seed data — План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Связать лекции с тестами (optional FK), убрать дедлайны и оценки из UI студентов/преподавателей, наполнить приложение 4 полноценными курсами на основе МДК-репозиториев.

**Architecture:** Добавить `TestId?` в `Lecture` entity → миграция → DTO/mapper → сервис → фронтенд. Убрать `dueDate`/`maxScore`/`upcomingDeadlines`/`recentGrades` из frontend-компонентов. Расширить `DataSeeder` 4 курсами с лекциями, тестами, вопросами и заданиями.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, Next.js 14, TypeScript, Tailwind CSS 4

---

## Файловая структура

| Файл | Ответственность |
|------|-----------------|
| `CollegeLMS.API/Entities/Lecture.cs` | Добавить `TestId?`, `Test?` |
| `CollegeLMS.API/Data/Configurations/LectureConfiguration.cs` | FK + индекс |
| `CollegeLMS.API/Migrations/` | Новая миграция |
| `CollegeLMS.API/Dtos/Lecture/LectureResponse.cs` | `TestId?`, `TestTitle?` |
| `CollegeLMS.API/Dtos/Lecture/LectureRequest.cs` | `TestId?` |
| `CollegeLMS.API/Mappers/LectureMapper.cs` | Маппинг TestId |
| `CollegeLMS.API/Services/LectureService.cs` | Include Test, accept TestId |
| `CollegeLMS.API/Dtos/Student/StudentDashboardResponse.cs` | Убрать `upcomingDeadlines`, `recentGrades` |
| `CollegeLMS.API/Data/DataSeeder.cs` | 4 курса, лекции, тесты, вопросы, задания |
| `CollegeLMS.Next/app/my/dashboard/page.tsx` | Убрать дедлайны/оценки |
| `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx` | Убрать dueDate/maxScore |
| `CollegeLMS.Next/app/admin/courses/[id]/lectures/[lectureId]/edit/page.tsx` | Выпадающий список тестов |

---

## Task 1: Lecture Entity — добавить TestId

**Files:**
- Modify: `CollegeLMS.API/Entities/Lecture.cs`
- Modify: `CollegeLMS.API/Data/Configurations/LectureConfiguration.cs`

- [ ] **Step 1: Добавить TestId? и Test? в Lecture.cs**

```csharp
// CollegeLMS.API/Entities/Lecture.cs
public Guid? TestId { get; set; }
public Test? Test { get; set; }
```

- [ ] **Step 2: Обновить LectureConfiguration.cs — FK + индекс**

```csharp
// CollegeLMS.API/Data/Configurations/LectureConfiguration.cs
builder.HasOne(e => e.Test)
    .WithMany()
    .HasForeignKey(e => e.TestId)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasIndex(e => e.TestId);
```

- [ ] **Step 3: Проверить build**

Run: `dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore`

- [ ] **Step 4: Создать миграцию**

Run: `dotnet ef migrations add AddTestIdToLecture --project CollegeLMS.API -- --provider Npgsql`

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Entities/Lecture.cs CollegeLMS.API/Data/Configurations/LectureConfiguration.cs CollegeLMS.API/Migrations/
git commit -m "feat: add optional TestId FK to Lecture entity"
```

---

## Task 2: DTO и маппер для Lecture

**Files:**
- Modify: `CollegeLMS.API/Dtos/Lecture/LectureResponse.cs`
- Modify: `CollegeLMS.API/Dtos/Lecture/LectureRequest.cs`
- Modify: `CollegeLMS.API/Mappers/LectureMapper.cs`

- [ ] **Step 1: Добавить TestId? и TestTitle? в LectureResponse**

```csharp
// CollegeLMS.API/Dtos/Lecture/LectureResponse.cs
public Guid? TestId { get; set; }
public string? TestTitle { get; set; }
```

- [ ] **Step 2: Добавить TestId? в LectureRequest**

```csharp
// CollegeLMS.API/Dtos/Lecture/LectureRequest.cs
public Guid? TestId { get; set; }
```

- [ ] **Step 3: Обновить маппер — маппить TestId и TestTitle**

```csharp
// CollegeLMS.API/Mappers/LectureMapper.cs — в ToResponse
TestId = lecture.TestId,
TestTitle = lecture.Test?.Title,
```

- [ ] **Step 4: Проверить build**

Run: `dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore`

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Dtos/ CollegeLMS.API/Mappers/LectureMapper.cs
git commit -m "feat: add TestId to Lecture DTOs and mapper"
```

---

## Task 3: LectureService — поддержка TestId

**Files:**
- Modify: `CollegeLMS.API/Services/LectureService.cs`

- [ ] **Step 1: Обновить CreateAsync — принимать и сохранять TestId**

```csharp
// В CreateAsync: передать TestId при создании
var lecture = new Lecture
{
    Id = Guid.NewGuid(),
    CourseId = request.CourseId,
    Title = request.Title,
    Content = request.Content,
    Order = request.Order,
    TestId = request.TestId, // <-- добавить
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
};
```

- [ ] **Step 2: Обновить UpdateAsync — обновлять TestId**

```csharp
// В UpdateAsync: обновить TestId
existing.TestId = request.TestId;
```

- [ ] **Step 3: Обновить GetByIdAsync / GetAllAsync — Include Test**

```csharp
// Добавить .Include(l => l.Test) в запросы
var lecture = await db.Lectures
    .Include(l => l.Test)
    .AsNoTracking()
    .FirstOrDefaultAsync(l => l.Id == id, ct);
```

- [ ] **Step 4: Проверить build**

Run: `dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore`

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Services/LectureService.cs
git commit -m "feat: support TestId in LectureService CRUD"
```

---

## Task 4: UI — убрать дедлайны и оценки (студенты)

**Files:**
- Modify: `CollegeLMS.Next/app/my/dashboard/page.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx`

- [ ] **Step 1: Убрать upcomingDeadlines и recentGrades из dashboard**

В `app/my/dashboard/page.tsx`:
- Удалить секцию с `upcomingDeadlines` (карточки дедлайнов)
- Удалить секцию с `recentGrades` (карточки оценок)
- Оставить: курсы, прогресс-бар

- [ ] **Step 2: Убрать dueDate и maxScore из course detail (student view)**

В `app/(authenticated)/courses/[id]/page.tsx`:
- В вкладке Assignments: убрать отображение `dueDate` и `maxScore` из карточек заданий
- Убрать `submissionCount`

- [ ] **Step 3: Проверить build**

Run: `cd CollegeLMS.Next && npm run build`

- [ ] **Step 4: Commit**

```bash
git add CollegeLMS.Next/app/my/dashboard/page.tsx CollegeLMS.Next/app/
git commit -m "feat: remove deadlines and grades from student UI"
```

---

## Task 5: UI — убрать дедлайны и оценки (преподаватели)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx`

- [ ] **Step 1: Убрать dueDate и maxScore из teacher course view**

В `app/(authenticated)/courses/[id]/page.tsx` (вкладка Assignments для.teacher):
- Убрать `dueDate` из отображения
- Убрать `maxScore` из отображения
- Убрать `submissionCount`

- [ ] **Step 2: Проверить build**

Run: `cd CollegeLMS.Next && npm run build`

- [ ] **Step 3: Commit**

```bash
git add CollegeLMS.Next/app/
git commit -m "feat: remove deadlines and grades from teacher UI"
```

---

## Task 6: Seed data — 4 курса на основе МДК

**Files:**
- Modify: `CollegeLMS.API/Data/DataSeeder.cs`

**Источники данных (README репозиториев):**
1. `mdk0104_IP` — Системное программирование (.NET, C#, ASP.NET Core, EF)
2. `mdk0103_IP` — Мобильные приложения (Kotlin, Jetpack Compose)
3. `mdk0102` — Тестирование модулей (xUnit, Moq, Selenium)
4. `mdk0103_IB` — Мобильные приложения ИБ

- [ ] **Step 1: Прочитать README всех 4 репозиториев для контента**

Использовать `gh api repos/artemovsergey/{repo}/readme --jq .content | base64 -d` для каждого репо.

- [ ] **Step 2: Добавить 4 курса в DataSeeder**

```csharp
// В SeedCourses() — добавить 4 новых курса
var courses = new List<Course>
{
    // ... существующие курсы ...
    
    new Course
    {
        Id = Guid.NewGuid(),
        Title = "Системное программирование",
        Description = "МДК 01.04 — C#, .NET, ASP.NET Core, EF Core, JWT, RabbitMQ",
        TeacherId = existingTeacher.Id,
        Status = CourseStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
    new Course
    {
        Id = Guid.NewGuid(),
        Title = "Разработка мобильных приложений",
        Description = "МДК 01.03 — Kotlin, Jetpack Compose, ViewModel, Room, Retrofit",
        TeacherId = existingTeacher.Id,
        Status = CourseStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
    new Course
    {
        Id = Guid.NewGuid(),
        Title = "Тестирование программных модулей",
        Description = "МДК 01.02 — ручное тестирование, xUnit, Moq, Selenium, CI/CD",
        TeacherId = existingTeacher.Id,
        Status = CourseStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
    new Course
    {
        Id = Guid.NewGuid(),
        Title = "Мобильные приложения (ИБ)",
        Description = "МДК 01.03 ИБ — Kotlin, Jetpack Compose + бэкенд (ASP.NET Core)",
        TeacherId = existingTeacher.Id,
        Status = CourseStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
};
```

- [ ] **Step 3: Добавить лекции для каждого курса (по 5-8)**

На основе README — названия лекций из разделов. Каждая лекция с Order, Title, Content (краткое описание).

- [ ] **Step 4: Добавить задания для каждого курса (по 5-8)**

Названия лабораторных работ из репозиториев. `MaxScore = 100`, `Order`.

- [ ] **Step 5: Добавить тесты для каждого курса (по 2-3)**

```csharp
var tests = new List<Test>
{
    new Test
    {
        Id = Guid.NewGuid(),
        CourseId = course1.Id,
        Title = "Тест по основам C#",
        Description = "Проверка знаний основ синтаксиса C#",
        TimeLimitMinutes = 30,
        MaxAttempts = 3,
        PassingScore = 60,
        Type = TestType.SelfStudy,
        AutoCheck = true,
        ShuffleQuestions = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
    // ... ещё тесты
};
```

- [ ] **Step 6: Добавить вопросы к тестам (по 5-10 на тест)**

```csharp
var questions = new List<TestQuestion>
{
    new TestQuestion
    {
        Id = Guid.NewGuid(),
        TestId = test1.Id,
        Text = "Какой модификатор доступа делает член доступным только внутри класса?",
        Type = QuestionType.SingleChoice,
        Options = System.Text.Json.JsonSerializer.Serialize(new[] { "public", "private", "protected", "internal" }),
        CorrectAnswer = "private",
        Points = 10,
        OrderIndex = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    },
    // ... ещё вопросы
};
```

- [ ] **Step 7: Привязать тесты к лекциям (обновить Lecture.TestId)**

После создания тестов — обновить соответствующие лекции, установив `TestId`.

- [ ] **Step 8: Привязать курсы к существующим группам**

```csharp
var courseGroups = new List<CourseGroup>
{
    new CourseGroup { CourseId = course1.Id, GroupId = existingGroup.Id },
    // ...
};
```

- [ ] **Step 9: Проверить build**

Run: `dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore`

- [ ] **Step 10: Commit**

```bash
git add CollegeLMS.API/Data/DataSeeder.cs
git commit -m "feat: seed 4 МДК courses with lectures, tests, questions, assignments"
```

---

## Task 7: Финальная проверка

- [ ] **Step 1: dotnet build**

Run: `dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore`

- [ ] **Step 2: npm run build (frontend)**

Run: `cd CollegeLMS.Next && npm run build`

- [ ] **Step 3: Проверить миграцию**

Run: `dotnet ef migrations add CheckLectureTestId --project CollegeLMS.API -- --provider Npgsql`
(Если миграция пуста — значит предыдущая была корректной)

- [ ] **Step 4: Финальный commit (если есть правки)**

```bash
git add -A
git commit -m "feat: lecture-test link, UI cleanup, seed data — complete"
```
