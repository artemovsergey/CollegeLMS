# M3 «Занятия» — дизайн

Дата: 2026-08-17
Статус: утверждён (решения см. ниже)
База: `docs/spec/userstories.md`, `2026-08-16-ui-courses-lessons-schedule-notifications-design.md` (раздел M3, строки 103–141)

## Контекст

M2 «Курсы: CRUD, активность, копирование, соавторы» завершён и задеплоен.
M3 — крупнейший милстоун. Разбит на два независимых этапа с отдельными
ветками и деплоями: **M3a «Бэкенд»** и **M3b «Фронтенд»**.

## Принятые решения (утверждены пользователем)

1. **Декомпозиция**: M3a (бэкенд) → отдельная ветка/PR/деплой → M3b (фронтенд).
2. **Маршруты**: полный breaking-переход на `/api/courses/{courseId}/lessons`,
   старый путь `/lectures` удаляется без redirect (фронт и API деплоятся синхронно).
3. **Прогресс студента**: страница «Мой прогресс» остаётся, считается только по
   тестам (задания удаляются полностью).
4. **CourseDocument**: копируется при дублировании курса (полная копия курса).
5. **IsCurrent**: преподаватель (владелец/соавтор) помечает занятие текущим;
   студент видит бейдж «Сейчас идёт». Уникальность — частичный уникальный индекс
   (одно текущее на курс) в `Data/DbConstraints.cs`.

## Объём M3a (бэкенд)

### 1. Удаление заданий (задач Assignment)

Удалить целиком:

- Сущности `Entities/Assignment.cs`, `Entities/AssignmentSubmission.cs`
- `Entities/Enums/AssignmentStatus.cs` (если существует)
- Конфигурации `Data/Configurations/AssignmentConfiguration.cs`,
  `AssignmentSubmissionConfiguration.cs`
- Связи: `Course.Assignments`, `CourseMaterial.AssignmentId`,
  `Student.Submissions`, DbSet в `AppDbContext`
- `CourseMaterial.Assignment` навигация; в конфигурации материала убрать FK
- Контроллер `AssignmentsController` (если существует), сервис, интерфейс, DTO, мапперы
- `CourseResponse.AssignmentCount` → публичное поле `LessonCount` на месте
- Валидаторы, SwaggerExamples, references во фронтенд-типах (во M3b)

> Внимание: `Assignment` (задача) ≠ `TestAssignment` (тест→группа) — тестовая
> связь остаётся, не трогать.

### 2. Lecture → Lesson (rename по всему бэкенду)

- Таблица `lectures` → `lessons` (миграция)
- Сущность `Entities/Lecture.cs` → `Lesson.cs`, конфигурация `LessonConfiguration.cs`
- Сервис/интерфейс/DTO/мапперы: `Lecture*` → `Lesson*`
- Контроллер: `/api/courses/{courseId}/lectures` → `/api/courses/{courseId}/lessons`
- `CourseResponse.LectureCount` → `LessonCount`
- `Course.Lectures` → `Course.Lessons`
- Все вхождения `lecture` в именах методов, параметров, переменных, комментариях

### 3. Позиция занятия (Order + reorder)

- `Lesson.Order` — компактные целые 1..n в рамках курса
- `POST /api/courses/{courseId}/lessons` — телу добавить `AfterLessonId?`:
  `null` = в начало, иначе после указанного занятия (позиция пересчитывается)
- `PUT /api/courses/{courseId}/lessons/reorder` — `{ lessonIds: Guid[] }` —
  полный порядок занятий курса, значения Order пересчитываются 1..n

### 4. Текущее занятие (IsCurrent)

- `Lesson.IsCurrent` (bool, по умолчанию false)
- `PATCH /api/courses/{courseId}/lessons/{id}/current` — `{ isCurrent: bool }`;
  доступ: преподаватель (владелец/соавтор) или админ
- Уникальность: частичный уникальный индекс `WHERE is_current` на `(course_id)`
  в `Data/DbConstraints.cs` (идемпотентный SQL)
- При установке `true` у занятия X — сброс текущего у остальных занятий курса

### 5. CourseDocument (документация курса)

- Сущность `Entities/CourseDocument.cs`: Id, CourseId, FileName, StoredPath,
  ContentType, SizeBytes, встроенные CreatedAt/UpdatedAt
- Конфигурация: FK на курс, `HasMaxLength` для строк
- `POST /api/courses/{courseId}/documents` (multipart/form-data, IFormFile)
- `GET /api/courses/{courseId}/documents` — список
- `GET /api/courses/{courseId}/documents/{id}/download` — физический файл
- `DELETE /api/courses/{courseId}/documents/{id}` — удаление файла + записи
- Доступ на создание/удаление: преподаватель (владелец/соавтор) или админ;
  скачивание/список: все участники курса
- **Duplicate**: копия курса получает копии файлов + записи (физическое копирование)
- Проверка прав: через `ICourseAccessService` (владелец || соавтор || админ)

### 6. Миграция данных (одна миграция на M3a)

- Drop таблиц `assignments`, `assignment_submissions` (+ данные)
- Rename `lectures` → `lessons`
- Add `is_current`, `order` в `lessons`
- Create `course_documents`
- Порядок: по `order` в `lessons` = текущий порядок по PK/существующему полю

## Объём M3b (фронтенд)

(Кратко — детали в отдельном дизайне перед стартом M3b)

- Переименование типов/страниц/маршрутов на lessons
- Табы курса, dnd-kit (drag-and-drop) сортировка занятий → PUT reorder
- Бейдж «Сейчас идёт» в списке занятий (студенческий вид)
- UI документации курса (upload/download/delete)
- Замена UI заданий (удаление), прогресс тесты-only
- Обновление типов API во всех страницах

## Критерии готовности M3a

- [ ] `dotnet build` проходит
- [ ] Миграция применяется на чистую БД (локально через docker compose)
- [ ] Старые маршруты `/lectures` и `/assignments*` недоступны (404)
- [ ] Postman-коллекция обновлена
- [ ] SwaggerExamples для всех новых/изменённых ответов
- [ ] Модульные + интеграционные тесты (TDD для нового функционала)
- [ ] VPS: миграция применяется при старте API, smoke на рабочей БД

## Файлы

- Дизайн-док: `docs/superpowers/specs/2026-08-17-m3-lessons-design.md`
- План M3a: `docs/superpowers/plans/2026-08-17-m3a-lessons-backend.md`