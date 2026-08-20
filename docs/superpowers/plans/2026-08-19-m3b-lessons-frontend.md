# M3b «Занятия: фронтенд» Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перевести фронтенд CollegeLMS.Next на новую модель M3a: роуты `/lessons`, dnd-сортировка и «Сейчас идёт», вкладка «Документы», удаление UI заданий.

**Architecture:** Только `CollegeLMS.Next/` + e2e. Сначала типы и API-хелперы, затем компоненты и страницы, потом удаление заданий и e2e. Каждый коммит компилируется (npm run build).

**Tech Stack:** Next.js 14 (App Router), TypeScript, Tailwind CSS 4, shadcn/ui, `@dnd-kit/core|sortable|utilities`, axios, Playwright.

## Global Constraints

- Все тексты на русском; тексты ошибок в тостах на русском
- Компоненты: shadcn/ui примитивы из `components/ui/` (button, badge, dialog, alert-dialog, select, input, textarea, label, switch — уже есть), иконки Lucide
- Адаптивность: 1366×768 и ~393px, touch-target ≥44px
- `useParams` параметры: курс — `id`, занятие — `lessonId`
- Бэкенд-эндпоинты (уже в master): `GET|POST /api/courses/{courseId}/lessons`, `PUT /api/courses/{courseId}/lessons/{id}`, `DELETE .../{id}`, `PUT /api/courses/{courseId}/lessons/reorder`, `PATCH /api/courses/{courseId}/lessons/{id}/current`, `GET|POST /api/courses/{courseId}/documents`, `GET|DELETE /api/courses/{courseId}/documents/{id}`, `GET /api/courses/{courseId}/documents/{id}/download`
- DTO: `LessonResponse { id, courseId, title, content, order, kind, isCurrent, testId, testTitle }`, `CourseDocumentResponse { id, courseId, fileName, contentType, sizeBytes, createdAt }`
- Node на хосте НЕТ — npm-команды через docker: `docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next -e NEXT_PUBLIC_API_URL=http://localhost:8080 node:20-alpine sh -c "<CMD>"` (node_modules уже установлен на хосте; node_modules в контейнере не пересоздавать — монтируется `/src`)
- Проверка: `npm run build` (docker, с `--no-lint` если падает линт на старых файлах), `npm run dev` smoke, `npx playwright test`
- Git-префиксы: `feat:` / `fix:` / `docs:` / `refactor:` / `chore:` / `test:`
- После каждого коммита с кодом — `npm run build` (docker) ДО commit

---

### Task 1: Типы API (`types/index.ts`)

**Files:**
- Modify: `CollegeLMS.Next/types/index.ts`

**Interfaces:**
- Consumes: DTO-контракты бэкенда M3a (см. Global Constraints)
- Produces: `LessonResponse`, `CourseDocumentResponse`, `LessonKind` union, `MaterialResponse.lessonId`, `CourseResponse.lessonCount/documentCount`, `CourseProgressResponse` без assignments, `CreateTestRequest.lessonId`

- [ ] **Step 1: Переписать `types/index.ts`**

Заменить блоки:

`CourseResponse` (строки 77–90) — заменить `lectureCount` на `lessonCount`, удалить `assignmentCount`, добавить `documentCount`:

```ts
export interface CourseResponse {
  id: string
  title: string
  description: string
  teacherId: string
  teacherName: string
  groupNames: string
  status: string
  isActive: boolean
  authorIds: string[]
  authorNames: string
  lessonCount: number
  documentCount: number
}
```

`LectureResponse` (строки 105–114) — заменить целиком:

```ts
export type LessonKind = "Lecture" | "Practice" | "SelfStudy"

export interface LessonResponse {
  id: string
  courseId: string
  title: string
  content: string
  order: number
  kind: LessonKind
  isCurrent: boolean
  testId: string | null
  testTitle: string | null
}
```

Удалить целиком `AssignmentResponse` (строки 116–125) и `SubmissionResponse` (строки 127–136).

`MaterialResponse` (строки 183–192) — заменить `lectureId`/`assignmentId` на `lessonId`:

```ts
export interface MaterialResponse {
  id: string
  courseId: string
  lessonId: string | null
  fileName: string
  fileSize: number
  mimeType: string
  createdAt: string
}
```

`CourseProgressResponse` (строки 365–374) — удалить `totalAssignments`/`completedAssignments`:

```ts
export interface CourseProgressResponse {
  courseId: string
  courseTitle: string
  totalTests: number
  completedTests: number
  averageScore: number
  completionPercent: number
}
```

`CreateTestRequest` (строки 249–258) — `lectureId?: string | null` → `lessonId?: string | null`.

После `MaterialResponse` добавить:

```ts
export interface CourseDocumentResponse {
  id: string
  courseId: string
  fileName: string
  contentType: string
  sizeBytes: number
  createdAt: string
}
```

- [ ] **Step 2: Проверить компиляцию типов**

Run: `cd CollegeLMS.Next && npx tsc --noEmit` (docker, см. Global Constraints)
Expected: ошибки только в файлах, которые ещё не обновлены (задачи 2–13 их исправят).

---

### Task 2: API-хелперы (`lib/api.ts`, `lib/lessonTypes.ts`)

**Files:**
- Modify: `CollegeLMS.Next/lib/api.ts`
- Create: `CollegeLMS.Next/lib/lessonTypes.ts`
- Delete: `CollegeLMS.Next/lib/lectureTypes.ts`

