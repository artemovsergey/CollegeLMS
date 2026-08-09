# Тест к каждой лекции — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Преподаватель создаёт тест к лекции прямо со страницы лекции, студент проходит его и видит результат (баллы, проценты, «Пройден/Не пройден»), статус отображается в списке занятий курса.

**Architecture:** Бэкенд тестов уже реализован (`TestingController`, `TestingService`, сущности Test/TestQuestion/TestAttempt/TestAnswer, связь `Lecture.TestId`). План вносит 5 точечных изменений в `TestingService`/DTO/валидаторы (привязка теста к лекции при создании, доступ студента по записи на курс, фикс семантики PassingScore как проценты, валидатор сабмита, новый DTO результатов) и строит фронтенд: блок теста на странице лекции (teacher+student), страницу прохождения, статус-значки в списках занятий.

**Tech Stack:** .NET 10 (EF Core, FluentValidation, xUnit+Moq+Bogus+InMemory), Next.js 14 (App Router, shadcn/ui, Tailwind v4, axios, sonner).

## Global Constraints

- Все сообщения об ошибках — на русском, Swagger summaries — на русском.
- `Result<T>` везде, без try-catch в сервисах/контроллерах; `AsNoTracking()` на чтении; `CancellationToken ct` на всех асинхронных методах.
- Primary constructor DI; мапперы — статические extension-методы в `CollegeLMS.API/Mappers/`; интерфейсы в `CollegeLMS.API/Interfaces/`.
- Формат ответа API: `{"isSuccess": ..., "data": ..., "errorMessage": ..., "statusCode": ...}` (Result<T>).
- Enum в JSON — строки; варианты вопросов и правильные ответы — через перевод строки `\n`.
- Git: `git add -A` (никогда не перечислять файлы), коммиты с префиксом `feature:`.
- Локального dotnet и node НЕТ — сборка/тесты через Docker: `docker compose build api`, `docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test ...`, фронт — `docker compose build collegelms-next`.
- Схема БД НЕ меняется (миграции не нужны).
- Ветка: `feature/lecture-tests` (создать из master в Task 0).
- Незакоммиченные изменения из прошлой задачи (`CollegeLMS.API/Data/DataSeeder.cs`, `import/mdk0901_course.json`, design doc) — сначала закоммитить в master как есть (Task 0).

---

### Task 0: Ветка и стартовый коммит

**Files:** (нет изменений кода)

- [ ] **Step 1: Закоммитить текущие изменения в master**

```bash
git add -A
git commit -m "docs: дизайн фичи «Тест к каждой лекции» + сид МДК 09.01 (из прошлой задачи)"
```

Expected: коммит создан, `git status` чистый.

- [ ] **Step 2: Создать ветку**

```bash
git fetch origin
git checkout -b feature/lecture-tests
```

Expected: ветка `feature/lecture-tests` создана.

- [ ] **Step 3: Убедиться, что проект собирается**

```bash
docker compose build api
```

Expected: образ `collegelms-api` собран без ошибок (build-стадия Dockerfile выполняет `dotnet build`).

---

### Task 1: Привязка теста к лекции при создании (`LectureId` в `CreateTestRequest`)

**Files:**
- Modify: `CollegeLMS.API/Dtos/TestDtos.cs:3-12` (класс `CreateTestRequest`)
- Modify: `CollegeLMS.API/Services/TestingService.cs:58-104` (`CreateAsync`)
- Test: `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` (добавить 3 теста)

**Interfaces:**
- Consumes: существующие `CreateTestRequest`, `Test`, `Lecture`, `Result<T>`.
- Produces: `CreateTestRequest.LectureId: Guid?` — при создании теста с `LectureId` у лекции устанавливается `Lecture.TestId` (доступно Task 2: `db.Lectures.FirstOrDefaultAsync(l => l.TestId == testId)`).

- [ ] **Step 1: Добавить поле в DTO**

В `CollegeLMS.API/Dtos/TestDtos.cs` в конец класса `CreateTestRequest` (после строки `public Guid CourseId { get; set; }`):

```csharp
    public Guid? LectureId { get; set; }
```

- [ ] **Step 2: Написать падающие тесты**

В конец файла `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task CreateAsync_LinksTestToLecture_WhenLectureIdProvided()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Лекция 1",
            Order = 1,
            LectureType = LectureType.Lecture,
        };
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateTestRequest
            {
                Title = "Тест к лекции",
                Description = "Описание",
                CourseId = course.Id,
                Type = "SelfStudy",
                LectureId = lecture.Id,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        var linked = await _db.Lectures.FindAsync([lecture.Id]);
        linked!.TestId.Should().Be(result.Data!.Id);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenLectureBelongsToAnotherCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var otherCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Другой курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.AddRange(course, otherCourse);
        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = otherCourse.Id,
            Title = "Лекция",
            Order = 1,
            LectureType = LectureType.Lecture,
        };
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateTestRequest
            {
                Title = "Тест",
                CourseId = course.Id,
                Type = "SelfStudy",
                LectureId = lecture.Id,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenLectureAlreadyHasTest()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        var existingTest = TestFixture.CreateFaker().Generate();
        existingTest.CourseId = course.Id;
        existingTest.Course = course;
        _db.Tests.Add(existingTest);
        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Лекция",
            Order = 1,
            LectureType = LectureType.Lecture,
            TestId = existingTest.Id,
        };
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateTestRequest
            {
                Title = "Тест",
                CourseId = course.Id,
                Type = "SelfStudy",
                LectureId = lecture.Id,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }
```

- [ ] **Step 3: Запустить тесты — убедиться, что падают**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~CreateAsync_LinksTestToLecture|FullyQualifiedName~CreateAsync_ReturnsFail_WhenLectureBelongsToAnotherCourse|FullyQualifiedName~CreateAsync_ReturnsFail_WhenLectureAlreadyHasTest"
```

Expected: FAIL (свойство `LectureId` отсутствует — ошибка компиляции).

- [ ] **Step 4: Реализовать в сервисе**

В `CollegeLMS.API/Services/TestingService.cs` в `CreateAsync` сразу после блока проверки курса (строки 68-82, после проверки прав преподавателя) вставить:

```csharp
        Lecture? lecture = null;
        if (request.LectureId.HasValue)
        {
            lecture = await db.Lectures.FirstOrDefaultAsync(
                l => l.Id == request.LectureId.Value,
                ct
            );
            if (lecture is null)
                return Result<TestResponse>.Fail("Лекция не найдена", 404);
            if (lecture.CourseId != request.CourseId)
                return Result<TestResponse>.Fail("Лекция не принадлежит этому курсу", 400);
            if (lecture.TestId.HasValue)
                return Result<TestResponse>.Fail("У лекции уже есть тест", 400);
        }
```

Затем перед `await db.SaveChangesAsync(ct);` (строка 96) добавить (связь хранится только в `Lecture.TestId` — поля `LectureId` у сущности `Test` нет):

```csharp
        if (lecture is not null)
            lecture.TestId = test.Id;
```

- [ ] **Step 5: Запустить тесты — должны пройти**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~TestingServiceTests"
```

Expected: PASS, все тесты TestingServiceTests зелёные.

- [ ] **Step 6: Собрать API и закоммитить**

```bash
docker compose build api
git add -A
git commit -m "feature: привязка теста к лекции при создании (CreateTestRequest.LectureId)"
```

---

### Task 2: Доступ студента к тесту по записи на курс

**Files:**
- Modify: `CollegeLMS.API/Services/TestingService.cs:435-446` (`StartTestAsync`)
- Test: `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` (добавить 2 теста)

**Interfaces:**
- Consumes: `Lecture.TestId` (Task 1), `CourseGroup` (уже есть), `Student.GroupId` (уже есть).
- Produces: доступ по CourseGroup: если у теста есть лекция и группа студента приписана к курсу лекции — доступ разрешён. Используется фронтом без изменений API.

- [ ] **Step 1: Написать падающие тесты**

В конец `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task StartTestAsync_AllowsAccess_WhenStudentEnrolledInCourse()
    {
        var groupId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = groupId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var courseId = Guid.NewGuid();
        var course = new Course
        {
            Id = courseId,
            Title = "Курс",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);

        var test = TestFixture.CreateFaker().Generate();
        test.CourseId = courseId;
        test.Course = course;
        test.MaxAttempts = 3;
        _db.Tests.Add(test);
        _db.TestQuestions.Add(
            new TestQuestion
            {
                Id = Guid.NewGuid(),
                Text = "Вопрос 1",
                Type = QuestionType.SingleChoice,
                Options = "A\nB\nC",
                CorrectAnswer = "A",
                Points = 10,
                OrderIndex = 1,
                TestId = test.Id,
            }
        );
        _db.Lectures.Add(
            new Lecture
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Лекция",
                Order = 1,
                LectureType = LectureType.Lecture,
                TestId = test.Id,
            }
        );
        _db.CourseGroups.Add(
            new CourseGroup { Id = Guid.NewGuid(), CourseId = courseId, GroupId = groupId }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.StartTestAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StartTestAsync_ReturnsFail_WhenNotEnrolledAndNoAssignment()
    {
        var groupId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = groupId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        test.MaxAttempts = 3;
        _db.Tests.Add(test);
        _db.TestQuestions.Add(
            new TestQuestion
            {
                Id = Guid.NewGuid(),
                Text = "Вопрос 1",
                Type = QuestionType.SingleChoice,
                Options = "A\nB\nC",
                CorrectAnswer = "A",
                Points = 10,
                OrderIndex = 1,
                TestId = test.Id,
            }
        );
        _db.Lectures.Add(
            new Lecture
            {
                Id = Guid.NewGuid(),
                CourseId = test.CourseId,
                Title = "Лекция",
                Order = 1,
                LectureType = LectureType.Lecture,
                TestId = test.Id,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.StartTestAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }
```

