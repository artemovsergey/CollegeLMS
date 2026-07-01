# CollegeLMS — User Stories

Декомпозиция task.md на пользовательские истории. Каждая история — атомарная единица работы для одного Agent.

---

## 1. SiteService — Сервис сайта колледжа

### UC-1: Посетитель может просмотреть главную страницу

**Acceptance Criteria:**
- [ ] Главная страница загружается с информацией о колледже
- [ ] Дизайн соответствует цветам логотипа
- [ ] Страница адаптивна (мобильная/десктопная версия)
- [ ] Меню сайта отображается корректно

**Dependencies:** SiteService, WordPress import data

### UC-2: Посетитель может просмотреть список новостей

**Acceptance Criteria:**
- [ ] GET /api/news возвращает список новостей с пагинацией
- [ ] Новости отсортированы по дате (сначала новые)
- [ ] Каждая новость содержит заголовок, дату, анонс, изображение

**API:** `GET /api/news` — получить список новостей

**Dependencies:** SiteService, WordPress import

### UC-3: Посетитель может просмотреть конкретную новость

**Acceptance Criteria:**
- [ ] GET /api/news/{id} возвращает полную новость
- [ ] 404 если новость не найдена

**API:** `GET /api/news/{id}` — получить новость по ID

### UC-4: Администратор может управлять новостями (CRUD)

**Acceptance Criteria:**
- [ ] POST /api/news — создать новость (админ)
- [ ] PUT /api/news/{id} — редактировать новость
- [ ] DELETE /api/news/{id} — удалить новость (мягкое удаление)
- [ ] Только пользователи с ролью Admin могут выполнять эти действия
- [ ] Валидация: заголовок обязателен и ≤ 255 символов

**API:**
- `POST /api/news`
- `PUT /api/news/{id}`
- `DELETE /api/news/{id}`

**Dependencies:** AuthService

### UC-5: Посетитель может просмотреть страницы сайта

**Acceptance Criteria:**
- [ ] GET /api/pages возвращает список публичных страниц
- [ ] GET /api/pages/{slug} возвращает конкретную страницу по slug
- [ ] Страницы имеют иерархическую структуру (parent/child)

**API:**
- `GET /api/pages` — список страниц
- `GET /api/pages/{slug}` — страница по slug

### UC-6: Администратор может импортировать данные с WordPress

**Acceptance Criteria:**
- [ ] Скрипт импорта переносит pages, posts, categories, tags, users
- [ ] Импорт идемпотентен (повторный запуск не создаёт дубликатов)
- [ ] Медиа-файлы сохраняются локально

**Dependencies:** WordPress REST API данные в import/

---

## 2. AuthService — Сервис аутентификации и авторизации

### UC-7: Пользователь может войти в систему

**Acceptance Criteria:**
- [ ] POST /api/auth/login — принимает email + password
- [ ] Возвращает JWT токен (access token)
- [ ] Неверный email или пароль → 401
- [ ] Пароль хешируется BCrypt

**API:** `POST /api/auth/login`
- Request: `{ email, password }`
- Response: `{ token, user: { id, email, fullName, role } }`

### UC-8: Пользователь может выйти из системы

**Acceptance Criteria:**
- [ ] POST /api/auth/logout — инвалидирует токен (блокировка в кэше)
- [ ] Истёкший токен → 401

**API:** `POST /api/auth/logout`

**Dependencies:** Redis

### UC-9: Пользователь может получить свой профиль

**Acceptance Criteria:**
- [ ] GET /api/auth/profile — возвращает данные текущего пользователя
- [ ] Требуется JWT токен

**API:** `GET /api/auth/profile`

### UC-10: Администратор может управлять пользователями (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/users — список пользователей с фильтрацией по роли
- [ ] POST /api/users — создать пользователя (админ)
- [ ] PUT /api/users/{id} — редактировать пользователя
- [ ] DELETE /api/users/{id} — удалить пользователя (деактивация)
- [ ] Роли: Admin, Teacher, Student, Dispatcher