**Interfaces:**
- Produces:
  - `uploadCourseDocument(courseId: string, file: File): Promise<CourseDocumentResponse>`
  - `downloadCourseDocument(courseId: string, id: string, fileName: string): Promise<void>`
  - `deleteCourseDocument(courseId: string, id: string): Promise<void>`
  - `reorderLessons(courseId: string, lessonIds: string[]): Promise<void>`
  - `setCurrentLesson(courseId: string, id: string, isCurrent: boolean): Promise<void>`
  - `LESSON_KIND_LABELS: Record<LessonKind, string>` и `LESSON_KIND_VARIANTS: Record<LessonKind, "default" | "secondary" | "outline" | "destructive">`

- [ ] **Step 1: Добавить хелперы в `lib/api.ts`**

В конец файла (после `export default api`):

```ts
function unwrap<T>(res: { data: Result<T> }): T {
  if (!res.data.isSuccess || res.data.data === null) {
    throw new Error(res.data.errorMessage ?? "Ошибка запроса")
  }
  return res.data.data
}

export async function uploadCourseDocument(
  courseId: string,
  file: File,
): Promise<CourseDocumentResponse> {
  const form = new FormData()
  form.append("file", file)
  const res = await api.post<Result<CourseDocumentResponse>>(
    `/api/courses/${courseId}/documents`,
    form,
  )
  return unwrap(res)
}

export async function deleteCourseDocument(courseId: string, id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/courses/${courseId}/documents/${id}`)
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка удаления")
}

