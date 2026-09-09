# MaxBot Уведомления — Бейджи — Дашборд Диспетчера — Документы. План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Починить уведомления бота Max и дайджест, исправить нумерацию пар, добавить тип «перенос» и информативные бейджи изменений в веб и бот, реализовать дашборд диспетчера (преподаватели на день) и скачивание шаблонов документов.

**Architecture:** Монолит Clean Architecture. Фаза 1 — фиксы/стилизация бота (`.Net` MaxBot + тесты). Фаза 2 — Backend (enum `Move`, расширение `ChangeTag`) → бот (маркеры) → фронт (tooltip). Фаза 3 — новый диспетчерский endpoint + страницы фронта. Фаза 4 — roadmap мини-приложения (документация, код за рамками).

**Tech Stack:** .NET 10 / ASP.NET Core, EF Core `/Npgsql`, xUnit + Moq + Bogus, FluentAssertions, WebApplicationFactory, Next.js 14 + Tailwind v4 + shadcn/ui, CSharpier.

## Global Constraints

- `Result<T>` — везде; без try-catch в контроллерах/сервисах (кроме fail-safe доставки в боте).
- Primary constructor DI; `CancellationToken ct` на всех асинхронных методах; `AsNoTracking()` на чтении.
- Мапперы в `Mappers/`, интерфейсы в `Interfaces/`, DI в `Extensions/ServiceCollectionExtensions.cs`.
- Сообщения и Swagger-документация на русском.
- `ScheduleChangeType.Move` — новый enum-тип.
- Дайджест бота: только будни (пн–пт); изменения — всем подписчикам независимо от `NotifyDays`.
- Дефолт `NotifyDays = [1, 2, 3, 4, 5]`.
- Форматирование: CSharpier (`dotnet csharpier format .`). Коммиты по фазам.
- Проверка: `dotnet build` + `dotnet test` (CollegeLMS.Tests), `npm run build` (CollegeLMS.Next).

---
## Фаза 1 — Бот Max: фиксы и стилизация

### Task 1.1: Фикс нумерации пар в недельном расписании бота

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs:82,179`
- Test: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`

**Interfaces:**
- Consumes: `MessageFormatter.FormatWeekSchedule(List<ScheduleResponse> entries, DateTime weekStart, string entityName, bool showGroup = false)`
- Produces: то же поведение, но строки пар начинаются с `*{NumberPair}.*` (жирный номер → не ordered-list, MAX не перенумеровывает)

- [ ] **Step 1: Написать падающий тест**

В `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs` добавить:

```csharp
[Fact]
public void FormatWeekSchedule_PairNumbers_AreBoldNotOrderedList()
{
    var entries = new List<ScheduleResponse>();
    for (var i = 1; i <= 7; i++)
        entries.Add(Entries()[0] with { NumberPair = i, Subject = $"Предмет{i}" });

    var text = MessageFormatter.FormatWeekSchedule(entries, new DateTime(2026, 9, 7), "Группа 101");

    text.Should().Contain("*7.* 📚 Предмет7");
    text.Should().NotMatch(@"  \d+\.");
}
```

- [ ] **Step 2: Запустить — тест падает**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~FormatWeekSchedule_PairNumbers"`
Expected: FAIL — текущий формат `  7. …` и отсутствие `*7.*`.

- [ ] **Step 3: Исправить `FormatWeekSchedule`**

В обеих перегрузках (строки 82 и 179) заменить префикс строки пары:

```csharp
var pairLine = $"*{e.NumberPair}.* {type} {e.Subject} ({e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}, {e.Room})";
```

(в перегрузке без даты — без `var pairLine`, просто заменить инлайн-строку на ту же с `*{e.NumberPair}.*`).

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests`
Expected: PASS (все тесты, включая `FormatWeekSchedule_HeaderHasDateRange`, `FormatWeekSchedule_ShowGroup_IncludesGroupName`).

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "fix: номера пар в недельном расписании бота — жирный номер вместо ordered-list"
```

### Task 1.2: Уведомления об изменениях — всем подписчикам всегда

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/ChangeNotifier.cs:66-98`
- Test: `CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs`

**Interfaces:**
- Consumes: `ChangeNotifier.SelectRecipients(List<UserSettings>, Dictionary<Guid,string>, Dictionary<Guid,string>, List<ScheduleRevision>)`
- Produces: фильтр только по `NotifyEnabled` (и совпадению группы/ФИО); `NotifyDays` больше не учитывается

- [ ] **Step 1: Обновить тесты**

В `ChangeNotifierTests.cs`:
- Тест `SelectRecipients_DayNotInNotifyDays_IsExcluded` заменить:

```csharp
[Fact]
public void SelectRecipients_DayNotInNotifyDays_StillMatches()
{
    var recipients = ChangeNotifier.SelectRecipients(
        [Student(days: [3])],
        GroupNames,
        TeacherNames,
        [Rev()]
    );

    recipients.Should().ContainSingle(x => x.ChatId == 100);
}
```

- Тест `SelectRecipients_SundayRevision_MatchesNotifyDay7` заменить на проверку, что воскресенье тоже приходит даже без дня в списке:

```csharp
[Fact]
public void SelectRecipients_SundayRevision_StillMatches()
{
    var recipients = ChangeNotifier.SelectRecipients(
        [Student(days: [1])],
        GroupNames,
        TeacherNames,
        [Rev(dayName: "Воскресенье")]
    );

    recipients.Should().ContainSingle(x => x.ChatId == 100);
}
```

- `SelectRecipients_NotifyDisabled_IsExcluded` оставить (NotifyEnabled остаётся фильтром).

- [ ] **Step 2: Запустить — тесты падают**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~ChangeNotifierTests"`
Expected: FAIL — новый тест не проходит (день 3 исключён).

