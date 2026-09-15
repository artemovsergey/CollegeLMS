# Дизайн: нативный диспетчер корректировок в Max-боте

Дата: 2026-09-15
Статус: утверждён пользователем

## Проблема

Единственный способ создать корректировку расписания — mini-app (импорт XLSX).
Меню диспетчера в боте ведёт кнопкой open_app в веб-приложение. Пользователь
хочет: диспетчер создаёт корректировки полностью из чата бота, без редиректа.

## Решение

Пошаговый визард (машина состояний на пользователя) внутри бота. Применение —
существующий эндпоинт `POST /api/schedule/correction/confirm`
(Bearer-токен диспетчера + заголовок Idempotency-Key), файл не нужен.

## Поток

1. `/dispatcher` → ввод пароля → «✅ Диспетчер авторизован» (без изменений).
2. Меню диспетчера:
   - «➕ Новая корректировка» — визард (callback `dispatcher:wizard`);
   - «📊 Отправить XLSX» — существующий сценарий;
   - «🔙 Меню».
   Кнопка open_app «Создать корректировку» удаляется.

## Шаги визарда

| # | Шаг | Ввод | Payload / формат |
|---|-----|------|------------------|
| 1 | Тип изменения | кнопки | `wiz:type:Add/Remove/Replace/Move` |
| 2 | День недели | кнопки Пн–Сб | `wiz:day:{1..6}` |
| 3 | Неделя | сетка 1..totalWeeks + «Сегодня» | `wiz:week:{n}` |
| 4 | Группа | пагинированный список | `wiz:group:{id}:{name}` |
| 5a | Снимаемая пара (Remove/Replace/Move) | пары дня из расписания группы | `wiz:rem:{pair}` |
| 5b | Новая пара (Add/Move) | кнопки 1..7 | `wiz:pair:{n}` |
| 6 | Предмет (Add/Replace/Move) | текст | состояние `AwaitingSubject` |
| 7 | Преподаватель | список / «— без преподавателя» | `wiz:teacher:{id}` / `wiz:teacher:none` |
| 8 | Примечание | текст или «—» | состояние `AwaitingNote` |
| 9 | Предпросмотр → «✅ Применить» | кнопки | `wiz:apply` / `wiz:cancel` |

- «❌ Отмена» (`wiz:cancel`) доступна на каждом шаге.
- Replace/Remove: `numberPair` = номер снимаемой пары; у Move дополнительно
  спрашивается новая пара; Add начинается с новой пары.
- Если у группы на выбранный день/неделю пар нет — Remove/Replace/Move
  невозможны, бот предлагает Add или отмену.
- Текстовые сообщения обрабатываются только в состояниях
  `AwaitingSubject` / `AwaitingNote`; вне визарда — прежнее поведение.

## Сборка записи (CorrectionPreviewEntry)

- Row=1, GroupId/GroupName, ChangeType, DayOfWeek (1..6), Week, NumberPair,
  Subject (null у Remove), TeacherId/TeacherName (nullable),
  RemovedSubject/RemovedTeacherId/RemovedTeacherName/RemovedNumberPair
  (у Remove/Replace/Move — из выбранной пары расписания), Note (null при «—»).

## Применение

`CollegeLmsApiClient.ConfirmCorrectionAsync(entry, token, ct)`:
POST `/api/schedule/correction/confirm`, Bearer, `Idempotency-Key: Guid.NewGuid()`.
Ответ: «✅ Применено записей: N» или ошибка API. После успеха визард сбрасывается.

## Архитектура

- `Services/DispatcherCorrectionWizard.cs` — чистая машина состояний:
  шаг, подсказка, клавиатура, приём ввода (кнопка/текст), сборка entry.
  Не зависит от Max API и БД — покрывается юнит-тестами.
- `Clients/CollegeLmsApiClient` — метод `ConfirmCorrectionAsync` + DTO
  (`CorrectionEntryDto`, `CorrectionConfirmResponse`).
- `Bot/MaxBotService` — клей: хранение состояния per user, клавиатуры,
  вызовы API, авторизация.

## Тесты

Юнит-тесты `DispatcherCorrectionWizardTests`:
- переходы по шагам для всех 4 типов;
- отмена на произвольном шаге;
- guard: Remove без пар на день;
- сборка entry: все поля для каждого типа;
- текстовый ввод предмета/примечания, «—» → null.

## Вне scope

Кросс-синхронизация с localStorage-избранным mini-app, редактирование
уже применённых корректировок, приём XLSX-файлов в чат.
