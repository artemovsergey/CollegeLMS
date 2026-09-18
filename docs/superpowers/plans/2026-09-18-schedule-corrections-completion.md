# План: завершение корректировок расписания (UC-SCH-18–30)

> **Для агентов:** выполнять по задачам, отмечая шаги `[ ]`. Спека: `docs/superpowers/specs/2026-09-18-schedule-corrections-completion-design.md`.

**Цель:** закрыть разрыв между текущей реализацией пакетных корректировок и эталонным ТЗ `docs/spec/task-schedule-management.md` (UC-SCH-18–30).

**Архитектура:** пакетный API остаётся каноническим; валидация и применение — через новый движок симуляции; legacy-маршруты не меняются.

**Стек:** .NET 10, EF Core (Npgsql), QuestPDF (PNG), Max REST API, Next.js 14.

## Глобальные ограничения

- `Result<T>` везде; без try-catch в контроллерах/сервисах; fail-safe только для внешних вызовов (MaxBot, генерация картинки).
- Сообщения и документация на русском; `HasMaxLength` для строк; `ValueGeneratedNever` для Guid PK; snake_case.
- Все команды проверяются локально; тесты — `dotnet test`; формат — `dotnet csharpier format .`.
- Legacy `/api/schedule/correction/preview|confirm|export` и `/api/schedule/history` не расширяются.

---

### Task 1: Paged-список пакетов и контракты DTO

**Файлы:**
- Modify: `CollegeLMS.API/Dtos/CorrectionBatchDtos.cs`
- Modify: `CollegeLMS.API/Mappers/CorrectionBatchMapper.cs`
- Modify: `CollegeLMS.API/Interfaces/ICorrectionBatchService.cs`
- Modify: `CollegeLMS.API/Services/CorrectionBatchService.cs`
- Modify: `CollegeLMS.API/Controllers/CorrectionBatchController.cs`

**Интерфейс:**
- `GetBatchesAsync(CorrectionBatchStatus? status, DateTime? from, DateTime? to, int? page, int? pageSize, CancellationToken ct) → Result<PagedResponse<CorrectionBatchResponse>>`
- `CorrectionBatchResponse += Errors, AppliedByUserId, AppliedByName, AppliedAt`; `CorrectionPositionResponse += Errors`
- `GET /api/schedule/correction/batches?status=&from=&to=&page=&pageSize=` (pageSize 1..100, default 20)

- [ ] Добавить поля в DTO и маппер (`ToDto(bool includePositions = true)`), `AppliedByName` резолвится из `Users`.
- [ ] Обновить сервис: фильтры `Status`, `CorrectionDate >= from`, `<= to`, сортировка `CreatedAt DESC`, пагинация.
- [ ] Обновить контроллер и Swagger; `dotnet build`.
- [ ] Коммит `feat: пагинация и фильтры списка пакетов корректировок`.

---

### Task 2: Парсер — best-effort строки для импорта