- [ ] **Step 2: Запустить тесты — убедиться, что падают**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~StartTestAsync_AllowsAccess_WhenStudentEnrolledInCourse|FullyQualifiedName~StartTestAsync_ReturnsFail_WhenNotEnrolledAndNoAssignment"
```

Expected: FAIL (первый тест возвращает 403 «Тест не доступен для вашей группы»).

- [ ] **Step 3: Реализовать**

Заменить блок проверки доступа в `StartTestAsync` (строки 435-446) на:

```csharp
        var hasAssignment = await db.TestAssignments.AsNoTracking().AnyAsync(
            a =>
                a.TestId == testId
                && a.GroupId == student.GroupId
                && a.OpenDate <= DateTime.UtcNow
                && a.CloseDate >= DateTime.UtcNow,
            ct
        );
        var lecture = await db.Lectures.AsNoTracking().FirstOrDefaultAsync(
            l => l.TestId == testId,
            ct
        );
        var enrolled = lecture is not null
            && await db.CourseGroups.AsNoTracking().AnyAsync(
                cg => cg.CourseId == lecture.CourseId && cg.GroupId == student.GroupId,
                ct
            );
        if (!hasAssignment && !enrolled)
            return Result<StartTestResponse>.Fail("Тест не доступен для вашей группы", 403);
```

- [ ] **Step 4: Запустить все тесты сервиса**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~TestingServiceTests"
```

Expected: PASS (включая старый тест `StartTestAsync` с TestAssignment — он проходит через ветку `hasAssignment`).

- [ ] **Step 5: Собрать и закоммитить**

```bash
docker compose build api
git add -A
git commit -m "feature: доступ студента к тесту по записи на курс"
```

---

### Task 3: Фикс семантики PassingScore (проценты, а не абсолютные баллы)

**Files:**
- Modify: `CollegeLMS.API/Services/TestingService.cs:617-640` (`GetMyResultAsync`), `:699-720` (`GetStatsAsync`), добавить приватный хелпер
- Test: `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` (добавить 3 теста)

**Interfaces:**
- Consumes: `TestResultResponse.Percentage` (уже есть в DTO).
- Produces: `Passed = percentage >= test.PassingScore` (проценты). Фронтенд-статусы (Task 11) полагаются на это.

- [ ] **Step 1: Написать падающие тесты**

В конец `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task GetMyResultAsync_PassedByPercentage_WhenMaxScoreNot100()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            PassingScore = 70,
            ShowCorrectAnswers = false,
            TimeLimitMinutes = 60,
            MaxAttempts = 1,
            Type = TestType.SelfStudy,
            CourseId = Guid.NewGuid(),
        };
        _db.Tests.Add(test);
        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Status = AttemptStatus.Completed,
                Score = 15,
                MaxScore = 20,
                Test = test,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyResultAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Percentage.Should().Be(75);
        result.Data.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyResultAsync_NotPassedByPercentage_WhenBelowThreshold()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            PassingScore = 70,
            ShowCorrectAnswers = false,
            TimeLimitMinutes = 60,
            MaxAttempts = 1,
            Type = TestType.SelfStudy,
            CourseId = Guid.NewGuid(),
        };
        _db.Tests.Add(test);
        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Status = AttemptStatus.Completed,
                Score = 12,
                MaxScore = 20,
                Test = test,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyResultAsync(test.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Percentage.Should().Be(60);
        result.Data.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsAsync_PassedCount_UsesPercentage()
    {
        var adminId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = adminId,
                FullName = "Admin",
                Email = "a@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Admin,
            }
        );

        var test = TestFixture.CreateFaker().Generate();
        test.PassingScore = 50;
        _db.Tests.Add(test);

        for (int i = 0; i < 2; i++)
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                RecordBookNumber = $"ЗК-00{i}",
            };
            student.User = new User
            {
                Id = student.UserId,
                FullName = $"Студент {i}",
                Email = $"s{i}@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            };
            _db.Students.Add(student);
            _db.TestAttempts.Add(
                new TestAttempt
                {
                    Id = Guid.NewGuid(),
                    TestId = test.Id,
                    StudentId = student.Id,
                    Status = AttemptStatus.Completed,
                    Score = i == 0 ? 80 : 150,
                    MaxScore = 200,
                    Student = student,
                }
            );
        }
        await _db.SaveChangesAsync();

        var result = await _sut.GetStatsAsync(test.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalAttempts.Should().Be(2);
        result.Data.PassedCount.Should().Be(1);
        result.Data.FailedCount.Should().Be(1);
    }
```

- [ ] **Step 2: Запустить тесты — убедиться, что падают**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~GetMyResultAsync_PassedByPercentage|FullyQualifiedName~GetMyResultAsync_NotPassedByPercentage|FullyQualifiedName~GetStatsAsync_PassedCount_UsesPercentage"
```

Expected: FAIL (`Passed` сейчас сравнивает Score с PassingScore напрямую).

- [ ] **Step 3: Реализовать хелпер и правки**

В `TestingService.cs` добавить приватный хелпер в конец класса (перед закрывающей `}` класса, после метода `CanEditTest`):

```csharp
    private static int Percent(int score, int maxScore) =>
        maxScore > 0 ? score * 100 / maxScore : 0;
