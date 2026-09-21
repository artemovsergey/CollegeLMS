# Дизайн-спецификация: завершение корректировок расписания (UC-SCH-18–30)

**Дата:** 2026-09-18
**Статус:** утверждено пользователем в чате
**Цель:** довести пакетный флоу корректировок до эталонного ТЗ `docs/spec/task-schedule-management.md` (UC-SCH-18–30): пагинация и фильтры, импорт с ошибками, валидация и применение с симуляцией неприменённых позиций, расписание дня, экспорт FILE-3, уведомления (включая снятого преподавателя и «сам.р.»), PNG-картинка в канал Max.

## 1. Границы

**Входит:** backend API корректировок, MaxBot-уведомления и картинка, веб-страница `/dispatcher/correction`, тесты, документация.

**Не входит:**
- нерабочие дни (`NonWorkingDay` в проекте отсутствует — зависимость UC-SCH-13/14);
- legacy-маршруты `/api/schedule/correction/preview|confirm|export` и `/api/schedule/history` — остаются без изменений (ими пользуется мини-приложение Max);
- бот-визард `UC-SCH-40` и мини-приложение `/max/dispatcher` в batch-модели (UC-SCH-43) — отдельные истории.

## 2. Ключевые решения

| Вопрос | Решение |
|--------|---------|
| Канонический API | `/api/schedule/correction/batches/*`, `/api/schedule/correction/day`, `/api/schedule/history` |
| Импорт с ошибками | Парсер отдаёт best-effort строку для каждой непустой строки; пакет создаётся даже при data/logic-ошибках; ошибки пересчитываются при чтении и в применении (в БД не хранятся) |
| Structural-ошибки | Нет даты/шапки/данных — пакет не создаётся, ответ с `batchId: null` и списком ошибок |
| Валидация пакета | Единый движок симуляции `CorrectionApplyEngine`: порядок строк, учёт неприменённых позиций, проверки группы/пары/преподавателя/«сам.р.»/«вм.X» |
| Применение | Две фазы: (1) симуляция без мутаций → `400` со списком «Строка N: сообщение»; (2) транзакция с трекингом, `Status` пакета помечен concurrency token — повторная/параллельная запись → `DbUpdateConcurrencyException` → `409` |
| «сам.р.» | Только `Remove`; пара и неделя сохраняются, бейдж «Сам.р.»; при обычном снятии неделя удаляется, при последней — запись |
| Расписание дня | `GET /api/schedule/correction/day?groupId&date&batchId` — база + применённые бейджи + pending-позиции пакета |
| Экспорт | `Корректировка_dd.MM.yyyy_HH-mm-ss.xlsx` (FILE-3); клиент берёт имя из `Content-Disposition`, с fallback-генерацией |
| Уведомления | Получатели: группа/преподаватель позиции и снятый преподаватель; дайджест с примечанием и бейджем «Сам.р.»; fail-safe |
| Картинка | API рендерит PNG через QuestPDF `GenerateImages()` (встроенный Lato, кириллица) и отправляет в бот `POST /notify/correction-image`; бот грузит `POST /uploads?type=image` и постит в `MaxBot:CorrectionChannelId`; пустой канал → warning в логах; fail-safe |
| Идемпотентность | Повторный apply → `409` (статус) + защита гонки conditional UPDATE; заголовок `Idempotency-Key` принимается |

## 3. Контракты API

### 3.1 Пакеты

`GET /api/schedule/correction/batches?status=&from=&to=&page=&pageSize=`
→ `Result<PagedResponse<CorrectionBatchResponse>>`, pageSize 1..100 (PAG-2), по умолчанию 20.

`CorrectionBatchResponse` дополняется:
- `errors: ScheduleValidationError[]` — пересчитываются при чтении;
- `appliedAt: DateTime?`, `appliedByUserId: Guid?`, `appliedByName: string?`, `positions[].errors` — ошибки конкретной позиции (подмножество).

`POST /api/schedule/correction/batches/{id}/apply` — ошибки валидации → `400` с сообщениями «Строка N: …», повторно → `409`, пустой пакет → `400`.

### 3.2 Расписание дня

`GET /api/schedule/correction/day?groupId={id}&date={yyyy-MM-dd}&batchId={id?}`

```json
{
  "date": "2026-09-17",
  "week": 3,
  "dayOfWeek": 4,
  "groupId": "…",
  "groupName": "ПО262",
  "entries": [
    {
      "numberPair": 1,
      "subject": "Математика",
      "room": "301",
      "teacherId": "…",
      "teacherName": "Иванов И.И.",
      "note": null,
      "isSelfStudy": false,
      "pendingChangeType": "Replace",
      "changeTags": [{ "changeType": "Add", "week": 3, "removedNumberPair": null, "removedSubject": null, "note": null }]
    }
  ]
}
```

Семантика: `changeTags` — применённые изменения; `pendingChangeType` — тип неприменённой позиции пакета, породившей/изменившей строку; снятые без «сам.р.» строки не выводятся; с «сам.р.» выводятся с `isSelfStudy: true`.

### 3.3 Предметы и история