**Файлы:**
- Modify: `CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs` (`CorrectionPreviewResponse.AllEntries`)
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs`
- Modify: `CollegeLMS.API/Services/CorrectionBatchService.cs` (ImportAsync)

**Поведение:**
- `Entries` — как раньше: только строки без ошибок (legacy-совместимость).
- `AllEntries` — каждая непустая строка: `GroupId = Guid.Empty` при ненайденной группе, `NumberPair = 0` при некорректном, «сырые» предмет/преподаватель; строка сохраняется для исправления в редакторе.
- `ImportAsync`: structural-ошибки → пакет не создаётся (`batchId: null`, errors); иначе пакет создаётся из `AllEntries`, ошибки пересчитываются при чтении.

- [ ] Расширить DTO и парсер (обе ветки: одиночная строка и «вм.X»-переносы дают best-effort записи).
- [ ] Переключить `ImportAsync` на `AllEntries`, всегда создавать пакет при наличии даты.
- [ ] `dotnet build`.
- [ ] Коммит `feat: импорт корректировок создаёт пакет даже при ошибках строк`.

---

### Task 3: Движок симуляции и валидации пакета

**Файлы:**
- Create: `CollegeLMS.API/Services/CorrectionApplyEngine.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`
- Modify: `CollegeLMS.API/Interfaces/ICorrectionBatchService.cs` (использование)

**Интерфейс:**
```csharp
public sealed class CorrectionApplyEngine(AppDbContext db)
{
    public Task<List<ScheduleValidationError>> ValidateBatchAsync(CorrectionBatch batch, CancellationToken ct);
    public Task<CorrectionApplyOutcome> ExecuteBatchAsync(CorrectionBatch batch, Guid appliedByUserId, CancellationToken ct);
}
public sealed class CorrectionApplyOutcome
{
    public List<ScheduleHistory> History { get; init; } = [];
    public List<ScheduleChangeDto> Changes { get; init; } = [];
    public List<SimulatedEntry> Entries { get; init; } = [];
}
```

**Правила (см. спеку §4):** порядок строк; группа/пара/«сам.р.»/«вм.X»; совмещённые пары разрешены; две фазы — валидация (AsNoTracking) и выполнение (tracked, мутации `Weeks`/удаление/создание + `ScheduleHistory` с `Removed*`).

- [ ] Реализовать движок и модели `SimulatedEntry`/`SlotKey`.
- [ ] Зарегистрировать в DI, `dotnet build`.
- [ ] Unit-тесты (Task 10) на снятие добавленного, «сам.р.», перенос, замену.
- [ ] Коммит `feat: движок валидации и применения пакета корректировок`.

---

### Task 4: Apply — транзакция, ошибки «Строка N», защита гонки

**Файлы:**
- Modify: `CollegeLMS.API/Services/CorrectionBatchService.cs`
- Modify: `CollegeLMS.API/Controllers/CorrectionBatchController.cs`

**Поведение:**
- Пустой пакет → `400`; повторный apply → `409`.
- Валидация → `400` с сообщениями «Строка N: …» (join `\n`).
- Транзакция: `Status` пакета — concurrency token (`DbUpdateConcurrencyException` → `409`), выполнение движком, `HistoryId` позициям, commit; откат при сбое.
- Уведомления MaxBot после commit (fail-safe).

- [ ] Переписать `ApplyAsync`, обновить контроллер/Swagger.
- [ ] `dotnet build`.
- [ ] Коммит `feat: транзакционное применение пакета с защитой повторного apply`.

---

### Task 5: Расписание дня и предметы преподавателя

**Файлы:**
- Modify: `CollegeLMS.API/Dtos/ScheduleCorrectionDtos.cs` (`CorrectionDayResponse/Entry`)
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (`GetDayAsync`)
- Modify: `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs` (`GET correction/day`)
- Modify: `CollegeLMS.API/Interfaces/IScheduleCorrectionService.cs`
- Modify: `CollegeLMS.API/Services/ScheduleService.cs`, `Interfaces/IScheduleService.cs`, `Controllers/ScheduleController.cs` (subjects `teacherId`)

**Поведение:** база + применённые `changeTags` + pending-оверлей из позиций `batchId`; снятые без «сам.р.» исключаются; «сам.р.» с флагом; сортировка по паре.

- [ ] Реализовать DTO/сервис/контроллер day-endpoint.
- [ ] Добавить `teacherId` в subjects (записи + история преподавателя).
- [ ] `dotnet build`.
- [ ] Коммит `feat: расписание дня корректировки и предметы преподавателя`.

---

### Task 6: История — фильтры по дате и типу

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs`
- Modify: `CollegeLMS.API/Controllers/ScheduleCorrectionController.cs`

**Поведение:** `date` → `Week + DayOfWeek`; `from/to` → `AppliedAt`; `changeType`; pageSize 1..100.

- [ ] Расширить сигнатуру и запрос, обновить Swagger.
- [ ] `dotnet build`.
- [ ] Коммит `feat: фильтры журнала изменений`.

---

### Task 7: Экспорт FILE-3

**Файлы:**
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (`ExportManualAsync`)
- Modify: `CollegeLMS.Next/api/correction.ts` (`exportBatch` → `{ blob, fileName }`)

**Поведение:** имя `Корректировка_dd.MM.yyyy_HH-mm-ss.xlsx`; клиент берёт из `Content-Disposition`, fallback — та же маска.

- [ ] Исправить формат имени, обновить клиент, `dotnet build`.
- [ ] Коммит `fix: имя файла корректировки по FILE-3`.

---

### Task 8: Уведомления — снятый преподаватель и «сам.р.»