- [ ] **Step 3: Убрать фильтр `NotifyDays`**

В `ChangeNotifier.cs` в `SelectRecipients` изменить условие (строка 80):

```csharp
if (!s.NotifyEnabled)
    continue;
```

Удалить локальную `var day = DayIndexFromName(r.DayOfWeek);` и метод `DayIndexFromName` (больше не используется; проверить, что не используется в тестах).

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests`
Expected: PASS.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "fix: уведомления об изменениях приходят всем подписчикам, независимо от NotifyDays"
```

### Task 1.3: Дайджест только по будням

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/ScheduleNotifier.cs:86-92`

**Interfaces:**
- Consumes: существующая логика таймера и рассылки.
- Produces: пропуск субботы (день 6) и воскресенья (0) — дайджест только пн–пт.

- [ ] **Step 1: Добавить проверку субботы**

В `ScheduleNotifier.SendNotificationsAsync` (строки 86-92):

```csharp
var dayOfWeek = (int)today.DayOfWeek; // 0 = Воскресенье, 6 = Суббота
if (dayOfWeek is 0 or 6)
{
    _logger.LogInformation("Weekend — no notifications");
    return;
}
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.MaxBot/CollegeLMS.MaxBot.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "feat: дайджест расписания только по будням (пн–пт)"
```

### Task 1.4: Дефолт `NotifyDays = [1..5]`

**Files:**
- Modify: `CollegeLMS.MaxBot/Models/UserSettings.cs:12`
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs:164`

**Interfaces:**
- Consumes: `UserSettings.NotifyDays` (int[])
- Produces: дефолт для новых пользователей — все будни

- [ ] **Step 1: Поменять дефолт**

`UserSettings.cs`:

```csharp
public int[] NotifyDays { get; set; } = [1, 2, 3, 4, 5];
```

`MaxBotService.cs:164` (в `HandleBotStartedAsync` создание `UserSettings`):

```csharp
NotifyDays = [1, 2, 3, 4, 5],
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.MaxBot/CollegeLMS.MaxBot.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Коммит**

```bash
git add -A
git commit -m "feat: дефолтные дни уведомлений — все будни [1-5]"
```

### Task 1.5: Стилизация сообщений бота (структура и читаемость)

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs`
- Test: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`

**Interfaces:**
- Consumes: `FormatDaySchedule`, `FormatWeekSchedule`, `FormatChangeNotification`
- Produces: единая шапка, разделители `────────`, жирный номер пары, эмодзи-секции

- [ ] **Step 1: Добавить тесты на структуру**

```csharp
[Fact]
public void FormatDaySchedule_HasHorizontalRuleBetweenSlots()
{
    var entries = new List<ScheduleResponse>
    {
        Entries()[0],
        Entries()[0] with { NumberPair = 2, Subject = "Физика", StartTime = new TimeSpan(10, 50, 0) },
    };

    var text = MessageFormatter.FormatDaySchedule(entries, new DateTime(2026, 9, 7), "Группа 101");

    text.Should().Contain("────────");
    text.Should().Contain("*1.* 📖 Математика");
    text.Should().Contain("*2.* 📖 Физика");
}
```

- [ ] **Step 2: Стилизовать `FormatDaySchedule` (DateTime-перегрузка, строки 107-146)**

Заменить цикл на:

```csharp
foreach (var e in entries.OrderBy(x => x.NumberPair))
{
    var type = e.LessonType switch
    {
        "Lecture" => "📖",
        "Practice" => "✏️",
        "Lab" => "🔬",
        "Exam" => "📝",
        _ => "📚",
    };

    sb.AppendLine($"*{e.NumberPair}.* {type} {e.Subject}");
    sb.AppendLine($"    🕐 {e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}  📍 {e.Room}");

    if (showGroup && e.GroupName.Length > 0)
        sb.AppendLine($"    🏫 {e.GroupName}");

    if (e.TeacherName is not null)
        sb.AppendLine($"    👨‍🏫 {e.TeacherName}");

    sb.AppendLine("────────");
}
```

Убрать лишний пустой `AppendLine` (разделитель заменяет его).

- [ ] **Step 3: Стилизовать `FormatChangeNotification` (строки 228-246)**

Заменить на блочную структуру:

```csharp
var sb = new System.Text.StringBuilder();
sb.AppendLine("🔔 *Изменение в расписании*");
sb.AppendLine();
sb.AppendLine($"{revision.GroupName} · {revision.DayOfWeek} · Нед. {revision.Week} · Пара {revision.NumberPair}");
sb.AppendLine();
sb.AppendLine($"📖 {revision.Subject} — {FormatChangeNotificationTitle(revision.ChangeType)}");

if (revision.TeacherName is not null)
    sb.AppendLine($"👨‍🏫 Преподаватель: {revision.TeacherName}");

if (revision.Note is not null)
    sb.AppendLine($"📝 Примечание: {revision.Note}");

