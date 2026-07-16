# Дизайн-спека: Lecture↔Test, очистка UI, seed data

**Дата:** 2026-07-16
**Статус:** Утверждён

---

## 1. Связь Lecture ↔ Test

### Проблема
Сейчас `Lecture` и `Test` — сиблинги под `Course`. Нет прямой связи.

### Решение
Добавить nullable FK `TestId` в `Lecture`.

### Изменения

**Entity (`Entities/Lecture.cs`):**
```csharp
public Guid? TestId { get; set; }
public Test? Test { get; set; }
```

**EF Configuration (`Data/Configurations/LectureConfiguration.cs`):**
- `HasOne(e => e.Test).WithMany().HasForeignKey(e => e.TestId).OnDelete(DeleteBehavior.SetNull)`
- Индекс на `TestId`

**Миграция:** `dotnet ef migrations add AddTestIdToLecture`

**DTO:**
- `LectureResponse` — добавить `TestId?`, `TestTitle?`
- `LectureRequest` — добавить `TestId?`

**Маппер (`Mappers/LectureMapper.cs`):** обновить маппинг

**Сервис (`Services/LectureService.cs`):**
- `CreateAsync` — принять `TestId?`, сохранить
- `UpdateAsync` — принять `TestId?`, обновить (включая сброс в null)
- `GetByIdAsync` — включить `.Include(l => l.Test)`

**Контроллер (`Controllers/LectureController.cs`):** без изменений (DTO уже передаётся)

**Фронтенд:**
- Форма создания/редактирования лекции — выпадающий список тестов курса (Select, опционально)
- Карточка лекции — показать название теста (если прикреплён)

---

## 2. UI: убрать дедлайны и оценки

### Студенты

**`app/my/dashboard/page.tsx`:**
- Убрать секцию `upcomingDeadlines`
- Убрать секцию `recentGrades`
- Оставить: курсы, прогресс

**`app/(authenticated)/courses/[id]/page.tsx`:**
- Убрать `dueDate` из карточек заданий (Assignments tab)
- Убрать `maxScore` из карточек заданий
- Убрать `submissionCount`

### Преподаватели

**`app/(authenticated)/courses/[id]/page.tsx`:**
- Убрать `dueDate` из вкладки Assignments
- Убрать `maxScore` из вкладки Assignments
- Убрать `submissionCount`

### Backend (Response DTOs)
- `StudentDashboardResponse` — убрать `upcomingDeadlines`, `recentGrades`
- Оставить `DashboardService` без изменений (данные всё ещё нужны для внутренней логики)

---

## 3. Seed data: 4 курса на основе МДК

### Источники
1. **mdk0104_IP** — Системное программирование (.NET)
2. **mdk0103_IP** — Мобильные приложения (Kotlin)
3. **mdk0102** — Тестирование модулей
4. **mdk0103_IB** — Мобильные приложения ИБ

### Структура каждого курса
- 5-8 лекций (на основе README репозитория)
- 5-8 заданий (лабораторные работы)
- 2-3 теста с вопросами (SingleChoice, MultipleChoice)
- Материалы: ссылки на файлы из GitHub репозитория

### Реализация
- Расширить `DataSeeder.cs` — добавить курсы, лекции, тесты, вопросы, задания
- Данные: названия, описания, содержание — из README репозиториев
- Привязать к существующим группам и преподавателям
