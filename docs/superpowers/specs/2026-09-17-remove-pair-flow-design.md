# Направляющий флоу «Снять пару» — дизайн

> Утверждено пользователем 2026-09-17. Заменяет табличную форму ручной корректировки
> (веб `ManualCorrectionForm` и max `DispatcherManual`) направляющим UX «группа → дата → пары → причина → подтверждение».

## Контекст

- **Web** — вкладка «Корректировка расписания → Вручную» (`dispatcher/correction/page.tsx`) рендерит `ManualCorrectionForm` (таблица строк с Add/Remove/Replace/Move, скачивание XLSX).
- **Бот** — `components/max/DispatcherManual.tsx` использует ту же табличную модель (вкладка «Тип операции», список `ops`, `ConfirmOpsSheet`).
- **Backend** — `ScheduleCorrectionService` (preview/export/confirm/history), `ConfirmAsync` применяет операции идемпотентно (`Idempotency-Key`), `ApplyEntryAsync` для `Remove`: снимает неделю из `Weeks`; при «сам.р.» (`IsSelfStudyNote`) пара остаётся с бейджем. `ScheduleService.GetAllAsync` уже фильтрует по дате (`StudyWeek.WeekOf` + день).
- Роли: `Dispatcher`, `Admin`.

## Решение

Таблица ручной корректировки заменяется направляющим флоу снятия пары. Сейчас реализуется
только операция `Remove`; каркас флоу (шаг операции) проектируется расширяемым, чтобы позже
без переделки добавить Add/Replace/Move в тех же интерфейсах.

Общие принципы:

- Неделя вычисляется сервером (`StudyWeek.ForDate`) — клиент не знает и не передаёт неделю для отображения; в `confirm` week берётся из ответа «дня».
- Причина «сам.р.» → пара не удаляется, остаётся с бейджем (существующая логика `ApplyEntryAsync`).
- Одна операция за действие (по одной), с диалогом подтверждения перед применением.
- Повторное снятие пары, уже снятой/отмеченной на эту неделю, заблокировано на UI и отклоняется сервером (занятие не найдено — 400).
- Ошибки сервера показываются целиком (тост/сообщение), статус флоу не сбрасывается.
- Сообщения на русском, запросы через `lib/api.ts`, справочники из БД.

## Поток

```
1. Группа  → выбор из списка групп БД
2. Дата    → календарь Пн–Пт
3. Пары    → GET /api/schedule/correction/day?groupId=&date= (сервер: week, пары, changeTags)
            активные на неделю; уже снятые/«сам.р.» на эту неделю — неактивны
4. Причина → радио: «Удалить из расписания» / «Самостоятельная работа (сам.р.)» / «Своё примечание»
5. Подтверждение → диалог с деталями (группа, дата, пара, предмет, преподаватель, причина) → «Применить»
6. Применение → confirmCorrection([entry], uuid) → тост успеха → перезагрузка списка пар
```

## Backend

### `GET /api/schedule/correction/day?groupId={guid}&date={yyyy-MM-dd}`

- Авторизация: `[Authorize(Roles = "Dispatcher,Admin")]`, маршрут в `ScheduleCorrectionController`.
- Валидация (fail → `400`): `groupId` обязателен и существует в БД; `date` обязательна, `DayOfWeek` — не выходной.
- `week = StudyWeek.ForDate(date)`, `dayOfWeek = date.DayOfWeek` (единый источник с `ParseWorkbookAsync`).
- Возвращает пары дня: фильтр `GroupId == groupId && DayOfWeek == day && Weeks.Contains(week)`, сортировка по `NumberPair`, с `changeTags`.
- Реализация в `ScheduleCorrectionService` новым методом `GetDayAsync(Guid groupId, DateTime date, CancellationToken ct)` в `IScheduleCorrectionService`.
- Реализация переиспользует существующий путь построения `ScheduleResponse` (включая `changeTags`) — без дублирования правил маппинга.

### DTO

```csharp
public class CorrectionDayResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public List<ScheduleResponse> Entries { get; set; } = [];
}
```

