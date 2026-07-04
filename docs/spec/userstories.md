# CollegeLMS — User Stories

Декомпозиция task.md на пользовательские истории. Каждая история — атомарная единица работы для одного Agent.

---

## Общие критерии приемки (для всех User Stories)

### Авторизация и доступ

| Код | Критерий |
|-----|----------|
| AUTH-1 | `401 Unauthorized` если запрос без JWT токена |
| AUTH-2 | `403 Forbidden` если у пользователя нет прав на операцию |
| AUTH-3 | `404 Not Found` если ресурс не существует (не раскрывать существование) |

### Валидация

| Код | Критерий |
|-----|----------|
| VAL-1 | `400 Bad Request` при невалидных данных (FluentValidation) |
| VAL-2 | Сообщения об ошибках на русском языке |
| VAL-3 | `[Required]`, `[MaxLength]` на всех строковых полях |

### Пагинация (для списков)

| Код | Критерий |
|-----|----------|
| PAG-1 | Параметры `?page={n}&pageSize={m}` (по умолчанию page=1, pageSize=20) |
| PAG-2 | Максимальный pageSize = 100 |
| PAG-3 | Ответ содержит `{ items, totalCount, page, pageSize, totalPages }` |

### UI состояния (для страниц с интерфейсом)

| Код | Критерий |
|-----|----------|
| UI-1 | **Loading**: скелетон/спиннер пока данные загружаются |
| UI-2 | **Empty**: "Нет данных" когда список пуст |
| UI-3 | **Error**: сообщение об ошибке с кнопкой "Повторить" |
| UI-4 | **Success**: тост/уведомление после успешной мутации |

---

## 1. SiteService — Сервис сайта колледжа

### UC-1: Посетитель может просмотреть главную страницу

**Acceptance Criteria:**
- [ ] Главная страница загружается с информацией о колледже
- [ ] Дизайн соответствует цветам логотипа (DESIGN.md)
- [ ] Страница адаптивна (мобильная/десктопная версия)
- [ ] Меню сайта отображается корректно (иерархия страниц из WP)
- [ ] Хедер: логотип, название колледжа, навигация
- [ ] Футер: контакты, ссылки, копирайт
- [ ] UI-1, UI-2, UI-3

**Dependencies:** SiteService, WordPress import data

### UC-2: Посетитель может просмотреть список новостей

**Acceptance Criteria:**
- [ ] GET /api/news возвращает список новостей с пагинацией
- [ ] Новости отсортированы по дате (сначала новые)
- [ ] Каждая новость содержит: заголовок, дату, анонс (первые N символов), изображение (preview)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-1, AUTH-2, AUTH-3
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/news?page={n}&pageSize={m}` — список новостей

**Dependencies:** SiteService, WordPress import

### UC-3: Посетитель может просмотреть конкретную новость

**Acceptance Criteria:**
- [ ] GET /api/news/{id} возвращает полную новость (заголовок, дата, полный текст, изображения)
- [ ] AUTH-3 (404 если новость не найдена)
- [ ] UI-1, UI-3

**API:** `GET /api/news/{id}` — получить новость по ID

### UC-4: Администратор может управлять новостями (CRUD)

**Acceptance Criteria:**
- [ ] POST /api/news — создать новость (только Admin)
- [ ] PUT /api/news/{id} — редактировать новость
- [ ] DELETE /api/news/{id} — мягкое удаление (IsDeleted = true, не затрагивая CreatedAt/UpdatedAt)
- [ ] Только пользователи с ролью Admin могут выполнять эти действия (AUTH-2)
- [ ] Валидация: заголовок обязателен и ≤ 255 символов (VAL-1, VAL-2, VAL-3)
- [ ] UI-1, UI-4

**API:**
- `POST /api/news` — создать новость
- `PUT /api/news/{id}` — обновить новость
- `DELETE /api/news/{id}` — мягко удалить новость

**Dependencies:** AuthService

### UC-5: Посетитель может просмотреть страницы сайта

**Acceptance Criteria:**
- [ ] GET /api/pages возвращает список публичных страниц (только isPublished = true)
- [ ] GET /api/pages/{slug} возвращает конкретную страницу по slug
- [ ] AUTH-3 (404 если slug не существует)
- [ ] Страницы имеют иерархическую структуру (parentId/children)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/pages?page={n}&pageSize={m}` — список страниц
- `GET /api/pages/{slug}` — страница по slug

### UC-6: Администратор может импортировать данные с WordPress

**Acceptance Criteria:**
- [ ] POST /api/site/import — запустить импорт (админ)
- [ ] Импорт делится на 2 этапа:
  - Этап 1 — структура: страницы, категории, меню (из `import/wp_structure_raw.json`)
  - Этап 2 — контент: тело страниц и новостей, изображения, даты (через WordPress REST API: `/wp-json/wp/v2/pages?per_page=100`, `/wp-json/wp/v2/posts?per_page=100`, медиа через `/wp-json/wp/v2/media?per_page=100`)
- [ ] Скрипт импорта переносит: pages (211), posts/новости (3720), categories (10), tags, users, медиа
- [ ] Импорт идемпотентен (повторный запуск не создаёт дубликатов — проверка по WP ID)
- [ ] Медиа-файлы загружаются и сохраняются локально (FileService)
- [ ] Меню: импорт структуры страниц с parent/child для построения навигации
- [ ] Прогресс импорта: `{ total, processed, errors }`
- [ ] UI-1, UI-3, UI-4