- `GET /api/schedule/subjects?teacherId={id}&q=` — при `teacherId` только предметы этого преподавателя (из записей и истории).
- `GET /api/schedule/history?groupId=&teacherId=&week=&date=&from=&to=&changeType=&page=&pageSize=` — фильтры комбинируются; `date` вычисляет неделю и день; `from/to` — по `appliedAt`; pageSize 1..100.

## 4. Алгоритм симуляции

`CorrectionApplyEngine.SimulateAsync(positions, execute: bool, appliedByUserId, ct)`:

1. Загружает группы и записи расписания по затронутым группам (день/неделя — из позиций); в режиме execute — tracked.
2. Виртуальные списки по ключу `(groupId, day, week)`, позиции обрабатываются по `Row`.
3. Проверки: группа существует; № пары 1–8; «сам.р.» только для снятия; ровно одно из «предмет/преподаватель» запрещено; преподаватель по имени находится; для переноса указана старая пара; для Add/Replace/Move указан предмет.
4. `Remove`: поиск снимаемой пары (по паре, предмету/преподавателю, если заданы); не найдено → ошибка. Обычное снятие — удаление недели (при последней — записи); «сам.р.» — пометка.
5. `Replace`/`Move`: снятие пары `RemovedNumberPair ?? NumberPair` по предмету/преподавателю, затем создание новой пары на `NumberPair` с временем из справочника звонков.
6. `Add`: создание новой пары (совмещённые пары разрешены).
7. В execute-режиме пишется `ScheduleHistory` (с `Removed*`), заполняются `position.Status = Applied`, `position.HistoryId`, формируются `ScheduleChangeDto` с `CorrectionDate` для уведомлений.

Ошибки: `{ row, column, level, message }`, где message = «Строка N: текст» для batch-валидации.

## 5. Файлы

**Backend API**
- `Dtos/CorrectionBatchDtos.cs` — paging, errors, applied info, day-DTO.
- `Dtos/ScheduleCorrectionDtos.cs` — `AllEntries` в превью.
- `Interfaces/ICorrectionBatchService.cs`, `Interfaces/IScheduleCorrectionService.cs`, `Interfaces/IScheduleService.cs` — сигнатуры.
- `Services/CorrectionBatchService.cs` — list paging/фильтры, import, apply, ссылка на ошибки.
- `Services/ScheduleCorrectionService.cs` — парсер (все строки), day-endpoint, экспорт FILE-3.
- `Services/CorrectionApplyEngine.cs` (новый) — симуляция/валидация/применение.
- `Services/ScheduleService.cs` — subjects по преподавателю.
- `Controllers/CorrectionBatchController.cs`, `Controllers/ScheduleCorrectionController.cs`, `Controllers/ScheduleController.cs` — маршруты, Swagger.
- `Mappers/CorrectionBatchMapper.cs` — новые поля.
- `Extensions/ServiceCollectionExtensions.cs` — регистрация движка.
- `Services/CorrectionImageService.cs` (новый) — PNG через QuestPDF.
- `Services/MaxBotHttpClient.cs` — отправка картинки.

**MaxBot**
- `Program.cs` — `POST /notify/correction-image`.
- `Clients/MaxApiClient.cs` — upload image + send attachment.
- `Services/CorrectionImageSender.cs` (новый) — канал, retry, fail-safe.
- `Services/ChangeNotifier.cs` — снятый преподаватель.
- `Services/MessageFormatter.cs` — бейдж «Сам.р.».
- `MaxBotOptions.cs` — `CorrectionChannelId`.

**Frontend**
- `types/correction.ts`, `api/correction.ts`, `api/schedule.ts`.
- `components/CorrectionBatchList.tsx`, `components/CorrectionPositionEditor.tsx`, `components/RemovePairPicker.tsx`, `app/(authenticated)/dispatcher/correction/page.tsx`.

**Инфраструктура**
- `docker-compose.yml` — `MaxBot__CorrectionChannelId`.

**Документация**
- Postman, спека `task-dispatcher-corrections.md` (пометка о каноническом API).
## 6. Тестирование

- Unit API: импорт с ошибками и без, симуляция (включая «снятие добавленного»), «сам.р.», гонка/повтор apply, формат имени файла, day-overlay, фильтры истории, PNG непустой и валидный.
- Интеграционные: списки/пагинация, импорт, apply `400/409`, day, export `Content-Disposition`.
- MaxBot: получатель-снятый преподаватель, бейдж «сам.р.», upload/отправка изображения (fake handler), пустой канал.
- Гейты: `dotnet build`, `dotnet csharpier format .`, `dotnet test CollegeLMS.Tests`, `dotnet test CollegeLMS.MaxBot.Tests`, `npm run build` (CollegeLMS.Next).

## 7. Definition of Done

- [ ] `dotnet build` проходит, `dotnet csharpier check` чистый.
- [ ] `dotnet test CollegeLMS.Tests` и `CollegeLMS.MaxBot.Tests` зелёные.
- [ ] `npm run build` проходит; страница `/dispatcher/correction` работает.
- [ ] Swagger содержит новые/изменённые endpoint-ы с русской документацией.
- [ ] `docker compose up --build -d --profile max-bot` поднимается.
- [ ] Ветка слита в master, push → CD.