**Файлы:**
- Modify: `CollegeLMS.MaxBot/Services/ChangeNotifier.cs`
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs`
- Test: `CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs`, `MessageFormatterTests.cs`

- [ ] Матч получателя по `TeacherName` **или** `RemovedTeacherName` — одно сообщение на чат.
- [ ] В дайджест добавить строку «🟣 сам.р. (самостоятельная работа)» при примечании `сам.р.`.
- [ ] `dotnet test CollegeLMS.MaxBot.Tests`.
- [ ] Коммит `feat: уведомления о корректировке снятому преподавателю и «сам.р.»`.

---

### Task 9: PNG-картинка в канал Max

**Файлы:**
- Create: `CollegeLMS.API/Services/CorrectionImageService.cs` (QuestPDF `GenerateImages`, `RasterDpi = 144`, высота страницы по числу строк)
- Modify: `CollegeLMS.API/Services/MaxBotHttpClient.cs` (`SendCorrectionImageAsync`)
- Modify: `CollegeLMS.MaxBot/Clients/MaxApiClient.cs` (upload/send image)
- Create: `CollegeLMS.MaxBot/Services/CorrectionImageSender.cs`
- Modify: `CollegeLMS.MaxBot/Program.cs` (`POST /notify/correction-image`)
- Modify: `CollegeLMS.MaxBot/MaxBotOptions.cs`, `docker-compose.yml`

**Поведение:** канал пуст → warning и пропуск; retry при `attachment.not.ready`; сбой картинки не влияет на применение (fail-safe логирование).

- [ ] API: рендер PNG и отправка multipart в бот.
- [ ] Бот: upload (`POST /uploads?type=image`) → `POST /messages?chat_id=` с вложением image.
- [ ] Конфиг `MaxBot__CorrectionChannelId`, документация.
- [ ] Сборка `dotnet build`, тесты MaxBot.
- [ ] Коммит `feat: PNG-картинка корректировки в канал Max`.

---

### Task 10: Тесты API

**Файлы:**
- Modify: `CollegeLMS.Tests/Unit/Services/CorrectionBatchServiceTests.cs`
- Create/Modify: `CollegeLMS.Tests/Unit/Services/CorrectionApplyEngineTests.cs`, `CorrectionImageServiceTests.cs`
- Modify: `CollegeLMS.Tests/Integration/Controllers/ScheduleCorrectionControllerTests.cs`
- Create: `CollegeLMS.Tests/Integration/Controllers/CorrectionBatchControllerTests.cs`

**Кейсы:** импорт с ошибками (пакет создан), исправление позиции, симуляция «снятие добавленного», «сам.р.», apply `400` со «Строка N», повтор `409`, pageSize-клампы, day-overlay, FILE-3, PNG-сигнатура.

- [ ] Написать тесты, `dotnet test CollegeLMS.Tests`.
- [ ] Коммит `test: покрытие корректировок UC-SCH-18–30`.

---

### Task 11: Frontend

**Файлы:**
- Modify: `CollegeLMS.Next/types/correction.ts`, `api/correction.ts`, `api/schedule.ts`
- Modify: `components/CorrectionBatchList.tsx`, `components/CorrectionPositionEditor.tsx`, `components/RemovePairPicker.tsx`
- Modify: `app/(authenticated)/dispatcher/correction/page.tsx`

**Поведение:** paged-список с фильтрами статус/период; ошибки пакета и блокировка apply; выбор пары через day-endpoint с pending-бейджами; предметы по преподавателю; «вм.X» для переноса; applied-инфо; автоскачивание FILE-3; UI-1…UI-4.

- [ ] Реализовать, `npm run build` в `CollegeLMS.Next/`.
- [ ] Коммит `feat: веб-редактор корректировок — day-флоу, ошибки, пагинация`.

---

### Task 12: Документация

**Файлы:**
- Modify: `docs/spec/CollegeLMS.postman_collection.json`
- Modify: `docs/diagrams/sequence/correction-batch-apply.puml`, `docs/diagrams/class/correction-batch-service.puml`
- Modify: `docs/spec/task-dispatcher-corrections.md` (канонический batch API)

- [ ] Обновить Postman, диаграммы, спеку.
- [ ] Коммит `docs: корректировки UC-SCH-18–30 — API, диаграммы, Postman`.

---

## Финальная проверка

- [ ] `dotnet build` && `dotnet csharpier check .`
- [ ] `dotnet test CollegeLMS.Tests` && `dotnet test CollegeLMS.MaxBot.Tests`
- [ ] `npm run build --prefix CollegeLMS.Next`
- [ ] `docker compose up --build -d --profile max-bot`
- [ ] Merge в master + push.
