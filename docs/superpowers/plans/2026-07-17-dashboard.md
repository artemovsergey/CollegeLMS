# Dashboard Implementation Plan

> **For agentic workers:** Inline execution

**Goal:** Переработать дашборды студента и преподавателя — убрать счётчики, заменить на сетку карточек курсов с прогрессом.

**Architecture:** Новые DTO (`CourseWithProgressDto`, `CourseBriefDto`), обновлённый `DashboardService`, упрощённый фронтенд с общим компонентом `CourseCard`.

**Tech Stack:** .NET 10, ASP.NET Core, Next.js 14, shadcn/ui

## Global Constraints

- Result<T> pattern везде
- AsNoTracking на чтении
- Сообщения на русском
- snake_case в БД (не меняется, миграция не нужна)

---

### Task 1: Backend DTOs — новые + удалить старые

**Files:**
- Modify: `CollegeLMS.API/Dtos/StudentDashboardResponse.cs` — заменить содержимое
- Modify: `CollegeLMS.API/Dtos/TeacherDashboardResponse.cs` — заменить содержимое
- Delete: удалить `RecentSubmissionDto`, `RecentGradeDto`, `UpcomingDeadlineDto` (они внутри тех же файлов)

**Interfaces:**
- Produces: `StudentDashboardResponse.Courses` → `List<CourseWithProgressDto>`
- Produces: `TeacherDashboardResponse.Courses` → `List<CourseBriefDto>`

- [ ] **Step 1: Write StudentDashboardResponse.cs**

```csharp
namespace CollegeLMS.API.Dtos;

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

- [ ] **Step 2: Write TeacherDashboardResponse.cs**

```csharp
namespace CollegeLMS.API.Dtos;

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

---

### Task 2: Backend DashboardService

**Files:**
- Modify: `CollegeLMS.API/Services/DashboardService.cs`

**Interfaces:**
- Consumes: новые DTO из Task 1
- Implements: `IDashboardService`

- [ ] **Step 1: Write DashboardService.cs**

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<Result<TeacherDashboardResponse>> GetTeacherDashboardAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher is null)
            return Result<TeacherDashboardResponse>.Fail("Преподаватель не найден", 404);

        var courses = await db
            .Courses.AsNoTracking()
            .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
            .Where(c => c.TeacherId == teacher.Id)
            .Select(c => new CourseBriefDto
            {
                Id = c.Id,
                Title = c.Title,
                GroupNames = string.Join(", ", c.CourseGroups.Select(cg => cg.Group.Name)),
            })
            .ToListAsync(ct);

        return Result<TeacherDashboardResponse>.Ok(new TeacherDashboardResponse { Courses = courses });
    }

    public async Task<Result<StudentDashboardResponse>> GetStudentDashboardAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student is null)
            return Result<StudentDashboardResponse>.Fail("Студент не найден", 404);

        var courseIds = await db
            .CourseGroups.AsNoTracking()
            .Where(cg => cg.GroupId == student.GroupId)
            .Select(cg => cg.CourseId)
            .ToListAsync(ct);

        var courses = await db
            .Courses.AsNoTracking()
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Assignments)
            .Where(c => courseIds.Contains(c.Id))
            .ToListAsync(ct);

        var result = new List<CourseWithProgressDto>();
        foreach (var course in courses)
        {
            var totalAssignments = course.Assignments.Count;
            var completedAssignments = await db.AssignmentSubmissions.CountAsync(
                s => course.Assignments.Select(a => a.Id).Contains(s.AssignmentId)
                     && s.StudentId == student.Id && s.Score.HasValue,
                ct
            );

            var totalTests = await db.Tests.CountAsync(t => t.CourseId == course.Id, ct);
            var completedTests = await db.TestAttempts.CountAsync(
                a => a.StudentId == student.Id && a.Test.CourseId == course.Id
                     && a.Status == Entities.Enums.AttemptStatus.Completed,
                ct
            );

            var total = totalAssignments + totalTests;
            var completed = completedAssignments + completedTests;
            var percent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

            result.Add(new CourseWithProgressDto
            {
                Id = course.Id,
                Title = course.Title,
                TeacherName = course.Teacher?.User?.FullName ?? "",
                CompletionPercent = percent,
                CompletedItems = completed,
                TotalItems = total,
            });
        }

        return Result<StudentDashboardResponse>.Ok(new StudentDashboardResponse { Courses = result });
    }
}
```

---

### Task 3: Backend DashboardController — обновить Swagger

**Files:**
- Modify: `CollegeLMS.API/Controllers/DashboardController.cs`

- [ ] **Step 1: Update DashboardController.cs** — обновить summaries под новый DTO

---

### Task 4: Backend build + test

- [ ] **Step 1: dotnet build** — `docker run --rm -v /home/user1/CollegeLMS:/app -w /app/CollegeLMS.API mcr.microsoft.com/dotnet/sdk:10.0 dotnet build`
- [ ] **Step 2: dotnet test** — `docker run --rm -v /home/user1/CollegeLMS:/app -w /app/CollegeLMS.Tests mcr.microsoft.com/dotnet/sdk:10.0 dotnet test --filter "FullyQualifiedName~DashboardServiceTests"`

---

### Task 5: Frontend — обновить типы

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts`

