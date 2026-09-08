# Дизайн-спецификация: Корректировка расписания (импорт XLSX + история + уведомления в боте Max)

**Дата:** 2026-09-08
**Статус:** утверждено пользователем
**Цель:** реализовать оперативную корректировку расписания через файл `Корректировка.xlsx`, историю изменений и уведомления затронутым группам/преподавателям в мессенджере MAX.

## Контекст

Диспетчер периодически вносит изменения в расписание через табличный файл `Корректировка.xlsx` (структура: «Снимается по расписанию» / «Вводится в расписание», № пары, примечание). При импорте определяется набор операций над парами, расписание обновляется, группы и преподаватели из файла получают уведомления в боте MAX, ведётся история изменений.

В проекте уже есть:
- `ScheduleImportService` — полный импорт расписания (матричный формат) с превью/подтверждением.
- `ScheduleService` — CRUD записей расписания (`ScheduleEntry` с `Weeks: List<int>`).
- `CollegeLMS.MaxBot` — бот расписания в мессенджере MAX (день/неделя/дата, настройки, ежедневные уведомления `ScheduleNotifier` в 07:30 МСК).
- Frontend страница `dispatcher/correction` — заглушка «Раздел в разработке».

## Принятые решения

| Вопрос | Решение |
|--------|---------|
| Механизм внесения | Загрузка XLSX → превью → подтверждение (аналогично текущему импорту расписания) |
| Формат операций | Строго по колонкам: «Снимается» / «Вводится», № пары (F) обязателен |
| Область недель | Корректировка на конкретную дату → неделя семестра N, применяется только к неделе N |
| Модель данных | Манипулируем `ScheduleEntry` (добавляем/снимаем/меняем `Weeks`) |
| История | Отдельная сущность `ScheduleHistory` — метаданные операций |
| Механизм уведомлений | Прямой HTTP POST из API в MaxBot при подтверждении |
| Хранение в боте | Своя таблица `schedule_revisions` для пункта «Мои изменения» |
| Маркировка в UI | Бейджи в общем расписании, только для недели изменения |
| Оповещение бота | Мгновенное сообщение при подтверждении |

## Архитектура

```
[Frontend: dispatcher/correction] → POST /api/schedule/correction/preview|confirm
                                            │
                                          API (ScheduleCorrectionService)
                                            │ парсит XLSX, применяет через ScheduleEntry
                                            │ пишет в ScheduleHistory
                                            ▼
                                  [MaxBotHttpClient] → HTTP POST /notify
                                            │
                                          MaxBot
                                            │ сохраняет schedule_revisions
                                            │ рассылает подписчикам (group/teacher)
                                            ▼
                                     MAX Messenger (пользователи)
```

## Секция 1 — Backend: модель данных (CollegeLMS.API)

### 1.1 Enum `ScheduleChangeType` — `Entities/Enums/ScheduleChangeType.cs`

```csharp
namespace CollegeLMS.API.Entities.Enums;
public enum ScheduleChangeType { Add, Remove, Replace }
```

- `Add` — ввести занятие на неделю N
- `Remove` — снять занятие на неделю N
- `Replace` — заменить (снять + ввести) на неделю N

### 1.2 Сущность `ScheduleHistory` — `Entities/ScheduleHistory.cs`

Наследует `Entity` (Id, CreatedAt, UpdatedAt). Хранит **метаданные операции** (не полный snapshot):

| Поле | Тип | Назначение |
|------|-----|-----------|
| Id | Guid | PK |
| ChangeType | ScheduleChangeType | Add / Remove / Replace |
| AppliedAt | DateTime | когда применено |
| AppliedByUserId | Guid | ID диспетчера |
| GroupId | Guid | группа |
| TeacherId | Guid? | преподаватель |
| Subject | string | предмет (вводимой/действующей пары) |
| Room | string? | аудитория |
| DayOfWeek | DayOfWeek | день недели |
| NumberPair | int | пара |
| Week | int | неделя семестра (из даты A3) |
| Note | string? | примечание (колонка G) |
| RemovedSubject | string? | для Replace — снятый предмет |
| RemovedTeacherId | Guid? | для Replace — снятый преподаватель |
| RemovedRoom | string? | для Replace — снятая аудитория |
| RemovedNumberPair | int? | для Replace — снятая пара |

- Для `Remove`: заполнены Subject/Teacher/Room/NumberPair/Week (снимаемое).
- Для `Add`: заполнены Subject/Teacher/Room/NumberPair/Week (вводимое).
- Для `Replace`: заполнены и «вводимые», и `Removed*` поля.

### 1.3 EF Configuration — `Data/Configurations/ScheduleHistoryConfiguration.cs`

- Таблица `schedule_history`, snake_case
- `HasKey`, `ValueGeneratedNever` для Id
- String props: `HasMaxLength` (Subject 200, Room 50, Note 200, RemovedSubject 200, RemovedRoom 50)
- `DayOfWeek`, `ChangeType`: `HasConversion<string>()` + `HasMaxLength`
- FK: Group (Restrict), Teacher (SetNull)
- Индексы: `group_id`, `day_of_week`, `applied_at`

