# Дизайн: UI-фиксы, курсы, занятия, расписание, уведомления

Дата: 2026-08-16
Статус: утверждён пользователем
Масштаб: 5 милстоунов (M1–M5), ветки `feature/*`, полный цикл по AGENTS.md

## Контекст

Баг-репорт и требования пользователя по CollegeLMS. Исследование кодовой базы выявило корневые причины:

1. **«0 курсов в моих курсах»**: фронтенд `app/(authenticated)/courses/page.tsx:50` шлёт `?teacherId=${user.id}` (User.Id), бэкенд `CourseService.GetAllAsync` (CourseService.cs:44-45) фильтрует по `Teacher.Id` сущности — пересечение пустое. Дашборд фильтрует корректно (`DashboardService.cs:24`).
2. **Ошибка при клике на логотип**: `(authenticated)/layout.tsx` ведёт всех на `/my/dashboard` (студенческая, `[Authorize(Roles="Student")]`) — преподавателю 403.
3. **Курсы не «мои»**: страница `/courses` называется «Курсы», не отдаёт преподавателю его курсы (см. п.1).
4. `Lecture` в коде называется «лекция», хотя покрывает все типы занятий; `Assignment`/`AssignmentSubmission` — удаляются по решению пользователя.

## Решения пользователя (зафиксированы)

- Быстрый вход: селект роли + вход по кнопке (убрать подсказки `(admin)` из опций).
- «Алло удар» = **Loader**: центрировать по вертикали/горизонтали.
- Задания (Assignment + Submission + «Мои работы» + баллы/прогресс) — **удалить полностью**, включая студенческую часть.
- Вкладка «Документация» — видна **только преподавателю**.
- Уведомления — только инфраструктура (без автогенерации событий).
- Категория преподавателя: Без категории / Первая / Высшая.
- Копирование курса: занятия + материалы + документация (без групп/тестов/заданий).
- Активность курса: отдельное поле `IsActive`.
- Учебные недели: дата старта семестра в конфиге.
- Архитектура: перенос `Lecture → Lesson`; порядок M1→M5.

---

## M1 — UI-фиксы + профиль + аватары + баг курсов

Фокус: быстрые видимые исправления, профиль, аватары. Frontend-heavy + небольшой backend.

### Backend

**Сущности:**
- `User.AvatarPath` (string?, null — нет аватара). Все роли могут иметь аватар, но менять его может только владелец НЕ роли Student (см. ниже).
- `Teacher.Category` (enum `TeacherCategory`: `None` («Без категории») / `First` («Первая») / `Higher` («Высшая»)), HasConversion<string> + HasMaxLength.

**Миграция:** `AddUserAvatarAndTeacherCategory`.

**Endpoints:**
- `PUT /api/auth/profile` — расширить `UpdateProfileRequest`: для преподавателя дополнительно принимаются `CyclicalCommission` и `Category` (обновляет связку User + Teacher). Для остальных — как раньше (FullName, Email).
- `POST /api/profile/avatar` (multipart, `[Authorize(Roles = "Teacher,Admin,Dispatcher")]` — Student менять аватар НЕ может) — ImageSharp: ресайз до 256×256 (центрированный кроп), JPEG, сохраняется `uploads/avatars/{userId}.jpg`, возвращает AvatarUrl. Старый файл удаляется при замене.
- `ProfileResponse` — добавить `AvatarUrl`; `TeacherData` — добавить `Category` (строка на русском).

**Доступ:** владелец профиля. GET /api/auth/profile уже отдаёт свои данные.

### Frontend

1. **Логин** (`app/login/page.tsx`):
   - `QUICK_LOGINS`: опции без подсказок логина — просто «Администратор», «Преподаватель», «Студент», «Диспетчер».
   - Мобильный логотип: `maxHeight: 4rem → 6rem` (96px).
   - Убрать красные звёздочки: `FormField` — новый prop `showAsterisk` (default true), на логине/пароле передать `false`. Валидация («Введите логин») сохраняется.

2. **Шапка** (`components/AuthenticatedShell.tsx`):
   - Текст: «Колледж связи» вместо «Ставропольский колледж связи» + «имени В.А. Петрова».
   - Клик по логотипу → по роли: Admin→`/admin`, Dispatcher→`/schedule`, Teacher→`/teacher/dashboard`, Student→`/my/dashboard`.
   - Логотип/аватар отцентрированы по вертикали.

3. **Левое меню** (`(authenticated)/layout.tsx` + `lib/menus.ts`): удалить пункт «Настройки» (и секцию «Профиль» целиком) у всех ролей; у преподавателя «Курсы» → «Мои курсы».

4. **Правое меню**: аватар (или инициалы), ФИО, **email** (вместо login), бейдж роли; пункты: Профиль, Сменить пароль (dialog), Уведомления (Bell + бейдж-счётчик — бейдж будет жить в M5, сейчас без счётчика), Выйти.