```

В `GetMyResultAsync` заменить строки 617-624 на:

```csharp
        var percentage = Percent(attempt.Score, attempt.MaxScore);

        return Result<TestResultResponse>.Ok(
            new TestResultResponse
            {
                AttemptId = attempt.Id,
                Score = attempt.Score,
                MaxScore = attempt.MaxScore,
                Percentage = percentage,
                Passed = percentage >= attempt.Test.PassingScore,
```

В `GetStatsAsync` заменить строки 699-720 на:

```csharp
        var passCount = attempts.Count(a => Percent(a.Score, a.MaxScore) >= test.PassingScore);

        return Result<TestStatsResponse>.Ok(
            new TestStatsResponse
            {
                TotalAttempts = count,
                PassedCount = passCount,
                FailedCount = count - passCount,
                AverageScore = count > 0 ? attempts.Average(a => a.Score) : 0,
                MedianScore = median,
                MaxScore = count > 0 ? scores.Max() : 0,
                MinScore = count > 0 ? scores.Min() : 0,
                StudentResults = attempts
                    .Select(a => new StudentResultDto
                    {
                        StudentName = a.Student?.User?.FullName ?? string.Empty,
                        GroupName = a.Student?.Group?.Name ?? string.Empty,
                        Score = a.Score,
                        MaxScore = a.MaxScore,
                        Passed = Percent(a.Score, a.MaxScore) >= test.PassingScore,
                    })
                    .ToList(),
            }
        );
```

- [ ] **Step 4: Запустить все тесты сервиса**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~TestingServiceTests"
```

Expected: PASS. Существующие тесты `GetMyResultAsync_ReturnsResult` (100% при Score=10/MaxScore=10) и `GetStatsAsync_ReturnsStats` (80/100 при PassingScore=50) остаются зелёными.

- [ ] **Step 5: Собрать и закоммитить**

```bash
docker compose build api
git add -A
git commit -m "feature: проходной балл теста считается в процентах"
```

---

### Task 4: Валидатор SubmitAnswersRequest + отклонение чужих вопросов

**Files:**
- Modify: `CollegeLMS.API/Validators/TestRequestValidator.cs` (добавить класс)
- Modify: `CollegeLMS.API/Services/TestingService.cs:524-528` (`SubmitAnswersAsync`)
- Test: `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` (добавить 1 тест)

**Interfaces:**
- Consumes: `SubmitAnswersRequest` (уже есть).
- Produces: сабмит с QuestionId, не принадлежащими тесту → 400 «Тест содержит неизвестные вопросы».

- [ ] **Step 1: Написать падающий тест**

В конец `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task SubmitAnswersAsync_ReturnsFail_WhenUnknownQuestion()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var test = TestFixture.CreateFaker().Generate();
        test.AutoCheck = true;
        test.TimeLimitMinutes = 60;
        _db.Tests.Add(test);

        var question = new TestQuestion
        {
            Id = Guid.NewGuid(),
            Text = "Q1",
            Type = QuestionType.SingleChoice,
            CorrectAnswer = "A",
            Points = 10,
            OrderIndex = 1,
            TestId = test.Id,
        };
        _db.TestQuestions.Add(question);

        var attempt = new TestAttempt
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            StudentId = student.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            Status = AttemptStatus.InProgress,
            Test = test,
        };
        _db.TestAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAnswersAsync(
            test.Id,
            attempt.Id,
            new SubmitAnswersRequest
            {
                Answers = new List<AnswerDto>
                {
                    new() { QuestionId = Guid.NewGuid(), GivenAnswer = "A" },
                },
            },
            studentUserId,
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }
```

- [ ] **Step 2: Запустить тест — убедиться, что падает**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~SubmitAnswersAsync_ReturnsFail_WhenUnknownQuestion"
```

Expected: FAIL (сейчас чужой вопрос молча пропускается, результат success с Score=0).

- [ ] **Step 3: Добавить валидатор**

В конец `CollegeLMS.API/Validators/TestRequestValidator.cs` добавить:

```csharp
public class SubmitAnswersRequestValidator : AbstractValidator<SubmitAnswersRequest>
{
    public SubmitAnswersRequestValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull()
            .WithMessage("Ответы обязательны");
        RuleForEach(x => x.Answers)
            .ChildRules(answers =>
            {
                answers
                    .RuleFor(a => a.QuestionId)
                    .NotEmpty()
                    .WithMessage("Идентификатор вопроса обязателен");
            });
    }
}
```

- [ ] **Step 4: Добавить проверку в сервис**

В `SubmitAnswersAsync` после загрузки вопросов (строка 527, после `var questions = await db.TestQuestions...ToListAsync(ct);`) добавить:

```csharp
        var unknownQuestionIds = request
            .Answers.Select(a => a.QuestionId)
            .Distinct()
            .Where(id => !questions.Any(q => q.Id == id))
            .ToList();
        if (unknownQuestionIds.Count > 0)
            return Result<AttemptResponse>.Fail("Тест содержит неизвестные вопросы", 400);
```

- [ ] **Step 5: Запустить все тесты сервиса**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~TestingServiceTests"
```

Expected: PASS (таймаут-тест падает раньше проверки, дубликаты валидны — Distinct).

- [ ] **Step 6: Собрать и закоммитить**

```bash
docker compose build api
git add -A
git commit -m "feature: валидатор ответов на тест + отклонение чужих вопросов"
```

---

### Task 5: `GetAllMyResultsAsync` возвращает `List<MyTestResultDto>` (с флагом Passed)

**Files:**
- Modify: `CollegeLMS.API/Dtos/TestDtos.cs` (добавить класс `MyTestResultDto`)
- Modify: `CollegeLMS.API/Interfaces/ITestingService.cs` (сигнатура `GetAllMyResultsAsync`)
- Modify: `CollegeLMS.API/Services/TestingService.cs:643-662` (`GetAllMyResultsAsync`)
- Modify: `CollegeLMS.API/Controllers/TestingController.cs:328-338` (`GetAllMyResults` — тип в SwaggerResponse)
- Test: `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` (добавить 1 тест)

**Interfaces:**
- Consumes: `Percent` хелпер (Task 3).
- Produces: `GET /api/my/test-results` → `Result<List<MyTestResultDto>>` где `MyTestResultDto { TestId, TestTitle, Score, MaxScore, Percentage, Passed, CompletedAt }`. Используется Task 11 (статус-значки).

- [ ] **Step 1: Добавить DTO**

В конец `CollegeLMS.API/Dtos/TestDtos.cs` добавить:

```csharp
public class MyTestResultDto
{
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
}
```

- [ ] **Step 2: Обновить интерфейс**

В `CollegeLMS.API/Interfaces/ITestingService.cs` заменить сигнатуру:

```csharp
    Task<Result<List<AttemptResponse>>> GetAllMyResultsAsync(Guid currentUserId, CancellationToken ct = default);
```

на:

```csharp
    Task<Result<List<MyTestResultDto>>> GetAllMyResultsAsync(
        Guid currentUserId,
        CancellationToken ct = default
    );
```

- [ ] **Step 3: Написать падающий тест**

В конец `CollegeLMS.Tests/Unit/Services/TestingServiceTests.cs` добавить:

```csharp
    [Fact]
    public async Task GetAllMyResultsAsync_ReturnsResultsWithPassedFlag()
    {
        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
            }
        );
        _db.Students.Add(student);

        var test = new Test
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            PassingScore = 60,
            TimeLimitMinutes = 60,
            MaxAttempts = 1,
            Type = TestType.SelfStudy,
            CourseId = Guid.NewGuid(),
        };
        _db.Tests.Add(test);

        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CompletedAt = DateTime.UtcNow.AddHours(-2),
                Status = AttemptStatus.Completed,
                Score = 40,
                MaxScore = 100,
                Test = test,
            }
        );
        _db.TestAttempts.Add(
            new TestAttempt
            {
                Id = Guid.NewGuid(),
                TestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddHours(-1),
                Status = AttemptStatus.Completed,
                Score = 80,
                MaxScore = 100,
                Test = test,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllMyResultsAsync(studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Passed.Should().BeTrue();
        result.Data[0].TestId.Should().Be(test.Id);
        result.Data[1].Passed.Should().BeFalse();
    }
```

- [ ] **Step 4: Запустить тест — убедиться, что падает**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~GetAllMyResultsAsync_ReturnsResultsWithPassedFlag"
```

Expected: FAIL (метод возвращает `AttemptResponse`, нет поля `Passed`).

- [ ] **Step 5: Реализовать в сервисе**

Заменить тело `GetAllMyResultsAsync` (строки 643-662) на:

```csharp
    public async Task<Result<List<MyTestResultDto>>> GetAllMyResultsAsync(
        Guid currentUserId,
        CancellationToken ct
    )
    {
        var student = await db
            .Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);
        if (student is null)
            return Result<List<MyTestResultDto>>.Fail("Студент не найден", 404);

        var attempts = await db
            .TestAttempts.AsNoTracking()
            .Include(a => a.Test)
            .Where(a => a.StudentId == student.Id && a.Status == AttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync(ct);

        var results = attempts
            .Select(a =>
            {
                var percentage = Percent(a.Score, a.MaxScore);
                return new MyTestResultDto
                {
                    TestId = a.TestId,
                    TestTitle = a.Test?.Title ?? string.Empty,
                    Score = a.Score,
                    MaxScore = a.MaxScore,
                    Percentage = percentage,
                    Passed = a.Test is not null && percentage >= a.Test.PassingScore,
                    CompletedAt = a.CompletedAt ?? a.StartedAt,
                };
            })
            .ToList();

        return Result<List<MyTestResultDto>>.Ok(results);
    }
```

- [ ] **Step 6: Обновить контроллер**

В `CollegeLMS.API/Controllers/TestingController.cs` заменить строку 330:

```csharp
    [SwaggerResponse(200, "Результаты получены", typeof(Result<List<AttemptResponse>>))]
```

на:

```csharp
    [SwaggerResponse(200, "Результаты получены", typeof(Result<List<MyTestResultDto>>))]
```

- [ ] **Step 7: Запустить все тесты сервиса**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --filter "FullyQualifiedName~TestingServiceTests"
```

Expected: PASS.

- [ ] **Step 8: Собрать и закоммитить**

```bash
docker compose build api
git add -A
git commit -m "feature: GET /api/my/test-results возвращает результаты с флагом пройден/не пройден"
```

---

### Task 6: SwaggerExamples и Postman-коллекция

**Files:**
- Create: `CollegeLMS.API/SwaggerExamples/AttemptResponseExample.cs`
- Create: `CollegeLMS.API/SwaggerExamples/TestResultResponseExample.cs`
- Create: `CollegeLMS.API/SwaggerExamples/TestStatsResponseExample.cs`
- Modify: `docs/spec/CollegeLMS.postman_collection.json`

**Interfaces:**
- Consumes: типы `AttemptResponse`, `TestResultResponse`, `TestStatsResponse` (уже есть в `Dtos/TestDtos.cs`).
- Produces: примеры-классы в стиле `TestResponseExample` (anonymous object, поля camelCase, русские данные, GUID-константы).

- [ ] **Step 1: AttemptResponseExample.cs**

Создать `CollegeLMS.API/SwaggerExamples/AttemptResponseExample.cs`:

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class AttemptResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            testId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            startedAt = DateTime.UtcNow.AddMinutes(-15),
            completedAt = DateTime.UtcNow,
            status = "Completed",
            score = 34,
            maxScore = 40,
        };
}
```

- [ ] **Step 2: TestResultResponseExample.cs**

Создать `CollegeLMS.API/SwaggerExamples/TestResultResponseExample.cs`:

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class TestResultResponseExample
{
    public static object Create() =>
        new
        {
            attemptId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            score = 34,
            maxScore = 40,
            percentage = 85,
            passed = true,
            completedAt = DateTime.UtcNow,
            answerReviews = new[]
            {
                new
                {
                    questionId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    questionText = "Чему равен интеграл от x?",
                    givenAnswer = "x^2/2 + C",
                    correctAnswer = "x^2/2 + C",
                    isCorrect = true,
                    points = 5,
                },
            },
        };
}
```

- [ ] **Step 3: TestStatsResponseExample.cs**