**Источники:**
- Структура: `import/wp_structure_raw.json` (уже выгружен)
- Полный контент: WordPress REST API (скрипт-парсер в `scripts/parsing_stvcc.py`)
- Пример API-запроса: `GET /wp-json/wp/v2/pages?per_page=100&_fields=id,slug,title,link,parent,content,date,featured_media`

**Dependencies:** WordPress REST API (live или кэш в `import/`), FileService

---

## 2. AuthService — Сервис аутентификации и авторизации

### UC-7: Пользователь может войти в систему

**Acceptance Criteria:**
- [ ] POST /api/auth/login — принимает email + password
- [ ] Возвращает JWT токен (access token, срок действия 24ч)
- [ ] Неверный email или пароль → 401 (AUTH-1)
- [ ] Пароль хешируется BCrypt
- [ ] Rate limiting: не более 5 попыток в минуту с одного IP
- [ ] VAL-1, VAL-2, VAL-3 (email — валидный email, password — не пустой)

**API:** `POST /api/auth/login`
- Request: `{ email, password }`
- Response: `{ token, user: { id, email, fullName, role } }`

### UC-8: Пользователь может выйти из системы

**Acceptance Criteria:**
- [ ] POST /api/auth/logout — инвалидирует токен (добавляет в Redis blacklist)
- [ ] Запрос с невалидным/истёкшим токеном → 401 (AUTH-1)
- [ ] UI-4 (тост "Вы вышли из системы")

**API:** `POST /api/auth/logout`

**Dependencies:** Redis

### UC-9: Пользователь может получить свой профиль

**Acceptance Criteria:**
- [ ] GET /api/auth/profile — возвращает данные текущего пользователя
- [ ] AUTH-1 (требуется JWT токен)
- [ ] AUTH-3 (404 только если пользователь удалён — нестандартный случай)
- [ ] UI-1, UI-3

**API:** `GET /api/auth/profile`

### UC-10: Администратор может управлять пользователями (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/users — список пользователей с фильтрацией по роли (Admin/Teacher/Student/Dispatcher)
- [ ] POST /api/users — создать пользователя (админ, VAL-1, VAL-2, VAL-3)
- [ ] PUT /api/users/{id} — редактировать пользователя
- [ ] DELETE /api/users/{id} — деактивировать пользователя (IsActive = false, не удалять физически)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-2 (только Admin)
- [ ] AUTH-3 (404 если пользователь не найден)
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/users?role={role}&page={n}&pageSize={m}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

**Dependencies:** AuthService

### UC-11: Администратор может сменить роль пользователя

**Acceptance Criteria:**
- [ ] PATCH /api/users/{id}/role — изменить роль
- [ ] AUTH-2 (только Admin)
- [ ] AUTH-3 (404 если пользователь не найден)
- [ ] VAL-1, VAL-2 (роль должна быть из списка: Admin, Teacher, Student, Dispatcher)
- [ ] UI-4 (тост "Роль изменена")

**API:** `PATCH /api/users/{id}/role`

---

## 3. ScheduleService — Сервис расписания занятий

### UC-12: Студент/Преподаватель может просмотреть расписание по группе

**Acceptance Criteria:**
- [ ] GET /api/schedule?groupId={id} возвращает расписание для группы
- [ ] Расписание отсортировано по дню недели и времени начала
- [ ] Каждая запись содержит: предмет, преподаватель, аудитория, время начала/конца, день недели, тип занятия (лекция/практика)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-1, AUTH-3
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/schedule?groupId={id}&page={n}&pageSize={m}`

**Dependencies:** GroupService (LearningService)

### UC-13: Пользователь может просмотреть расписание по преподавателю

**Acceptance Criteria:**
- [ ] GET /api/schedule?teacherId={id} возвращает расписание преподавателя
- [ ] AUTH-3 (404 если преподаватель не найден)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/schedule?teacherId={id}&page={n}&pageSize={m}`

### UC-14: Пользователь может просмотреть расписание по аудитории

**Acceptance Criteria:**
- [ ] GET /api/schedule?room={name} возвращает расписание для аудитории
- [ ] Пустой результат если аудитория не найдена (не 404 — аудитории нет в БД, просто нет записей)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/schedule?room={name}&page={n}&pageSize={m}`

### UC-15: Диспетчер может создать/редактировать запись расписания

**Acceptance Criteria:**
- [ ] POST /api/schedule — создать запись (только Dispatcher/Admin)
- [ ] PUT /api/schedule/{id} — редактировать запись
- [ ] DELETE /api/schedule/{id} — удалить запись
- [ ] AUTH-2 (только Dispatcher/Admin)
- [ ] AUTH-3 (404 если запись не найдена)
- [ ] Валидация пересечений: одна группа не может быть в двух местах одновременно, преподаватель не может вести две пары одновременно, аудитория не может быть занята
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-3, UI-4

**API:**
- `POST /api/schedule`
- `PUT /api/schedule/{id}`
- `DELETE /api/schedule/{id}`

**Dependencies:** AuthService (роль Dispatcher)

### UC-16: Пользователь может смотреть расписание на день/неделю/месяц

**Acceptance Criteria:**
- [ ] GET /api/schedule?groupId={id}&period=day — расписание на день (сегодня)
- [ ] GET /api/schedule?groupId={id}&period=week — на неделю
- [ ] GET /api/schedule?groupId={id}&period=month — на месяц
- [ ] GET /api/schedule?groupId={id}&period=year — на год
- [ ] GET /api/schedule?groupId={id}&view=calendar — календарный вид (сетка)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/schedule?groupId={id}&period={day|week|month|year}&view={list|calendar}`

### UC-17: Пользователь может экспортировать расписание