- [ ] **Step 1: Update types**

Заменить `StudentDashboardResponse` и `TeacherDashboardResponse`:

```typescript
export interface CourseWithProgressDto {
  id: string
  title: string
  teacherName: string
  completionPercent: number
  completedItems: number
  totalItems: number
}

export interface StudentDashboardResponse {
  courses: CourseWithProgressDto[]
}

export interface CourseBriefDto {
  id: string
  title: string
  groupNames: string
}

export interface TeacherDashboardResponse {
  courses: CourseBriefDto[]
}
```

Удалить `SubmissionResponse` из TeacherDashboardResponse (если была ссылка).

---

### Task 6: Frontend — CourseCard компонент

**Files:**
- Create: `CollegeLMS.Next/components/CourseCard.tsx`

- [ ] **Step 1: Create component**

```tsx
"use client"

import { useRouter } from "next/navigation"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"

interface CourseCardProps {
  id: string
  title: string
  subtitle: string
  href: string
  progress?: {
    percent: number
    completed: number
    total: number
  }
}

export default function CourseCard({ id, title, subtitle, href, progress }: CourseCardProps) {
  const router = useRouter()

  return (
    <Card
      key={id}
      className="cursor-pointer hover:shadow-md transition-shadow"
      onClick={() => router.push(href)}
    >
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
        <CardDescription>{subtitle}</CardDescription>
      </CardHeader>
      {progress && (
        <CardContent>
          <Progress value={progress.percent} className="h-2" />
          <p className="text-xs text-muted-foreground mt-1">
            {progress.completed} из {progress.total} выполнено ({progress.percent}%)
          </p>
        </CardContent>
      )}
    </Card>
  )
}
```

---

### Task 7: Frontend — StudentDashboardPage

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/my/dashboard/page.tsx`

- [ ] **Step 1: Rewrite page**

```tsx
"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, StudentDashboardResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import CourseCard from "@/components/CourseCard"

export default function StudentDashboardPage() {
  const { token, user } = useAuth()

  const [dashboard, setDashboard] = useState<StudentDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<StudentDashboardResponse>>("/api/my/dashboard")
      const body = res.data
      if (body.isSuccess && body.data) {
        setDashboard(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchDashboard()
    }
  }, [token, fetchDashboard])

  if (loading) return <LoadingSpinner className="py-16" />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      {user && (
        <h2 className="text-xl font-semibold">Здравствуйте, {user.fullName}</h2>
      )}

      {error && <ErrorBanner message={error} />}

      {dashboard && dashboard.courses.length === 0 && (
        <p className="text-muted-foreground">У вас нет активных курсов</p>
      )}

      {dashboard && dashboard.courses.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {dashboard.courses.map(c => (
            <CourseCard
              key={c.id}
              id={c.id}
              title={c.title}
              subtitle={c.teacherName}
              href={`/my/courses/${c.id}`}
              progress={{ percent: c.completionPercent, completed: c.completedItems, total: c.totalItems }}
            />
          ))}
        </div>
      )}
    </div>
  )
}
```

---

### Task 8: Frontend — TeacherDashboardPage

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/teacher/dashboard/page.tsx`

- [ ] **Step 1: Rewrite page**

```tsx
"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, TeacherDashboardResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import CourseCard from "@/components/CourseCard"

export default function TeacherDashboardPage() {
  const { token, user } = useAuth()

  const [dashboard, setDashboard] = useState<TeacherDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<TeacherDashboardResponse>>("/api/teacher/dashboard")
      const body = res.data
      if (body.isSuccess && body.data) {
        setDashboard(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchDashboard()
    }
  }, [token, fetchDashboard])

  if (loading) return <LoadingSpinner className="py-16" />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      {user && (
        <h2 className="text-xl font-semibold">Здравствуйте, {user.fullName}</h2>
      )}

      {error && <ErrorBanner message={error} />}

      {dashboard && dashboard.courses.length === 0 && (
        <p className="text-muted-foreground">У вас нет курсов</p>
      )}

      {dashboard && dashboard.courses.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {dashboard.courses.map(c => (
            <CourseCard
              key={c.id}
              id={c.id}
              title={c.title}
              subtitle={c.groupNames}
              href={`/courses/${c.id}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}
```

---

### Task 9: Backend + Frontend build verify

- [ ] **Step 1: dotnet build**
- [ ] **Step 2: dotnet test** — DashboardServiceTests + старые тесты
- [ ] **Step 3: npm install + npm run build** (CollegeLMS.Next)
