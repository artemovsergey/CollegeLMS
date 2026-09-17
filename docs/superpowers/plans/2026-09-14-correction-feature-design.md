# План реализации: корректировки расписания — веб-форма, бот, данные

> Спецификация: `docs/superpowers/specs/2026-09-14-correction-feature-design.md` (18 пунктов, утверждена 2026-09-14).
> Ветка: `feature/schedule-views` (после переделок mini-app MAX, чистое рабочее дерево).

## Контекст

Дорабатываем фичу корректировок расписания тремя срезами: **A** — веб-форма (`ManualCorrectionForm.tsx`), **B** — backend (новый endpoint subjects + валидация confirm), **C** — мини-приложение MAX (`DispatcherManual`, `DayFeed`/`WeekFeed`, `ChangesView`, `SearchSheet`, `MaxShell`). Пункты 1, 2, 6 (группа, № пары, добавление/удаление строк) уже реализованы и сохраняются.

Проверено при исследовании:
- `ScheduleResponse` уже содержит `ChangeTags` (backend `ScheduleDtos.cs` + маппер `ScheduleMapper.ToDto` со списком тегов) — пункт 11 готов на backend, не готов рендер в `DayFeed`/`WeekFeed` (пп. 13, 18).
- `ChangeTagBadge.tsx` существует (веб, tooltip) — для бота нужна короткая версия без tooltip (`ChangeBadge.tsx`).
- Бэкенд фильтрует теги по неделе через `GetChangeTagsAsync(items, week)`; нужен лимит/сортировка в новом subjects-эндпоинте.
- `ConfirmAsync` сейчас бросает `InvalidOperationException` при невозможности найти снимаемое занятие — надо превратить в валидацию до транзакции (п. 10).
- Журнал `GetJournalAsync` группирует по (предмет, неделя) без дня недели, дата всегда понедельник — по критерию приёмки нужен `dayOfWeek` + фактическая дата + фильтр «только проведённые (дата ≤ сегодня)».

## Гейты

- **G1** (backend): `dotnet build` + `dotnet test` — зелёные.
- **G2** (frontend): `npm run build` — зелёный.
- **G3** (обзор): Swagger + Postman endpoint; визуальная проверка бота (Playwright) на 393px.

---

## Task 01 — Backend: эндпоинт `GET /api/schedule/subjects?q=`

Уникальные предметы из БД для выпадающих списков (веб-форма и бот).

Файлы:
- `CollegeLMS.API/Dtos/ScheduleSubjectsDto.cs` — **новый**:
  ```csharp
  public class SubjectsResponse
  {
      public List<string> Subjects { get; set; } = new();
  }
  ```
- `CollegeLMS.API/Interfaces/IScheduleService.cs` — добавить `Task<Result<SubjectsResponse>> GetSubjectsAsync(string? q, CancellationToken ct = default);`
- `CollegeLMS.API/Services/ScheduleService.cs` — реализация:
  - SELECT DISTINCT `Subject` из `ScheduleEntries` (непустые), UNION `Subject` + `RemovedSubject` из `ScheduleHistory` (непустые, `RemovedSubject` только если не null);
  - фильтр `q` — `case-insensitive contains` (`ToLower().Contains(qLower)`), пустой `q` — без фильтра;
  - `OrderBy(s => s)`; лимит `Take(200)`; `AsNoTracking()`.
- `CollegeLMS.API/Controllers/ScheduleController.cs` — `[HttpGet("subjects")] [AllowAnonymous]`, `[FromQuery] string? q`, аннотации Swagger (summary на русском, 200/500), `ProducesResponseType`.
- `CollegeLMS.API/SwaggerExamples/SubjectsResponseExample.cs` — **новый** пример успешного ответа (3–4 предмета).
- `docs/spec/CollegeLMS.postman_collection.json` — добавить GET `/api/schedule/subjects?q=Матем` в коллекцию (папка Schedule).

**Чеклист:**
- [ ] DTO создан и зарегистрирован в пространстве имён `CollegeLMS.API.Dtos`
- [ ] Метод в интерфейсе и реализация (DISTINCT по трём источникам, фильтр, сортировка, лимит 200)
- [ ] Эндпоинт с `[AllowAnonymous]` и Swagger-документацией
- [ ] SwaggerExample + Postman
- [ ] `dotnet build` проходит

---

## Task 02 — Backend: валидация `ConfirmAsync` до применения (п. 10)

Сейчас невалидные операции (Remove/Replace/Move на отсутствующее занятие) проваливаются внутри транзакции и кидают `InvalidOperationException`. Нужно: проверить заранее, при ошибке вернуть `400` со списком сообщений и **ничего не применять**.