**Acceptance Criteria:**
- [ ] GET /api/schedule/export?groupId={id}&format=pdf — скачать PDF
- [ ] GET /api/schedule/export?groupId={id}&format=xlsx — скачать Excel
- [ ] Файл генерируется на сервере и возвращается как download (Content-Disposition: attachment)
- [ ] AUTH-1 (только авторизованные пользователи)
- [ ] AUTH-3 (404 если группа не найдена)
- [ ] UI-1, UI-3

**API:** `GET /api/schedule/export?groupId={id}&format={pdf|xlsx}`

### UC-18: Диспетчер может импортировать расписание из файла

**Acceptance Criteria:**
- [ ] POST /api/schedule/import — загрузить файл расписания (multipart/form-data)
- [ ] Поддерживаемые форматы: xlsx, csv (sample-файлы в `docs/samples/schedule.xlsx`)
- [ ] AUTH-2 (только Dispatcher/Admin)
- [ ] Валидация: проверка на пересечения, пропущенные поля
- [ ] Импорт идемпотентен (заменяет существующие записи на дату, не дублирует)
- [ ] Ответ: `{ imported: N, errors: [{ row, message }] }`
- [ ] UI-1, UI-3, UI-4

**API:** `POST /api/schedule/import`

**Dependencies:** FileService, ScheduleService

---

## 4. LearningService — Сервис учебного процесса

### UC-19: Администратор может управлять группами (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/groups — список групп с пагинацией
- [ ] POST /api/groups — создать группу
- [ ] PUT /api/groups/{id} — редактировать
- [ ] DELETE /api/groups/{id} — удалить (мягкое удаление, если есть студенты — 409 Conflict)
- [ ] Группа содержит: название, курс (1-4), куратор (TeacherId), список студентов
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-2 (только Admin/Teacher)
- [ ] AUTH-3 (404 если группа не найдена)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/groups?page={n}&pageSize={m}`
- `POST /api/groups`
- `PUT /api/groups/{id}`
- `DELETE /api/groups/{id}`

### UC-20: Администратор может управлять студентами (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/students — список студентов (с фильтром по группе)
- [ ] POST /api/students — добавить студента (создаётся User + Student, генерируется номер зачётки)
- [ ] PUT /api/students/{id} — редактировать
- [ ] Студент: ФИО, дата рождения, группа, номер зачётки (уникальный, авто-генерация)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-2 (только Admin)
- [ ] AUTH-3 (404 если студент не найден)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/students?groupId={id}&page={n}&pageSize={m}`
- `POST /api/students`
- `PUT /api/students/{id}`

**Dependencies:** AuthService, GroupService

### UC-21: Администратор может управлять преподавателями (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/teachers — список преподавателей
- [ ] POST /api/teachers — добавить преподавателя (создаётся User + Teacher)
- [ ] PUT /api/teachers/{id} — редактировать
- [ ] Преподаватель: ФИО, кафедра, должность
- [ ] PAG-1, PAG-2, PAG-3
- [ ] AUTH-2 (только Admin)
- [ ] AUTH-3 (404 если преподаватель не найден)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/teachers?page={n}&pageSize={m}`
- `POST /api/teachers`
- `PUT /api/teachers/{id}`

**Dependencies:** AuthService

### UC-22: Преподаватель может создать учебный курс

**Acceptance Criteria:**
- [ ] POST /api/courses — создать курс (автор = текущий преподаватель)
- [ ] GET /api/courses — список курсов (с фильтром по преподавателю/группе/статусу)
- [ ] GET /api/courses/{id} — детали курса
- [ ] PUT /api/courses/{id} — редактировать
- [ ] DELETE /api/courses/{id} — удалить курс (мягкое удаление)
- [ ] Курс: название, описание, преподаватель (автор), группа, статус (Draft/Active/Archived), обложка (опционально)
- [ ] AUTH-2 (только автор курса или Admin может редактировать/удалять)
- [ ] AUTH-3 (404 если курс не найден)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/courses?teacherId={id}&groupId={id}&status={status}&page={n}&pageSize={m}`
- `POST /api/courses`
- `GET /api/courses/{id}`
- `PUT /api/courses/{id}`
- `DELETE /api/courses/{id}`

**Dependencies:** GroupService, TeacherService, AuthService

### UC-23: Преподаватель может управлять лекциями курса

**Acceptance Criteria:**
- [ ] POST /api/courses/{courseId}/lectures — добавить лекцию
- [ ] GET /api/courses/{courseId}/lectures — список лекций (сортировка по порядковому номеру)
- [ ] GET /api/courses/{courseId}/lectures/{id} — просмотр лекции
- [ ] PUT /api/courses/{courseId}/lectures/{id} — редактировать лекцию
- [ ] DELETE /api/courses/{courseId}/lectures/{id} — удалить лекцию
- [ ] Лекция: тема, содержание (Markdown), порядковый номер, видео-ссылка (опционально)
- [ ] AUTH-2 (только автор курса или Admin)
- [ ] AUTH-3 (404 если курс/лекция не найдена)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/courses/{courseId}/lectures`
- `POST /api/courses/{courseId}/lectures`
- `GET /api/courses/{courseId}/lectures/{id}`
- `PUT /api/courses/{courseId}/lectures/{id}`
- `DELETE /api/courses/{courseId}/lectures/{id}`

**Dependencies:** CourseService

### UC-24: Преподаватель может управлять заданиями курса

**Acceptance Criteria:**
- [ ] POST /api/courses/{courseId}/assignments — создать задание
- [ ] GET /api/courses/{courseId}/assignments — список заданий курса
- [ ] GET /api/courses/{courseId}/assignments/{id} — просмотр задания
- [ ] PUT /api/courses/{courseId}/assignments/{id} — редактировать
- [ ] DELETE /api/courses/{courseId}/assignments/{id} — удалить задание
- [ ] Задание: название, описание, срок сдачи (dueDate), макс. балл (1-100), порядковый номер
- [ ] AUTH-2 (только автор курса или Admin)
- [ ] AUTH-3 (404 если курс/задание не найдено)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/courses/{courseId}/assignments`
- `POST /api/courses/{courseId}/assignments`
- `GET /api/courses/{courseId}/assignments/{id}`
- `PUT /api/courses/{courseId}/assignments/{id}`
- `DELETE /api/courses/{courseId}/assignments/{id}`