return sb.ToString().TrimEnd();
```

Обновить существующий тест `FormatChangeNotification_ContainsMetaHeaderAndFields`:
- `text.Should().Contain("🔔 *Изменение в расписании*")` (добавить `*`),
- `text.Should().Contain("📖 История — замена")` (было `(замена)`),
- `text.Should().Contain("👨‍🏫 Преподаватель: Петренко В.Б.")`,
- `text.Should().Contain("📝 Примечание: вм.4 п")`.
- Тест `FormatChangeNotification_WithoutNoteAndTeacher_HidesOptionalLines` оставить (проверки `NotContain("👨‍🏫")`).

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests`
Expected: PASS.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: стилизация сообщений бота — разделители, блочная структура, жирные заголовки"
```

---
## Фаза 2 — Тип «перенос» и бейджи изменений

### Task 2.1: Enum `ScheduleChangeType.Move` + детект «переноса» в корректировке

**Files:**
- Modify: `CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs`
- Modify: `CollegeLMS.API/Services/ScheduleCorrectionService.cs` (детект)
- Test: `CollegeLMS.Tests/Unit/Services/ScheduleCorrectionServiceTests.cs`

**Interfaces:**
- Consumes: `ScheduleChangeType` (Add, Remove, Replace)
- Produces: новый `Move`; детект в `BuildReplaceAsync`: если `removed.NumberPair != addPair` → на паре X снимается и вводится на пару F = «перенос» (тип `Move`), иначе `Replace`.

- [ ] **Step 1: Написать падающий тест**

В `ScheduleCorrectionServiceTests.cs` заменить тест `PreviewAsync_MoveToBusyPair_AddsEntryAnyway` (строка ~252) — ожидаемый тип:

```csharp
entry.ChangeType.Should().Be(ScheduleChangeType.Move);
entry.NumberPair.Should().Be(4);
entry.RemovedNumberPair.Should().Be(2);
```

И тест `PreviewAsync_AddOnlyWithMoveNote_ReturnsReplaceMove` (строка ~314):

```csharp
entry.ChangeType.Should().Be(ScheduleChangeType.Move);
entry.NumberPair.Should().Be(4);
entry.RemovedNumberPair.Should().Be(2);
entry.Subject.Should().Be("Математика");
entry.RemovedSubject.Should().Be("Математика");
```

- [ ] **Step 2: Запустить — тесты падают**

Run: `dotnet test tests/CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleCorrectionServiceTests"`
Expected: FAIL — ожидается Move, приходит Replace.

- [ ] **Step 3: Добавить enum**

`ScheduleChangeType.cs`:

```csharp
public enum ScheduleChangeType
{
    Add,
    Remove,
    Replace,
    Move,
}
```

- [ ] **Step 4: Детект Move в `ScheduleCorrectionService`**

В `BuildReplaceAsync` (после нахождения `removed`, в `return`, строка ~567) изменить `ChangeType`:

```csharp
ChangeType = removed.NumberPair != addPair
    ? ScheduleChangeType.Move
    : ScheduleChangeType.Replace
```

В `ApplyEntryAsync` case `Move` обработать как `Replace` (общий код замены): в `switch (entry.ChangeType)` заменить `default:` на `case ScheduleChangeType.Replace: case ScheduleChangeType.Move:`.