Изменения в `CollegeLMS.API/Services/ScheduleCorrectionService.cs`:
- Выделить приватный helper по правилам `ApplyEntryAsync`:
  - `Remove` — ищем занятие: `GroupId == X && DayOfWeek == day && NumberPair == NumberPair && Weeks.Contains(Week)` + опционально `NormalizeSubject(RemovedSubject)` и `RemovedTeacherId`;
  - `Replace`/`Move` — ищем: `GroupId == X && DayOfWeek == day && NumberPair == (RemovedNumberPair ?? NumberPair) && Weeks.Contains(Week)` + `Subject == NormalizeSubject(RemovedSubject ?? "")` + учитель (`e.TeacherId == RemovedTeacherId` либо `null`).
- В начале `ConfirmAsync` (после проверки идемпотентного ключа, **до** `BeginTransactionAsync`):
  - проверить `db.Groups.AnyAsync(GroupId)` для каждой записи и целевое занятие по правилам выше;
  - собрать сообщения вида `Группа {group} не найдена.`, `Занятие на {день} {week}-й неделе, пара {pair} не найдено.`;
  - при любых ошибках — `return Result<CorrectionConfirmResult>.Fail(string.Join("; ", errors), 400);`, ничего не записывать.
- Логика `ApplyEntryAsync` остаётся прежней (после успешной pre-validation её throw-ветки недостижимы; транзакция, откат и уведомление MaxBot не меняются).
- Обновить тест `ConfirmAsync_FailedOperation_RollsBackAllEntries`: вместо `Assert.ThrowsAsync<InvalidOperationException>` ожидать `IsSuccess == false`, `StatusCode == 400`, в БД не появилось ни `ScheduleEntries`, ни `ScheduleHistory`.

**Чеклист:**
- [ ] Helper поиска целевого занятия вынесен и используется в pre-validation
- [ ] `ConfirmAsync` возвращает `400` со списком ошибок при невалидных записях
- [ ] При ошибке транзакция не открывается, БД не меняется
- [ ] Тест обновлён на новый контракт; `dotnet test` по этому классу зелёный

---

## Task 03 — Backend: журнал с `dayOfWeek`, фактической датой и фильтром «проведённые» (критерий приёмки)

`CollegeLMS.API/Dtos/JournalDtos.cs`:
- `JournalEntryItem` — добавить `public int DayOfWeek { get; set; }`.

`CollegeLMS.API/Services/ScheduleService.cs` — `GetJournalAsync`:
- группировать по `(Subject, Week, DayOfWeek)` вместо `(Subject, Week)`;
- `Date = MondayOf(SemesterStart) + (week-1)*7 + (dayOfWeek-1)`;
- фильтровать элементы по `Date <= DateTime.UtcNow.Date` (только проведённые); недели из `entry.Weeks` в диапазоне `1..TotalWeeks` оставляем;
- `DayOfWeek` выставлять в item.

Фронтенд-парность (`CollegeLMS.Next/api/schedule.ts`, `JournalEntryItem`): добавить `dayOfWeek: number`. `CollegeLMS.Next/components/max/JournalView.tsx` — подпись даты дополнить днём недели (`dayLabelFromInt(item.dayOfWeek)`).

**Чеклист:**
- [ ] `JournalEntryItem.DayOfWeek` добавлен (DTO + TS-тип)
- [ ] Дата — фактический день, а не понедельник
- [ ] Будущие занятия не попадают в журнал
- [ ] `dotnet test` по новому тесту (см. Task 08) зелёный

---

## Task 04 — Frontend общие компоненты: `fetchSubjects`, чипы примечаний, очищаемый преподаватель (пп. 3–5)

### 04a. API-клиент
`CollegeLMS.Next/api/schedule.ts`:
```ts
export interface SubjectsResponse { subjects: string[] }
export async function fetchSubjects(q = ""): Promise<Result<SubjectsResponse>> {
  const qs = new URLSearchParams()
  if (q) qs.set("q", q)
  const { data } = await api.get<Result<SubjectsResponse>>(
    `/api/schedule/subjects${qs.toString() ? `?${qs.toString()}` : ""}`,
  )
  return data
}
```

### 04b. Чипы примечаний (общий компонент для веб-формы и бота — п. 5, 11)
`CollegeLMS.Next/components/NoteChips.tsx` — **новый**:
- пропсы: `value: string`, `onChange: (v: string) => void`, опционально `hints?: string[]` (по умолчанию `["сам.р.", "замена", "перенос"]`);
- кнопки-чипы над полем; активный чип (значение совпадает) подсвечен; повторный клик очищает поле.

### 04c. Очищаемый преподаватель
`CollegeLMS.Next/components/ui/native-select.tsx` — убедиться, что `NativeSelect` рендерит пустое значение как отдельный пункт; в веб-форме добавить в начало списка предмет преподавателей пункт `Не указан` (`value=""`), при котором в строке `removedTeacherName`/`addedTeacherName` = `""`.

