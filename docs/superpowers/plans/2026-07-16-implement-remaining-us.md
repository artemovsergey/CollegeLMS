# Реализация оставшихся User Stories — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Реализовать 19 невыполненных User Stories в 4 сервисах

**Architecture:** Модульный монолит, Clean Architecture, REST API, Result<T>, FluentValidation, ручные мапперы

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core + Npgsql, xUnit + Moq + Bogus, Next.js 14, Tailwind CSS 4, shadcn/ui

**Порядок выполнения:**
1. TestingService (UC-39..45) — P0, полностью новый сервис
2. AuthService (UC-14, UC-15) — P1, расширение существующего
3. LearningService (UC-37, UC-38, UC-46..52) — P1, смешанное
4. ScheduleService (UC-23) — P2, расширение

---

## Модуль 1: TestingService (UC-39..45)

### Entity Layer
- `Entities/Test.cs` + `Entities/TestQuestion.cs` + `Entities/TestAttempt.cs` + `Entities/TestAnswer.cs` + `Entities/TestAssignment.cs`
- `Entities/Enums/QuestionType.cs` + `Entities/Enums/TestType.cs` + `Entities/Enums/AttemptStatus.cs`
- `Data/Configurations/TestConfiguration.cs`, `TestQuestionConfiguration.cs`, `TestAttemptConfiguration.cs`, `TestAnswerConfiguration.cs`, `TestAssignmentConfiguration.cs`
- Migration: `AddTestingEntities`

### API Layer
- `Dtos/Test*Request.cs`, `Dtos/Test*Response.cs`
- `Mappers/TestMapper.cs`
- `Interfaces/ITestingService.cs`
- `Services/TestingService.cs`
- `Controllers/TestingController.cs`
- `Validators/Test*RequestValidator.cs`
- `SwaggerExamples/Test*Example.cs`
- DI в `ServiceCollectionExtensions.cs`

### Frontend
- Страница курса: раздел «Тесты»
- Страница теста: детали, начало, прохождение
- Страница результатов: `/my/test-results`

---

## Модуль 2: AuthService (UC-14, UC-15)

### UC-14: Change password
- `Dtos/ChangePasswordRequest.cs`
- `AuthService.ChangePasswordAsync()`
- `AuthController.ChangePassword()`
- `Validators/ChangePasswordRequestValidator.cs`

### UC-15: Toggle active
- `UserService.ToggleActiveAsync()`
- `UserController.ToggleActive()`

---

## Модуль 3: LearningService (UC-37, UC-38, UC-46..52)

### UC-37: Semesters
- Entity + Config + Migration + Service + Controller + DTOs + Validator

### UC-38: Student import
- `StudentService.ImportAsync()`
- `StudentController.Import()`

### UC-46: Specialties
- Entity + Config + Migration + Service + Controller + DTOs + Validator

### UC-47: Course → Group
- `CourseService.AssignGroupsAsync()`, `GetGroupsAsync()`, `RemoveGroupAsync()`
- `CourseController` новые endpoint'ы

### UC-48: Student progress
- `DashboardService` / `CourseService.GetProgressAsync()`
- `CourseController.GetProgress()`

### UC-49: Exams
- Entity + Config + Migration + Service + Controller + DTOs + Validator

### UC-50: Retakes
- `ExamService.CreateRetakeAsync()`, `GetRetakesAsync()`, `UpdateRetakeStatusAsync()`
- `ExamController` новые endpoint'ы

### UC-51: Student transfer
- `StudentService.TransferAsync()`, `GetTransfersAsync()`
- `StudentController` новые endpoint'ы

### UC-52: Stipend
- Entity + Config + Migration + Service + Controller + DTOs

---

## Модуль 4: ScheduleService (UC-23)

### UC-23: Calendar view
- Добавить `view=calendar` в `ScheduleService.GetAllAsync()`
- Календарная сетка во фронтенде