- [ ] **Step 5: Проверить сборку и тесты**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj && dotnet test tests/CollegeLMS.Tests --filter "FullyQualifiedName~ScheduleCorrectionServiceTests"`
Expected: BUILD SUCCEEDED + PASS (включая обновлённые тесты Move и неизменённые `PreviewAsync_ReplaceSameSlot_ReturnsReplaceEntry` → Replace, `PreviewAsync_ReplaceWithMoveNote_ProducesTwoEntries` → Replace+Remove).

- [ ] **Step 6: Коммит**

```bash
git add -A
git commit -m "feat: тип изменения Move (перенос) с детектом по смене пары"
```

### Task 2.2: Расширить `ChangeTag` деталями

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleDtos.cs` (`ChangeTag`)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs:91-98` (`GetChangeTagsAsync`)

**Interfaces:**
- Consumes: `ChangeTag { ChangeType, Week }` → расширяется
- Produces: `ChangeTag { ChangeType, Week, RemovedNumberPair, RemovedSubject, Note }` — для tooltip фронта и маркеров бота

- [ ] **Step 1: Расширить DTO**

`ScheduleDtos.cs`:

```csharp
public class ChangeTag
{
    public ScheduleChangeType ChangeType { get; set; }
    public int Week { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? RemovedSubject { get; set; }
    public string? Note { get; set; }
}
```

- [ ] **Step 2: Заполнять поля в `GetChangeTagsAsync`**

`ScheduleService.cs:96`:

```csharp
g.Select(h => new ChangeTag
    {
        ChangeType = h.ChangeType,
        Week = h.Week,
        RemovedNumberPair = h.RemovedNumberPair,
        RemovedSubject = h.RemovedSubject,
        Note = h.Note,
    })
    .ToList()
```

- [ ] **Step 3: Собрать**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "feat: ChangeTag с деталями (RemovedNumberPair, RemovedSubject, Note) для бейджей"
```

### Task 2.3: Бот — маркеры изменений в расписании

**Files:**
- Modify: `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs` (`ScheduleResponse` + новый `ChangeTag`)
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs` (маркеры)
- Test: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`

**Interfaces:**
- Consumes: API `ChangeTag` (ChangeType/Week/RemovedNumberPair/Note)
- Produces: бот получает `ChangeTags` и отображает рядом с парой `⚠️`-маркеры

- [ ] **Step 1: Тест**

```csharp
[Fact]
public void FormatDaySchedule_ChangeTags_ShowsMarkers()
{
    var entries = new List<ScheduleResponse>
    {
        Entries()[0] with
        {
            ChangeTags =
            [
                new ChangeTag { ChangeType = "Move", Week = 1, RemovedNumberPair = 2 },
            ],
        },
    };

    var text = MessageFormatter.FormatDaySchedule(entries, new DateTime(2026, 9, 7), "Группа 101");

    text.Should().Contain("🔄 перенос с пары 2");
}
```

- [ ] **Step 2: DTO бота**

`CollegeLmsApiDtos.cs`:

```csharp
public record ScheduleResponse
{
    ...
    public List<ChangeTag> ChangeTags { get; init; } = [];
}

public record ChangeTag
{
    public string ChangeType { get; init; } = "";
    public int Week { get; init; }
    public int? RemovedNumberPair { get; init; }
    public string? RemovedSubject { get; init; }
    public string? Note { get; init; }
}
```

- [ ] **Step 3: Маркеры в `FormatDaySchedule` и `FormatWeekSchedule`**

После строки-слота (перед разделителем/пустой строкой), в обоих форматтерах:

```csharp
foreach (var tag in e.ChangeTags)
{
    var marker = tag.ChangeType switch
    {
        "Add" => "🟢 добавлено",
        "Remove" => "🔴 снято",
        "Move" => tag.RemovedNumberPair.HasValue
            ? $"🔄 перенос с пары {tag.RemovedNumberPair}"
            : "🔄 перенос",
        _ => "🔵 замена",
    };
    sb.AppendLine($"    ⚠️ {marker} (нед. {tag.Week})");
}
```

В `FormatDaySchedule` вставить перед `sb.AppendLine("────────");`, в `FormatWeekSchedule` — после строки пары и переноса строки.

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test tests/CollegeLMS.MaxBot.Tests`
Expected: PASS.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: бот показывает маркеры изменений (добавлено/снято/перенос/замена)"
```

### Task 2.4: Фронтенд — тип Move и детальный tooltip бейджей

**Files:**
- Modify: `CollegeLMS.Next/types/correction.ts`
- Modify: `CollegeLMS.Next/components/ChangeTagBadge.tsx`
- Modify: `CollegeLMS.Next/app/(authenticated)/dispatcher/correction/page.tsx:37-56` (CHANGE_TYPE_META)

**Interfaces:**
- Consumes: API `ChangeTag` (новые поля)
- Produces: тип `Move` в union; tooltip показывает «перенос с пары N», «вместо: Предмет»

- [ ] **Step 1: Типы**

`correction.ts`:

```ts
export type CorrectionChangeType = "Add" | "Remove" | "Replace" | "Move"

export interface ChangeTag {
  changeType: CorrectionChangeType
  week: number
  removedNumberPair: number | null
  removedSubject: string | null
  note: string | null
}
```

- [ ] **Step 2: `ChangeTagBadge.tsx`**

```tsx
import { ArrowRightLeft } from "lucide-react"

const CHANGE_META: Record<CorrectionChangeType, { label: string; icon: LucideIcon; className: string }> = {
  Add: { label: "Добавлено", icon: Plus, className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300" },
  Remove: { label: "Снято", icon: Minus, className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300" },
  Replace: { label: "Замена", icon: Repeat, className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300" },
  Move: { label: "Перенос", icon: ArrowRightLeft, className: "bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300" },
}

export default function ChangeTagBadge({ tag }: { tag: ChangeTag }) {
  const meta = CHANGE_META[tag.changeType]
  const Icon = meta.icon

  const detail = tag.changeType === "Move" && tag.removedNumberPair
    ? ` — перенос с пары ${tag.removedNumberPair}`
    : tag.removedSubject
      ? ` — вместо: ${tag.removedSubject}`
      : ""

  return (
    <TooltipProvider delayDuration={0}>
      <Tooltip>
        <TooltipTrigger asChild>
          <span className={cn("inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium leading-none", meta.className)}>
            <Icon className="size-3" aria-hidden />
            {meta.label} · нед. {tag.week}
          </span>
        </TooltipTrigger>
        <TooltipContent>
          {meta.label} — неделя {tag.week}
          {detail}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
```

- [ ] **Step 3: `correction/page.tsx` — CHANGE_TYPE_META**

Добавить в мапу (импортировать `ArrowRightLeft` из `lucide-react`):

```tsx
Move: {
  label: "Перенос",
  icon: ArrowRightLeft,
  className: "bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300",
},
```

- [ ] **Step 4: Проверка сборки фронта**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: бейджи изменений — тип Перенос и детальный tooltip"
```

---
## Фаза 3 — Дашборд диспетчера и шаблоны документов

### Task 3.1: Backend — эндпоинт дашборда диспетчера

**Files:**
- Create: `CollegeLMS.API/Interfaces/IDispatcherDashboardService.cs`
- Create: `CollegeLMS.API/Services/DispatcherDashboardService.cs`
- Create: `CollegeLMS.API/Dtos/DispatcherDashboardDtos.cs`
- Create: `CollegeLMS.API/Mappers/DispatcherDashboardMapper.cs`
- Modify: `CollegeLMS.API/Controllers/DashboardController.cs` (+ endpoint)
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (DI)

**Interfaces:**
- Consumes: `AppDbContext`, `ScheduleImportService.GetPairTime(DayOfWeek, int)`, `StudyWeek.ForDate(DateTime)` (существует в `CollegeLMS.API/Services/StudyWeek.cs`)
- Produces:
  - `GET /api/dispatcher/dashboard?date={yyyy-MM-dd}` — `Result<DispatcherDashboardResponse>`
  - DTO: `DispatcherDashboardResponse { Date, Week, DayOfWeek, Slots: List<DispatcherPairSlot>, Teachers: List<DispatcherTeacherStatus> }`
  - `DispatcherPairSlot { NumberPair, StartTime, EndTime, Entries: List<DispatcherEntry> }`
  - `DispatcherTeacherStatus { TeacherId, TeacherName, TotalPairs, Entries: List<DispatcherEntry> }`
  - `DispatcherEntry { GroupName, Subject, Room, StartTime, EndTime, LessonType, ChangeType }`

- [ ] **Step 1: DTO**

`DispatcherDashboardDtos.cs`:

```csharp
public class DispatcherDashboardResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public List<DispatcherPairSlot> Slots { get; set; } = [];
    public List<DispatcherTeacherStatus> Teachers { get; set; } = [];
}

