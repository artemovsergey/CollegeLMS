# M3b «Занятия: фронтенд» — дизайн

> Дата: 2026-08-19
> Статус: утверждён
> Бэкенд: M3a задеплоен (lessons, reorder, isCurrent, course_documents; задания удалены)

## Цель

Перевести фронтенд CollegeLMS.Next на новую модель занятий: роуты `/lessons`, единый список занятий с dnd-сортировкой и бейджем «Сейчас идёт», вкладка «Документы» курса, удаление UI заданий. После M3b фронт и бэкенд снова согласованы (на проде сейчас фронт обращается к удалённым эндпоинтам).

## Ограничения

- Меняем ТОЛЬКО `CollegeLMS.Next/` (+ e2e-спеки). Бэкенд не трогаем.
- Tailwind CSS v4, shadcn/ui-примитивы, Lucide-иконки (по DESIGN.md)
- Ручные мапперы полей, без библиотек кроме новых `@dnd-kit/*`
- Все тексты на русском
- Адаптивность: 1366×768 (Toshiba A665) и ~393px (Xiaomi Mi 9 SE), touch-target ≥44px
- Новые поля бэкенда: `LessonResponse { id, courseId, title, content, order, kind, isCurrent, testId, testTitle }`; `CourseDocumentResponse { id, courseId, fileName, contentType, sizeBytes, createdAt }`; `MaterialResponse.lessonId`; `CourseResponse.lessonCount, documentCount`; `CourseProgressResponse` без assignment-полей

## 1. Маршруты (App Router)

Переименовать (git mv):
- `app/(authenticated)/courses/[id]/lectures/new/page.tsx` → `.../lessons/new/page.tsx`
- `app/(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx` → `.../lessons/[lessonId]/page.tsx` (просмотр занятия + управление тестом)
- `app/(authenticated)/courses/[id]/lectures/[lectureId]/edit/page.tsx` → `.../lessons/[lessonId]/edit/page.tsx`
- `app/(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx` → `.../lessons/[lessonId]/test/page.tsx`

Удалить:
- `app/(authenticated)/courses/[id]/assignments/` (new, [assignmentId], [assignmentId]/submissions)
- `app/(authenticated)/my/submissions/page.tsx`
- `components/LectureForm.tsx` (→ заменяется на `LessonForm.tsx`)

Меню: в `app/(authenticated)/layout.tsx` убрать пункт «Мои работы» (`/my/submissions`, строка 15). Убедиться, что `/my/submissions` не упоминается в других местах (`lib/menus.ts`, e2e).

Все ссылки внутри фронта на `/courses/{id}/lectures/...` и `/courses/{id}/assignments/...` переписать (внутренние ссылки в `courses/[id]/page.tsx`, `my/courses/[id]/page.tsx` и др.).

## 2. Типы (`types/index.ts`)

- `LectureResponse` → `LessonResponse`:
  ```ts
  export interface LessonResponse {
    id: string;
    courseId: string;
    title: string;
    content: string;
    order: number;
    kind: "Lecture" | "Practice" | "SelfStudy";
    isCurrent: boolean;
    testId: string | null;
    testTitle: string | null;
  }
  ```
- Удалить `AssignmentResponse`, `SubmissionResponse` (строки 116–136).
- `MaterialResponse`: заменить `lectureId: string | null; assignmentId: string | null` на `lessonId: string | null`.
- `CourseResponse`: `lectureCount: number` → `lessonCount: number`; удалить `assignmentCount`; добавить `documentCount: number`.
- `CourseProgressResponse`: удалить `totalAssignments`, `completedAssignments`.
- `CreateTestRequest`: `lectureId` → `lessonId`.
- Добавить `CourseDocumentResponse`:
  ```ts
  export interface CourseDocumentResponse {
    id: string;
    courseId: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    createdAt: string;
  }
  ```
- `lib/lectureTypes.ts` → `lib/lessonTypes.ts` с `LESSON_KIND_LABELS = { Lecture: "Лекция", Practice: "Практика", SelfStudy: "Самостоятельная работа" }`.

## 3. API-слой (`lib/api.ts`)

- Заменить в вызовах URL: `/lectures` → `/lessons`; удалить вызовы `/assignments*`, `/submissions*`, `/my/submissions`.
- Новые функции:
  - `uploadCourseDocument(courseId, file: File): Promise<CourseDocumentResponse>` — `POST /api/courses/{id}/documents`, FormData (`file`), заголовок `Content-Type: multipart/form-data` (не задавать вручную — axios сам).
  - `getCourseDocuments(courseId)`, `downloadCourseDocument(courseId, id)` (window.open с bearer? — для скачивания использовать GET с `responseType: "blob"` и ручным созданием ObjectURL, т.к. нужен Authorization; либо передавать токен query — НЕ БЕЗОПАСНО. Выбор: blob + ObjectURL + revoke).
  - `deleteCourseDocument(courseId, id)`.
  - `reorderLessons(courseId, lessonIds: string[])` — `PUT /api/courses/{id}/lessons/reorder`.
  - `setCurrentLesson(courseId, id, isCurrent)` — `PATCH /api/courses/{id}/lessons/{id}/current`.

## 4. Форма занятия — `components/LessonForm.tsx`