### Применение

Существующий `POST /api/schedule/correction/confirm` (без изменений контракта). Клиент собирает одну запись `Remove`:

```typescript
{
  row: 0,
  changeType: "Remove",
  groupId, groupName,
  dayOfWeek, week,            // week — из ответа /correction/day
  numberPair: entry.numberPair,
  subject: null, teacherId: null, teacherName: null,
  removedSubject: entry.subject,
  removedTeacherId: entry.teacherId,
  removedTeacherName: entry.teacherName,
  removedNumberPair: null,
  note,
}
```

`ConfirmAsync`/`ApplyEntryAsync` не изменяются.

## Frontend (веб)

- Вкладка «Вручную» рендерит новый компонент `components/RemovePairFlow.tsx` вместо `ManualCorrectionForm`.
- `ManualCorrectionForm` и кнопка «Скачать XLSX» из ручной формы удаляются (экспорт остаётся в импорте XLSX/шаблонах).
- Компонент:
  - выбор группы (NativeSelect из `/api/groups`);
  - выбор даты (Input type=date, дни Пн–Пт; выходной блокируется);
  - список пар из `/correction/day`: карточки (№, время, предмет, преподаватель, аудитория); загрузка/пустота/ошибка — состояния;
  - активность пары: заблокировано, если в `changeTags` есть `Remove`/`Replace` с `week === day.week` (подпись «Уже снято»/«сам.р.»);
  - клик по паре → причина (радио три варианта + поле «Своё примечание» при выборе третьего) → диалог подтверждения → «Применить»;
  - успех → тост, сброс причины, перезагрузка списка;
  - ошибка применя — тост/сообщение, остаётся на том же шаге.
- Общий модуль построения записи: `lib/correction.ts` → `buildRemoveEntry(day, scheduleEntry, note)` используется вебом и mini-app.

## Frontend (max mini-app)

- `components/max/DispatcherManual.tsx` заменяется аналогичным флоу (шаги: группа → дата → пары → причина → bottom-sheet подтверждение), сохраняя проп `onApplied`.
- Стилизация — существующие классы max-app (`max-app__select`, `max-app__option`, конкретизирующие) и `ConfirmOpsSheet` для одного подтверждаемого действия.
- Компоненты поиска (GroupPicker) переиспользуются из текущего `DispatcherManual`.

## Тестирование

- **Unit** (`ScheduleCorrectionServiceTests`): `GetDayAsync` — валидация группы/даты (400), корректный `week`/`dayOfWeek`, фильтрация пар по неделе, сортировка, `changeTags`.
- **Интеграционные** (`Integration/Controllers`): `GET /api/schedule/correction/day` — 401/403 без роли, 400 невалидные входы, 200 с корректными данными.
- **Регресс**: существующие тесты корректировок (включая «сам.р.») — зелёные.
- **Frontend**: `npm run build`; ручной сценарий в вебе и mini-app.

## Критерии приёмки

- [ ] `dotnet build` и `dotnet test` проходят.
- [ ] `npm run build` проходит.
- [ ] `GET /api/schedule/correction/day` отдаёт пары дня с серверной `week` и `changeTags`, валидирует входы.
- [ ] Веб-вкладка «Вручную» = направляющий флоу снятия (без таблицы и XLSX-кнопки).
- [ ] Mini-app `DispatcherManual` = тот же направляющий флоу.
- [ ] Снятие пары с «сам.р.» → пара остаётся с бейджем; снятие без причины → неделя снимается.
- [ ] Повторное снятие снятой на неделе пары заблокировано на UI.
- [ ] Ошибки применя показываются целиком, статус флоу сохраняется.

## Будущее (не входит в эту итерацию)

- Расширение флоу на Add/Replace/Move («Все операции») в тех же интерфейсах.
- Агрегация уведомлений Max (одно сообщение на получателя с датой и всеми позициями).
- ~~Сущность-пакет корректировок (редактирование позиций после импорта).~~ ✅ Реализовано в `CorrectionBatch`/`CorrectionBatchService`.