5. **Лоадеры**: `LoadingSpinner` + экраны загрузки — центрировать по вертикали и горизонтали (flex min-h) на ключевых страницах.

6. **Профиль** (`app/my/profile/page.tsx`):
   - Убрать секцию «Смена пароля».
   - «Основные данные»: ФИО, Email (редактируется), загрузка аватара (кнопка, превью, только не-Student).
   - «Данные преподавателя»: Цикловая комиссия (Input), Категория (Select: Без категории/Первая/Высшая) — редактируются, сохраняются через PUT /api/auth/profile.
   - `ProfileResponse.AvatarUrl` — показывается в шапке.

7. **Баг курсов**: `courses/page.tsx` — не слать `teacherId` для роли Teacher (бэкенд автофильтрует по Teacher.Id). Заголовок «Мои курсы» для преподавателя, «Курсы» для админа.

### Тесты
- Unit: AuthService (profile update с категорией), TeacherService.
- Интеграционные: PUT profile (teacher category), POST avatar (успех/Student 403).
- E2E: логин (без звёздочек), клик логотипа преподавателем → /teacher/dashboard.

---

## M2 — Курсы: CRUD, активность, копирование, соавторы

### Backend

- `Course.IsActive` (bool, default true) + миграция.
- `CourseAuthor` (Guid Id, CourseId, TeacherId; UNIQUE(CourseId, TeacherId)) — соавторы. Владелец курса (`Course.TeacherId`) — автор по умолчанию, в CourseAuthors не дублируется.
- Helper `CanManageCourse(course, teacherId)` / `GetManagedCourseIds(teacherId)` в `CourseService` (или отдельном `CourseAccessService`): все проверки `course.TeacherId != teacher.Id` в LectureService, MaterialService, TestingService, (SubmissionService удаляется в M3) заменяются на проверку владельца ИЛИ соавтора.
- `GET /api/courses` — для роли Teacher: `TeacherId == teacher.Id OR Author в CourseAuthors`. Параметр `teacherId` убираем из фронта.
- `POST /api/courses/{id}/duplicate` — копирует: Title (+ « (копия)»), Description, занятия (Title, Content, Type, Order), материалы курса. НЕ копирует: группы (CourseGroup), тесты, IsCurrent-метку занятия, связки TestId (обнуляются). Автор = текущий пользователь. `IsActive=false` (черновик для правки). Документация (CourseDocument) появится в M3 — тогда же добавится в duplicate.
- `PATCH /api/courses/{id}/active` `{ isActive: bool }` — **только владелец** курса.
- Удаление курса (`DELETE /api/courses/{id}`) — **только владелец**; соавторы управляют занятиями/материалами, но не удаляют курс и не меняют активность. Менять активность/удалять может и администратор.

### Frontend

- «Мои курсы» (преподаватель): таблица + переключатель «Активен/Неактивен» (Switch), кнопка «Удалить» с AlertDialog, кнопка «Дублировать».
- Панель преподавателя: только `IsActive == true`.
- Форма курса (new/edit): Select «Создать на основе курса» (список моих курсов), мультиселект соавторов (список преподавателей), поле названия/описания.

---

## M3 — Занятия (крупнейший милстоун)

### Backend

**Удаление заданий (полностью):**
- Drop: таблицы `assignment_submissions`, `assignments`; сущности `Assignment`, `AssignmentSubmission`; конфигурации; `MaterialService` — поле `AssignmentId` у CourseMaterial, DTO `AssignmentResponse`; `AssignmentController`, `SubmissionController`; всё в DTO/мапперах/сервисах; тесты unit/integration/fixtures Bogus; фронт: `AssignmentService`? (нет такого — api/assignments.ts), страницы `/my/submissions` + пункт меню «Мои работы», страницы создания/редактирования задания, UI сдачи в студенческих курсах, счётчики заданий (lectureCount/assignmentCount → lectureCount/lessonCount).
- `CourseResponse`: `lectureCount` → `lessonCount`, убрать `assignmentCount`/`assignmentCount` поля.

**Lecture → Lesson:**
- Rename entity `Lecture` → `Lesson` (файл, таблица `lectures` → `lessons` через миграцию с `RenameTable`), `LectureType` остаётся (`Lecture/Practice/SelfStudy`), `LessonService`, `LessonController` (маршруты: `/api/courses/{courseId}/lessons`), DTO `Lesson{Request,Response}`, мапперы, тесты, фронтенд-типы и страницы (`lessons/new`, `lessons/[lessonId]`).

**Позиция / порядок:**
- `POST /api/courses/{id}/lessons` — тело + `position` (enum: `Beginning | AfterLesson{id?}` или int `afterOrder`): вставить в начало / после указанного занятия, переиндексировать `Order` 1..N.
- `PATCH /api/courses/{id}/lessons/reorder` `{ lessonIds: Guid[] }` — новый порядок целиком (для drag-and-drop).
- Проверка корректности: все id принадлежат курсу, нет дублей.