**Чеклист:**
- [ ] `fetchSubjects` в `api/schedule.ts`
- [ ] `NoteChips.tsx` создан (toggle-логика, активный чип)
- [ ] NativeSelect поддерживает пустое значение «Не указан»
- [ ] `npm run build` зелёный

---

## Task 05 — Веб-форма: предметы из БД, кнопки и модальное подтверждение (пп. 3, 4, 5, 7, 8)

`CollegeLMS.Next/components/ManualCorrectionForm.tsx`:
- **Предмет** («Снимается» и «Вводится»): заменить свободный `Input` на `NativeSelect`, загруженный через `fetchSubjects()` (лимит 200). Значение — сам предмет (как группа/преподаватель по имени).
- **Преподаватели**: NativeSelect + пункт «Не указан» (Task 04c).
- **Примечание**: над полем — `<NoteChips value={row.note} onChange={(v) => updateRow(index, { note: v })} />`.
- **Кнопки** (п. 7):
  - «Скачать XLSX» — как сейчас (`generate(false)`: `exportManualCorrection` → `downloadBlob`);
  - «Применить» — замена «Скачать и применить»:
    1. `exportManualCorrection(date, rows)` → blob;
    2. файл → `previewCorrection(file)`; при `preview.errors.length > 0` — показать **весь список ошибок** (модалка или тост со списком, п. 8) и остановиться;
    3. открыть модалку предпросмотра операций (тип, группа, день, неделя, пара, предмет, преподаватель) на основе `preview.entries`;
    4. по «Применить» → `confirmCorrection(preview.entries, crypto.randomUUID())`, по «Отмена» — закрыть.
  - Важно: файл при «Применить» скачивается тоже (п. 7: «формирует файл и показывает модальное окно»).
- **Модалка**: `CollegeLMS.Next/components/ui/dialog.tsx` (уже есть) — новый компонент `CollegeLMS.Next/components/CorrectionPreviewDialog.tsx` со списком операций и серверными ошибками.

**Чеклист:**
- [ ] Список предметов загружается из БД в обе колонки
- [ ] Преподаватель очищается через «Не указан»
- [ ] Чипы работают (подстановка + сброс)
- [ ] «Скачать XLSX» и «Применить» разделены; та же строка не падает на старый «Скачать и применить»
- [ ] Модалка предпросмотра: операции + «Применить»/«Отмена»
- [ ] Ошибки сервера выводятся целиком (список)
- [ ] `npm run build` зелёный

---

## Task 06 — Бот `DispatcherManual`: синхронизация с веб-формой (пп. 11, 14)

`CollegeLMS.Next/components/max/DispatcherManual.tsx`:
- **Предмет** (Add/Replace): заменить `Input` на «ввод фильтрует список подсказок» — поле ввода + список совпадений из `fetchSubjects(q)` (debounce ~250мс, как в `GroupPicker`), выбор подставляет `subject`.
- **Преподаватель**: уже очищаемый (`TeacherPicker`, кнопка «Сбросить») — оставить; при необходимости добавить пункт «Не указан».
- **Примечание**: добавить `<NoteChips value={note} onChange={setNote} />` над полем.
- **Подтверждение**: перед `confirmCorrection` показывать **bottom sheet**:
  - список `ops` (тип, группа, день, неделя, пара, предмет, преподаватель);
  - кнопки «Применить» / «Отмена»;
  - при ошибке `confirmCorrection` — оставить sheet с сообщением (full error), как п. 8.
  - Использовать существующий паттерн `.max-app__sheet` (SearchSheet) или новую CSS-обёртку `max-app__sheet` для модалки.
- Добавить медиа-проверку на 393px (Playwright): форма не выходит за экран.

**Чеклист:**
- [ ] Предмет-автокомплит из БД
- [ ] Чипы примечаний единые с веб-формой
- [ ] Bottom-sheet подтверждение с «Применить»/«Отмена»
- [ ] Ошибки применения видны целиком
- [ ] Визуальная проверка на 393px

---

## Task 07 — Бот: хирургия UI (пп. 12, 13, 15–18)

`CollegeLMS.Next/components/max/MaxShell.tsx`:
- **П. 17**: пятая вкладка «Журнал» (`href: "/max/journal"`, иконка `BookOpen`) — рендерится только при `useMaxContext().profile.role === "Teacher"`.
- **П. 12**: убрать дубль заголовка «Расписание» на экране расписания — проверить на рендере (Playwright), оставить один `Typography.Title` в `ScheduleView`.