Создать `CollegeLMS.API/SwaggerExamples/TestStatsResponseExample.cs`:

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class TestStatsResponseExample
{
    public static object Create() =>
        new
        {
            totalAttempts = 12,
            passedCount = 9,
            failedCount = 3,
            averageScore = 31.5,
            medianScore = 33.0,
            maxScore = 40,
            minScore = 12,
            studentResults = new[]
            {
                new
                {
                    studentName = "Иванов Иван Иванович",
                    groupName = "ИСП-31",
                    score = 34,
                    maxScore = 40,
                    passed = true,
                },
            },
        };
}
```

- [ ] **Step 4: Postman-коллекция**

В `docs/spec/CollegeLMS.postman_collection.json` добавить новую папку `"Tests"` в массив `"item"` (после папки `"Users"` — вставить перед закрывающей `]` массива `"item"`). Формат элементов — как в существующих (auth коллекции `{{jwt}}` применяется автоматически):

```json
    ,
    {
      "name": "Tests",
      "item": [
        {
          "name": "Create test with lecture",
          "request": {
            "method": "POST",
            "header": [{ "key": "Content-Type", "value": "application/json" }],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"title\": \"Тест к лекции 1\",\n  \"description\": \"Проверка усвоения материала лекции\",\n  \"timeLimitMinutes\": 30,\n  \"maxAttempts\": 2,\n  \"type\": \"SelfStudy\",\n  \"passingScore\": 60,\n  \"courseId\": \"{{courseId}}\",\n  \"lectureId\": \"{{lectureId}}\"\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/api/tests",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests"]
            }
          }
        },
        {
          "name": "Get tests by course",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{baseUrl}}/api/tests?courseId={{courseId}}",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests"],
              "query": [{ "key": "courseId", "value": "{{courseId}}" }]
            }
          }
        },
        {
          "name": "Start test",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{baseUrl}}/api/tests/{{testId}}/start",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests", "{{testId}}", "start"]
            }
          }
        },
        {
          "name": "Submit answers",
          "request": {
            "method": "POST",
            "header": [{ "key": "Content-Type", "value": "application/json" }],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"answers\": [\n    { \"questionId\": \"{{questionId}}\", \"givenAnswer\": \"A\" }\n  ]\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/api/tests/{{testId}}/attempt/{{attemptId}}/submit",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests", "{{testId}}", "attempt", "{{attemptId}}", "submit"]
            }
          }
        },
        {
          "name": "Get my result",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{baseUrl}}/api/tests/{{testId}}/results",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests", "{{testId}}", "results"]
            }
          }
        },
        {
          "name": "Get my test results",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{baseUrl}}/api/my/test-results",
              "host": ["{{baseUrl}}"],
              "path": ["api", "my", "test-results"]
            }
          }
        },
        {
          "name": "Get test stats",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{baseUrl}}/api/tests/{{testId}}/stats",
              "host": ["{{baseUrl}}"],
              "path": ["api", "tests", "{{testId}}", "stats"]
            }
          }
        }
      ]
    }
```

После вставки добавить переменные в массив `"variable"`:

```json
    ,
    { "key": "courseId", "value": "c1000000-0000-0000-0000-000000000001" },
    { "key": "lectureId", "value": "" },
    { "key": "testId", "value": "" },
    { "key": "questionId", "value": "" },
    { "key": "attemptId", "value": "" }
```

- [ ] **Step 5: Проверить JSON и закоммитить**

```bash
docker run --rm -v "$(pwd)":/app -w /app mcr.microsoft.com/dotnet/sdk:10.0 dotnet build CollegeLMS.API/CollegeLMS.API.csproj
python3 -c "import json; json.load(open('docs/spec/CollegeLMS.postman_collection.json')); print('JSON OK')" 2>/dev/null || node -e "JSON.parse(require('fs').readFileSync('docs/spec/CollegeLMS.postman_collection.json','utf8')); console.log('JSON OK')"
```

Expected: build без ошибок; вывод `JSON OK`.

- [ ] **Step 6: Закоммитить**

```bash
git add -A
git commit -m "docs: swagger examples для попыток/результатов/статистики + Postman-коллекция тестов"
```

---

### Task 7: Frontend — типы и правка admin/testing

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts:92-99` (LectureResponse), `:217-237` (TestResponse, CreateTestRequest), `:294-303` (TestAttemptResponse)
- Modify: `CollegeLMS.Next/app/admin/testing/page.tsx:1073` (`courseName` → `courseTitle`)

**Interfaces:**
- Consumes: бэкенд-контракты из Task 1 и Task 5.
- Produces: типы `StartTestResponse`, `TestQuestionDto`, `SubmitAnswersRequest`, `AnswerDto`, `TestResultResponse`, `AnswerReviewDto`, `TestStatsResponse`, `StudentResultDto`, `MyTestResultDto` — используются Tasks 8-11.

- [ ] **Step 1: Обновить LectureResponse**

В `CollegeLMS.Next/types/index.ts` заменить блок `LectureResponse` (строки 92-99) на:

```ts
export interface LectureResponse {
  id: string
  courseId: string
  title: string
  content: string
  order: number
  lectureType: "Lecture" | "Practice" | "SelfStudy"
  testId: string | null
  testTitle: string | null
}
```

- [ ] **Step 2: Обновить TestResponse и CreateTestRequest**

Заменить блоки (строки 217-237) на:

```ts
export interface TestResponse {
  id: string
  title: string
  description: string
  courseId: string
  courseTitle: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
  autoCheck: boolean
  showCorrectAnswers: boolean
  shuffleQuestions: boolean
  shuffleOptions: boolean
  questionCount: number
}

export interface CreateTestRequest {
  title: string
  description: string
  courseId: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
  lectureId?: string | null
}
```

- [ ] **Step 3: Обновить TestAttemptResponse**

Заменить блок (строки 294-303) на:

```ts
export interface TestAttemptResponse {
  id: string
  testId: string
  startedAt: string
  completedAt: string | null
  status: string
  score: number
  maxScore: number
}
```

- [ ] **Step 4: Добавить новые типы**

В конец файла `CollegeLMS.Next/types/index.ts` добавить:

```ts
export interface StartTestResponse {
  attemptId: string
  startedAt: string
  timeLimitMinutes: number
  questions: TestQuestionDto[]
}

export interface TestQuestionDto {
  id: string
  text: string
  type: string
  options: string
  orderIndex: number
}

export interface SubmitAnswersRequest {
  answers: AnswerDto[]
}

export interface AnswerDto {
  questionId: string
  givenAnswer: string
}

export interface TestResultResponse {
  attemptId: string
  score: number
  maxScore: number
  percentage: number
  passed: boolean
  completedAt: string
  answerReviews: AnswerReviewDto[]
}

export interface AnswerReviewDto {
  questionId: string
  questionText: string
  givenAnswer: string
  correctAnswer: string
  isCorrect: boolean
  points: number
}

export interface TestStatsResponse {
  totalAttempts: number
  passedCount: number
  failedCount: number
  averageScore: number
  medianScore: number
  maxScore: number
  minScore: number
  studentResults: StudentResultDto[]
}

export interface StudentResultDto {
  studentName: string
  groupName: string
  score: number
  maxScore: number
  passed: boolean
}

export interface MyTestResultDto {
  testId: string
  testTitle: string
  score: number
  maxScore: number
  percentage: number
  passed: boolean
  completedAt: string
}
```

- [ ] **Step 5: Правка admin/testing**

В `CollegeLMS.Next/app/admin/testing/page.tsx` строку 1073 заменить:

```tsx
                  <TableCell>{t.courseName}</TableCell>
```

на:

```tsx
                  <TableCell>{t.courseTitle}</TableCell>
```

- [ ] **Step 6: Собрать фронтенд**

```bash
docker compose build collegelms-next
```

Expected: сборка без ошибок (TypeScript-проверка проходит).

- [ ] **Step 7: Закоммитить**

```bash
git add -A
git commit -m "feature: типы фронтенда для тестов к лекциям"
```

---

### Task 8: Страница лекции — блок теста для преподавателя

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx`

**Interfaces:**
- Consumes: `TestResponse`, `CreateTestRequest`, `TestQuestionResponse`, `CreateTestQuestionRequest`, `UpdateTestQuestionRequest`, `TestStatsResponse`, `parseErrors` (все из Task 7).
- Produces: состояние `test`, `questions`, `stats` + модалки. После Task 8 преподаватель может: создать тест к лекции, добавить/редактировать/удалить вопросы, смотреть статистику.

- [ ] **Step 1: Обновить imports**

В `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx` заменить блок imports (строки 6-14) на:

```tsx
import type {
  Result,
  LectureResponse,
  CourseResponse,
  TestResponse,
  CreateTestRequest,
  TestQuestionResponse,
  CreateTestQuestionRequest,
  UpdateTestQuestionRequest,
  TestStatsResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { parseErrors } from "@/lib/errors"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"
import { LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS } from "@/lib/lectureTypes"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import FormField from "@/components/FormField"
import EmptyState from "@/components/EmptyState"
import { ClipboardList, BookOpenText, FileQuestion, Plus } from "lucide-react"
```

- [ ] **Step 2: Добавить состояние и хелперы**

Внутри компонента `LectureViewPage`, после строки `const [deleting, setDeleting] = useState(false)` добавить:

```tsx
  const [test, setTest] = useState<TestResponse | null>(null)
  const [questions, setQuestions] = useState<TestQuestionResponse[]>([])
  const [showCreateTest, setShowCreateTest] = useState(false)
  const [showQuestions, setShowQuestions] = useState(false)
  const [showCreateQuestion, setShowCreateQuestion] = useState(false)
  const [editingQuestionId, setEditingQuestionId] = useState<string | null>(null)
  const [deleteQuestionId, setDeleteQuestionId] = useState<string | null>(null)
  const [stats, setStats] = useState<TestStatsResponse | null>(null)
  const [showStats, setShowStats] = useState(false)
  const [statsLoading, setStatsLoading] = useState(false)
  const [testLoading, setTestLoading] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)

  const [formTestTitle, setFormTestTitle] = useState("")
  const [formTestDescription, setFormTestDescription] = useState("")
  const [formTestTimeLimit, setFormTestTimeLimit] = useState(30)
  const [formTestMaxAttempts, setFormTestMaxAttempts] = useState(1)
  const [formTestPassingScore, setFormTestPassingScore] = useState(60)
  const [formQText, setFormQText] = useState("")
  const [formQType, setFormQType] = useState("SingleChoice")
  const [formQOptions, setFormQOptions] = useState("")
  const [formQCorrect, setFormQCorrect] = useState("")
  const [formQPoints, setFormQPoints] = useState(1)