export async function downloadCourseDocument(
  courseId: string,
  id: string,
  fileName: string,
): Promise<void> {
  const res = await api.get<Blob>(`/api/courses/${courseId}/documents/${id}/download`, {
    responseType: "blob",
  })
  const url = URL.createObjectURL(res.data)
  const a = document.createElement("a")
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

export async function reorderLessons(courseId: string, lessonIds: string[]): Promise<void> {
  const res = await api.put<Result<null>>(`/api/courses/${courseId}/lessons/reorder`, {
    lessonIds,
  })
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка сохранения порядка")
}

export async function setCurrentLesson(
  courseId: string,
  id: string,
  isCurrent: boolean,
): Promise<void> {
  const res = await api.patch<Result<null>>(`/api/courses/${courseId}/lessons/${id}/current`, {
    isCurrent,
  })
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка обновления")
}
```

В начало файла добавить импорт типа: `import type { Result, CourseDocumentResponse } from "@/types"` (после `import { toast } from "sonner"`).

- [ ] **Step 2: Создать `lib/lessonTypes.ts`**

```ts
import type { LessonKind } from "@/types"

export const LESSON_KIND_LABELS: Record<LessonKind, string> = {
  Lecture: "Лекция",
  Practice: "Практика",
  SelfStudy: "Самостоятельная работа",
}

export const LESSON_KIND_VARIANTS: Record<
  LessonKind,
  "default" | "secondary" | "outline" | "destructive"
> = {
  Lecture: "default",
  Practice: "secondary",
  SelfStudy: "outline",
}
```

- [ ] **Step 3: Удалить `lib/lectureTypes.ts`**

`rm CollegeLMS.Next/lib/lectureTypes.ts` (все импорты переедут на lessonTypes в задачах 3–11).

- [ ] **Step 4: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: ошибки только в ещё не обновлённых файлах (импорты lectureTypes).

---

### Task 3: `LessonForm.tsx` + страницы создания/редактирования (роут `/lessons`)

**Files:**
- Create: `CollegeLMS.Next/components/LessonForm.tsx` (из `LectureForm.tsx`)
- Delete: `CollegeLMS.Next/components/LectureForm.tsx`
- Rename (git mv): `app/(authenticated)/courses/[id]/lectures/new/page.tsx` → `app/(authenticated)/courses/[id]/lessons/new/page.tsx`
- Rename (git mv): `app/(authenticated)/courses/[id]/lectures/[lectureId]/edit/page.tsx` → `app/(authenticated)/courses/[id]/lessons/[lessonId]/edit/page.tsx`

**Interfaces:**
- Produces: `<LessonForm courseId: string, lesson?: LessonResponse>`; роуты `/courses/{id}/lessons/new`, `/courses/{id}/lessons/{lessonId}/edit`

- [ ] **Step 1: Создать `components/LessonForm.tsx`**

Полное содержимое (по образцу LectureForm с правками):

```tsx
"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import type { Result, LessonResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import FormField from "@/components/FormField"
import FormErrorBanner from "@/components/FormErrorBanner"
import { parseErrors } from "@/lib/errors"
import { LESSON_KIND_LABELS } from "@/lib/lessonTypes"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

const LESSON_KIND_OPTIONS: { value: string; label: string }[] = Object.entries(
  LESSON_KIND_LABELS,
).map(([value, label]) => ({ value, label }))

interface LessonFormProps {
  courseId: string
  lesson?: LessonResponse
}

export default function LessonForm({ courseId, lesson }: LessonFormProps) {
  const router = useRouter()
  const isEdit = !!lesson

  const [title, setTitle] = useState(lesson?.title ?? "")
  const [content, setContent] = useState(lesson?.content ?? "")
  const [kind, setKind] = useState<string>(lesson?.kind ?? "Lecture")
  const [mode, setMode] = useState<"markup" | "preview">(isEdit ? "preview" : "markup")
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    const clientErrors: Record<string, string[]> = {}
    if (!title.trim()) clientErrors.title = ["Название занятия обязательно"]
    else if (title.trim().length < 3) clientErrors.title = ["Название должно содержать минимум 3 символа"]
    if (!content.trim()) clientErrors.content = ["Содержание обязательно"]
    setFieldErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return
    setSubmitting(true)
    try {
      const body = { title, content, kind }
      const res = isEdit
        ? await api.put<Result<LessonResponse>>(`/api/courses/${courseId}/lessons/${lesson!.id}`, body)
        : await api.post<Result<LessonResponse>>(`/api/courses/${courseId}/lessons`, body)
      if (res.data.isSuccess) {
        const id = res.data.data?.id
        router.push(id ? `/courses/${courseId}/lessons/${id}` : `/courses/${courseId}`)
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка сохранения")
      }
    } catch (err) {
      const parsed = parseErrors(err)
      setFieldErrors(parsed.fieldErrors)
      setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {formError && <FormErrorBanner message={formError} />}

      <FormField id="lesson-title" label="Название занятия" required error={fieldErrors.title?.[0]}>
        <Input
          id="lesson-title"
          required
          value={title}
          onChange={e => setTitle(e.target.value)}
        />
      </FormField>

      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <div className="flex rounded-md border bg-muted/40 p-0.5">
            <button
              type="button"
              onClick={() => setMode("markup")}
              className={`rounded-sm px-3 py-1 text-sm ${mode === "markup" ? "bg-card font-medium shadow-sm" : "text-muted-foreground"}`}
            >
              Разметка
            </button>
            <button
              type="button"
              onClick={() => setMode("preview")}
              className={`rounded-sm px-3 py-1 text-sm ${mode === "preview" ? "bg-card font-medium shadow-sm" : "text-muted-foreground"}`}
            >
              Предпросмотр
            </button>
          </div>
          <span className="text-xs text-muted-foreground">Markdown</span>
        </div>
        {mode === "preview" ? (
          <div className="min-h-40 max-h-96 overflow-y-auto rounded-md border bg-muted/40 p-4">
            <div className="prose max-w-none">
              <ReactMarkdown>{content || "Введите текст для предпросмотра"}</ReactMarkdown>
            </div>
          </div>
        ) : (
          <FormField id="lesson-content" label="Содержание" required error={fieldErrors.content?.[0]}>
            <Textarea
              id="lesson-content"
              required
              className="min-h-[200px] font-mono text-sm"
              value={content}
              onChange={e => setContent(e.target.value)}
            />
          </FormField>
        )}
      </div>

      <FormField id="lesson-kind" label="Тип занятия">
        <Select value={kind} onValueChange={setKind}>
          <SelectTrigger id="lesson-kind">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {LESSON_KIND_OPTIONS.map(opt => (
              <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </FormField>

      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>
          Отмена
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Сохранение..." : isEdit ? "Сохранить" : "Создать"}
        </Button>
      </div>
    </form>
  )
}
```

- [ ] **Step 2: Переименовать страницы (git mv) и поправить**

```bash
cd CollegeLMS.Next
git mv "app/(authenticated)/courses/[id]/lectures/new/page.tsx" "app/(authenticated)/courses/[id]/lessons/new/page.tsx"
git mv "app/(authenticated)/courses/[id]/lectures/[lectureId]/edit/page.tsx" "app/(authenticated)/courses/[id]/lessons/[lessonId]/edit/page.tsx"
```

В `lessons/new/page.tsx`: `import LectureForm from "@/components/LectureForm"` → `import LessonForm from "@/components/LessonForm"`; `<LectureForm courseId={courseId} />` → `<LessonForm courseId={courseId} />`.

В `lessons/[lessonId]/edit/page.tsx`: тип `LectureResponse` → `LessonResponse`; `params.lectureId` → `params.lessonId`; URL `/api/courses/${courseId}/lectures/${lectureId}` → `/api/courses/${courseId}/lessons/${lessonId}`; `LectureForm` → `LessonForm`; `lesson` вместо `lecture` в стейте (имя переменной — `lesson`).

- [ ] **Step 3: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: ошибки только в файлах задач 4–11.

---

### Task 4: Страница просмотра занятия (`lessons/[lessonId]/page.tsx`, тест внутри)

**Files:**
- Rename (git mv): `app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx` → `app/(authenticated)/courses/[id]/lessons/[lessonId]/page.tsx`
- Modify: этот файл (переменные, URL, типы, тексты)

- [ ] **Step 1: git mv**

```bash
cd CollegeLMS.Next
git mv "app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx" "app/(authenticated)/courses/[id]/lessons/[lessonId]/page.tsx"
```

- [ ] **Step 2: Правки внутри файла**

1. `LectureResponse` → `LessonResponse` (импорт, стейт `setLecture` → `setLesson`, переменная `lecture` → `lesson` — заменить все вхождения).
2. `params.lectureId as string` → `params.lessonId as string`.
3. URL в `fetchLesson`: `/api/courses/${courseId}/lectures/${lectureId}` → `/api/courses/${courseId}/lessons/${lessonId}`.
4. `DELETE`: `/api/courses/${courseId}/lectures/${lesson.id}` → `/api/courses/${courseId}/lessons/${lesson.id}`.
5. Импорт `LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS` из `@/lib/lectureTypes` → `LESSON_KIND_LABELS, LESSON_KIND_VARIANTS` из `@/lib/lessonTypes`; использования `lecture.lectureType` → `lesson.kind` (строки 388–390).
6. Ссылки на роуты внутри: `/courses/${courseId}/lectures/${lesson.id}/edit` → `/courses/${courseId}/lessons/${lesson.id}/edit`; `/courses/${courseId}/lectures/${lesson.id}/test` → `/courses/${courseId}/lessons/${lesson.id}/test` (в двух местах: строки 538 и 547).
7. `CreateTestRequest` body: `lectureId: lesson.id` → `lessonId: lesson.id`.
8. Тексты: «Создать тест к лекции» → «Создать тест к занятию» (2 места: кнопка строка 441 и DialogTitle строка 446); placeholder «Тест по лекции 1» → «Тест по занятию 1»; «Тест к лекции» (строки 410, 513) → «Тест к занятию».
9. `handleDelete` toast: «Занятие удалено» — уже ок.
10. Название функции компонента `LectureViewPage` → `LessonViewPage`.

- [ ] **Step 3: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: ошибки только в файлах задач 5–11.

---

### Task 5: Страница прохождения теста (`lessons/[lessonId]/test/page.tsx`)

**Files:**
- Rename (git mv): `app/(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx` → `app/(authenticated)/courses/[id]/lessons/[lessonId]/test/page.tsx`

- [ ] **Step 1: git mv**

```bash
cd CollegeLMS.Next
git mv "app/(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx" "app/(authenticated)/courses/[id]/lessons/[lessonId]/test/page.tsx"
```

- [ ] **Step 2: Правки внутри файла**

1. `LectureResponse` → `LessonResponse`; `params.lectureId` → `params.lessonId`; переменная `lecture` → `lesson` (все вхождения).
2. URL `/api/courses/${courseId}/lectures/${lectureId}` → `/api/courses/${courseId}/lessons/${lessonId}` (fetch).
3. Роуты `/courses/${courseId}/lectures/${lectureId}` → `/courses/${courseId}/lessons/${lessonId}` (4 места: строки 44, 151, 161, 201, 221).
4. Тексты: «У этой лекции нет теста» → «У этого занятия нет теста»; «Ошибка загрузки лекции» → «Ошибка загрузки занятия»; «Назад к лекции» → «Назад к занятию» (4 места); «Тест по лекции «...»» → «Тест по занятию «...»»; «по материалу лекции» → «по материалу занятия».
5. Компонент `LectureTestPage` → `LessonTestPage`.

- [ ] **Step 3: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: ошибки только в файлах задач 6–11.

---

### Task 6: Установка @dnd-kit

**Files:**
- Modify: `CollegeLMS.Next/package.json`, `package-lock.json` (если есть)

- [ ] **Step 1: Установить пакеты через docker**

```bash
docker run --rm -w /src/CollegeLMS.Next node:20-alpine sh -c "npm install @dnd-kit/core@^6.3.1 @dnd-kit/sortable@^10.0.0 @dnd-kit/utilities@^3.2.2"
```

(модифицирует package.json + package-lock.json в репозитории; node_modules уже установлен локально — docker-инстал записывает зависимости в монтированную папку node_modules, что допустимо; при конфликте прав — `sudo chown -R user1:user1 /home/user1/CollegeLMS/CollegeLMS.Next/node_modules`.)

- [ ] **Step 2: Проверка**

Run: `npx tsc --noEmit` (docker) — новые пакеты резолвятся без ошибок.
Expected: PASS (кроме известных незакрытых задач).

---

### Task 7: Компонент `LessonList.tsx` (список занятий преподавателя, dnd)

**Files:**
- Create: `CollegeLMS.Next/components/lesson/LessonList.tsx`

**Interfaces:**
- Consumes: `LessonResponse`, `reorderLessons`, `setCurrentLesson` (Task 2), `LESSON_KIND_LABELS/VARIANTS`
- Produces: `<LessonList courseId: string, lessons: LessonResponse[], canManage: boolean, onChanged: () => void, onOpen: (id: string) => void>` — для преподавателя dnd+переключатель current, для студента только список (без dnd); `onChanged` — вызов родителя для обновления списка после reorder/current/delete

- [ ] **Step 1: Создать файл**

```tsx
"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import type { LessonResponse } from "@/types"
import { reorderLessons, setCurrentLesson } from "@/lib/api"
import { LESSON_KIND_LABELS, LESSON_KIND_VARIANTS } from "@/lib/lessonTypes"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { toast } from "sonner"
import { GripVertical, FileCheck, Pencil, Trash2, BookOpenCheck } from "lucide-react"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import api from "@/lib/api"

function SortableRow({
  lesson,
  canManage,
  onOpen,
  onToggleCurrent,
  onEdit,
  onAskDelete,
}: {
  lesson: LessonResponse
  canManage: boolean
  onOpen: (id: string) => void
  onToggleCurrent: (id: string, value: boolean) => void
  onEdit: (id: string) => void
  onAskDelete: (id: string) => void
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: lesson.id,
    disabled: !canManage,
  })
  const style = { transform: CSS.Transform.toString(transform), transition }
  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`flex items-center gap-2 p-4 cursor-pointer hover:bg-muted/50 ${isDragging ? "opacity-50" : ""}`}
      onClick={() => onOpen(lesson.id)}
    >
      {canManage && (
        <button
          type="button"
          className="cursor-grab touch-none p-1 text-muted-foreground hover:text-foreground"
          {...attributes}
          {...listeners}
          aria-label="Перетащить занятие"
          onClick={e => e.stopPropagation()}
        >
          <GripVertical className="size-4" />
        </button>
      )}
      <span className="text-sm text-muted-foreground w-6 shrink-0">{lesson.order}</span>
      <span className="font-medium min-w-0 flex-1 truncate">{lesson.title}</span>
      <Badge className="shrink-0" variant={LESSON_KIND_VARIANTS[lesson.kind] ?? "outline"}>
        {LESSON_KIND_LABELS[lesson.kind] ?? lesson.kind}
      </Badge>
      {lesson.testId && (
        <Badge variant="outline" className="shrink-0">
          <FileCheck className="size-3 mr-1" />
          Тест
        </Badge>
      )}
      {canManage ? (
        <div className="flex items-center gap-1 shrink-0" onClick={e => e.stopPropagation()}>
          <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Switch
              checked={lesson.isCurrent}
              onCheckedChange={v => onToggleCurrent(lesson.id, v)}
              aria-label="Сейчас идёт"
            />
            <span className="hidden sm:inline">Сейчас идёт</span>
          </label>
          <Button variant="ghost" size="sm" onClick={() => onEdit(lesson.id)}>
            <Pencil className="size-4" />
          </Button>
          <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-fg" onClick={() => onAskDelete(lesson.id)}>
            <Trash2 className="size-4" />
          </Button>
        </div>
      ) : (
        lesson.isCurrent && (
          <Badge className="shrink-0" variant="default">
            <BookOpenCheck className="size-3 mr-1" />
            Сейчас идёт
          </Badge>
        )
      )}
    </div>
  )
}

export default function LessonList({
  courseId,
  lessons,
  canManage,
  onChanged,
}: {
  courseId: string
  lessons: LessonResponse[]
  canManage: boolean
  onChanged: () => void
}) {
  const router = useRouter()
  const [localLessons, setLocalLessons] = useState<LessonResponse[]>(lessons)
  const [reordering, setReordering] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }))

  if (localLessons !== lessons && !reordering) {
    setLocalLessons(lessons)
  }

  const sorted = [...localLessons].sort((a, b) => a.order - b.order)

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (!over || active.id === over.id) return
    const oldIndex = sorted.findIndex(l => l.id === active.id)
    const newIndex = sorted.findIndex(l => l.id === over.id)
    if (oldIndex === -1 || newIndex === -1) return
    const next = arrayMove(sorted, oldIndex, newIndex).map((l, i) => ({ ...l, order: i + 1 }))
    setLocalLessons(next)
    setReordering(true)
    try {
      await reorderLessons(courseId, next.map(l => l.id))
      toast.success("Порядок занятий сохранён")
      onChanged()
    } catch {
      toast.error("Не удалось сохранить порядок")
      setLocalLessons(lessons)
    } finally {
      setReordering(false)
    }
  }

  const handleToggleCurrent = async (id: string, value: boolean) => {
    setLocalLessons(prev =>
      prev.map(l => (l.id === id ? { ...l, isCurrent: value } : { ...l, isCurrent: value ? false : l.isCurrent })),
    )
    try {
      await setCurrentLesson(courseId, id, value)
      toast.success(value ? "Занятие отмечено как текущее" : "Текущее занятие снято")
      onChanged()
    } catch {
      toast.error("Не удалось обновить текущее занятие")
      setLocalLessons(lessons)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setDeleting(true)
    try {
      await api.delete(`/api/courses/${courseId}/lessons/${deleteId}`)
      toast.success("Занятие удалено")
      setDeleteId(null)
      onChanged()
    } catch {
      toast.error("Ошибка удаления занятия")
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {sorted.length === 0 ? (
        <p className="text-muted-foreground">Нет занятий</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={sorted.map(l => l.id)} strategy={verticalListSortingStrategy}>
            <div className="rounded-lg border bg-card divide-y">
              {sorted.map(l => (
                <SortableRow
                  key={l.id}
                  lesson={l}
                  canManage={canManage}
                  onOpen={id => router.push(`/courses/${courseId}/lessons/${id}`)}
                  onToggleCurrent={handleToggleCurrent}
                  onEdit={id => router.push(`/courses/${courseId}/lessons/${id}/edit`)}
                  onAskDelete={setDeleteId}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <AlertDialog open={deleteId !== null} onOpenChange={o => !o && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить занятие?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={deleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {deleting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
```

> Примечание: присваивание `setLocalLessons(lessons)` в теле рендера — синхронизация пропа (валидный React-паттерн «derive state from props», используется когда `onChanged` обновляет родителя); допустимо, т.к. происходит только при изменении пропа.

- [ ] **Step 2: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: PASS для этого файла (другие ошибки — из задач 8–11).

---

### Task 8: Компонент `DocumentsTab.tsx`

**Files:**
- Create: `CollegeLMS.Next/components/course/DocumentsTab.tsx`

**Interfaces:**
- Consumes: `uploadCourseDocument`, `downloadCourseDocument`, `deleteCourseDocument` (Task 2), `CourseDocumentResponse`
- Produces: `<DocumentsTab courseId: string, canManage: boolean>` (сам грузит список)

- [ ] **Step 1: Создать файл**

```tsx
"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, CourseDocumentResponse } from "@/types"
import api from "@/lib/api"
import { uploadCourseDocument, downloadCourseDocument, deleteCourseDocument } from "@/lib/api"
import { Button } from "@/components/ui/button"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"
import { FileText, UploadCloud, Trash2 } from "lucide-react"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

const MAX_SIZE = 50 * 1024 * 1024

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`
}

export default function DocumentsTab({
  courseId,
  canManage,
}: {
  courseId: string
  canManage: boolean
}) {
  const [documents, setDocuments] = useState<CourseDocumentResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)

  const fetchDocuments = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseDocumentResponse[]>>(`/api/courses/${courseId}/documents`)
      if (res.data.isSuccess && res.data.data) setDocuments(res.data.data)
    } catch {
      // ignore
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    fetchDocuments()
  }, [fetchDocuments])

  const uploadFile = async (file: File) => {
    if (file.size > MAX_SIZE) {
      toast.error("Файл больше 50 МБ")
      return
    }
    setUploading(true)
    try {
      await uploadCourseDocument(courseId, file)
      toast.success("Документ загружен")
      await fetchDocuments()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ошибка загрузки документа")
    } finally {
      setUploading(false)
    }
  }

  const handlePick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) uploadFile(file)
    e.target.value = ""
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setDeleting(true)
    try {
      await deleteCourseDocument(courseId, deleteId)
      toast.success("Документ удалён")
      setDeleteId(null)
      await fetchDocuments()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ошибка удаления")
    } finally {
      setDeleting(false)
    }
  }

  if (loading) return <LoadingSpinner size="lg" className="py-10" />

  return (
    <div className="flex flex-col gap-4">
      {canManage && (
        <div
          onDragOver={e => { e.preventDefault(); setDragging(true) }}
          onDragLeave={() => setDragging(false)}
          onDrop={e => {
            e.preventDefault()
            setDragging(false)
            const file = e.dataTransfer.files?.[0]
            if (file) uploadFile(file)
          }}
          className={`flex flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed p-6 transition-colors ${
            dragging ? "border-primary bg-primary/5" : "border-muted"
          }`}
        >
          <UploadCloud className="size-6 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Перетащите файл сюда или выберите через кнопку
          </p>
          <label>
            <input type="file" className="sr-only" onChange={handlePick} disabled={uploading} />
            <Button type="button" asChild disabled={uploading} className="pointer-events-none">
              <span>{uploading ? "Загрузка..." : "Загрузить документ"}</span>
            </Button>
          </label>
        </div>
      )}

      {documents.length === 0 ? (
        <EmptyState message="Документы ещё не загружены" />
      ) : (
        <div className="rounded-lg border bg-card divide-y">
          {documents.map(d => (
            <div key={d.id} className="flex items-center justify-between gap-3 p-4">
              <button
                type="button"
                className="flex items-center gap-3 min-w-0 text-left hover:text-primary transition-colors"
                onClick={() => downloadCourseDocument(courseId, d.id, d.fileName)}
              >
                <FileText className="size-5 shrink-0 text-muted-foreground" />
                <div className="flex flex-col gap-0.5 min-w-0">
                  <span className="font-medium truncate">{d.fileName}</span>
                  <span className="text-xs text-muted-foreground">
                    {formatSize(d.sizeBytes)} · {new Date(d.createdAt).toLocaleDateString("ru-RU")}
                  </span>
                </div>
              </button>
              {canManage && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-muted-foreground hover:text-fg shrink-0"
                  onClick={() => setDeleteId(d.id)}
                >
                  <Trash2 className="size-4" />
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      <AlertDialog open={deleteId !== null} onOpenChange={o => !o && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить документ?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={deleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {deleting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
```

- [ ] **Step 2: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: PASS для этого файла.

---

### Task 9: Страница курса преподавателя (`courses/[id]/page.tsx`)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/courses/[id]/page.tsx`

- [ ] **Step 1: Правки в файле**

1. Импорты: убрать `AssignmentResponse`; `LectureResponse` → `LessonResponse`; `LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS` → `LESSON_KIND_LABELS, LESSON_KIND_VARIANTS` (из `@/lib/lessonTypes`); добавить `LessonList` из `@/components/lesson/LessonList`, `DocumentsTab` из `@/components/course/DocumentsTab`.
2. `type Tab = "lessons" | "materials" | "groups" | "documents"`.
3. Стейт: `lectures` → `lessons: LessonResponse[]`; удалить `assignments` стейт и `fetchAssignments` (строки 112–122); добавить `documentsTab` не нужен (DocumentsTab сам грузит).
4. `fetchLectures` URL: `/api/courses/${courseId}/lectures` → `/api/courses/${courseId}/lessons`; переименовать в `fetchLessons`.
5. `useEffect` (строка 162): `Promise.all([fetchCourse(), fetchLessons(), fetchMaterials(), fetchCourseGroups(), fetchAvailableGroups()])`.
6. Табы (строка 260): массив `(["lessons", "materials", "groups", "documents"] as Tab[])`, лейблы: `t === "lessons" ? "Занятия" : t === "materials" ? "Материалы" : t === "groups" ? "Группы" : "Документы"`.
7. Блок `tab === "lessons"` (строки 275–332) — заменить целиком на:

```tsx
      {tab === "lessons" && (
        <div className="flex flex-col gap-3">
          {canEdit && (
            <div className="flex justify-end">
              <Button size="sm" onClick={() => router.push(`/courses/${courseId}/lessons/new`)}>
                + Занятие
              </Button>
            </div>
          )}
          <LessonList
            courseId={courseId}
            lessons={lessons}
            canManage={canEdit}
            onChanged={() => {
              fetchLessons()
              fetchCourse()
            }}
          />
        </div>
      )}
```

8. Блок `tab === "materials"` — оставить как есть.
9. После блока materials добавить:

```tsx
      {tab === "documents" && (
        <DocumentsTab courseId={courseId} canManage={canEdit} />
      )}
```

- [ ] **Step 2: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: PASS для этого файла.

---

### Task 10: Страница курса студента (`my/courses/[id]/page.tsx`)

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/my/courses/[id]/page.tsx`

- [ ] **Step 1: Правки в файле**

1. Импорты: убрать `AssignmentResponse`, `SubmissionResponse`, `LECTURE_TYPE_*`; `LectureResponse` → `LessonResponse`; добавить `LessonList`, `DocumentsTab`.
2. `type Tab = "lessons" | "materials" | "documents"`.
3. Стейт: `lectures` → `lessons`; удалить `assignments`, `submissions` стейты.
4. `fetchData` (строки 48–92): заменить на:

```tsx
  const fetchData = useCallback(async () => {
    try {
      const [courseRes, lessonsRes, materialsRes, testRes] = await Promise.all([
        api.get<Result<CourseResponse>>(`/api/courses/${courseId}`),
        api.get<Result<LessonResponse[]>>(`/api/courses/${courseId}/lessons`),
        api.get<Result<MaterialResponse[]>>(`/api/courses/${courseId}/materials`),
        api.get<Result<MyTestResultDto[]>>("/api/my/test-results"),
      ])
      if (courseRes.data.isSuccess && courseRes.data.data) setCourse(courseRes.data.data)
      if (lessonsRes.data.isSuccess && lessonsRes.data.data) setLessons(lessonsRes.data.data)
      if (materialsRes.data.isSuccess && materialsRes.data.data) setMaterials(materialsRes.data.data)
      if (testRes.data.isSuccess && testRes.data.data) {
        setMyTestResults(new Map(testRes.data.data.map(r => [r.testId, r])))
      }
    } catch {
      setError("Ошибка загрузки курса")
    } finally {
      setLoading(false)
    }
  }, [courseId])
```

5. Табы (строки 152–166): `(["lessons", "materials", "documents"] as Tab[])`, лейблы `t === "lessons" ? "Занятия" : t === "materials" ? "Материалы" : "Документы"`.
6. Блок `tab === "lessons"` (строки 168–238) — заменить целиком на:

```tsx
      {tab === "lessons" && (
        <LessonList
          courseId={courseId}
          lessons={lessons}
          canManage={false}
          onChanged={() => {}}
        />
      )}
```

7. Блок `tab === "materials"` — оставить.
8. Добавить `tab === "documents"` → `<DocumentsTab courseId={courseId} canManage={false} />`.
9. Убрать `user` из зависимости `fetchData` (он больше не используется в ней), `submissions` больше нет.
10. Импорт `useAuth` оставить (нужен для header/logout).

- [ ] **Step 2: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: PASS для этого файла.

---

### Task 11: Удаление UI заданий и «Моих работ»

**Files:**
- Delete: `app/(authenticated)/courses/[id]/assignments/` (вся папка: new, [assignmentId], [assignmentId]/submissions)
- Delete: `app/(authenticated)/my/submissions/page.tsx`
- Modify: `app/(authenticated)/layout.tsx`

- [ ] **Step 1: Удалить файлы**

```bash
cd CollegeLMS.Next
git rm -r "app/(authenticated)/courses/[id]/assignments"
git rm "app/(authenticated)/my/submissions/page.tsx"
```

- [ ] **Step 2: Убрать пункт меню**

В `app/(authenticated)/layout.tsx` из `studentMenu` удалить строку `{ href: "/my/submissions", label: "Мои работы", icon: FileCheck },` и убрать `FileCheck` из импорта lucide (строка 9), если он больше не используется.

- [ ] **Step 3: Проверить отсутствие ссылок**

```bash
grep -rn "assignments\|submissions\|lectures\|lectureId\|LectureForm\|lectureTypes" CollegeLMS.Next/app CollegeLMS.Next/components CollegeLMS.Next/lib CollegeLMS.Next/types --include=*.tsx --include=*.ts | grep -v "node_modules" || echo "ЧИСТО"
```

Ожидается: только легитимные `TestAssignment`/`assignments` в контексте назначений теста (`admin/testing`), либо пусто (кроме `/lessons`).
Если остались ссылки на `/assignments`/`/lectures` — поправить вручную.

- [ ] **Step 4: Проверка**

Run: `npx tsc --noEmit` (docker)
Expected: PASS.

---

### Task 12: e2e-тесты

**Files:**
- Modify: `CollegeLMS.Next/e2e/course-detail.spec.ts`, `CollegeLMS.Next/e2e/dashboards.spec.ts`, `CollegeLMS.Next/e2e/courses.spec.ts` (если там есть моки lectures/assignments)

- [x] **Step 1: `e2e/course-detail.spec.ts`**

Заменить во всех моках:
- `course` mock: `lectureCount: 2, assignmentCount: 1` → `lessonCount: 2, documentCount: 1`
- `**/api/courses/c1/lectures**` → `**/api/courses/c1/lessons**`; данные занятий:

```json
{ "id": "l1", "courseId": "c1", "title": "Введение", "content": "Текст лекции", "order": 1, "kind": "Lecture", "isCurrent": false, "testId": null, "testTitle": null },
{ "id": "l2", "courseId": "c1", "title": "Основы", "content": "Основной материал", "order": 2, "kind": "Practice", "isCurrent": false, "testId": null, "testTitle": null }
```

- Убрать моки `**/api/courses/c1/assignments**` (3 места), добавить мок `**/api/courses/c1/documents**`:

```json
{ "isSuccess": true, "data": [], "errorMessage": null, "statusCode": 200 }
```

- `materials` mock: `lectureId: null, assignmentId: null` → `lessonId: null`
- Тест «shows add lecture button for teacher» (строка 185): `page.getByRole("button", { name: "Добавить лекцию" })` → `{ name: "Занятие" }` (кнопка «+ Занятие»).
- Добавить тест:

```ts
  test("shows kind badge and current lesson for student", async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u3", email: "student@collegelms.ru", fullName: "Студент", role: "Student" })
      )
    })
    await page.route("**/api/courses/c1", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupNames: "Группа А", status: "Active", lessonCount: 1, documentCount: 0 },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/lessons**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "l1", courseId: "c1", title: "Введение", content: "Текст", order: 1, kind: "Lecture", isCurrent: true, testId: null, testTitle: null },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/materials**", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }) })
    )
    await page.route("**/api/courses/c1/documents**", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }) })
    )
    await page.route("**/api/my/test-results**", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }) })
    )

    await page.goto("/courses/c1", { waitUntil: "networkidle" })
    await expect(page.getByText("Сейчас идёт")).toBeVisible()
    await expect(page.getByText("Лекция")).toBeVisible()
  })