public class DispatcherPairSlot
{
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<DispatcherEntry> Entries { get; set; } = [];
}

public class DispatcherTeacherStatus
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int TotalPairs { get; set; }
    public List<DispatcherEntry> Entries { get; set; } = [];
}

public class DispatcherEntry
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public ScheduleChangeType? ChangeType { get; set; }
}
```

`using CollegeLMS.API.Entities.Enums;` в начале.

- [ ] **Step 2: Интерфейс**

`IDispatcherDashboardService.cs`:

```csharp
public interface IDispatcherDashboardService
{
    Task<Result<DispatcherDashboardResponse>> GetDailyAsync(DateTime date, CancellationToken ct);
}
```

- [ ] **Step 3: Сервис**

`DispatcherDashboardService.cs`:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

public class DispatcherDashboardService(AppDbContext db) : IDispatcherDashboardService
{
    public async Task<Result<DispatcherDashboardResponse>> GetDailyAsync(DateTime date, CancellationToken ct)
    {
        var day = date.DayOfWeek;
        var week = StudyWeek.ForDate(date);

        var entries = await db.ScheduleEntries
            .AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .Where(s => s.DayOfWeek == day && s.Weeks.Contains(week))
            .OrderBy(s => s.NumberPair)
            .ThenBy(s => s.StartTime)
            .ToListAsync(ct);

        var slots = new List<DispatcherPairSlot>();
        var usedPairs = entries.Select(e => e.NumberPair).Distinct().OrderBy(x => x).ToList();
        foreach (var p in usedPairs)
        {
            var (start, end) = ScheduleImportService.GetPairTime(day, p);
            var slotEntries = entries.Where(e => e.NumberPair == p)
                .Select(e => e.ToDispatcherEntry())
                .ToList();
            slots.Add(new DispatcherPairSlot { NumberPair = p, StartTime = start, EndTime = end, Entries = slotEntries });
        }

        var teachers = entries
            .Where(e => e.TeacherId.HasValue)
            .GroupBy(e => e.TeacherId!.Value)
            .Select(g => new DispatcherTeacherStatus
            {
                TeacherId = g.Key,
                TeacherName = g.First().Teacher?.User?.FullName ?? "—",
                TotalPairs = g.Count(),
                Entries = g.Select(e => e.ToDispatcherEntry()).ToList(),
            })
            .OrderBy(t => t.TeacherName)
            .ToList();

        return Result<DispatcherDashboardResponse>.Ok(new DispatcherDashboardResponse
        {
            Date = date,
            Week = week,
            DayOfWeek = (int)day,
            Slots = slots,
            Teachers = teachers,
        });
    }
}
```

- [ ] **Step 4: Маппер**

`DispatcherDashboardMapper.cs`:

```csharp
public static class DispatcherDashboardMapper
{
    public static DispatcherEntry ToDispatcherEntry(this ScheduleEntry e) =>
        new()
        {
            GroupId = e.GroupId,
            GroupName = e.Group?.Name ?? string.Empty,
            Subject = e.Subject,
            Room = e.Room,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            LessonType = e.LessonType.ToString(),
            ChangeType = null,
        };
}
```

Нужен сервис недели. Он существует: `CollegeLMS.API/Services/StudyWeek.cs` — `StudyWeek.ForDate(DateTime)`. В сервис добавить `using CollegeLMS.API.Services;` — namespace тот же, ничего дополнительно не нужно.

- [ ] **Step 5: Контроллер — добавить в `DashboardController.cs`**

```csharp
[HttpGet("api/dispatcher/dashboard")]
[Authorize(Roles = "Dispatcher,Admin")]
[SwaggerOperation(Summary = "Получить дашборд диспетчера: преподаватели и пары на дату")]
[SwaggerResponse(200, "Дашборд получен", typeof(Result<DispatcherDashboardResponse>))]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(403, "Доступ запрещён")]
[ProducesResponseType(typeof(Result<DispatcherDashboardResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
public async Task<ActionResult<Result<DispatcherDashboardResponse>>> GetDispatcherDashboard(
    [FromQuery] DateTime? date,
    CancellationToken ct
)
{
    var result = await service.GetDailyAsync(date ?? DateTime.UtcNow.Date, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

Изменить primary constructor: `DashboardController(IDashboardService service, IDispatcherDashboardService dispatcherService)` и использовать `dispatcherService`. Для простоты — добавить второй constructor с инъекцией `IDispatcherDashboardService`.

- [ ] **Step 6: DI**

`ServiceCollectionExtensions.cs` — добавить:

```csharp
builder.Services.AddScoped<IDispatcherDashboardService, DispatcherDashboardService>();
```

Проверить, что там регистрируется `IDashboardService` — добавить рядом.

- [ ] **Step 7: Собрать**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 8: Коммит**

```bash
git add -A
git commit -m "feat: backend дашборда диспетчера — преподаватели и слоты пар на дату"
```

### Task 3.2: Frontend — страница дашборда диспетчера

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/dispatcher/dashboard/page.tsx`