`CollegeLMS.Next/components/max/ScheduleView.tsx`:
- **П. 15** (иерархия расписания): неделя → день по возрастанию `dayOfWeek` → пары по возрастанию `numberPair`; убрать дубли заголовков дней. Убедиться, что `WeekFeed` итерирует дни в стабильном порядке (массив `WEEKDAYS` уже Mon→Sat; при наличии воскресенья `dayOfWeek=0` — отдельный заголовок). Сортировка пар — в `DayFeed` по `numberPair`.

`CollegeLMS.Next/components/max/DayFeed.tsx` (+ `WeekFeed` — проброс):
- **П. 13/18**: в строке пары рендерить короткий чип корректировки `components/max/ChangeBadge.tsx` (без tooltip): подпись вида `+ Замена · нед.3` из `entry.changeTags` (стили из `ChangeTagBadge.tsx`, адаптированные для мобильного: компактная подпись, `max-app__badge--*`).

`CollegeLMS.Next/components/max/SearchSheet.tsx`:
- **П. 14**: над полем поиска видимая подпись «Поиск» (`<label>`), поле получает `aria-label`/связь через `id`/`htmlFor`.

`CollegeLMS.Next/components/max/ChangesView.tsx` + `app/max/max.css`:
- **П. 16**: `.max-app__filter-row { margin-top: … }` (отступы между заголовком «Изменения», фильтром «Неделя» и списком).

`app/max/max.css`:
- **П. 13**: выравнивание `.max-app__tab` — иконка + подпись строго по центру (`align-items: center; justify-content: center; text-align: center`), при 5 вкладках на узких экранах без смещения (проверка 393px).

**Чеклист:**
- [ ] Вкладка «Журнал» только для Teacher
- [ ] Один заголовок «Расписание» на экране расписания
- [ ] Иерархия неделя→день→пара стабильна, дублей дней нет
- [ ] Подпись «Поиск» над полем в SearchSheet
- [ ] Отступы в ChangesView
- [ ] `ChangeBadge.tsx` создан и рендерится в DayFeed/WeekFeed
- [ ] `npm run build` зелёный; Playwright-проверка 393px

---

## Task 08 — Тесты

### Backend (xUnit)
`CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs`:
- `GetSubjectsAsync_ReturnsDistinctSubjects_WhenNoQuery` — повторяющиеся предметы схлопываются;
- `GetSubjectsAsync_FiltersByQuery_CaseInsensitive` — `q="мат"` вернёт только содержащие;
- `GetSubjectsAsync_LimitsTo200` (опционально);
- `GetJournalAsync_IncludesDayOfWeekAndConductedOnly` — будущее занятие (дата > сегодня) не попадает, у прошлого `DayOfWeek` и фактическая дата корректны.

`CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs`:
- `ConfirmAsync_InvalidRemove_ReturnsBadRequestAndAppliesNothing` — Remove на пустой слот → `400`, БД чистая.
- Обновить `ConfirmAsync_FailedOperation_RollsBackAllEntries` → `400` (см. Task 02).

### Интеграционные
`CollegeLMS.Tests/Integration/Controllers/ScheduleControllerTests.cs`:
- `GetSubjects_ReturnsOk_WithSubjects` — эндпоинт без авторизации (`[AllowAnonymous]`).

### Frontend
- `npm run build` (typecheck).
- E2E/Playwright script — визуальная проверка бота на 393px (вкладки, форма корректировки, чипы, bottom-sheet, бейджи изменений).

**Чеклист:**
- [ ] Unit-тесты subjects (distinct/filter/limit)
- [ ] Unit-тесты журнала (dayOfWeek + только проведённые)
- [ ] Unit-тест confirm: 400 + ничего не применено
- [ ] Интеграционный тест `/api/schedule/subjects`
- [ ] `dotnet test` полностью зелёный

---

## Task 09 — Документация

- `SwaggerExamples/SubjectsResponseExample.cs` (Task 01).
- Postman-коллекция (Task 01).
- (Опционально) PlantUML sequence/class не обязателен — фича не меняет архитектуру. Пропускаем.

**Чеклист:**
- [ ] SwaggerExample
- [ ] Postman
- [ ] Swagger UI отображает endpoint с русским summary

---

## Финальная проверка (verification-before-completion, G3)

- [ ] `dotnet build` — зелёный
- [ ] `dotnet test` — зелёный
- [ ] `npm run build` — зелёный
- [ ] `GET /api/schedule/subjects?q=` отвечает и фильтрует (на локальной БД/Postman)
- [ ] Confirm с некорректной операцией → 400, ничего не применено (Postman/тест)
- [ ] Playwright: проверка бота на 393px — вкладки, диспетчер (предмет из БД, чипы, bottom-sheet), бейджи изменений в расписании, OneHeader в расписании
- [ ] Commit'ы по фазам (metrics style: `feat:`, `test:`, `docs:`)