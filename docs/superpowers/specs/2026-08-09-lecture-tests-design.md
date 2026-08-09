# Дизайн: тест к каждой лекции

Дата: 2026-08-09
Статус: утверждён (ожидает реализации)

## Контекст

Пользователь: «преподаватель должен иметь возможность создать тест к каждой лекции, студент — пройти этот тест и видеть результат: прошёл/не прошёл, баллы/оценку».

**Бэкенд тестов уже реализован полностью**: `TestingController` (17 эндпоинтов, `[Route("api/tests")]`), `TestingService` (772 строки), сущности `Test`, `TestQuestion`, `TestAttempt`, `TestAnswer`, `TestAssignment`, связь `Lecture.TestId` (nullable FK, миграция `20260716121207_AddTestIdToLecture`), статистика `GetStatsAsync`. На фронтенде тесты доступны только админу в `/admin/testing`; студенческого флоу нет вообще, `LectureForm` не передаёт `TestId`.

## Решения пользователя

1. Создание теста — **на странице лекции** (кнопки «Создать тест к лекции», «Редактировать», «Статистика»).
2. Доступ студента — **по записи на курс** (CourseGroup); `TestAssignment` остаётся опциональным.
3. Результат — **баллы + «Пройден/Не пройден»**, без 5-балльной шкалы.
4. Типы вопросов — **только одиночный и множественный выбор** (без OpenAnswer).
5. Место показа результата — **страница лекции + значок статуса в списке занятий курса**.

## Изменения Backend (точечные)

| Изменение | Где |
|---|---|
| `LectureId?` в `CreateTestRequest`; валидация: лекция существует, принадлежит `CourseId` теста, у лекции ещё нет теста (конфликт → 400) | `Dtos/TestDtos.cs`, `Validators/TestRequestValidator.cs` |
| При создании теста с `LectureId` — установить `lecture.TestId = test.Id` (одна транзакция) | `Services/TestingService.cs` (`CreateAsync`) |
| **Доступ по CourseGroup**: в `StartTestAsync` — если у теста есть лекция (`test.Lecture`), студент записан на курс (CourseGroup) → доступ разрешён; проверка TestAssignment применяется только если тест назначен группам (или нет лекции) | `Services/TestingService.cs` (стр. ~435-444) |
| **Фикс PassingScore**: `Passed = Percentage >= PassingScore` вместо `Score >= PassingScore` (сейчас при MaxScore ≠ 100 семантика ломается) | `Services/TestingService.cs` (стр. ~623-624) |
| Валидатор `SubmitAnswersRequest`: не пусто, ≤ число вопросов теста, все QuestionId принадлежат тесту | `Validators/TestRequestValidator.cs` |
| SwaggerExamples: `AttemptResponseExample`, `TestResultResponseExample`, `TestStatsResponseExample`; `ProducesResponseType` на `TestingController` | `SwaggerExamples/`, `Controllers/TestingController.cs` |
| Postman-коллекция: добавить группу `Tests` (создание, старт, сабмит, результат, статистика) | `docs/spec/CollegeLMS.postman_collection.json` |

Без изменений: сущности, миграции, остальные эндпоинты тестов.

## Изменения Frontend (CollegeLMS.Next)

### 1. Страница лекции `(authenticated)/courses/[id]/lectures/[lectureId]/page.tsx`

**Преподаватель (canEdit):**
- Лекция без теста → блок-кнопка «+ Создать тест к лекции» → модальная форма (название, описание, таймер мин, попытки, проходной %, перемешивание вопросов/вариантов) → `POST /api/tests` с `lectureId` → конструктор вопросов (добавить 1+ вопросов: тип выбор, текст, варианты, правильные, баллы) → тест активен.
- Лекция с тестом → карточка «Тест»: название, вопросов N, балл прохождения, кнопки «Редактировать» (вопросы/настройки), «Статистика» → страница/модал статистики (`GET /api/tests/{id}/stats`).

**Студент:**
- Без попыток → кнопка «Пройти тест» → `/courses/[id]/lectures/[lectureId]/test`.
- С попытками → результат: «Пройден: 85% (34/40)» / «Не пройден: 40% (16/40)», кнопка «Пересдать» если остались попытки (по `MaxAttempts` и числу попыток), иначе «Попытки исчерпаны».

### 2. Страница прохождения `(authenticated)/courses/[id]/lectures/[lectureId]/test/page.tsx`

- `GET /api/tests/{testId}/start` → вопросы (без правильных ответов), таймер обратного отсчёта (`TimeLimitMinutes`, блок submit при истечении — сабмит с текущими ответами).
- Выбор одиночный (radio) / множественный (checkbox). Отправка `POST /api/tests/{testId}/attempt/{attemptId}/submit`.
- Результат с ответами: по каждому вопросу — верно/неверно, правильный ответ (если `ShowCorrectAnswers`), итог: баллы, %, «Пройден/Не пройден», кнопки «Вернуться к лекции».

### 3. Список занятий курса (учитель и студент)

- `(authenticated)/courses/[id]/page.tsx` и `(authenticated)/my/courses/[id]/page.tsx`: у лекции с `testId` — значок статуса: пройден (зелёный), не пройден (оранжевый), не начат (серый). Статус берётся из результатов студента (`GET /api/my/test-results` для текущего пользователя; для преподавателя — только иконка теста без статуса).
- `LectureForm.tsx`: убрать/не добавлять поле теста (привязка только через создание на странице лекции).

### 4. Типы и API-клиент

- `types/index.ts`: исправить `TestResponse.courseName → courseTitle` (+ добавить недостающие поля), убрать лишний `testId` из `TestQuestionResponse` при необходимости, добавить интерфейсы `StartTestResponse`, `TestQuestionDto`, `SubmitAnswersRequest`, `AnswerDto`, `TestResultResponse`, `AnswerReviewDto`, `TestStatsResponse`, `StudentResultDto`, `CreateTestRequest.lectureId`.
- `/admin/testing`: оставить только для Admin (кнопки для Teacher уже скрыты — скрыть весь пункт/страницу для не-admin).

## Что НЕ делаем (вне скоупа)

- OpenAnswer и ручная проверка; оценка 2–5; экспорт PDF/XLSX; `GET /api/tests/{testId}/attempt/{attemptId}`; пагинация `GET /api/tests`; отдельная страница «все тесты курса» у студента; назначение тестов группам в новом UI (остаётся в `/admin/testing`).

## Проверка

- Backend: `dotnet build` (Docker) + юнит/интеграционные тесты (обновить `TestingServiceTests` под новый `StartTestAsync` и фикс PassingScore; `TestingControllerTests` — создание с `LectureId`).
- Frontend: `npm run build`; ручная проверка: учитель создаёт тест → студент проходит → статусы в списке занятий.
- E2E (Playwright): создание теста преподавателем, прохождение студентом, отображение результата.