**Interfaces:**
- Consumes: `GET /api/dispatcher/dashboard?date=`
- Produces: два блока — «Слоты пар» (таблица время→пары) и «Преподаватели» (карточки со статусами и ссылкой на `/schedule?teacherId=`)

- [ ] **Step 1: Типы**

В начало файла:

```ts
interface DispatcherEntry {
  groupId: string
  groupName: string
  subject: string
  room: string
  startTime: string
  endTime: string
  lessonType: LessonType
  changeType: string | null
}

interface DispatcherPairSlot {
  numberPair: number
  startTime: string
  endTime: string
  entries: DispatcherEntry[]
}

interface DispatcherTeacherStatus {
  teacherId: string
  teacherName: string
  totalPairs: number
  entries: DispatcherEntry[]
}

interface DispatcherDashboardResponse {
  date: string
  week: number
  dayOfWeek: number
  slots: DispatcherPairSlot[]
  teachers: DispatcherTeacherStatus[]
}
```

- [ ] **Step 2: Загрузка данных**

Добавить state `dashboard: DispatcherDashboardResponse | null` и fetch в существующем `useEffect` (или новый):

```ts
useEffect(() => {
  api
    .get<Result<DispatcherDashboardResponse>>("/api/dispatcher/dashboard", {
      params: { date: todayStr() },
    })
    .then((res) => {
      if (res.data.isSuccess && res.data.data) setDashboard(res.data.data)
    })
    .catch(() => setError("Ошибка загрузки дашборда"))
}, [])
```

где `todayStr()` возвращает `new Date().toLocaleDateString("en-CA")` (yyyy-MM-dd).

- [ ] **Step 3: UI — карточка «Преподаватели» (перед Gantt)**

```tsx
{dashboard && (
  <>
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center justify-between text-base">
          <span>Преподаватели на {new Date(dashboard.date).toLocaleDateString("ru-RU")}</span>
          <span className="text-xs text-muted-foreground font-normal">
            Нед. {dashboard.week} · {dashboard.teachers.length} преподавателей
          </span>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {dashboard.teachers.length === 0 ? (
          <p className="text-sm text-muted-foreground py-10 text-center">Нет занятий на этот день</p>
        ) : (
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {dashboard.teachers.map((t) => (
              <Link
                key={t.teacherId}
                href={`/schedule?teacherId=${t.teacherId}`}
                className="rounded-lg border p-3 transition-colors hover:bg-accent/50"
              >
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium truncate">{t.teacherName}</p>
                  <span className="text-xs text-muted-foreground">{t.totalPairs} пар</span>
                </div>
                <div className="mt-2 space-y-1">
                  {t.entries.slice(0, 3).map((e, i) => (
                    <p key={i} className="text-xs text-muted-foreground truncate">
                      {e.startTime.slice(0, 5)} · {e.groupName} · {e.subject} · {e.room}
                    </p>
                  ))}
                  {t.entries.length > 3 && (
                    <p className="text-xs text-muted-foreground">+{t.entries.length - 3} ещё</p>
                  )}
                </div>
              </Link>
            ))}
          </div>
        )}
      </CardContent>
    </Card>

    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Слоты пар</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-xs text-muted-foreground">
                <th className="text-left py-2">Пара</th>
                <th className="text-left py-2">Время</th>
                <th className="text-left py-2">Занятия</th>
              </tr>
            </thead>
            <tbody>
              {dashboard.slots.map((slot) => (
                <tr key={slot.numberPair} className="border-b border-border/50">
                  <td className="py-2 align-top">{slot.numberPair}</td>
                  <td className="py-2 align-top whitespace-nowrap">
                    {slot.startTime.slice(0, 5)}–{slot.endTime.slice(0, 5)}
                  </td>
                  <td className="py-2">
                    {slot.entries.length === 0 ? (
                      <span className="text-xs text-muted-foreground">—</span>
                    ) : (
                      <ul className="space-y-1">
                        {slot.entries.map((e, i) => (
                          <li key={i} className="text-xs">
                            <span className="font-medium">{e.groupName}</span>
                            <span className="text-muted-foreground">
                              {" "}· {e.subject} · {e.room}
                            </span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  </>
)}
```

Добавить `import Link from "next/link"`. Импорт `Lessons`-типов: `LessonType` уже импортируется.

- [ ] **Step 4: Собрать**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: дашборд диспетчера — преподаватели на день и слоты пар"
```

### Task 3.3: Backend — шаблоны документов (xlsx)

**Files:**
- Create: `CollegeLMS.API/Interfaces/IDocumentsService.cs`
- Create: `CollegeLMS.API/Services/DocumentsService.cs`
- Create: `CollegeLMS.API/Dtos/DocumentTemplateDtos.cs`
- Create: `CollegeLMS.API/Controllers/DocumentsController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (DI)
- Modify: `CollegeLMS.API/appsettings.json` (+ `TemplatesPath`)

**Interfaces:**
- Consumes: ФС `import/schedule/` (файлы `Расписание.xlsx`, `Корректировка.xlsx`)
- Produces:
  - `GET /api/dispatcher/documents/templates` — список шаблонов
  - `GET /api/dispatcher/documents/templates/{fileName}/download` — скачивание файла

- [ ] **Step 1: DTO**

`DocumentTemplateDtos.cs`:

```csharp
public class DocumentTemplateResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Size { get; set; }
}
```

- [ ] **Step 2: Интерфейс**