**API:**
- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

**Dependencies:** AuthService

### UC-11: Администратор может сменить роль пользователя

**Acceptance Criteria:**
- [ ] PATCH /api/users/{id}/role — изменить роль
- [ ] Только админ может менять роли

**API:** `PATCH /api/users/{id}/role`

---

## 3. ScheduleService — Сервис расписания занятий

### UC-12: Студент/Преподаватель может просмотреть расписание по группе

**Acceptance Criteria:**
- [ ] GET /api/schedule?groupId={id} возвращает расписание для группы
- [ ] Расписание отсортировано по дню недели и времени
- [ ] Каждая запись содержит: предмет, преподаватель, аудитория, время, день недели

**API:** `GET /api/schedule?groupId={id}`

**Dependencies:** GroupService

### UC-13: Пользователь может просмотреть расписание по преподавателю

**Acceptance Criteria:**
- [ ] GET /api/schedule?teacherId={id} возвращает расписание преподавателя

**API:** `GET /api/schedule?teacherId={id}`

### UC-14: Пользователь может просмотреть расписание по аудитории

**Acceptance Criteria:**
- [ ] GET /api/schedule?room={name} возвращает расписание для аудитории

**API:** `GET /api/schedule?room={name}`

### UC-15: Диспетчер может создать/редактировать запись расписания

**Acceptance Criteria:**
- [ ] POST /api/schedule — создать запись (диспетчер/админ)
- [ ] PUT /api/schedule/{id} — редактировать запись
- [ ] DELETE /api/schedule/{id} — удалить запись
- [ ] Валидация: не пересекается ли расписание (группа/преподаватель/аудитория)

**API:**
- `POST /api/schedule`
- `PUT /api/schedule/{id}`
- `DELETE /api/schedule/{id}`

**Dependencies:** AuthService (роль Dispatcher)

### UC-16: Пользователь может смотреть расписание на день/неделю/месяц

**Acceptance Criteria:**
- [ ] GET /api/schedule?groupId={id}&period=week — расписание на неделю
- [ ] GET /api/schedule?groupId={id}&period=month — на месяц
- [ ] GET /api/schedule?groupId={id}&period=day — на день
- [ ] GET /api/schedule?groupId={id}&period=year — на год
- [ ] GET /api/schedule?groupId={id}&view=calendar — календарный вид

**API:** `GET /api/schedule?groupId={id}&period={day|week|month|year}&view={list|calendar}`

### UC-17: Пользователь может экспортировать расписание

**Acceptance Criteria:**
- [ ] GET /api/schedule/export?groupId={id}&format=pdf — PDF
- [ ] GET /api/schedule/export?groupId={id}&format=xlsx — Excel

**API:** `GET /api/schedule/export?groupId={id}&format={pdf|xlsx}`

### UC-18: Диспетчер может импортировать расписание из файла

**Acceptance Criteria:**
- [ ] POST /api/schedule/import — загрузить файл расписания
- [ ] Поддерживаемые форматы: xlsx, csv

**API:** `POST /api/schedule/import`

**Dependencies:** FileService, ScheduleService

---

## 4. LearningService — Сервис учебного процесса

### UC-19: Администратор может управлять группами (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/groups — список групп с пагинацией
- [ ] POST /api/groups — создать группу
- [ ] PUT /api/groups/{id} — редактировать
- [ ] DELETE /api/groups/{id} — удалить
- [ ] Группа содержит: название, курс, куратор, список студентов

**API:**
- `GET /api/groups`
- `POST /api/groups`
- `PUT /api/groups/{id}`
- `DELETE /api/groups/{id}`

### UC-20: Администратор может управлять студентами (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/students — список студентов (с фильтром по группе)
- [ ] POST /api/students — добавить студента (создаётся User+Student)
- [ ] PUT /api/students/{id} — редактировать
- [ ] Студент: ФИО, дата рождения, группа, номер зачётки