### 1.4 `ScheduleEntry` — НЕ меняем схему

Корректировка манипулирует полями существующей `ScheduleEntry`:
- **Add** → новая `ScheduleEntry` с `Weeks=[N]`
- **Remove** → убрать неделю N из `Weeks` найденной записи; если `Weeks` опустел — удалить запись
- **Replace** → Remove + Add

### 1.5 DTO и мапперы

- `Dtos/ScheduleHistoryDtos.cs` — `ScheduleHistoryResponse` (плоский)
- `Mappers/ScheduleHistoryMapper.cs` — `ScheduleHistory.ToDto()`

`ScheduleHistoryResponse`:
```csharp
Guid Id;
ScheduleChangeType ChangeType;
DateTime AppliedAt;
Guid? AppliedByUserId;
Guid GroupId; string GroupName;
Guid? TeacherId; string? TeacherName;
string Subject; string? Room;
DayOfWeek DayOfWeek; int NumberPair;
int Week; string? Note;
string? RemovedSubject; string? RemovedRoom; int? RemovedNumberPair;
```

## Секция 2 — Backend: парсер и сервис корректировок

### 2.1 Формат файла (реальный `Корректировка.xlsx`)

```
A1:    "КОРРЕКТИРОВКА"
A3:    "на 01.09.2026 г. (вторник)"   → дата → Week = ForDate(date), Day = date.DayOfWeek
A5:A6: "Группа"                      → заголовок; A7… = имя группы
B5:C5: "Снимается по расписанию"     → B = Предмет, C = Преподаватель
D5:E5: "Вводится в расписание"       → D = Предмет, E = Преподаватель
F5:F6: "№"                           → № пары
G5:G6: "Примеч."                     → примечание (информативно)
```

### 2.2 Определение операции по заполненности

- только `B/C` → **Remove**
- только `D/E` → **Add**
- `B/C` и `D/E` → **Replace**

№ пары (колонка F) применим к операции; для Replace — пара ввода (пара снятия — из «снятия»/Removed-полей либо из F).

### 2.3 Парсинг значений

- Предмет/Аудитория — нормализация через методы, переиспользуемые из `ScheduleImportService` (`NormalizeSubject`, `NormalizeTeacherName`).
- Преподаватель — сопоставление с БД по `Teacher.User.FullName`.
- Неделя/день — из даты A3.

### 2.4 `ScheduleCorrectionService` — `Services/ScheduleCorrectionService.cs`

```csharp
public class ScheduleCorrectionService(AppDbContext db, MaxBotHttpClient maxBot)
{
    public CorrectionPreviewResponse ParseCorrectionFile(Stream stream, CancellationToken ct);
    public Task<Result<CorrectionConfirmResult>> ConfirmAsync(CorrectionConfirmRequest request, CancellationToken ct);
}
```

**PreviewAsync**: парсит XLSX → список коррекций; для каждой — сопоставление группы/преподавателя, определение операции, валидация. Возвращает превью + ошибки.

**ConfirmAsync** (в транзакции):
1. Для каждой коррекции применить к `ScheduleEntry` (Add/Remove/Replace).
2. Для каждой коррекции записать `ScheduleHistory`.
3. После коммита — `maxBot.SendChangesAsync(changes, ct)` (HTTP POST /notify в MaxBot).

### 2.5 Валидация файла корректировки

Валидация выполняется в парсере/превью и блокирует подтверждение при проблемах. Ошибки понятны пользователю: строка/колонка + что исправить.

**Уровень 1 — Структура файла:**
| Ошибка | Сообщение |
|--------|-----------|
| Файл не XLSX / битый | «Файл не является корректным XLSX. Сохраните файл в формате .xlsx.» |
| Пустой лист | «В файле нет данных.» |
| Нет строки с датой (A3) | «Не найдена дата корректировки в ячейке A3 (например, „на 01.09.2026 г.“).» |
| Дата не распознана | «Дата в A3 не распознана. Формат: „на ДД.ММ.ГГГГ г.“.» |
| Не найден заголовок «Группа» (A5) | «Не найден заголовок „Группа“ в ячейке A5.» |
| Не найдены заголовки «Снимается…»/«Вводится…» | «Не найдены колонки „Снимается по расписанию“ или „Вводится в расписание“.» |

**Уровень 2 — Данные строк:**
| Условие | Сообщение |
|---------|-----------|
| Не указана группа (A пусто) | «Строка N: не указана группа.» |
| № пары (F) пуст/не число/вне 1-8 | «Строка N: не указан/некорректен № пары (F). Ожидается число 1–8.» |
| C/D/E заполнены частично | «Строка N: заполнен предмет, но не указан преподаватель (или наоборот).» |
| Ни «Снимается», ни «Вводится» | «Строка N: не заполнены ни „Снимается“, ни „Вводится“.» |
| Группа не найдена в БД | «Строка N: группа „ПО262“ не найдена в системе.» |
| Преподаватель не найден в БД | «Строка N: преподаватель „Петренко В.Б.“ не найден.» |
| День A3 = сб/вс | «Указана дата на выходной день. Корректировка применяется к учебным дням.» |