```csharp
public interface IDocumentsService
{
    Task<Result<List<DocumentTemplateResponse>>> GetTemplatesAsync(CancellationToken ct);
    Task<Result<DocumentDownloadResult>> DownloadAsync(string fileName, CancellationToken ct);
}

public class DocumentDownloadResult
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileName { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Сервис**

`DocumentsService.cs`:

```csharp
public class DocumentsService(IConfiguration config) : IDocumentsService
{
    private readonly string _templatesPath = config["TemplatesPath"] ?? Path.Combine("..", "import", "schedule");

    private static readonly (string FileName, string Name, string Description)[] Known =
    [
        ("Расписание.xlsx", "Расписание", "Шаблон расписания для импорта"),
        ("Корректировка.xlsx", "Корректировка", "Шаблон корректировки расписания для импорта"),
    ];

    public Task<Result<List<DocumentTemplateResponse>>> GetTemplatesAsync(CancellationToken ct)
    {
        var result = Known
            .Select(k => new DocumentTemplateResponse
            {
                FileName = k.FileName,
                Name = k.Name,
                Description = k.Description,
                Size = File.Exists(Path.Combine(_templatesPath, k.FileName))
                    ? new FileInfo(Path.Combine(_templatesPath, k.FileName)).Length
                    : 0,
            })
            .ToList();

        return Task.FromResult(Result<List<DocumentTemplateResponse>>.Ok(result));
    }