**API:**
- `GET /api/students`
- `POST /api/students`
- `PUT /api/students/{id}`

**Dependencies:** AuthService, GroupService

### UC-21: Администратор может управлять преподавателями (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/teachers — список преподавателей
- [ ] POST /api/teachers — добавить преподавателя
- [ ] Преподаватель: ФИО, кафедра, должность

**API:**
- `GET /api/teachers`
- `POST /api/teachers`
- `PUT /api/teachers/{id}`

**Dependencies:** AuthService

### UC-22: Преподаватель может создать учебный курс

**Acceptance Criteria:**
- [ ] POST /api/courses — создать курс
- [ ] GET /api/courses — список курсов (с фильтром по преподавателю)
- [ ] GET /api/courses/{id} — детали курса
- [ ] PUT /api/courses/{id} — редактировать
- [ ] Курс: название, описание, преподаватель, группа, список модулей

**API:**
- `GET /api/courses`
- `POST /api/courses`
- `GET /api/courses/{id}`
- `PUT /api/courses/{id}`

**Dependencies:** GroupService, TeacherService

### UC-23: Преподаватель может добавить лекцию в курс

**Acceptance Criteria:**
- [ ] POST /api/courses/{courseId}/lectures — добавить лекцию
- [ ] GET /api/courses/{courseId}/lectures — список лекций
- [ ] Лекция: тема, содержание (Markdown/HTML), вложения

**API:**
- `POST /api/courses/{courseId}/lectures`
- `GET /api/courses/{courseId}/lectures`

**Dependencies:** CourseService, FileService

### UC-24: Преподаватель может добавить практическое задание в курс

**Acceptance Criteria:**
- [ ] POST /api/courses/{courseId}/assignments — создать задание
- [ ] Задание: описание, срок сдачи, критерии оценки, файлы

**API:** `POST /api/courses/{courseId}/assignments`

### UC-25: Студент может просмотреть свои курсы

**Acceptance Criteria:**
- [ ] GET /api/my/courses — список курсов текущего студента
- [ ] GET /api/my/courses/{id} — детали курса с модулями

**API:**
- `GET /api/my/courses`
- `GET /api/my/courses/{id}`

### UC-26: Студент может просмотреть материалы курса

**Acceptance Criteria:**
- [ ] GET /api/courses/{courseId}/lectures/{id} — просмотр лекции
- [ ] GET /api/courses/{courseId}/materials — список файлов курса
- [ ] Студент может скачать прикреплённые файлы

**API:**
- `GET /api/courses/{courseId}/lectures/{id}`
- `GET /api/courses/{courseId}/materials`

**Dependencies:** FileService

### UC-27: Студент может загрузить файл для задания

**Acceptance Criteria:**
- [ ] POST /api/assignments/{id}/submit — загрузить выполненное задание
- [ ] Поддерживаемые форматы: PDF, DOC, DOCX, PPT, MP4, ZIP

**API:** `POST /api/assignments/{id}/submit`

**Dependencies:** FileService

### UC-28: Студент может просмотреть личный кабинет

**Acceptance Criteria:**
- [ ] GET /api/my/dashboard — сводка: курсы, оценки, посещаемость, задолженности

**API:** `GET /api/my/dashboard`

### UC-29: Преподаватель может просмотреть личный кабинет

**Acceptance Criteria:**
- [ ] GET /api/teacher/dashboard — сводка: группы, курсы, расписание, журнал

**API:** `GET /api/teacher/dashboard`

---

## 5. TestingService — Сервис тестирования

### UC-30: Преподаватель может создать тест

**Acceptance Criteria:**
- [ ] POST /api/tests — создать тест (в рамках курса)
- [ ] GET /api/tests — список тестов
- [ ] Тест: название, описание, время на прохождение, количество попыток

**API:**
- `POST /api/tests`
- `GET /api/tests`

**Dependencies:** CourseService

### UC-31: Преподаватель может добавить вопросы в тест