```

- [x] **Step 2: `e2e/dashboards.spec.ts`**

Удалить весь `test.describe("My submissions", ...)` (строки 112–142) и `test.describe("My submissions (no auth)", ...)` (строки 144–149).

- [x] **Step 3: `e2e/courses.spec.ts`**

Просмотреть и заменить любые моки `lectures`/`assignments` на `lessons` по образцу Step 1 (поля `lessonCount`/`documentCount`, мок `documents`).

- [x] **Step 4: Прогнать e2e**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next node:20-alpine sh -c "npx playwright test e2e/course-detail.spec.ts e2e/dashboards.spec.ts e2e/courses.spec.ts"
```

Expected: PASS (Playwright-браузеры уже установлены — см. `~/.cache/ms-playwright` на хосте; при отсутствии браузеров в контейнере — использовать `npx playwright test --project=chromium` с `PW_TEST_CONNECT_WS_ENDPOINT` или запускать на хосте через node_modules).

> Если в docker нет браузеров: запускать тесты на хосте через локальный node_modules нельзя (node отсутствует) — тогда проверить `docker run node:20-alpine sh -c "npx playwright install chromium"` с пробросом `~/.cache/ms-playwright` (монтировать `/root/.cache/ms-playwright`).

- [x] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: занятия — роуты /lessons, dnd-сортировка, «Сейчас идёт», документы курса (M3b)"
```

---

### Task 13: Финальная проверка и merge

- [x] **Step 1: `npm run build` (docker)**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next -e NEXT_PUBLIC_API_URL=http://localhost:8080 node:20-alpine sh -c "npm run build"
```