    public async Task<Result<DocumentDownloadResult>> DownloadAsync(string fileName, CancellationToken ct)
    {
        var known = Known.FirstOrDefault(k => k.FileName == fileName);
        if (known.FileName is null)
            return Result<DocumentDownloadResult>.Fail("Шаблон не найден", 404);

        var path = Path.GetFullPath(Path.Combine(_templatesPath, fileName));
        if (!File.Exists(path))
            return Result<DocumentDownloadResult>.Fail("Файл шаблона отсутствует на сервере", 404);

        var content = await File.ReadAllBytesAsync(path, ct);
        return Result<DocumentDownloadResult>.Ok(new DocumentDownloadResult
        {
            Content = content,
            FileName = fileName,
        });
    }
}
```

**Безопасность:** фильтр по `Known` защищает от path traversal — `fileName` никогда не используется для построения пути до whitelist-проверки.

- [ ] **Step 4: Контроллер**

`DocumentsController.cs`:

```csharp
[ApiController]
[Route("api/dispatcher/documents")]
[Produces("application/json")]
[Authorize(Roles = "Dispatcher,Admin")]
public class DocumentsController(IDocumentsService service) : ControllerBase
{
    [HttpGet("templates")]
    [SwaggerOperation(Summary = "Получить список шаблонов документов")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<DocumentTemplateResponse>>))]
    [ProducesResponseType(typeof(Result<List<DocumentTemplateResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<List<DocumentTemplateResponse>>>> GetTemplates(CancellationToken ct)
    {
        var result = await service.GetTemplatesAsync(ct);
        return Ok(result);
    }

    [HttpGet("templates/{fileName}/download")]
    [SwaggerOperation(Summary = "Скачать шаблон документа")]
    [SwaggerResponse(200, "Файл скачан")]
    [SwaggerResponse(404, "Шаблон не найден")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName, CancellationToken ct)
    {
        var result = await service.DownloadAsync(fileName, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }
}
```

- [ ] **Step 5: DI**

В `ServiceCollectionExtensions.cs`:

```csharp
builder.Services.AddScoped<IDocumentsService, DocumentsService>();
```

- [ ] **Step 6: appsettings.json**

```json
{ "TemplatesPath": "../import/schedule" }
```

(добавить в секцию верхнего уровня).

- [ ] **Step 7: Собрать**

Run: `dotnet build CollegeLMS.API/CollegeLMS.API.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 8: Коммит**

```bash
git add -A
git commit -m "feat: шаблоны документов — список и скачивание xlsx"
```

### Task 3.4: Frontend — страница «Документы»

**Files:**
- Modify: `CollegeLMS.Next/app/(authenticated)/dispatcher/documents/page.tsx`
- Create: `CollegeLMS.Next/api/documents.ts`

**Interfaces:**
- Consumes: `GET /api/dispatcher/documents/templates`, `GET .../templates/{fileName}/download`
- Produces: карточки шаблонов с кнопкой «Скачать»

- [ ] **Step 1: API-клиент**

`CollegeLMS.Next/api/documents.ts`:

```ts
import api from "@/lib/api"
import type { Result } from "@/types"

export interface DocumentTemplate {
  fileName: string
  name: string
  description: string
  size: number
}

export async function getTemplates(): Promise<DocumentTemplate[]> {
  const res = await api.get<Result<DocumentTemplate[]>>("/api/dispatcher/documents/templates")
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка загрузки шаблонов")
  return res.data.data ?? []
}

export function downloadTemplateUrl(fileName: string): string {
  return `/api/dispatcher/documents/templates/${encodeURIComponent(fileName)}/download`
}
```

- [ ] **Step 2: Страница**

`documents/page.tsx`:

```tsx
"use client"

import { useEffect, useState } from "react"
import { FileText, Download, Loader2 } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { getTemplates, downloadTemplateUrl, type DocumentTemplate } from "@/api/documents"
import { toast } from "sonner"

function formatSize(bytes: number): string {
  return bytes > 0 ? `${(bytes / 1024).toFixed(0)} КБ` : "—"
}

export default function DispatcherDocumentsPage() {
  const [templates, setTemplates] = useState<DocumentTemplate[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getTemplates()
      .then(setTemplates)
      .catch((err) => toast.error(err instanceof Error ? err.message : "Ошибка загрузки"))
      .finally(() => setLoading(false))
  }, [])

  const handleDownload = (fileName: string) => {
    const a = document.createElement("a")
    a.href = downloadTemplateUrl(fileName)
    a.download = fileName
    a.click()
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Документы</h2>

      {loading && <Loader2 className="size-6 animate-spin text-muted-foreground mx-auto py-20" />}

      {!loading && templates.length === 0 && (
        <Card>
          <CardContent>
            <p className="text-sm text-muted-foreground py-10 text-center">Шаблоны не найдены</p>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {templates.map((t) => (
          <Card key={t.fileName}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <FileText size={16} /> {t.name}
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <p className="text-sm text-muted-foreground">{t.description}</p>
              <p className="text-xs text-muted-foreground">{t.fileName} · {formatSize(t.size)}</p>
              <Button onClick={() => handleDownload(t.fileName)} disabled={t.size === 0}>
                <Download size={16} /> Скачать
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Проверить аутентификацию скачивания**

Файл скачивается прямым `a.href` без заголовка Authorization. Проверить, как `api` клиент передаёт токен (query/cookie). Если токен передаётся через `Authorization` header — прямое открытие не сработает. **Альтернатива:** fetch с токеном через `api` клиент + Blob:

В `documents.ts` заменить download на:

```ts
export async function downloadTemplate(fileName: string): Promise<Blob> {
  const res = await api.get(`/api/dispatcher/documents/templates/${encodeURIComponent(fileName)}/download`, {
    responseType: "blob",
  })
  return res.data as Blob
}
```

и в странице:

```tsx
const handleDownload = async (fileName: string) => {
  try {
    const blob = await downloadTemplate(fileName)
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = fileName
    a.click()
    URL.revokeObjectURL(url)
  } catch (err) {
    toast.error(err instanceof Error ? err.message : "Ошибка скачивания")
  }
}
```

- [ ] **Step 4: Собрать**

Run: `npm run build` (в `CollegeLMS.Next/`)
Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Коммит**

```bash
git add -A
git commit -m "feat: страница Документы — карточки шаблонов и скачивание"
```

---
## Фаза 4 — Верификация и roadmap мини-приложения (без кода)

### Task 4.1: Обновить постман-коллекцию и SwaggerExamples

**Files:**
- Modify: `docs/spec/CollegeLMS.postman_collection.json`
- Modify: `CollegeLMS.API/SwaggerExamples/` — примеры `DispatcherDashboardResponse`, `DocumentTemplateResponse`

- [ ] **Step 1:** Добавить `GET /api/dispatcher/dashboard` и `GET /api/dispatcher/documents/templates` в Postman-коллекцию (формат существующих запросов).

- [ ] **Step 2:** Создать `SwaggerExamples/DispatcherDashboardResponseExample.cs` и `SwaggerExamples/DocumentTemplateResponseExample.cs` по образцу существующих `*Example.cs`.

- [ ] **Step 3:** `dotnet build` + CSharpier:

```bash
dotnet csharpier format .
dotnet build
npm run build --prefix CollegeLMS.Next
```

Expected: build + format pass.

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "docs: Postman и Swagger-примеры для дашборда и документов"
```

### Task 4.2: Roadmap мини-приложения MAX (документация)

**Objective:** Не пишем код. Документируем в `docs/superpowers/specs/2026-09-09-miniapp-max-roadmap.md`: требования платформы (юрлицо/ИП/самозанятый, верификация, модерация), MAX UI (`@maxhub/max-ui`, React) и MAX Bridge, кнопка `open_app` для сообщений бота, этапы (стилизация уже сделана → мини-приложение).

- [ ] **Step 1:** Создать файл roadmap по шаблону ниже.

- [ ] **Step 2: Коммит**

```bash
git add -A
git commit -m "docs: roadmap мини-приложения MAX"
```

---
## Полная проверка после всех фаз

```bash
dotnet csharpier format . && dotnet build && dotnet test tests/CollegeLMS.Tests
npm run build --prefix CollegeLMS.Next
```

Expected: всё зелёное. Затем `verification-before-completion`, merge в master, push (CD на VPS сам применит миграции через compose profile `max-bot`).

## Self-Review

**Spec coverage:**
- Уведомления об изменениях всем → Task 1.2 ✓
- Ежедневный дайджест будни → Task 1.3 (пн–пт) ✓
- Нумерация пар → Task 1.1 ✓
- Стилизация бота → Task 1.5 ✓
- Тип «перенос» (Move) → Task 2.1, 2.3, 2.4 ✓
- Бейджи с детальным tooltip → Task 2.2, 2.4 ✓
- Дашборд диспетчера → Task 3.1, 3.2 ✓ (свободные аудитории — вне scope по решению пользователя)
- Шаблоны документов → Task 3.3, 3.4 ✓
- Мини-приложение → Task 4.2 (roadmap, код вне рамок по решению пользователя) ✓

**Placeholder scan:** нет TBD/TODO; код приведён полностью. В Task 3.3 используется whitelist вместо произвольных путей — защита от path traversal.

**Type consistency:** `DispatcherDashboardResponse`/`DispatcherPairSlot`/`DispatcherTeacherStatus`/`DispatcherEntry` совпадают между DTO, сервисом, фронтом; `ChangeTag.RemovedNumberPair/RemovedSubject/Note` согласованы backend↔бот↔фронт; `ScheduleChangeType.Move` — везде один enum.