**Acceptance Criteria:**
- [ ] POST /api/tests/{testId}/questions — добавить вопрос
- [ ] Типы вопросов: одиночный выбор, множественный выбор, открытый ответ
- [ ] PUT /api/tests/{testId}/questions/{id} — редактировать

**API:**
- `POST /api/tests/{testId}/questions`
- `PUT /api/tests/{testId}/questions/{id}`

### UC-32: Студент может пройти тест

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/start — начать тест (фиксация времени)
- [ ] POST /api/tests/{testId}/submit — отправить ответы
- [ ] Автоматическая проверка закрытых вопросов
- [ ] Ограничение по времени и количеству попыток

**API:**
- `GET /api/tests/{testId}/start`
- `POST /api/tests/{testId}/submit`

### UC-33: Студент может просмотреть результаты теста

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/results — результат текущего студента
- [ ] GET /api/my/results — все результаты студента

**API:**
- `GET /api/tests/{testId}/results`
- `GET /api/my/results`

### UC-34: Преподаватель может просмотреть статистику по тесту

**Acceptance Criteria:**
- [ ] GET /api/tests/{testId}/stats — статистика: средний балл, распределение, кто не сдал

**API:** `GET /api/tests/{testId}/stats`

---

## 6. JournalService — Сервис электронного журнала

### UC-35: Преподаватель может вести журнал на паре

**Acceptance Criteria:**
- [ ] GET /api/journal?lessonId={id} — журнал занятия
- [ ] POST /api/journal — запись в журнал (оценки, посещаемость)
- [ ] PUT /api/journal/{id} — редактировать запись

**API:**
- `GET /api/journal`
- `POST /api/journal`
- `PUT /api/journal/{id}`

**Dependencies:** ScheduleService, GroupService

### UC-36: Преподаватель может отмечать посещаемость

**Acceptance Criteria:**
- [ ] PATCH /api/journal/{id}/attendance — отметить присутствие/отсутствие
- [ ] Причины пропуска: уважительная, неуважительная

**API:** `PATCH /api/journal/{id}/attendance`

### UC-37: Преподаватель может фиксировать тему занятия

**Acceptance Criteria:**
- [ ] PATCH /api/journal/{id}/topic — указать/изменить тему занятия

**API:** `PATCH /api/journal/{id}/topic`

### UC-38: Студент может просмотреть свою посещаемость

**Acceptance Criteria:**
- [ ] GET /api/my/attendance — статистика посещаемости студента
- [ ] GET /api/my/grades — текущие оценки

**API:**
- `GET /api/my/attendance`
- `GET /api/my/grades`

---

## 7. CuratorService — Сервис классного руководителя

### UC-39: Классный руководитель может просмотреть список группы

**Acceptance Criteria:**
- [ ] GET /api/curator/group — информация о закреплённой группе
- [ ] GET /api/curator/group/students — список студентов группы

**API:**
- `GET /api/curator/group`
- `GET /api/curator/group/students`

**Dependencies:** GroupService

### UC-40: Классный руководитель может создавать мероприятия группы

**Acceptance Criteria:**
- [ ] POST /api/curator/events — создать мероприятие
- [ ] GET /api/curator/events — список мероприятий

**API:**
- `POST /api/curator/events`
- `GET /api/curator/events`

### UC-41: Классный руководитель может написать характеристику на студента

**Acceptance Criteria:**
- [ ] POST /api/curator/characteristics — создать характеристику
- [ ] GET /api/curator/characteristics/{studentId} — просмотреть

**API:**
- `POST /api/curator/characteristics`
- `GET /api/curator/characteristics/{studentId}`

### UC-42: Классный руководитель может просмотреть отчёт об успеваемости группы

**Acceptance Criteria:**
- [ ] GET /api/curator/progress — сводка успеваемости по группе
- [ ] Экспорт в PDF/Excel

**API:** `GET /api/curator/progress`

---