**Уровень 3 — Бизнес-логика:**
| Условие | Сообщение |
|---------|-----------|
| Add на неделю: пара занята (пересечение) | «Строка N: на {день} {неделя}-й неделе, пара {M} уже занята.» |
| Remove: пара на этой неделе не найдена | «Строка N: занятие на {день} {неделя}-й неделе, пара {M} не найдено.» |
| Replace: пары снятия и ввода совпадают | «Строка N: пара снятия и ввода одинакова.» |

**Формат ошибок (расширение `ScheduleValidationError` полем `level`):**
```json
{ "row": 7, "column": 4, "level": "structure|data|logic",
  "message": "Строка 7: заполнен предмет, но не указан преподаватель." }
```

Превью показывает ошибки; «Подтвердить» активно только при `errors == []`.

### 2.6 Новый клиент `MaxBotHttpClient` — `Services/MaxBotHttpClient.cs`

```csharp
public class MaxBotHttpClient(HttpClient http)
{
    public Task SendChangesAsync(IReadOnlyList<ScheduleChangeDto> changes, CancellationToken ct);
}
```

Базовый URL — `MaxBot:BaseUrl` (в compose → `http://max-bot:8080`). Fail-safe (недоступность бота не роняет подтверждение).

## Секция 3 — Backend: Endpoints

`POST /api/schedule/correction/preview`, `POST /api/schedule/correction/confirm`, `GET /api/schedule/history`. Роли `Dispatcher,Admin`. Стиль Swagger как в `ScheduleController`. DI в `ServiceCollectionExtensions`.

## Секция 4 — MaxBot: уведомления + «Мои изменения»

### 4.1 Endpoint `POST /notify` (`MapPost` в `Program.cs`)
Принимает JSON со списком изменений, сохраняет `ScheduleRevision`, рассылает уведомления подписчикам (студент по GroupId, преподаватель по TeacherId).

### 4.2 `ScheduleRevision` entity + EF config (в MaxBot)
Таблица `schedule_revisions`: Id, GroupId, TeacherId?, Type, DayOfWeek, NumberPair, Subject, Room?, Note?, Week, CreatedAt. Индексы по group_id/teacher_id.

### 4.3 Пункт меню «Мои изменения»
- Кнопка `🔔 Мои изменения` (payload `changes`) в `ShowMainMenuAsync`.
- Колбэк `changes` → читает `schedule_revisions` по текущему фильтру (GroupId/TeacherId), сортирует по CreatedAt desc, форматирует.
- Пусто → «Изменений нет 🎉».

### 4.4 Формат уведомления (мгновенное)
```
🔔 Изменение в расписании

ПО262 · Вторник
Нед. 1 · Пара 2
📖 История (замена)
Преподаватель: Петренко В.Б.
Примечание: вм.4 п
```

## Секция 5 — Frontend (CollegeLMS.Next)

### 5.1 Страница `dispatcher/correction`
Загрузка XLSX → превью-таблица (тип операции, группа, пара, предмет, преподаватель, неделя), кнопка «Подтвердить» активна только без ошибок. Внизу — журнал изменений из `GET /api/schedule/history`.

### 5.2 Маркировка в общем расписании
- В `ScheduleResponse` добавить `changeTags: string[]` — из `ScheduleHistory` по group+day+pair+week.
- `ScheduleTable`/`SemesterView` — бейдж на паре для активной недели.
- «Снятые» пары недели — отдельными компактными строками «снято».

### 5.3 API модуль `api/correction.ts`
`previewCorrection(file)`, `confirmCorrection(payload)`, `fetchScheduleHistory(params)`.

## Секция 6 — Тестирование и проверки

Юнит (парсер/применение/история), интеграционные (WebApplicationFactory), MaxBot.Tests (`/notify`, `ScheduleRevision`, формат сообщения). Гейты: `dotnet build`, `dotnet test CollegeLMS.Tests` + MaxBot.Tests, `npm run build`, `docker compose --profile max-bot up --build`.

## Definition of Done

- [ ] `dotnet build` проходит
- [ ] `dotnet test CollegeLMS.Tests` + MaxBot.Tests зелёные
- [ ] `npm run build` работает
- [ ] Swagger показывает новые endpoints с русской документацией
- [ ] Postman-коллекция обновлена
- [ ] PlantUML диаграммы (ER, Class, Sequence)
- [ ] `docker compose --profile max-bot up --build` работает
- [ ] Feature-ветка слита в master
- [ ] Push в master → CD развернул на VPS

## За рамками (отложенные задачи)

- Подписки вебхука MAX (`POST /subscriptions`) — prod вместо long-polling
- Миграции EF вместо `EnsureCreated` (MaxBot)
- Мини-приложение с расписанием в MAX
- Экспорт/печать корректировок