Expected: `Compiled successfully` / `Route (app)` список без ошибок.

- [x] **Step 2: Полный e2e-прогон**

```bash
docker run --rm -v /home/user1/CollegeLMS:/src -w /src/CollegeLMS.Next node:20-alpine sh -c "npx playwright test"
```

Expected: PASS (с учётом обновлённых спеков).

- [x] **Step 3: Smoke на compose**

Поднять/пересобрать `collegelms-next` (docker compose up --build -d), открыть `http://localhost/` → войти преподавателем → курс → проверить: табы (Занятия/Материалы/Группы/Документы), dnd-перетаскивание, тумблер «Сейчас идёт», создание занятия (kind), загрузка/скачивание/удаление документа; студентом: бейдж «Сейчас идёт», документы read-only. Адаптивность 1366×768 и ~393px (DevTools).

- [x] **Step 4: Роуты 404**

Проверить, что `/courses/{id}/lectures/*`, `/courses/{id}/assignments/*`, `/my/submissions` отдают 404 (next not-found/страница логина).

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "test: e2e под новую модель занятий (M3b)"
```

- [x] **Step 6: Sync и merge**

```bash
git fetch origin && git pull --rebase origin master
git checkout master && git pull --rebase origin master
git merge feature/m3b-lessons-frontend --no-edit
git push origin master
```

Expected: fast-forward merge, push в master → CD на VPS (миграций не будет — бэкенд уже применён в M3a).