**Dependencies:** CourseService

### UC-25: Студент может просмотреть свои курсы

**Acceptance Criteria:**
- [ ] GET /api/my/courses — список курсов текущего студента (по его группе)
- [ ] GET /api/my/courses/{id} — детали курса с лекциями и заданиями
- [ ] AUTH-1, AUTH-3
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/my/courses`
- `GET /api/my/courses/{id}`

**Dependencies:** CourseService, GroupService

### UC-26: Студент может просмотреть лекции и материалы курса

**Acceptance Criteria:**
- [ ] GET /api/courses/{courseId}/lectures — список лекций курса
- [ ] GET /api/courses/{courseId}/lectures/{id} — просмотр лекции
- [ ] GET /api/courses/{courseId}/materials — список файлов курса
- [ ] GET /api/materials/{id}/download — скачать файл
- [ ] AUTH-2 (только студенты группы курса имеют доступ, AUTH-2 если не принадлежит)
- [ ] AUTH-3 (404 если курс/лекция/материал не найден)
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/courses/{courseId}/lectures`
- `GET /api/courses/{courseId}/lectures/{id}`
- `GET /api/courses/{courseId}/materials`
- `GET /api/materials/{id}/download`

**Dependencies:** FileService, CourseService

### UC-27: Студент может отправить выполненное задание

**Acceptance Criteria:**
- [ ] POST /api/assignments/{id}/submit — загрузить выполненное задание (multipart + comment)
- [ ] GET /api/assignments/{id}/submissions (студент) — свои отправки по заданию
- [ ] GET /api/my/submissions — все свои отправки
- [ ] Студент может прикрепить файл и оставить комментарий
- [ ] AUTH-2 (только студенты группы курса)
- [ ] AUTH-3 (404 если задание не найдено)
- [ ] VAL-1, VAL-2 (файл не обязателен, но комментарий ≤ 2000 символов)
- [ ] Повторная отправка: создаёт новую версию (не заменяет старую)
- [ ] UI-1, UI-3, UI-4

**API:**
- `POST /api/assignments/{id}/submit`
- `GET /api/my/submissions`

**Dependencies:** FileService

### UC-28: Преподаватель может оценивать отправки студентов

**Acceptance Criteria:**
- [ ] GET /api/assignments/{id}/submissions — список отправок по заданию (преподаватель видит всех студентов)
- [ ] PATCH /api/submissions/{id}/grade — выставить оценку с комментарием
- [ ] Оценка от 0 до макс. балла задания
- [ ] AUTH-2 (только преподаватель-автор курса)
- [ ] AUTH-3 (404 если отправка не найдена)
- [ ] VAL-1, VAL-2 (балл должен быть в диапазоне)
- [ ] UI-1, UI-3, UI-4

**API:**
- `GET /api/assignments/{id}/submissions`
- `PATCH /api/submissions/{id}/grade`

**Dependencies:** FileService

### UC-29: Студент может просмотреть личный кабинет

**Acceptance Criteria:**
- [ ] GET /api/my/dashboard — сводка: курсы (количество), лекции (прочитано/всего), задания (сдано/всего), непроверенные отправки
- [ ] GET /api/my/submissions — список всех отправок с оценками
- [ ] AUTH-1
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/my/dashboard`
- `GET /api/my/submissions`

### UC-30: Преподаватель может просмотреть личный кабинет

**Acceptance Criteria:**
- [ ] GET /api/teacher/dashboard — сводка: курсы (количество), студенты (всего), недавние отправки (ожидающие проверки)
- [ ] GET /api/teacher/dashboard включает список последних отправок для проверки (≤ 10)
- [ ] AUTH-1 (только Teacher/Admin)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/teacher/dashboard`

### UC-31: Преподаватель может загружать файлы к курсу

