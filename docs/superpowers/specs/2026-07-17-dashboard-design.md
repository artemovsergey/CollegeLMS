# Dashboard Design

## Scope

Переработать дашборды студента и преподавателя в CollegeLMS. Убрать
счётчики/статистику, заменить на сетку карточек курсов с прогрессом.

Роли: **Student**, **Teacher**. Администратору отдельный дашборд не нужен
(использует /admin).

---

## Студент — `/my/dashboard`

### Приветствие

«Здравствуйте, {User.FullName}» — имя из JWT / профиля.

### Карточки курсов

Сетка `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`. Каждая карточка — shadcn `Card`:

- Название курса
- Преподаватель (teacherName)
- Прогресс-бар: `N из M заданий выполнено` + процент
- Клик → `/my/courses/{courseId}`

Прогресс считается по заданиям (не лекциям): сколько `AssignmentSubmission`
со `Score != null` из общего числа `Assignment` в курсе + тесты
(`TestAttempt.Status == Completed` из общего числа `Test`).

### API

**`GET /api/my/dashboard`**

```json
{
  "isSuccess": true,
  "data": {
    "courses": [
      {
        "id": "guid",
        "title": "Основы программирования",
        "teacherName": "Иванов И.И.",
        "completionPercent": 45.0,
        "completedItems": 5,
        "totalItems": 11
      }
    ]
  }
}
```

DTO (удалить старые `RecentGradeDto`, `UpcomingDeadlineDto`,
`StudentDashboardResponse.CoursesCount`):

```csharp
public class StudentDashboardResponse
{
    public List<CourseWithProgressDto> Courses { get; set; } = [];
}

public class CourseWithProgressDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public double CompletionPercent { get; set; }
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
}
```

Логика `DashboardService.GetStudentDashboardAsync`:

1. Найти студента по `userId`
2. Найти `CourseId` через `CourseGroup` по `student.GroupId`
3. Загрузить курсы с `Teacher` + `User`
4. Для каждого курса: посчитать `completedItems = completedAssignments +
   completedTests` и `totalItems = totalAssignments + totalTests`.
   `completionPercent = completedItems / totalItems * 100`.
   Можно одним SQL-запросом через группировку, либо циклом (курсов обычно 3-8).
5. Вернуть DTO

---

## Преподаватель — `/teacher/dashboard`

### Приветствие

«Здравствуйте, {User.FullName}» — из JWT / profile.

### Карточки курсов

Сетка `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`. Каждая карточка — shadcn `Card`:

- Название курса
- Группы (groupNames через запятую)
- Клик → `/courses/{courseId}`

### API

**`GET /api/teacher/dashboard`**

```json
{
  "isSuccess": true,
  "data": {
    "courses": [
      {
        "id": "guid",
        "title": "Основы программирования",
        "groupNames": "ИСП-31, ИСП-32"
      }
    ]
  }
}
```

DTO (удалить `CoursesCount`, `StudentsCount`, `RecentSubmissions`,
`RecentSubmissionDto`):

```csharp
public class TeacherDashboardResponse
{
    public List<CourseBriefDto> Courses { get; set; } = [];
}

public class CourseBriefDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string GroupNames { get; set; } = string.Empty;
}
```

Логика `DashboardService.GetTeacherDashboardAsync`:

1. Найти преподавателя по `userId`
2. Найти курсы `WHERE TeacherId == teacher.Id`
3. Для каждого — подтянуть `CourseGroups.Group.Name` для groupNames
4. Вернуть DTO

---

## Фронтенд

### StudentDashboardPage (`/my/dashboard`)

```tsx
export default function StudentDashboardPage() {
  // Fetch /api/my/dashboard → StudentDashboardResponse
  // Render: greeting + grid of CourseCard
}
```

### TeacherDashboardPage (`/teacher/dashboard`)

```tsx
export default function TeacherDashboardPage() {
  // Fetch /api/teacher/dashboard → TeacherDashboardResponse
  // Render: greeting + grid of CourseCard
}
```

Общий компонент `CourseCard`:

```tsx
// props: title, subtitle, progress?, href
<Card className="cursor-pointer hover:shadow-md transition-shadow"
      onClick={() => router.push(href)}>
  <CardHeader>
    <CardTitle>{title}</CardTitle>
    <CardDescription>{subtitle}</CardDescription>
  </CardHeader>
  {progress !== undefined && (
    <CardContent>
      <Progress value={progress} className="h-2" />
      <p className="text-xs text-muted-foreground mt-1">{progress}%</p>
    </CardContent>
  )}
</Card>
```

Компонент `Progress` уже есть в shadcn (`components/ui/progress.tsx`).

### Что удалить

- `RecentGradeDto` (backend + frontend types)
- `UpcomingDeadlineDto` (backend + frontend types)
- `RecentSubmissionDto` (backend + frontend types)
- `CoursesCount`, `StudentsCount` из ответов
- Пустые секции `upcomingDeadlines`, `recentGrades` из старого дашборда студента

---

## Границы

**Не входит в этот спринт:**
- Дашборд администратора
- Фильтрация/поиск на дашборде
- Избранные курсы
- Уведомления
- Обновление в реальном времени (SignalR не в MVP)

---

## Порядок реализации

1. Backend: новые DTO, обновить DashboardService, удалить старые DTO
2. Backend: обновить DashboardController (Swagger summaries)
3. Backend: миграция не нужна (только DTO/сервисы)
4. Frontend: обновить типы (`types/index.ts`)
5. Frontend: новый `CourseCard` компонент
6. Frontend: переписать `my/dashboard/page.tsx`
7. Frontend: переписать `teacher/dashboard/page.tsx`
8. dotnet test + npm run build