**Текущее занятие:**
- `Lesson.IsCurrent` (bool). Уникально на курс: при установке сбрасывать остальные (UPDATE ... SET is_current=false WHERE course_id=@courseId). CHECK через сервис (не БД).
- `POST /api/courses/{id}/lessons/{lessonId}/current` — отметить текущим; `DELETE .../current` — снять.

**Документация:**
- `CourseDocument` (Guid Id, CourseId, FileName, FilePath, FileSize, MimeType, CreatedAt) + конфигурация.
- `POST /api/courses/{id}/documents` (multipart, ≤50 МБ, canEdit), `GET .../documents` (список, только canEdit), `GET /api/documents/{id}/download` (только canEdit), `DELETE /api/documents/{id}` (canEdit).
- Хранение: `uploads/documents/{courseId}/{guid}_{name}` через FileService.

**Seed:** уроки уже сидятся из `import/mdk0901_course.json` (90 занятий) — обновить сид под Lesson.

### Frontend

- Вкладки курса: «Занятия» / «Материалы» / «Группы» / «Документация» (только canEdit).
- «+ Занятие» (dialog или страница): Название, Тип (лекция/практика/самостоятельная), Содержание, Позиция (в начало / после занятия N), кнопка «Отметить текущим».
- Список занятий: пронумерован, drag-and-drop через @dnd-kit (core + sortable, добавить в package.json), grip-handle (GripVertical), touch-активация (y=2 / long-press), после drop → PATCH reorder + локальная оптимистичная перестановка.
- Текущее занятие: бейдж «Текущее» + подсветка (ring/фон), у студента — read-only.
- Документация: список файлов, загрузка (input file), скачивание, удаление.
- Студенческий вид курса: только Занятия + Материалы, занятия с типами и подсветкой текущего.

---

## M4 — Расписание

### Backend

- **Seed** (DataSeeder, идемпотентно): группы «ИП-235», «ИВ-234», «ИП-232», «ИП-236» (курс 2–3); записи `ScheduleEntry` для тестового преподавателя (Иванов / teacher@collegelms.ru) по модели scheduleTeacher: день недели 1–5, пара 1–6, предмет (МДК 09.01, МДК 09.02), группа, weeks (список недель 1–22), тип занятия.
- **Конфиг**: `Schedule:SemesterStart` (дата) в appsettings.json; helper `ScheduleWeekCalculator` — номер текущей учебной недели (1..N, 22), clamp.
- `GET /api/teacher/dashboard` — добавить в ответ `TodaySchedule` (записи на текущий день недели для текущего преподавателя, с именами групп).
- `GET /api/schedule` — поддержать параметр `week` (фильтр Weeks.Contains(week)); `view=calendar` расширить данными недели.

### Frontend

- **Панель преподавателя**: карточка «Расписание на сегодня» (пары, время, группа, предмет).
- **«Моё расписание»** (пункт меню преподавателя, страница `/teacher/schedule`): по умолчанию — текущий преподаватель, текущая учебная неделя; фильтры: преподаватель (любой), группа (любая), день; недельный вид Пн–Пт × пары 1–6, подсветка занятий текущей недели (из weeks), кнопки ◀/▶ недели, «Сегодня».

---

## M5 — Уведомления (инфраструктура)

### Backend
- `Notification` (Guid Id, Guid UserId, string Title, string Message, bool IsRead, DateTime CreatedAt) + конфигурация + миграция.
- `NotificationController` (`/api/notifications`): `GET /` (мои, сортировка по CreatedAt desc, пагинация), `GET /unread-count`, `PATCH /{id}/read`, `PATCH /read-all`, `DELETE /{id}` (опционально).
- Сервис `NotificationService` с методом `CreateAsync(userId, title, message)` (пока вызывается вручную/тестами; автогенерация событий — следующий этап).

### Frontend
- Пункт «Уведомления» в правом меню: Bell + бейдж unread-count (polling при открытии меню), страница `/notifications`: список, «отметить прочитанным», «прочитать все», пустое состояние.

---

## Сквозные решения

- Каждый милстоун: ветка `feature/{area}`, фазы по AGENTS.md (backend → docs → tests → frontend → e2e → devops → merge), гейты G1/G2/G3.
- Все комментарии и сообщения — на русском.
- Иконки: lucide-react (уже в проекте), пользовательский запрос на замену текстовых обозначений — по ходу работы.
- Адаптивность: таблицы на 393px — overflow-x-auto; логотип/аватар — проверка на 1366×768 и 393px.
- E2E: обновить устаревшие спеки (auth.spec.ts ожидает старые подписи, course-detail.spec.ts — «Добавить лекцию»).