## 8. PerformService — Сервис успеваемости

### UC-43: Преподаватель может управлять ведомостями

**Acceptance Criteria:**
- [ ] POST /api/grade-sheets — создать ведомость
- [ ] GET /api/grade-sheets — список ведомостей
- [ ] PUT /api/grade-sheets/{id}/grades — заполнить оценки

**API:**
- `POST /api/grade-sheets`
- `GET /api/grade-sheets`
- `PUT /api/grade-sheets/{id}/grades`

### UC-44: Преподаватель может отмечать задолженности

**Acceptance Criteria:**
- [ ] POST /api/debts — создать запись о задолженности
- [ ] GET /api/debts?studentId={id} — задолженности студента
- [ ] PATCH /api/debts/{id} — закрыть задолженность

**API:**
- `POST /api/debts`
- `GET /api/debts`
- `PATCH /api/debts/{id}`

### UC-45: Студент может просмотреть зачётную книжку

**Acceptance Criteria:**
- [ ] GET /api/my/record-book — зачётная книжка: все предметы, оценки, семестры

**API:** `GET /api/my/record-book`

### UC-46: Администратор может вести учёт выпускников

**Acceptance Criteria:**
- [ ] POST /api/graduates — зарегистрировать выпускника
- [ ] GET /api/graduates — список выпускников с фильтром по году
- [ ] Экспорт в PDF/Excel

**API:**
- `POST /api/graduates`
- `GET /api/graduates`

---

## 9. AdmissionService — Сервис приёмной комиссии

### UC-47: Абитуриент может подать заявление на поступление

**Acceptance Criteria:**
- [ ] POST /api/admissions — подать заявление (ФИО, телефон, email, специальность)
- [ ] GET /api/admissions/status — проверить статус заявления

**API:**
- `POST /api/admissions`
- `GET /api/admissions/status`

### UC-48: Секретарь может просмотреть список заявлений

**Acceptance Criteria:**
- [ ] GET /api/admissions — список заявлений с фильтрами (статус, дата)
- [ ] PATCH /api/admissions/{id} — изменить статус (принято/отклонено)

**API:**
- `GET /api/admissions`
- `PATCH /api/admissions/{id}`

---

## 10. HrService — Сервис отдела кадров

### UC-49: Сотрудник отдела кадров может управлять сотрудниками (CRUD)

**Acceptance Criteria:**
- [ ] GET /api/hr/employees — список сотрудников
- [ ] POST /api/hr/employees — добавить сотрудника
- [ ] PUT /api/hr/employees/{id} — редактировать
- [ ] Сотрудник: ФИО, должность, кафедра, дата приёма, телефон

**API:**
- `GET /api/hr/employees`
- `POST /api/hr/employees`
- `PUT /api/hr/employees/{id}`

---

## Приоритеты реализации (MVP → потом)

| Приоритет | Сервисы | Порядок |
|-----------|---------|---------|
| P0 (MVP) | AuthService + UserService | 1 |
| P0 (MVP) | SiteService | 2 |
| P0 (MVP) | ScheduleService | 3 |
| P0 (MVP) | LearningService (Groups, Courses) | 4 |
| P1 | TestingService | 5 |
| P1 | JournalService | 6 |
| P2 | PerformService | 7 |
| P2 | CuratorService | 8 |
| P3 | AdmissionService | 9 |
| P3 | HrService | 10 |

---

## Всего User Stories: 49

- SiteService: 6 (UC-1 — UC-6)
- AuthService: 5 (UC-7 — UC-11)
- ScheduleService: 7 (UC-12 — UC-18)
- LearningService: 11 (UC-19 — UC-29)
- TestingService: 5 (UC-30 — UC-34)
- JournalService: 4 (UC-35 — UC-38)
- CuratorService: 4 (UC-39 — UC-42)
- PerformService: 4 (UC-43 — UC-46)
- AdmissionService: 2 (UC-47 — UC-48)
- HrService: 1 (UC-49)