```

После блока `const canManage = ...` (строка 70) добавить:

```tsx
  const isStudent = user?.role === "Student"
```

- [ ] **Step 3: Добавить fetch-логику**

После `useEffect(() => { Promise.all([fetchLecture(), fetchCourse()]) }, [fetchLecture, fetchCourse])` добавить:

```tsx
  const fetchTest = useCallback(async () => {
    if (!lecture?.testId) return
    try {
      const res = await api.get<Result<TestResponse>>(`/api/tests/${lecture.testId}`)
      if (res.data.isSuccess && res.data.data) setTest(res.data.data)
    } catch {
      // ignore
    }
  }, [lecture?.testId])

  const fetchQuestions = useCallback(async () => {
    if (!test) return
    try {
      const res = await api.get<Result<TestQuestionResponse[]>>(`/api/tests/${test.id}/questions`)
      if (res.data.isSuccess && res.data.data) setQuestions(res.data.data)
    } catch {
      // ignore
    }
  }, [test])

  useEffect(() => {
    if (lecture?.testId) fetchTest()
  }, [lecture?.testId, fetchTest])

  useEffect(() => {
    if (test && canManage) fetchQuestions()
  }, [test, canManage, fetchQuestions])
```

- [ ] **Step 4: Добавить обработчики**

После `handleDelete` (строки 72-85) добавить:

```tsx
  const resetTestForm = () => {
    setFormTestTitle("")
    setFormTestDescription("")
    setFormTestTimeLimit(30)
    setFormTestMaxAttempts(1)
    setFormTestPassingScore(60)
    setFormError(null)
    setFormFieldErrors({})
  }

  const handleCreateTest = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!lecture) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: CreateTestRequest = {
        title: formTestTitle,
        description: formTestDescription,
        courseId: lecture.courseId,
        maxAttempts: formTestMaxAttempts,
        timeLimitMinutes: formTestTimeLimit,
        passingScore: formTestPassingScore,
        type: "SelfStudy",
        lectureId: lecture.id,
      }
      await api.post<Result<TestResponse>>("/api/tests", body)
      toast.success("Тест создан")
      setShowCreateTest(false)
      await fetchLecture()
      await fetchTest()
      setShowQuestions(true)
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const resetQuestionForm = () => {
    setFormQText("")
    setFormQType("SingleChoice")
    setFormQOptions("")
    setFormQCorrect("")
    setFormQPoints(1)
    setEditingQuestionId(null)
    setFormError(null)
    setFormFieldErrors({})
  }

  const handleCreateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!test) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: CreateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: questions.length + 1,
      }
      await api.post<Result<TestQuestionResponse>>(`/api/tests/${test.id}/questions`, body)
      toast.success("Вопрос добавлен")
      setShowCreateQuestion(false)
      resetQuestionForm()
      await fetchQuestions()
      await fetchTest()
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleUpdateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!test || !editingQuestionId) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: UpdateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: questions.find(q => q.id === editingQuestionId)?.orderIndex ?? questions.length + 1,
      }
      await api.put(`/api/tests/${test.id}/questions/${editingQuestionId}`, body)
      toast.success("Вопрос обновлён")
      setShowCreateQuestion(false)
      resetQuestionForm()
      await fetchQuestions()
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDeleteQuestion = async (id: string) => {
    if (!test) return
    try {
      await api.delete(`/api/tests/${test.id}/questions/${id}`)
      toast.success("Вопрос удалён")
      setDeleteQuestionId(null)
      await fetchQuestions()
      await fetchTest()
    } catch {
      toast.error("Ошибка удаления вопроса")
    }
  }

  const fillQuestionForm = (q: TestQuestionResponse) => {
    setEditingQuestionId(q.id)
    setFormQText(q.text)
    setFormQType(q.type)
    setFormQOptions(q.options)
    setFormQCorrect(q.correctAnswer)
    setFormQPoints(q.points)
    setFormError(null)
    setFormFieldErrors({})
  }

  const openStats = async () => {
    if (!test) return
    setShowStats(true)
    setStatsLoading(true)
    try {
      const res = await api.get<Result<TestStatsResponse>>(`/api/tests/${test.id}/stats`)
      if (res.data.isSuccess && res.data.data) setStats(res.data.data)
      else setStats(null)
    } catch {
      setStats(null)
    } finally {
      setStatsLoading(false)
    }
  }
```

- [ ] **Step 5: Добавить JSX — карточка теста преподавателя**

Перед `{canManage && (` (строка 115) добавить переменные-константы JSX. Сначала вставить блок учителя после `</div>` заголовка лекции (строка 125) и перед блоком контента лекции (строка 127):

```tsx
      {canManage && lecture.testId && (
        <div className="rounded-lg border bg-card p-6 flex flex-col gap-4">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <ClipboardList className="h-5 w-5 text-primary" />
              <div className="flex flex-col gap-0.5">
                <span className="font-medium">{test?.title ?? "Тест к лекции"}</span>
                <span className="text-xs text-muted-foreground">
                  Вопросов: {test?.questionCount ?? questions.length} · Проходной балл:{" "}
                  {test?.passingScore ?? "-"}%
                </span>
              </div>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowQuestions(true)}>
                <FileQuestion className="size-4 mr-1" />
                Вопросы
              </Button>
              <Button variant="outline" size="sm" onClick={openStats}>
                <BookOpenText className="size-4 mr-1" />
                Статистика
              </Button>
            </div>
          </div>
          {questions.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Вопросов ещё нет — добавьте хотя бы один, чтобы студенты могли пройти тест.
            </p>
          )}
        </div>
      )}