- По структуре текущего `LectureForm` (markdown-разметка/предпросмотр через ReactMarkdown).
- Поле типа: `kind` (Select из `LESSON_KIND_LABELS`), значения: `Lecture | Practice | SelfStudy`.
- POST `POST /api/courses/{id}/lessons` (тело: `{ title, content, kind, testId }`), PUT `PUT /api/courses/{id}/lessons/{id}`.
- После создания — `router.push(/courses/${courseId}/lessons/${lesson.id})`.
- `app/(authenticated)/courses/[id]/lessons/new/page.tsx` и `[lessonId]/edit/page.tsx` используют `LessonForm`.

## 5. Таб «Занятия» (преподаватель `courses/[id]/page.tsx`)

- `Tab` тип: `"lessons" | "materials" | "groups" | "documents"`.
- Список занятий — новый компонент `components/lesson/LessonList.tsx` (для преподавателя) и `components/lesson/LessonListStudent.tsx` (для студента), либо один компонент с пропом `canManage`.
- Карточка занятия: заголовок, бейдж kind, `testTitle` (если есть — иконка FileCheck), при `isCurrent` у студента — бейдж «Сейчас идёт» (голубой/College Blue).
- Преподаватель:
  - dnd-сортировка: установить `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`. `DndContext` + `SortableContext` (vertical), drag-handle (GripVertical), `onDragEnd` → локальный optimistic-reorder массива → `reorderLessons()` → при ошибке откат + тост (sonner), при успехе — тост «Порядок сохранён».
  - Тумблер «Сейчас идёт»: Switch (shadcn `components/ui/switch.tsx`, уже есть) с текстовым лейблом «Сейчас идёт» у каждого занятия → `setCurrentLesson()` → обновить `isCurrent` локально (не более одного true; бэкенд гарантирует).
  - Кнопки «Редактировать», «Удалить» (удаление с AlertDialog), «+ Занятие» (ссылка на `lessons/new`).
  - Сортировка списка по `order` перед отрисовкой.
- Студент: список без dnd, без кнопок управления, клик по занятию — на `/courses/{courseId}/lessons/{lessonId}` (как сейчас студент ходит в ту же зону `/courses/...`).

## 6. Таб «Документы» (преподаватель и студент)

Новый компонент `components/course/DocumentsTab.tsx` (универсальный, проп `canManage`):
- Загрузка (только canManage): drag&drop-зона + кнопка «Загрузить документ» (input type=file), лимит 50 МБ (проверка `file.size <= 50*1024*1024`, иначе тост-ошибка), `uploadCourseDocument()` → обновить список + тост.
- Список: таблица/строки — иконка FileText, имя файла (клик — скачивание), размер (форматирование КБ/МБ), дата (locale ru), кнопка удаления (только canManage) с AlertDialog подтверждением.
- Скачивание через blob (см. §3), имя файла из ответа Content-Disposition или `fileName` из DTO.
- Пустое состояние: EmptyState «Документы ещё не загружены».
- Доступ: GET documents — любой авторизованный с доступом к курсу; загрузка/удаление — Admin/Teacher (бэкенд проверяет права).

## 7. Прогресс и дашборды

- `app/my/courses/[id]/progress/page.tsx` — убрать блоки/поля assignments (типы уже обновлены).
- `my/courses/[id]/page.tsx` (студент): таб «Занятия» — карточки с бейджем «Сейчас идёт», таб «Документы»; убрать вызовы `GET /assignments/{aid}/submissions?studentId=` и карточку «Задания».
- `courses/[id]/page.tsx` (преподаватель): убрать «+ Задание» и блок заданий; карточки «Статистика» оставить (test-results).

## 8. Тесты (e2e)

- `e2e/course-detail.spec.ts`, `e2e/courses.spec.ts`, `e2e/dashboards.spec.ts` — обновить моки: `/api/courses/{id}/lessons` (kind, isCurrent), `/api/courses/{id}/documents`, `/api/courses/{id}/materials` (lessonId); убрать моки `/assignments*`, `/my/submissions`.
- Добавить e2e-тест: преподаватель создаёт занятие (kind=Practice), видит его в списке с бейджем «Практика»; студент видит бейдж «Сейчас идёт» (мок isCurrent=true).
- dnd-перетаскивание — НЕ покрываем Playwright (ненадёжно), проверка вручную на 1366px и 393px.
- `npm run build` (в docker) и `npm run dev` — smoke всех страниц курса (teacher + student).

## 9. Критерии готовности

- [ ] `npm run build` проходит (docker, CollegeLMS.Next)
- [ ] `npm run dev` — страницы курса рендерятся: список занятий (dnd), форма занятия (kind), документы (upload/download/delete), тест студента
- [ ] `npx playwright test` — e2e проходят
- [ ] Роуты `/lectures/*`, `/assignments/*`, `/my/submissions` отсутствуют (404 в Next)
- [ ] В UI нет упоминаний «Задание»/«Мои работы» (кроме назначений теста TestAssignment — они остаются)
- [ ] Проверены 1366×768 и ~393px
- [ ] Merge в master → CD на VPS

## Файлы

- Дизайн-док M3: `docs/superpowers/specs/2026-08-17-m3-lessons-design.md`
- План M3a: `docs/superpowers/plans/2026-08-17-m3a-lessons-backend.md`
- Этот дизайн: `docs/superpowers/specs/2026-08-19-m3b-lessons-frontend-design.md`