**Acceptance Criteria:**
- [ ] POST /api/courses/{courseId}/materials — загрузить файл (multipart/form-data)
- [ ] GET /api/courses/{courseId}/materials — список файлов курса
- [ ] GET /api/materials/{id}/download — скачать файл
- [ ] DELETE /api/materials/{id} — удалить файл
- [ ] Файл: имя, размер, MIME-тип, дата загрузки
- [ ] AUTH-2 (только автор курса или Admin)
- [ ] AUTH-3 (404 если материал не найден)
- [ ] Валидация: макс. размер файла 100MB (VAL-1)
- [ ] Разрешённые MIME-типы: application/pdf, image/*, video/mp4, application/vnd.openxmlformats-officedocument.*, application/msword
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/courses/{courseId}/materials`
- `GET /api/courses/{courseId}/materials`
- `GET /api/materials/{id}/download`
- `DELETE /api/materials/{id}`

**Dependencies:** FileService

---

## 5. TestingService — Сервис тестирования

### UC-32: Преподаватель может создать тест

**Acceptance Criteria:**
- [ ] POST /api/tests — создать тест (в рамках курса)
- [ ] GET /api/tests — список тестов (с фильтром по курсу)
- [ ] GET /api/tests/{id} — детали теста
- [ ] Тест: название, описание, время на прохождение (минуты), количество попыток, тип (control/self-study)
- [ ] AUTH-2 (только преподаватель-автор курса или Admin)
- [ ] AUTH-3 (404 если курс не найден)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/tests`
- `GET /api/tests?courseId={id}&page={n}&pageSize={m}`
- `GET /api/tests/{id}`

**Dependencies:** CourseService

### UC-33: Преподаватель может добавить вопросы в тест

**Acceptance Criteria:**
- [ ] POST /api/tests/{testId}/questions — добавить вопрос
- [ ] GET /api/tests/{testId}/questions — список вопросов
- [ ] PUT /api/tests/{testId}/questions/{id} — редактировать
- [ ] DELETE /api/tests/{testId}/questions/{id} — удалить вопрос
- [ ] Типы вопросов: SingleChoice (одиночный выбор), MultipleChoice (множественный выбор), OpenAnswer (открытый ответ)
- [ ] Вопрос: текст, тип, варианты ответов (для выбора), правильный ответ (для автопроверки), баллы
- [ ] AUTH-2 (только автор курса или Admin)
- [ ] AUTH-3 (404 если тест/вопрос не найден)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-3, UI-4

**API:**
- `POST /api/tests/{testId}/questions`
- `GET /api/tests/{testId}/questions`
- `PUT /api/tests/{testId}/questions/{id}`
- `DELETE /api/tests/{testId}/questions/{id}`

### UC-34: Студент может пройти тест

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/start — начать тест (фиксируется время начала, блокируется повторный старт если превышены попытки)
- [ ] GET /api/tests/{testId}/attempt/{attemptId} — получить вопросы теста (без правильных ответов)
- [ ] POST /api/tests/{testId}/attempt/{attemptId}/submit — отправить ответы
- [ ] Автоматическая проверка закрытых вопросов (SingleChoice, MultipleChoice)
- [ ] Ограничение по времени: при истечении — 408 Request Timeout, попытка фиксируется как сданная
- [ ] Ограничение по количеству попыток: при превышении — 409 Conflict
- [ ] AUTH-2 (только студенты группы курса)
- [ ] AUTH-3 (404 если тест не найден)
- [ ] UI-1, UI-3, UI-4
- [ ] UI-2 (если тест не содержит вопросов — нельзя начать)

**API:**
- `GET /api/tests/{testId}/start`
- `POST /api/tests/{testId}/attempt/{attemptId}/submit`

### UC-35: Студент может просмотреть результаты теста

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/results — результат текущего студента по тесту (баллы, правильные ответы, ошибки)
- [ ] GET /api/my/test-results — все результаты студента по всем тестам
- [ ] AUTH-1, AUTH-3
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/tests/{testId}/results`
- `GET /api/my/test-results`

### UC-36: Преподаватель может просмотреть статистику по тесту

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/stats — статистика: средний балл, медиана, распределение оценок, список не сдавших (балл < проходного)
- [ ] Экспорт статистики в PDF/Excel (опционально)
- [ ] AUTH-2 (только автор курса или Admin)
- [ ] AUTH-3 (404 если тест не найден)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/tests/{testId}/stats`

---

## 6. JournalService — Сервис электронного журнала

**Модель данных:** JournalEntry (StudentId, ScheduleEntryId, Date, IsPresent, Grade, Topic, TeacherComment)

### UC-37: Преподаватель может вести журнал на паре

**Acceptance Criteria:**
- [ ] GET /api/journal?scheduleEntryId={id} — журнал занятия (список студентов группы с оценками/посещаемостью)
- [ ] POST /api/journal — массовая запись: `[{ studentId, isPresent, grade, topic }]`
- [ ] PUT /api/journal/{id} — редактировать запись (изменить оценку/посещаемость/тему)
- [ ] При создании: если запись уже существует — 409 Conflict (использовать PUT)
- [ ] AUTH-2 (только преподаватель, ведущий эту группу)
- [ ] AUTH-3 (404 если запись/занятие не найдено)
- [ ] VAL-1, VAL-2 (оценка 2-5 или зачёт/незачёт? — должно быть настроено)
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/journal?scheduleEntryId={id}`
- `POST /api/journal`
- `PUT /api/journal/{id}`

**Dependencies:** ScheduleService, GroupService

### UC-38: Преподаватель может отмечать посещаемость

**Acceptance Criteria:**
- [ ] PATCH /api/journal/{id}/attendance — отметить присутствие/отсутствие
- [ ] Причины пропуска: Respectful (уважительная), Disrespectful (неуважительная)
- [ ] AUTH-2 (только преподаватель)
- [ ] AUTH-3 (404 если запись не найдена)
- [ ] VAL-1, VAL-2 (причина из перечисления)
- [ ] UI-1, UI-3, UI-4

**API:** `PATCH /api/journal/{id}/attendance`

### UC-39: Преподаватель может фиксировать тему занятия

**Acceptance Criteria:**
- [ ] PUT /api/journal?scheduleEntryId={id}/topic — указать/изменить тему занятия (для всех записей занятия одной строкой)
- [ ] AUTH-2 (только преподаватель)
- [ ] AUTH-3 (404 если запись не найдена)
- [ ] VAL-1, VAL-2, VAL-3 (тема ≤ 500 символов)
- [ ] UI-1, UI-3, UI-4

**API:** `PUT /api/journal?scheduleEntryId={id}/topic`

### UC-40: Студент может просмотреть свою посещаемость

**Acceptance Criteria:**
- [ ] GET /api/my/attendance — статистика посещаемости: всего занятий, пропусков (уваж/неуваж), процент
- [ ] GET /api/my/grades — текущие оценки по предметам (средний балл по каждому курсу)
- [ ] AUTH-1
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/my/attendance`
- `GET /api/my/grades`

---

## 7. CuratorService — Сервис классного руководителя

### UC-41: Классный руководитель может просмотреть список группы

**Acceptance Criteria:**
- [ ] GET /api/curator/group — информация о закреплённой группе (куратор привязан к группе)
- [ ] GET /api/curator/group/students — список студентов группы (ФИО, контакты родителей)
- [ ] AUTH-2 (только куратор группы или Admin)
- [ ] AUTH-3 (404 если группа не закреплена за куратором)
- [ ] UI-1, UI-2, UI-3

**API:**
- `GET /api/curator/group`
- `GET /api/curator/group/students`

**Dependencies:** GroupService

### UC-42: Классный руководитель может создавать мероприятия группы

**Acceptance Criteria:**
- [ ] POST /api/curator/events — создать мероприятие (название, дата, описание, тип)
- [ ] GET /api/curator/events — список мероприятий (сортировка по дате)
- [ ] AUTH-2 (только куратор или Admin)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/curator/events`
- `GET /api/curator/events?page={n}&pageSize={m}`

### UC-43: Классный руководитель может написать характеристику на студента

**Acceptance Criteria:**
- [ ] POST /api/curator/characteristics — создать характеристику (студентId, текст, дата)
- [ ] GET /api/curator/characteristics/{studentId} — просмотреть характеристики студента
- [ ] AUTH-2 (только куратор группы студента или Admin)
- [ ] AUTH-3 (404 если студент не найден)
- [ ] VAL-1, VAL-2, VAL-3 (текст ≤ 5000 символов)
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/curator/characteristics`
- `GET /api/curator/characteristics/{studentId}`

### UC-44: Классный руководитель может просмотреть отчёт об успеваемости группы

**Acceptance Criteria:**
- [ ] GET /api/curator/progress — сводка успеваемости по группе: студенты, средний балл по каждому предмету, количество задолженностей
- [ ] Экспорт в PDF/Excel
- [ ] AUTH-2 (только куратор или Admin)
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/curator/progress`

---

## 8. PerformService — Сервис успеваемости

### UC-45: Преподаватель может управлять ведомостями

**Acceptance Criteria:**
- [ ] POST /api/grade-sheets — создать ведомость (курсId, группаId, семестр, тип: экзамен/зачёт/дифзачёт)
- [ ] GET /api/grade-sheets — список ведомостей (фильтр по курсу/группе/семестру)
- [ ] PUT /api/grade-sheets/{id}/grades — заполнить оценки (массово: `[{ studentId, grade }]`)
- [ ] AUTH-2 (только преподаватель-автор курса или Admin)
- [ ] AUTH-3 (404 если ведомость/курс/группа не найдены)
- [ ] VAL-1, VAL-2 (оценка: 2-5 для экзамена, зачёт/незачёт для зачёта)
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/grade-sheets`
- `GET /api/grade-sheets`
- `PUT /api/grade-sheets/{id}/grades`

### UC-46: Преподаватель может отмечать задолженности

**Acceptance Criteria:**
- [ ] POST /api/debts — создать запись о задолженности (студентId, предмет, тип: задание/контрольная/экзамен)
- [ ] GET /api/debts?studentId={id} — задолженности студента
- [ ] PATCH /api/debts/{id} — закрыть задолженность (isCleared = true, dateCleared)
- [ ] AUTH-2 (только преподаватель-предметник или Admin)
- [ ] AUTH-3 (404 если запись не найдена)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/debts`
- `GET /api/debts?studentId={id}`
- `PATCH /api/debts/{id}`

### UC-47: Студент может просмотреть зачётную книжку

**Acceptance Criteria:**
- [ ] GET /api/my/record-book — зачётная книжка: все предметы (курсы) по семестрам, оценки (экзамен/зачёт), дата сдачи
- [ ] AUTH-1
- [ ] UI-1, UI-2, UI-3

**API:** `GET /api/my/record-book`

### UC-48: Администратор может вести учёт выпускников

**Acceptance Criteria:**
- [ ] POST /api/graduates — зарегистрировать выпускника (студентId, год выпуска, диплом, специальность)
- [ ] GET /api/graduates — список выпускников с фильтром по году выпуска
- [ ] AUTH-2 (только Admin)
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `POST /api/graduates`
- `GET /api/graduates?year={yyyy}&page={n}&pageSize={m}`

---

## 9. AdmissionService — Сервис приёмной комиссии

### UC-49: Абитуриент может подать заявление на поступление

**Acceptance Criteria:**
- [ ] POST /api/admissions — подать заявление (ФИО, телефон, email, специальность, дата рождения)
- [ ] GET /api/admissions/status — проверить статус заявления (по email + номеру заявления)
- [ ] AUTH не требуется (публичная форма)
- [ ] VAL-1, VAL-2, VAL-3 (email — валидный, телефон — формат +7...)
- [ ] Rate limiting: не более 3 заявлений с одного email/телефона в день
- [ ] UI-1, UI-3, UI-4

**API:**
- `POST /api/admissions`
- `GET /api/admissions/status?email={email}&applicationId={id}`

### UC-50: Секретарь может просмотреть список заявлений

**Acceptance Criteria:**
- [ ] GET /api/admissions — список заявлений с фильтрами (статус, дата от/до, специальность)
- [ ] PATCH /api/admissions/{id} — изменить статус (New/Approved/Rejected/Documents) с комментарием
- [ ] AUTH-2 (только Admin/Secretary)
- [ ] AUTH-3 (404 если заявление не найдено)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] VAL-1, VAL-2
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/admissions?status={status}&from={date}&to={date}&specialty={spec}&page={n}&pageSize={m}`
- `PATCH /api/admissions/{id}`

---

## 10. HrService — Сервис отдела кадров

### UC-51: Сотрудник отдела кадров может управлять сотрудниками (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/hr/employees — список сотрудников (фильтр по кафедре/должности)
- [ ] POST /api/hr/employees — добавить сотрудника
- [ ] PUT /api/hr/employees/{id} — редактировать
- [ ] Сотрудник: ФИО, должность, кафедра, дата приёма, телефон, email
- [ ] AUTH-2 (только Admin/HR)
- [ ] AUTH-3 (404 если сотрудник не найден)
- [ ] PAG-1, PAG-2, PAG-3
- [ ] VAL-1, VAL-2, VAL-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/hr/employees?department={}&position={}&page={n}&pageSize={m}`
- `POST /api/hr/employees`
- `PUT /api/hr/employees/{id}`

---

## 11. FileService — Сервис хранения файлов

**Модель данных:** FileRecord (Id, FileName, OriginalName, MimeType, SizeBytes, StoragePath, CourseId, UploadedById, CreatedAt)

### UC-52: Пользователь может загрузить файл в систему

**Acceptance Criteria:**
- [ ] POST /api/files/upload — загрузить файл (multipart/form-data, до 100MB)
- [ ] POST /api/files/upload-chunked — загрузка больших файлов чанками (сборка на сервере)
- [ ] Файл сохраняется в локальную FS (на MVP) или MinIO (prod)
- [ ] Ответ: `{ id, fileName, mimeType, sizeBytes, url }`
- [ ] AUTH-1, VAL-1 (макс. размер, разрешённые MIME)
- [ ] UI-1, UI-3, UI-4

**API:**
- `POST /api/files/upload`
- `POST /api/files/upload-chunked`

### UC-53: Пользователь может скачать/удалить файл

**Acceptance Criteria:**
- [ ] GET /api/files/{id}/download — скачать файл (Content-Disposition: attachment)
- [ ] DELETE /api/files/{id} — удалить файл (только автор или Admin)
- [ ] AUTH-1, AUTH-2, AUTH-3
- [ ] UI-1, UI-3, UI-4

**API:**
- `GET /api/files/{id}/download`
- `DELETE /api/files/{id}`

---

## 12. NoticeService — Сервис уведомлений (P2)

**Архитектура:** асинхронная очередь через RabbitMQ (или in-memory на MVP)

### UC-54: Система отправляет уведомление пользователю

**Acceptance Criteria:**
- [ ] POST /api/notices/send — отправить уведомление (тип: email/push/SMS)
- [ ] Каналы: email (SMTP), push (Firebase — для мобильного приложения)
- [ ] Шаблоны: "Новое задание", "Оценка выставлена", "Изменение расписания"
- [ ] AUTH-2 (только сервисный ключ / Admin)
- [ ] VAL-1, VAL-2, VAL-3

**API:** `POST /api/notices/send`

### UC-55: Пользователь может просмотреть свои уведомления

**Acceptance Criteria:**
- [ ] GET /api/my/notices — список уведомлений пользователя
- [ ] PATCH /api/my/notices/{id}/read — отметить как прочитанное
- [ ] GET /api/my/notices/unread-count — количество непрочитанных (для бейджа)
- [ ] AUTH-1, PAG-1, PAG-2, PAG-3
- [ ] UI-1, UI-2, UI-3, UI-4

**API:**
- `GET /api/my/notices?page={n}&pageSize={m}`
- `PATCH /api/my/notices/{id}/read`
- `GET /api/my/notices/unread-count`

---

## 13. ReportService — Сервис генерации отчётов (P2)

### UC-56: Пользователь может сгенерировать отчёт

**Acceptance Criteria:**
- [ ] POST /api/reports/generate — запросить генерацию отчёта (тип, параметры, формат: pdf/xlsx)
- [ ] Типы отчётов: успеваемость группы, посещаемость за период, сводка по курсу
- [ ] Асинхронная генерация: ответ `{ reportId, status: "processing" }`
- [ ] AUTH-1, AUTH-2, VAL-1, VAL-2
- [ ] UI-1, UI-3, UI-4

**API:** `POST /api/reports/generate`

### UC-57: Пользователь может скачать готовый отчёт

**Acceptance Criteria:**
- [ ] GET /api/reports/{id}/status — проверить статус (processing/ready/error)
- [ ] GET /api/reports/{id}/download — скачать готовый отчёт
- [ ] GET /api/reports — список сгенерированных отчётов пользователя
- [ ] AUTH-1, AUTH-3
- [ ] UI-1, UI-3

**API:**
- `GET /api/reports/{id}/status`
- `GET /api/reports/{id}/download`
- `GET /api/reports?page={n}&pageSize={m}`

---

## 14. Mobile — ReactNative (P3 — будущая фаза)

**Статус:** Отложено до завершения MVP.
**Клиент:** ReactNative (iOS + Android).
**API:** Те же endpoints, что и для Next.js (один REST API на все клиенты).

### Запланированные UC (без детализации):

| UC | Описание |
|----|----------|
| UC-58 | Студент может просмотреть расписание (мобильная версия) |
| UC-59 | Студент может просмотреть оценки и посещаемость |
| UC-60 | Преподаватель может отметить посещаемость с телефона |
| UC-61 | Пользователь получает push-уведомления |

---

## 15. Infrastructure — Инфраструктурные истории

Сервисы, не имеющие REST API, но необходимые для работы системы.

### UC-62: CI/CD pipeline развёртывает приложение на VPS

**Acceptance Criteria:**
- [ ] GitHub Actions workflow при push в master:
  - [ ] `dotnet build` — сборка API
  - [ ] `dotnet test` — прогон тестов (если упали — пайплайн красный)
  - [ ] `npm run build` — сборка frontend
  - [ ] `docker compose build` — сборка образов
  - [ ] Deploy на VPS через SSH + docker compose up
- [ ] Quality gates: CSharpier check, сборка, тесты

**Dependencies:** Docker, GitHub Secrets (SSH_KEY, VPS_HOST)

### UC-63: Система логирует события в Serilog

**Acceptance Criteria:**
- [ ] Serilog пишет в: консоль (dev), файл (prod)
- [ ] Уровни: Information, Warning, Error, Fatal
- [ ] Логи ротируются: ежедневно, retention 30 дней
- [ ] CorrelationId пробрасывается через весь request pipeline
- [ ] Исключения пишутся с полным stack trace

### UC-64: База данных бекапится раз в неделю

**Acceptance Criteria:**
- [ ] cron: каждое воскресенье в 03:00
- [ ] pg_dump → .sql.gz → upload в локальную папку `backups/`
- [ ] Retention: 4 недели (старые бекапы удаляются)
- [ ] Восстановление: ручное, скриптом `scripts/restore.sh`

**Dependencies:** PostgreSQL, cron/systemd timer

---

| Приоритет | Сервисы | UC | Статус |
|-----------|---------|----|--------|
| P0 (MVP) | AuthService | UC-7..11 | ✅ Реализовано |
| P0 (MVP) | SiteService (News + Pages) | UC-1..6 | ❌ Не начато |
| P0 (MVP) | ScheduleService | UC-12..18 | ❌ Не начато |
| P0 (MVP) | LearningService (Groups, Courses) | UC-19..31 | ✅ Реализовано |
| P1 | TestingService | UC-32..36 | ❌ Не начато |
| P1 | JournalService | UC-37..40 | ❌ Не начато |
| P1 | FileService | UC-52..53 | ❌ Не начато |
| P2 | PerformService | UC-45..48 | ❌ Не начато |
| P2 | CuratorService | UC-41..44 | ❌ Не начато |
| P2 | NoticeService | UC-54..55 | ❌ Не начато |
| P2 | ReportService | UC-56..57 | ❌ Не начато |
| P3 | AdmissionService | UC-49..50 | ❌ Не начато |
| P3 | HrService | UC-51 | ❌ Не начато |
| P3 | ReactNative (Mobile) | UC-58..61 | ⏸ Отложено |
| Инфра | CI/CD, Logging, Backup | UC-62..64 | ❌ Не начато |

---

## Итого: 64 User Story

| Сервис | Количество | Статус |
|--------|-----------|--------|
| SiteService | 6 (UC-1 — UC-6) | ❌ |
| AuthService | 5 (UC-7 — UC-11) | ✅ |
| ScheduleService | 7 (UC-12 — UC-18) | ❌ |
| LearningService | 13 (UC-19 — UC-31) | ✅ |
| TestingService | 5 (UC-32 — UC-36) | ❌ |
| JournalService | 4 (UC-37 — UC-40) | ❌ |
| CuratorService | 4 (UC-41 — UC-44) | ❌ |
| PerformService | 4 (UC-45 — UC-48) | ❌ |
| AdmissionService | 2 (UC-49 — UC-50) | ❌ |
| HrService | 1 (UC-51) | ❌ |
| FileService | 2 (UC-52 — UC-53) | ❌ |
| NoticeService | 2 (UC-54 — UC-55) | ❌ |
| ReportService | 2 (UC-56 — UC-57) | ❌ |
| ReactNative (Mobile) | 4 (UC-58 — UC-61) | ⏸ |
| Infrastructure | 3 (UC-62 — UC-64) | ❌ |

---

## Изменения против предыдущей версии

1. **Имя файла**: `userstrories.md` → `userstories.md` (исправлена опечатка)
2. **Добавлен раздел "Общие критерии приемки"** — авторизация (AUTH-1..3), валидация (VAL-1..3), пагинация (PAG-1..3), UI состояния (UI-1..4)
3. **Во все User Stories добавлены**: AUTH, VAL, PAG, UI коды
4. **UC-4**: уточнено мягкое удаление (IsDeleted)
5. **UC-7**: добавлен rate limiting
6. **UC-12..15**: уточнены фильтры, валидация пересечений
7. **UC-26**: добавлена проверка принадлежности студента к группе (AUTH-2)
8. **UC-27**: уточнена версионность отправок (новая версия, не замена)
9. **UC-34**: уточнены коды при истечении времени (408) и превышении попыток (409)
10. **UC-37**: определена модель данных JournalEntry
11. **Добавлены UI состояния** (loading/empty/error/success) в каждую UC с интерфейсом
12. **Обновлена таблица приоритетов**: добавлен столбец статуса реализации
13. **Добавлены новые сервисы**: FileService (UC-52..53), NoticeService (UC-54..55), ReportService (UC-56..57)
14. **Добавлен ReactNative**: 4 запланированных UC (UC-58..61) — отложено на P3
15. **Добавлены инфраструктурные UC**: CI/CD pipeline (UC-62), Serilog (UC-63), Backup (UC-64)
16. **UC-6**: добавлен импорт меню (parent/child структура страниц)
17. **UC-18**: добавлена ссылка на sample-файлы расписания
18. **UC-31**: добавлены разрешённые MIME-типы (PDF, video, office docs)