```

- [ ] **Step 6: Добавить JSX — блок создания теста**

После блока из Step 5 (перед `<div className="rounded-lg border bg-card p-6">` с контентом) добавить:

```tsx
      {canManage && !lecture.testId && (
        <Dialog open={showCreateTest} onOpenChange={o => { if (o) resetTestForm(); setShowCreateTest(o) }}>
          <DialogTrigger asChild>
            <Button className="self-start">
              <Plus className="size-4 mr-1" />
              Создать тест к лекции
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-lg">
            <DialogHeader>
              <DialogTitle>Создать тест к лекции</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleCreateTest} className="flex flex-col gap-4">
              {formError && <ErrorBanner message={formError} />}
              <FormField id="t-title" label="Название теста" required error={formFieldErrors.title}>
                <Input
                  id="t-title"
                  value={formTestTitle}
                  onChange={e => setFormTestTitle(e.target.value)}
                  placeholder="Тест по лекции 1"
                />
              </FormField>
              <FormField id="t-desc" label="Описание" error={formFieldErrors.description}>
                <Textarea
                  id="t-desc"
                  value={formTestDescription}
                  onChange={e => setFormTestDescription(e.target.value)}
                />
              </FormField>
              <div className="grid grid-cols-3 gap-4">
                <FormField id="t-max" label="Попыток" required error={formFieldErrors.maxAttempts}>
                  <Input
                    id="t-max"
                    type="number"
                    min={1}
                    value={formTestMaxAttempts}
                    onChange={e => setFormTestMaxAttempts(Number(e.target.value))}
                  />
                </FormField>
                <FormField id="t-time" label="Время (мин)" required error={formFieldErrors.timeLimitMinutes}>
                  <Input
                    id="t-time"
                    type="number"
                    min={1}
                    value={formTestTimeLimit}
                    onChange={e => setFormTestTimeLimit(Number(e.target.value))}
                  />
                </FormField>
                <FormField id="t-pass" label="Проходной %" required error={formFieldErrors.passingScore}>
                  <Input
                    id="t-pass"
                    type="number"
                    min={0}
                    max={100}
                    value={formTestPassingScore}
                    onChange={e => setFormTestPassingScore(Number(e.target.value))}
                  />
                </FormField>
              </div>
              <div className="flex gap-2 justify-end pt-2">
                <Button type="button" variant="ghost" onClick={() => setShowCreateTest(false)}>
                  Отмена
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Создание..." : "Создать"}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      )}
```

- [ ] **Step 7: Добавить JSX — конструктор вопросов**

В конец return (после `</AlertDialog>` и перед `</div>`, строка 146-147) добавить:

```tsx
      <Dialog open={showQuestions} onOpenChange={setShowQuestions}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Вопросы теста «{test?.title ?? ""}»</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Вопросов: {questions.length}</span>
              <Dialog open={showCreateQuestion} onOpenChange={o => { if (o) resetQuestionForm(); setShowCreateQuestion(o) }}>
                <DialogTrigger asChild>
                  <Button size="sm">
                    <Plus className="size-4 mr-1" />
                    Добавить вопрос
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-lg">
                  <DialogHeader>
                    <DialogTitle>{editingQuestionId ? "Редактировать вопрос" : "Новый вопрос"}</DialogTitle>
                  </DialogHeader>
                  <form
                    onSubmit={editingQuestionId ? handleUpdateQuestion : handleCreateQuestion}
                    className="flex flex-col gap-4"
                  >
                    {formError && <ErrorBanner message={formError} />}
                    <FormField id="q-text" label="Текст вопроса" required error={formFieldErrors.text}>
                      <Textarea id="q-text" value={formQText} onChange={e => setFormQText(e.target.value)} />
                    </FormField>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="q-type">Тип</Label>
                      <Select value={formQType} onValueChange={setFormQType}>
                        <SelectTrigger id="q-type">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="SingleChoice">Один вариант</SelectItem>
                          <SelectItem value="MultipleChoice">Несколько вариантов</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <FormField
                      id="q-options"
                      label="Варианты ответов"
                      hint="По одному варианту на строку"
                      required
                      error={formFieldErrors.options}
                    >
                      <Textarea
                        id="q-options"
                        value={formQOptions}
                        onChange={e => setFormQOptions(e.target.value)}
                        placeholder={"Вариант А\nВариант Б\nВариант В"}
                      />
                    </FormField>
                    <FormField
                      id="q-correct"
                      label="Правильный ответ"
                      hint={
                        formQType === "MultipleChoice"
                          ? "Несколько вариантов — по одному на строку, в порядке списка"
                          : "Текст варианта, который считается верным"
                      }
                      required
                      error={formFieldErrors.correctAnswer}
                    >
                      <Textarea
                        id="q-correct"
                        value={formQCorrect}
                        onChange={e => setFormQCorrect(e.target.value)}
                      />
                    </FormField>
                    <FormField id="q-points" label="Баллы" required error={formFieldErrors.points}>
                      <Input
                        id="q-points"
                        type="number"
                        min={1}
                        value={formQPoints}
                        onChange={e => setFormQPoints(Number(e.target.value))}
                      />
                    </FormField>
                    <div className="flex gap-2 justify-end pt-2">
                      <Button type="button" variant="ghost" onClick={() => setShowCreateQuestion(false)}>
                        Отмена
                      </Button>
                      <Button type="submit" disabled={submitting}>
                        {submitting ? "Сохранение..." : "Сохранить"}
                      </Button>
                    </div>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
            {questions.length === 0 ? (
              <EmptyState message="Вопросов пока нет" />
            ) : (
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">№</TableHead>
                      <TableHead>Текст</TableHead>
                      <TableHead>Тип</TableHead>
                      <TableHead className="w-20">Баллы</TableHead>
                      <TableHead className="w-32">Действия</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {questions.map(q => (
                      <TableRow key={q.id}>
                        <TableCell className="text-muted-foreground">{q.orderIndex}</TableCell>
                        <TableCell className="max-w-md truncate font-medium">{q.text}</TableCell>
                        <TableCell>
                          <Badge variant="outline">
                            {q.type === "MultipleChoice" ? "Несколько вариантов" : "Один вариант"}
                          </Badge>
                        </TableCell>
                        <TableCell>{q.points}</TableCell>
                        <TableCell>
                          <div className="flex gap-1">
                            <Button variant="ghost" size="sm" onClick={() => fillQuestionForm(q)}>
                              Ред.
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-muted-foreground hover:text-fg"
                              onClick={() => setDeleteQuestionId(q.id)}
                            >
                              Удал.
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleteQuestionId !== null} onOpenChange={o => !o && setDeleteQuestionId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить вопрос?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteQuestionId && handleDeleteQuestion(deleteQuestionId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <Dialog open={showStats} onOpenChange={setShowStats}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Статистика теста «{test?.title ?? ""}»</DialogTitle>
          </DialogHeader>
          {statsLoading ? (
            <LoadingSpinner size="lg" className="py-10" />
          ) : stats ? (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Попыток</p>
                  <p className="text-lg font-semibold">{stats.totalAttempts}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Пройдено</p>
                  <p className="text-lg font-semibold text-emerald-600">{stats.passedCount}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Не пройдено</p>
                  <p className="text-lg font-semibold text-orange-600">{stats.failedCount}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Средний балл</p>
                  <p className="text-lg font-semibold">{stats.averageScore.toFixed(1)}</p>
                </div>
              </div>
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Студент</TableHead>
                      <TableHead>Группа</TableHead>
                      <TableHead>Баллы</TableHead>
                      <TableHead>Статус</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {stats.studentResults.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={4} className="text-center text-muted-foreground">
                          Пока никто не проходил тест
                        </TableCell>
                      </TableRow>
                    ) : (
                      stats.studentResults.map((r, i) => (
                        <TableRow key={i}>
                          <TableCell className="font-medium">{r.studentName}</TableCell>
                          <TableCell>{r.groupName}</TableCell>
                          <TableCell>
                            {r.score} / {r.maxScore}
                          </TableCell>
                          <TableCell>
                            <Badge variant={r.passed ? "default" : "destructive"}>
                              {r.passed ? "Пройден" : "Не пройден"}
                            </Badge>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground">Статистика недоступна</p>
          )}
        </DialogContent>
      </Dialog>
```

- [ ] **Step 8: Проверить сборку**

```bash
docker compose build collegelms-next
```

Expected: сборка без ошибок (TypeScript-ошибки приведут к FAIL).

- [ ] **Step 9: Закоммитить**

```bash
git add -A
git commit -m "feature: блок теста для преподавателя на странице лекции"
```

---

### Task 9: Страница прохождения теста студентом

**Files:**
- Create: `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx`

**Interfaces:**
- Consumes: `StartTestResponse`, `TestQuestionDto`, `SubmitAnswersRequest`, `AnswerDto`, `TestResultResponse` (Task 7); API `GET /api/tests/{testId}/start`, `POST /api/tests/{testId}/attempt/{attemptId}/submit`, `GET /api/tests/{testId}/results`.
- Produces: страница `/courses/{courseId}/lectures/{lectureId}/test` — старт теста, вопросы с выбором, таймер, submit, разбор результата. Ссылается из Task 10.

- [ ] **Step 1: Создать страницу**

Создать `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx`:

```tsx
"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type {
  Result,
  LectureResponse,
  StartTestResponse,
  TestQuestionDto,
  TestResultResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"

const questionTypeLabels: Record<string, string> = {
  SingleChoice: "Один вариант",
  MultipleChoice: "Несколько вариантов",
}

export default function LectureTestPage() {
  const { user } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const lectureId = params.lectureId as string

  const [lecture, setLecture] = useState<LectureResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [test, setTest] = useState<StartTestResponse | null>(null)
  const [starting, setStarting] = useState(false)
  const [answers, setAnswers] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<TestResultResponse | null>(null)
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null)

  useEffect(() => {
    if (user?.role !== "Student") {
      router.replace(`/courses/${courseId}/lectures/${lectureId}`)
    }
  }, [user, courseId, lectureId, router])

  const fetchLecture = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse>>(`/api/courses/${courseId}/lectures/${lectureId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLecture(body.data)
        if (!body.data.testId) {
          setError("У этой лекции нет теста")
          setLoading(false)
        }
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
        setLoading(false)
      }
    } catch {
      setError("Ошибка загрузки лекции")
      setLoading(false)
    }
  }, [courseId, lectureId])

  useEffect(() => {
    fetchLecture()
  }, [fetchLecture])

  const handleStart = async () => {
    if (!lecture?.testId) return
    setStarting(true)
    setError(null)
    try {
      const res = await api.get<Result<StartTestResponse>>(`/api/tests/${lecture.testId}/start`)
      if (res.data.isSuccess && res.data.data) {
        setTest(res.data.data)
        setSecondsLeft(res.data.data.timeLimitMinutes * 60)
      } else {
        setError(res.data.errorMessage ?? "Не удалось начать тест")
      }
    } catch {
      setError("Не удалось начать тест")
    } finally {
      setStarting(false)
    }
  }

  const toggleOption = (questionId: string, option: string, single: boolean) => {
    setAnswers(prev => {
      const current = prev[questionId] ?? []
      if (single) return { ...prev, [questionId]: [option] }
      return {
        ...prev,
        [questionId]: current.includes(option)
          ? current.filter(o => o !== option)
          : [...current, option],
      }
    })
  }

  const doSubmit = useCallback(async () => {
    if (!test || !lecture?.testId) return
    setSubmitting(true)
    try {
      const body = {
        answers: Object.entries(answers).map(([questionId, options]) => ({
          questionId,
          givenAnswer: options.join("\n"),
        })),
      }
      const subRes = await api.post(`/api/tests/${lecture.testId}/attempt/${test.attemptId}/submit`, body)
      if (!subRes.data.isSuccess) {
        toast.error(subRes.data.errorMessage ?? "Ошибка отправки")
        setSubmitting(false)
        return
      }
      const res = await api.get<Result<TestResultResponse>>(`/api/tests/${lecture.testId}/results`)
      if (res.data.isSuccess && res.data.data) {
        setResult(res.data.data)
        setTest(null)
        setSecondsLeft(null)
      } else {
        toast.error("Не удалось получить результат")
        setSubmitting(false)
      }
    } catch {
      toast.error("Ошибка отправки ответов")
      setSubmitting(false)
    }
  }, [test, lecture?.testId, answers])

  useEffect(() => {
    if (secondsLeft === null) return
    if (secondsLeft <= 0) {
      doSubmit()
      return
    }
    const timer = setTimeout(() => setSecondsLeft(s => (s ?? 0) - 1), 1000)
    return () => clearTimeout(timer)
  }, [secondsLeft, doSubmit])

  if (loading) return <LoadingSpinner size="lg" className="py-20" />

  if (error && !test && !result) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error} />
        <Button variant="ghost" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
      </div>
    )
  }

  if (result) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
        <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        <div className="rounded-lg border bg-card p-6 flex flex-col items-center gap-3">
          <Badge variant={result.passed ? "default" : "destructive"} className="text-base px-4 py-1">
            {result.passed ? "Пройден" : "Не пройден"}
          </Badge>
          <p className="text-3xl font-bold">{result.percentage}%</p>
          <p className="text-sm text-muted-foreground">
            {result.score} из {result.maxScore} баллов
          </p>
          <p className="text-xs text-muted-foreground">
            Завершён: {new Date(result.completedAt).toLocaleString("ru-RU")}
          </p>
        </div>
        <div className="flex flex-col gap-3">
          {result.answerReviews.map(r => (
            <div key={r.questionId} className="rounded-lg border bg-card p-4 flex flex-col gap-2">
              <div className="flex items-start justify-between gap-3">
                <p className="font-medium">{r.questionText}</p>
                <Badge variant={r.isCorrect ? "default" : "destructive"}>
                  {r.isCorrect ? `+${r.points}` : "0"}
                </Badge>
              </div>
              <p className="text-sm">
                Ваш ответ: <span className={r.isCorrect ? "text-emerald-600" : "text-destructive"}>{r.givenAnswer || "—"}</span>
              </p>
              {!r.isCorrect && r.correctAnswer && (
                <p className="text-sm text-muted-foreground">Правильный ответ: {r.correctAnswer}</p>
              )}
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (!test) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
        <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        <div className="rounded-lg border bg-card p-8 flex flex-col items-center gap-4">
          <h2 className="text-xl font-semibold">Тест по лекции «{lecture?.title ?? ""}»</h2>
          <p className="text-sm text-muted-foreground">
            Отвечайте на вопросы по материалу лекции. После отправки вы увидите результат.
          </p>
          {error && <ErrorBanner message={error} />}
          <Button onClick={handleStart} disabled={starting}>
            {starting ? "Начинаем..." : "Начать тест"}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        {secondsLeft !== null && (
          <Badge variant="outline" className="tabular-nums">
            {Math.floor(secondsLeft / 60)}:{String(secondsLeft % 60).padStart(2, "0")}
          </Badge>
        )}
      </div>

      <div className="flex flex-col gap-4">
        {test.questions.map(q => (
          <div key={q.id} className="rounded-lg border bg-card p-4 flex flex-col gap-3">
            <div className="flex items-start justify-between gap-3">
              <p className="font-medium">{q.text}</p>
              <Badge variant="outline">{questionTypeLabels[q.type] ?? q.type}</Badge>
            </div>
            <div className="flex flex-col gap-2">
              {q.options.split("\n").filter(o => o.trim()).map(option => {
                const single = q.type === "SingleChoice"
                const checked = (answers[q.id] ?? []).includes(option)
                return (
                  <label
                    key={option}
                    className={`flex items-start gap-3 rounded-md border p-3 cursor-pointer transition-colors ${
                      checked ? "border-primary bg-primary/5" : "hover:bg-muted/50"
                    }`}
                  >
                    <input
                      type={single ? "radio" : "checkbox"}
                      name={single ? q.id : undefined}
                      checked={checked}
                      onChange={() => toggleOption(q.id, option, single)}
                      className="mt-1 accent-primary"
                    />
                    <span className="text-sm">{option}</span>
                  </label>
                )
              })}
            </div>
          </div>
        ))}
      </div>

      <Button onClick={doSubmit} disabled={submitting} className="self-end">
        {submitting ? "Отправка..." : "Завершить тест"}
      </Button>
    </div>
  )
}
```

- [ ] **Step 2: Собрать фронтенд**

```bash
docker compose build collegelms-next
```

Expected: сборка без ошибок.

- [ ] **Step 3: Закоммитить**

```bash
git add -A
git commit -m "feature: страница прохождения теста студентом"
```

---

### Task 10: Страница лекции — блок теста для студента

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx`

**Interfaces:**
- Consumes: `TestResultResponse`, `TestResponse`, `TestAttemptResponse` (Task 7); страница `/courses/{id}/lectures/{lectureId}/test` (Task 9).
- Produces: студент видит «Пройти тест» (нет результата), либо результат + «Пересдать»/«Попытки исчерпаны».

- [ ] **Step 1: Добавить state и fetch**

В `CollegeLMS.Next/app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx` после `const [formQPoints, setFormQPoints] = useState(1)` добавить:

```tsx
  const [studentResult, setStudentResult] = useState<TestResultResponse | null>(null)
  const [attemptCount, setAttemptCount] = useState(0)
```

В шаге 3 Task 8 `fetchTest` уже загружает тест. После `useEffect(() => { if (test && canManage) fetchQuestions() }, ...)` добавить (перед `handleDelete`):

```tsx
  const fetchStudentData = useCallback(async () => {
    if (!lecture?.testId || !isStudent) return
    try {
      const [resultRes, attemptsRes] = await Promise.all([
        api.get<Result<TestResultResponse>>(`/api/tests/${lecture.testId}/results`),
        api.get<Result<TestAttemptResponse[]>>(`/api/tests/${lecture.testId}/attempts`),
      ])
      if (resultRes.data.isSuccess && resultRes.data.data) setStudentResult(resultRes.data.data)
      if (attemptsRes.data.isSuccess && attemptsRes.data.data) {
        setAttemptCount(attemptsRes.data.data.length)
      }
    } catch {
      setStudentResult(null)
    }
  }, [lecture?.testId, isStudent])

  useEffect(() => {
    if (lecture?.testId && isStudent) fetchStudentData()
  }, [lecture?.testId, isStudent, fetchStudentData])
```

Тип `TestAttemptResponse` добавить в import type из `@/types` в шаге 1:

```tsx
  TestAttemptResponse,
```

- [ ] **Step 2: Добавить JSX блока студента**

После блока учителя (после `{canManage && !lecture.testId && (...)}` из Task 8 Step 6) и перед `<div className="rounded-lg border bg-card p-6">` с контентом добавить:

```tsx
      {isStudent && lecture.testId && (
        <div className="rounded-lg border bg-card p-6 flex items-center justify-between gap-4">
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-3">
              <ClipboardList className="h-5 w-5 text-primary" />
              <span className="font-medium">{test?.title ?? lecture.testTitle ?? "Тест к лекции"}</span>
            </div>
            {studentResult ? (
              <p className="text-sm text-muted-foreground">
                {studentResult.passed ? (
                  <span className="text-emerald-600 font-medium">
                    Пройден: {studentResult.percentage}% ({studentResult.score}/{studentResult.maxScore})
                  </span>
                ) : (
                  <span className="text-orange-600 font-medium">
                    Не пройден: {studentResult.percentage}% ({studentResult.score}/{studentResult.maxScore})
                  </span>
                )}
                {" "}· {new Date(studentResult.completedAt).toLocaleDateString("ru-RU")}
              </p>
            ) : (
              <p className="text-sm text-muted-foreground">
                {test?.questionCount && test.questionCount > 0
                  ? `Вопросов: ${test.questionCount} · Проходной балл: ${test.passingScore}%`
                  : "Тест ещё не содержит вопросов"}
              </p>
            )}
          </div>
          {studentResult ? (
            attemptCount < (test?.maxAttempts ?? 1) ? (
              <Button onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/test`)}>
                Пересдать
              </Button>
            ) : (
              <Badge variant="outline">Попытки исчерпаны</Badge>
            )
          ) : (
            <Button
              disabled={!(test?.questionCount && test.questionCount > 0)}
              onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/test`)}
            >
              Пройти тест
            </Button>
          )}
        </div>
      )}
```

- [ ] **Step 3: Собрать фронтенд**

```bash
docker compose build collegelms-next
```

Expected: сборка без ошибок.

- [ ] **Step 4: Закоммитить**

```bash
git add -A
git commit -m "feature: блок теста для студента на странице лекции"
```

---

### Task 11: Статус-значки теста в списке занятий курса

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx` (список занятий)
- Modify: `CollegeLMS.Next/app/(authenticated)/my/courses/[id]/page.tsx` (список занятий студента)

**Interfaces:**
- Consumes: `MyTestResultDto` (Task 7); `GET /api/my/test-results` (Task 5).
- Produces: у лекций с `testId` значок: «Тест: пройден» (зелёный), «Тест: не пройден» (оранжевый), «Тест» (серый).

- [ ] **Step 1: Общая логика — helper**

Создать `CollegeLMS.Next/lib/testStatus.ts`:

```ts
import type { MyTestResultDto } from "@/types"

export type TestStatus = "passed" | "failed" | "none"

export function testStatusFor(
  testId: string | null,
  results: Map<string, MyTestResultDto>,
): TestStatus {
  if (!testId) return "none"
  const r = results.get(testId)
  if (!r) return "none"
  return r.passed ? "passed" : "failed"
}

export const TEST_STATUS_LABELS: Record<TestStatus, string> = {
  passed: "Тест: пройден",
  failed: "Тест: не пройден",
  none: "Тест",
}

export const TEST_STATUS_VARIANTS: Record<TestStatus, "default" | "secondary" | "outline" | "destructive"> = {
  passed: "default",
  failed: "secondary",
  none: "outline",
}
```

- [ ] **Step 2: courses/[id]/page.tsx — преподаватель/студент**

В `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx`:

1. В import type добавить `MyTestResultDto`; добавить import:

```tsx
import { TEST_STATUS_LABELS, TEST_STATUS_VARIANTS, testStatusFor } from "@/lib/testStatus"
```

2. Добавить state после `const [addGroupError, setAddGroupError] = useState<string | null>(null)`:

```tsx
  const [myTestResults, setMyTestResults] = useState<Map<string, MyTestResultDto>>(new Map())
```

3. Добавить fetch в `useEffect` после `fetchLectures` (в существующем `useEffect` для загрузки, найти блок после fetchAssignments — добавить отдельный `useEffect`):

```tsx
  useEffect(() => {
    if (user?.role !== "Student") return
    api.get<Result<MyTestResultDto[]>>("/api/my/test-results").then(res => {
      if (res.data.isSuccess && res.data.data) {
        setMyTestResults(new Map(res.data.data.map(r => [r.testId, r])))
      }
    }).catch(() => {
      // ignore
    })
  }, [user?.role])
```

4. В рендере лекций (найти блок `{lectures.map(l => (` — в табе «Занятия») внутри строки с `{l.title}` добавить рядом бейдж лекции; найти место, где рендерится `Badge variant={LECTURE_TYPE_VARIANTS[l.lectureType] ?? "outline"}` и добавить после него:

```tsx
                  {l.testId && (
                    <Badge
                      variant={
                        user?.role === "Student"
                          ? TEST_STATUS_VARIANTS[testStatusFor(l.testId, myTestResults)]
                          : "outline"
                      }
                    >
                      {user?.role === "Student"
                        ? TEST_STATUS_LABELS[testStatusFor(l.testId, myTestResults)]
                        : "Тест"}
                    </Badge>
                  )}
```

Точное место: в `(authenticated)/courses/[id]/page.tsx` после строки 286 (после `</Badge>` типа лекции, внутри блока `lectures.map(l => ...)`, перед закрывающим `</div>` строки лекции) вставить:

- [ ] **Step 3: my/courses/[id]/page.tsx — студент**

В `CollegeLMS.Next/app/(authenticated)/my/courses/[id]/page.tsx`:

1. В import type добавить `MyTestResultDto`; добавить import:

```tsx
import { TEST_STATUS_LABELS, TEST_STATUS_VARIANTS, testStatusFor } from "@/lib/testStatus"
```

2. Добавить state после `const [submissions, setSubmissions] = ...`:

```tsx
  const [myTestResults, setMyTestResults] = useState<Map<string, MyTestResultDto>>(new Map())
```

3. В `fetchData` (внутри `try`, после материалов) добавить:

```tsx
      const testRes = await api.get<Result<MyTestResultDto[]>>("/api/my/test-results")
      if (testRes.data.isSuccess && testRes.data.data) {
        setMyTestResults(new Map(testRes.data.data.map(r => [r.testId, r])))
      }
```

4. В рендере лекций (строки 168-182) — рядом с бейджем типа лекции добавить:

```tsx
                  {l.testId && (
                    <Badge variant={TEST_STATUS_VARIANTS[testStatusFor(l.testId, myTestResults)]}>
                      {TEST_STATUS_LABELS[testStatusFor(l.testId, myTestResults)]}
                    </Badge>
                  )}
```

- [ ] **Step 4: Собрать фронтенд**

```bash
docker compose build collegelms-next
```

Expected: сборка без ошибок.

- [ ] **Step 5: Закоммитить**

```bash
git add -A
git commit -m "feature: статус теста в списке занятий курса"
```

---

### Task 12: Полная проверка

**Files:** (проверка, без кода)

- [ ] **Step 1: Все тесты .NET**

```bash
docker run --rm -v "$(pwd)":/app -w /app -v nuget_test_cache:/root/.nuget/packages mcr.microsoft.com/dotnet/sdk:10.0 dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj
```

Expected: PASS (все юнит + интеграционные).

- [ ] **Step 2: Полный compose**

```bash
docker compose up -d --build
```

Expected: api, collegelms-next, loadbalancer, db, redis стартовали (`docker compose ps` — все healthy/running).

- [ ] **Step 3: API-проверка end-to-end**

```bash
BASE=http://localhost:5026/api
TOKEN=$(curl -s $BASE/auth/login -H 'Content-Type: application/json' -d '{"login":"teacher","password":"teacher"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['token'])")
COURSE_ID=$(curl -s $BASE/courses -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json;print(json.load(sys.stdin)['data'][0]['id'])")
LECTURE_ID=$(curl -s $BASE/courses/$COURSE_ID/lectures -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json;print(json.load(sys.stdin)['data'][0]['id'])")
TEST_ID=$(curl -s $BASE/tests -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -d "{\"title\":\"E2E Тест\",\"courseId\":\"$COURSE_ID\",\"lectureId\":\"$LECTURE_ID\",\"maxAttempts\":2,\"timeLimitMinutes\":30,\"passingScore\":60,\"type\":\"SelfStudy\"}" | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['id'])")
Q_ID=$(curl -s $BASE/tests/$TEST_ID/questions -H 'Content-Type: application/json' -H "Authorization: Bearer $TOKEN" -d '{"text":"2+2?","type":"SingleChoice","options":"3\n4\n5","correctAnswer":"4","points":1,"orderIndex":1}' | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['id'])")
STOKEN=$(curl -s $BASE/auth/login -H 'Content-Type: application/json' -d '{"login":"student","password":"student"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['token'])")
START=$(curl -s $BASE/tests/$TEST_ID/start -H "Authorization: Bearer $STOKEN")
ATTEMPT_ID=$(echo "$START" | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['attemptId'])")
curl -s $BASE/tests/$TEST_ID/attempt/$ATTEMPT_ID/submit -H 'Content-Type: application/json' -H "Authorization: Bearer $STOKEN" -d "{\"answers\":[{\"questionId\":\"$Q_ID\",\"givenAnswer\":\"4\"}]}"
echo
curl -s $BASE/tests/$TEST_ID/results -H "Authorization: Bearer $STOKEN"
echo
```

Expected: тест создан и привязан к лекции; старт без 403 (доступ по записи на курс); submit → score 1/1, percentage 100, passed true; результаты содержат passed=true.

- [ ] **Step 4: Проверка фронта в браузере**

Открыть `http://localhost:3000/login` → войти как student (`student`/`student`), открыть курс → в списке занятий у первой лекции значок «Тест: пройден» (зелёный). Войти как teacher (`teacher`/`teacher`) → открыть лекцию → блок «Тест» с кнопками «Вопросы»/«Статистика».

- [ ] **Step 5: Закоммитить остатки (если есть) и подготовить ветку**

```bash
git status
```

Если есть незакоммиченные изменения — `git add -A && git commit -m "chore: правки по результатам проверки"`.

- [ ] **Step 6: Полная верификация перед merge (по желанию — E2E)**

E2E-тесты Playwright требуют node (на хосте нет) — проверка вручную по Step 3-4 достаточна. При наличии окружения: `npm run test:e2e` в `CollegeLMS.Next/`.

---

### Task 13: Merge и закрытие

**Files:** (git-операции)

- [ ] **Step 1: Проверить статус ветки**

```bash
git log master..HEAD --oneline
git status
```

Expected: 12+ коммитов feature-ветки, рабочее дерево чистое.

- [ ] **Step 2: Слияние в master**

```bash
git checkout master
git merge feature/lecture-tests
```

Expected: merge без конфликтов.

- [ ] **Step 3: Пуш (CD деплой)**

```bash
export GH_TOKEN=$(git config --get gh-token 2>/dev/null || echo "${GITHUB_TOKEN}")
git push
```

Expected: push в origin, GitHub Actions CD запущен, API и фронт обновлены на VPS.
