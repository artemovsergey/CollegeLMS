# MaxBot: меню-навигация по расписанию — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Убрать слэш-команды из UI MaxBot и дать кнопочную навигацию по расписанию: день, неделя, конкретная дата (календарь) — для студента (группа) и преподавателя.

**Architecture:** Дата-центричная навигация. Бот оперирует реальными датами (якорь), передаваемыми в callback-payload; состояния в памяти нет. Дата → запрос `week=StudyWeek.ForDate(date)` + `dayOfWeek=ToApiDay(date.DayOfWeek)` в существующее API `/api/schedule` (фильтры складываются, бэкенд не меняется). Новые форматтеры: callback-payload парсер, календарная сетка, даты в заголовках дня/недели.

**Tech Stack:** .NET 10, xUnit + FluentAssertions, CSharpier, `CollegeLMS.MaxBot` (BackgroundService бот MAX).

## Global Constraints

- Без изменений: `MaxApiClient`, `ScheduleNotifier`, `CollegeLMS.API`, фронтенд, deploy.
- Все сообщения интерфейса — на русском; код — на русском (комментарии отсутствуют).
- Слэш-команды в UI не показываем; `/start`, `/help`, `/settings` сохраняются для совместимости, `/schedule`, `/week` удаляются.
- Время: даты в payload — `yyyy-MM-dd`, месяцы — `yyyy-MM`, локальная зона `_tz`.
- Часовой пояс: `_tz` — из `TimeZoneProvider` (МСК). Добавить `StudyWeek.Now(TimeZoneInfo tz)`.
- Гит-сообщения: `feat: …`.
- Проверка форматирования: `dotnet csharpier check <files>`; сборка: `dotnet build CollegeLMS.slnx`; тесты: `dotnet test CollegeLMS.MaxBot.Tests`.

---

### Task 1: CallbackPayload — разбор и генерация payload

**Files:**
- Create: `CollegeLMS.MaxBot/Services/CallbackPayload.cs`
- Test: `CollegeLMS.MaxBot.Tests/CallbackPayloadTests.cs`

**Interfaces:**
- Produces: `sealed record CallbackPayload(string Action, string? Param1, string? Param2)` со статическими методами `Parse(string?)`, `Day(DateTime)`, `DayPrev(DateTime)`, `DayNext(DateTime)`, `Week(DateTime)`, `WeekPrev(DateTime)`, `WeekNext(DateTime)`, `Cal(DateTime)`, `CalPrev(DateTime)`, `CalNext(DateTime)`, `TryParseDate(string?)`, `TryParseMonth(string?)`.

- [ ] **Step 1: Write the failing test**

`CollegeLMS.MaxBot.Tests/CallbackPayloadTests.cs`:
```csharp
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class CallbackPayloadTests
{
    [Fact]
    public void Parse_DayPayload_SplitsParts()
    {
        var p = CallbackPayload.Parse("day:2026-09-08");
        p.Should().NotBeNull();
        p!.Action.Should().Be("day");
        p.Param1.Should().Be("2026-09-08");
        p.Param2.Should().BeNull();
    }

    [Fact]
    public void Parse_ThreePartPayload_SplitsAll()
    {
        var p = CallbackPayload.Parse("page:groups:2");
        p.Should().NotBeNull();
        p!.Action.Should().Be("page");
        p.Param1.Should().Be("groups");
        p.Param2.Should().Be("2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Blank_ReturnsNull(string? payload)
    {
        CallbackPayload.Parse(payload).Should().BeNull();
    }

    [Fact]
    public void FactoryMethods_ProduceExpectedPayload()
    {
        var d = new DateTime(2026, 9, 8);
        CallbackPayload.Day(d).Should().Be("day:2026-09-08");
        CallbackPayload.DayPrev(d).Should().Be("dayprev:2026-09-08");
        CallbackPayload.DayNext(d).Should().Be("daynext:2026-09-08");
        CallbackPayload.Week(d).Should().Be("week:2026-09-08");
        CallbackPayload.WeekPrev(d).Should().Be("weekprev:2026-09-08");
        CallbackPayload.WeekNext(d).Should().Be("weeknext:2026-09-08");
        CallbackPayload.Cal(d).Should().Be("cal:2026-09");
        CallbackPayload.CalPrev(d).Should().Be("calprev:2026-09");
        CallbackPayload.CalNext(d).Should().Be("calnext:2026-09");
    }

    [Fact]
    public void TryParseDate_ValidAndInvalid()
    {
        CallbackPayload.TryParseDate("2026-09-08").Should().Be(new DateTime(2026, 9, 8));
        CallbackPayload.TryParseDate("nope").Should().BeNull();
        CallbackPayload.TryParseDate(null).Should().BeNull();
    }

    [Fact]
    public void TryParseMonth_ValidAndInvalid()
    {
        CallbackPayload.TryParseMonth("2026-09").Should().Be(new DateTime(2026, 9, 1));
        CallbackPayload.TryParseMonth("2026").Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~CallbackPayloadTests" -nologo`
Expected: FAIL (тип не существует: `CallbackPayload`).

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.MaxBot/Services/CallbackPayload.cs`:
```csharp
using System.Globalization;

namespace CollegeLMS.MaxBot.Services;

public sealed record CallbackPayload(string Action, string? Param1, string? Param2)
{
    public static CallbackPayload? Parse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;
        var parts = payload.Split(':', 3);
        return new CallbackPayload(
            parts[0],
            parts.Length > 1 ? parts[1] : null,
            parts.Length > 2 ? parts[2] : null
        );
    }

    public static string Day(DateTime date) => $"day:{date:yyyy-MM-dd}";
    public static string DayPrev(DateTime date) => $"dayprev:{date:yyyy-MM-dd}";
    public static string DayNext(DateTime date) => $"daynext:{date:yyyy-MM-dd}";
    public static string Week(DateTime date) => $"week:{date:yyyy-MM-dd}";
    public static string WeekPrev(DateTime date) => $"weekprev:{date:yyyy-MM-dd}";
    public static string WeekNext(DateTime date) => $"weeknext:{date:yyyy-MM-dd}";
    public static string Cal(DateTime month) => $"cal:{month:yyyy-MM}";
    public static string CalPrev(DateTime month) => $"calprev:{month:yyyy-MM}";
    public static string CalNext(DateTime month) => $"calnext:{month:yyyy-MM}";

    public static DateTime? TryParseDate(string? text)
    {
        if (text is null)
            return null;
        return DateTime.TryParseExact(
            text,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
        )
            ? date
            : null;
    }

    public static DateTime? TryParseMonth(string? text)
    {
        if (text is null)
            return null;
        return DateTime.TryParseExact(
            text,
            "yyyy-MM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var month
        )
            ? month
            : null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~CallbackPayloadTests" -nologo`
Expected: PASS (все тесты).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: maxbot callback payload parser"
```

---

### Task 2: MessageFormatter — даты в заголовках, индексы дней, помощники

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs`
- Test: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`

**Interfaces:**
- Consumes: `ScheduleResponse` из `CollegeLMS.MaxBot.Clients`.
- Produces (сохраняя старые перегрузки для `ScheduleNotifier`):
  - `static int DayIndex(int apiDay)` — 0(Вс)→7, иначе `apiDay`.
  - `static int DayOffset(int apiDay)` — сдвиг от понедельника недели: Вс→6, иначе `apiDay-1`.
  - `static DateTime DateForWeekDay(DateTime weekStart, int apiDay)`.
  - `static string FormatDaySchedule(List<ScheduleResponse> entries, DateTime date, string entityName)`.
  - `static string FormatWeekSchedule(List<ScheduleResponse> entries, DateTime weekStart, string entityName)`.
  - `static string FormatShortDate(DateTime date)` — `dd.MM`.
  - `static string FormatLongDate(DateTime date)` — «Понедельник, 07.09».
  - `static string DayLabelForDate(DateTime date)`, `static string DayAbbrForDate(DateTime date)`.

- [ ] **Step 1: Write the failing tests**

`CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`:
```csharp
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MessageFormatterTests
{
    private static List<ScheduleResponse> Entries() =>
    [
        new ScheduleResponse
        {
            DayOfWeek = 1,
            NumberPair = 1,
            Subject = "Математика",
            Room = "405",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            TeacherName = "Иванов И.И.",
            LessonType = "Lecture",
        },
    ];

    [Fact]
    public void FormatDaySchedule_IncludesDateInHeader()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Entries(),
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
        text.Should().Contain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_Empty_ShowsHoliday()
    {
        var text = MessageFormatter.FormatDaySchedule(
            [],
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("Расписания нет — выходной!");
    }

    [Fact]
    public void FormatWeekSchedule_HeaderHasDateRange()
    {
        var weekStart = new DateTime(2026, 9, 7);
        var text = MessageFormatter.FormatWeekSchedule(Entries(), weekStart, "Группа 101");

        text.Should().Contain("07.09–13.09");
        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
    }

    [Fact]
    public void FormatWeekSchedule_SundayEntries_HeaderUsesIndex7()
    {
        var weekStart = new DateTime(2026, 9, 7);
        var entries = new List<ScheduleResponse>
        {
            Entries()[0] with { DayOfWeek = 0 },
        };
        var text = MessageFormatter.FormatWeekSchedule(entries, weekStart, "Группа 101");

        text.Should().Contain("Воскресенье, 13.09");
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    public void DayIndex_MapsApiDayToLabelIndex(int apiDay, int expected)
    {
        MessageFormatter.DayIndex(apiDay).Should().Be(expected);
    }

    [Fact]
    public void DateForWeekDay_MapsApiDayToDate()
    {
        var weekStart = new DateTime(2026, 9, 7);
        MessageFormatter.DateForWeekDay(weekStart, 1).Should().Be(new DateTime(2026, 9, 7));
        MessageFormatter.DateForWeekDay(weekStart, 0).Should().Be(new DateTime(2026, 9, 13));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~MessageFormatterTests" -nologo`
Expected: FAIL (новые члены не существуют / сигнатура не совпадает).

- [ ] **Step 3: Implement**

`CollegeLMS.MaxBot/Services/MessageFormatter.cs` — добавить/заменить:
```csharp
    public static int DayIndex(int apiDay) => apiDay == 0 ? 7 : apiDay;

    public static int DayOffset(int apiDay) => apiDay == 0 ? 6 : apiDay - 1;

    public static DateTime DateForWeekDay(DateTime weekStart, int apiDay) =>
        weekStart.AddDays(DayOffset(apiDay));

    public static string FormatShortDate(DateTime date) => date.ToString("dd.MM");

    public static string FormatLongDate(DateTime date) =>
        $"{DayNames[DayIndex((int)date.DayOfWeek)]}, {FormatShortDate(date)}";

    public static string DayLabelForDate(DateTime date) =>
        DayNames[DayIndex((int)date.DayOfWeek)];

    public static string DayAbbrForDate(DateTime date) =>
        DayAbbr[DayIndex((int)date.DayOfWeek)];

    // Новая перегрузка с датой (не меняет старую — её использует ScheduleNotifier)
    public static string FormatDaySchedule(
        List<ScheduleResponse> entries,
        DateTime date,
        string entityName
    )
    {
        var header = $"📋 *{FormatLongDate(date)}* — {entityName}";
        if (entries.Count == 0)
            return $"{header}\n\nРасписания нет — выходной!";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine();

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

            if (e.TeacherName is not null)
                sb.AppendLine($"    👨‍🏫 {e.TeacherName}");

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    // Новая перегрузка с неделей (старая остаётся, но теперь на её место
    // в MaxBotService приходит эта)
    public static string FormatWeekSchedule(
        List<ScheduleResponse> entries,
        DateTime weekStart,
        string entityName
    )
    {
        var weekEnd = weekStart.AddDays(6);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(
            $"📅 *Неделя {FormatShortDate(weekStart)}–{FormatShortDate(weekEnd)}* — {entityName}"
        );
        sb.AppendLine();

        var grouped = entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key);

        foreach (var group in grouped)
        {
            var date = DateForWeekDay(weekStart, group.Key);
            sb.AppendLine($"*{DayNames[DayIndex(group.Key)]}, {FormatShortDate(date)}*");
            foreach (var e in group.OrderBy(x => x.NumberPair))
            {
                var type = e.LessonType switch
                {
                    "Lecture" => "📖",
                    "Practice" => "✏️",
                    "Lab" => "🔬",
                    "Exam" => "📝",
                    _ => "📚",
                };
                sb.AppendLine(
                    $"  {e.NumberPair}. {type} {e.Subject} ({e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}, {e.Room})"
                );
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~MessageFormatterTests" -nologo`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: maxbot date-aware schedule formatting"
```

---

### Task 3: CalendarFormatter — сетка календаря месяца

**Files:**
- Create: `CollegeLMS.MaxBot/Services/CalendarFormatter.cs`
- Test: `CollegeLMS.MaxBot.Tests/CalendarFormatterTests.cs`

**Interfaces:**
- Consumes: `MaxButton` из `CollegeLMS.MaxBot.Models.Max`, `CallbackPayload`.
- Produces:
  - `static string MonthTitle(DateTime month)` — «Сентябрь 2026».
  - `static List<List<MaxButton>> BuildGrid(DateTime month)` — строки недель; дата → `day:yyyy-MM-dd`; пустые ячейки опущены.
  - `static bool CanGoPrev(DateTime month)` / `static bool CanGoNext(DateTime month, DateTime maxMonth)` — границы календаря.

- [ ] **Step 1: Write the failing tests**

`CollegeLMS.MaxBot.Tests/CalendarFormatterTests.cs`:
```csharp
using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class CalendarFormatterTests
{
    [Fact]
    public void MonthTitle_FormatsRussianMonth()
    {
        CalendarFormatter.MonthTitle(new DateTime(2026, 9, 1)).Should().Be("Сентябрь 2026");
        CalendarFormatter.MonthTitle(new DateTime(2027, 1, 1)).Should().Be("Январь 2027");
    }

    [Fact]
    public void BuildGrid_Sep2026_FirstButtonIsTuesdayFirst()
    {
        // Сентябрь 2026: 1-е — вторник. Первая строка начинается с вторника.
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 9, 1));

        var all = buttons.SelectMany(x => x).ToList();
        all.Should().HaveCount(30);
        all[0].Payload.Should().Be("day:2026-09-01");
        all[0].Text.Should().Be("1");
    }

    [Fact]
    public void BuildGrid_DaysGroupedByWeek()
    {
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 9, 1));

        buttons.Should().NotBeEmpty();
        foreach (var row in buttons)
            row.Should().NotBeEmpty();
        // Количество строк = ceil((offset+30)/7) для сентября 2026: ceil(1+30 /7)=5
        buttons.Should().HaveCount(5);
    }

    [Fact]
    public void BuildGrid_DecemberHas31Days()
    {
        var buttons = CalendarFormatter.BuildGrid(new DateTime(2026, 12, 1));
        buttons.SelectMany(x => x).Should().HaveCount(31);
    }

    [Fact]
    public void Bounds_ControlArrows()
    {
        var semesterStart = new DateTime(2026, 9, 1);
        var maxMonth = new DateTime(2026, 12, 1);

        CalendarFormatter.CanGoPrev(semesterStart).Should().BeFalse();
        CalendarFormatter.CanGoPrev(new DateTime(2026, 10, 1)).Should().BeTrue();
        CalendarFormatter.CanGoNext(new DateTime(2026, 12, 1), maxMonth).Should().BeFalse();
        CalendarFormatter.CanGoNext(new DateTime(2026, 11, 1), maxMonth).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~CalendarFormatterTests" -nologo`
Expected: FAIL (тип не существует).

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.MaxBot/Services/CalendarFormatter.cs`:
```csharp
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

public static class CalendarFormatter
{
    private static readonly string[] MonthNames =
    [
        "Январь",
        "Февраль",
        "Март",
        "Апрель",
        "Май",
        "Июнь",
        "Июль",
        "Август",
        "Сентябрь",
        "Октябрь",
        "Ноябрь",
        "Декабрь",
    ];

    public static string MonthTitle(DateTime month) =>
        $"{MonthNames[month.Month - 1]} {month.Year}";

    public static List<List<MaxButton>> BuildGrid(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        var offset = ((int)start.DayOfWeek + 6) % 7; // Пн = 0, Вс = 6

        var grid = new List<List<MaxButton>>();
        var row = new List<MaxButton>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            if ((offset + day - 1) % 7 == 0 && row.Count > 0)
            {
                grid.Add(row);
                row = new List<MaxButton>();
            }

            var date = start.AddDays(day - 1);
            row.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = day.ToString(),
                    Payload = CallbackPayload.Day(date),
                }
            );
        }

        if (row.Count > 0)
            grid.Add(row);

        return grid;
    }

    public static bool CanGoPrev(DateTime month)
    {
        var thisFirst = new DateTime(month.Year, month.Month, 1);
        var firstSemester = new DateTime(
            StudyWeek.SemesterStart.Year,
            StudyWeek.SemesterStart.Month,
            1
        );
        return thisFirst > firstSemester;
    }

    public static bool CanGoNext(DateTime month, DateTime maxMonth)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var maxFirst = new DateTime(maxMonth.Year, maxMonth.Month, 1);
        return first < maxFirst;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~CalendarFormatterTests" -nologo`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: maxbot calendar grid"
```

---

### Task 4: StudyWeek — текущая локальная дата

**Files:**
- Modify: `CollegeLMS.MaxBot/Services/StudyWeek.cs`
- Test: дополнение в `CollegeLMS.MaxBot.Tests/StudyWeekTests.cs` (новый)

**Interfaces:**
- Consumes: `TimeZoneInfo`.
- Produces: `static DateTime Now(TimeZoneInfo tz)` — сегодняшняя дата в локальной зоне (`.Date`).
  - Существующие `SemesterStart`, `MondayOf`, `ForDate`, `Current` остаются (используются).

- [ ] **Step 1: Write the failing test**

`CollegeLMS.MaxBot.Tests/StudyWeekTests.cs`:
```csharp
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class StudyWeekTests
{
    [Fact]
    public void Now_UsesProvidedTimeZoneAsDate()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        var now = StudyWeek.Now(tz);

        now.Kind.Should().Be(DateTimeKind.Unspecified);
        now.TimeOfDay.Should().Be(TimeSpan.Zero);
        // дата в диапазоне, близком к локальному «сейчас» — проверяем отклонение < 2 дней от UTC
        now.Should().BeAfter(DateTime.UtcNow.Date.AddDays(-2));
    }

    [Fact]
    public void ForDate_Boundary_MondayOfNextWeek()
    {
        // Семестр начинается 2026-09-01 (вторник) — это неделя 1.
        StudyWeek.ForDate(new DateTime(2026, 9, 1)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 7)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 13)).Should().Be(1);
        StudyWeek.ForDate(new DateTime(2026, 9, 14)).Should().Be(2);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~StudyWeekTests" -nologo`
Expected: FAIL (`StudyWeek.Now` не существует).

- [ ] **Step 3: Implement**

`CollegeLMS.MaxBot/Services/StudyWeek.cs` — добавить метод:
```csharp
    public static DateTime Now(TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter "FullyQualifiedName~StudyWeekTests" -nologo`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: maxbot StudyWeek.Now"
```

---

### Task 5: MaxBotService — экраны навигации и рефакторинг callback-обработчика

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`

**Interfaces:**
- Consumes: `CallbackPayload`, `CalendarFormatter`, `MessageFormatter` (перегрузки с датами), `StudyWeek.Now`, существующие `MaxApiClient`, `CollegeLmsApiClient`, `MaxBotDbContext`, `IUserSettings`.
- Produces: приватные методы `ShowMainMenuAsync(chatId, userId, ct)`, `ShowDayAsync(chatId, userId, DateTime date, ct)`, `ShowWeekAsync(chatId, userId, DateTime anchor, ct)`, `ShowCalendarAsync(chatId, userId, DateTime month, ct)`.
- Сохраняет существующие публичные сценарии: роли, списки групп/преподавателей, настройки, уведомления.

Сценарий изменений по шагам:

- [ ] **Step 1: Убрать мёртвый код**

Удалить поле `private readonly ConcurrentDictionary<long, InteractionState> _states = new();`,
соответствующую обработку в `HandleMessageAsync` (`if (_states.TryGetValue(userId, out _)) { ... }`)
и `private record InteractionState(string Step, Dictionary<string, string> Data);`.

- [ ] **Step 2: Обновить текстовые команды**

В `HandleMessageAsync`:
- `/help` — новый текст:
```csharp
"🤖 *Бот расписания*\n\n"
    + "Всё управление — кнопками под сообщениями.\n"
    + "/start — начать заново\n"
    + "/settings — настройки\n"
    + "/help — справка"
```
- Удалить ветки `/schedule`, `/week`.
- Ветка «не распознано» (после `/settings`) — вместо «Не понял команду…»:
```csharp
await _max.SendMessageAsync(
    chatId,
    "Используй кнопки меню — команды писать не нужно.",
    ct: ct
);
await ShowMainMenuAsync(chatId, userId, ct);
```

- [ ] **Step 3: Переписать HandleCallbackAsync**

Заголовок метода (первый блок — как сейчас: payload null → возврат, `AnswerCallbackAsync`, лог). Затем `var p = CallbackPayload.Parse(payload); if (p is null) { menu; return; }` и switch:
```csharp
        switch (p.Action)
        {
            case "menu":
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "today":
                await ShowDayAsync(chatId, userId, StudyWeek.Now(_tz), ct);
                break;
            case "day":
                var day = CallbackPayload.TryParseDate(p.Param1);
                if (day is not null)
                    await ShowDayAsync(chatId, userId, day.Value, ct);
                break;
            case "dayprev":
                await ShowDayAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "daynext":
                await ShowDayAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "week":
                await ShowWeekAfterParse(chatId, userId, p.Param1, 0, ct);
                break;
            case "weekprev":
                await ShowWeekAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "weeknext":
                await ShowWeekAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "cal":
                var month = CallbackPayload.TryParseMonth(p.Param1);
                if (month is not null)
                    await ShowCalendarAsync(chatId, userId, month.Value, ct);
                break;
            case "calprev":
                await ShowCalendarAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "calnext":
                await ShowCalendarAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "role":
                await HandleRoleSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "group":
                await HandleGroupSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "teacher":
                await HandleTeacherSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "page":
                var listName = p.Param1;
                if (listName == "groups")
                    await ShowGroupSelectionAsync(chatId, userId, int.Parse(p.Param2!), ct);
                if (listName == "teachers")
                    await ShowTeacherSelectionAsync(chatId, userId, int.Parse(p.Param2!), ct);
                break;
            case "settings":
                if (p.Param1 == "group")
                    await ShowGroupSelectionAsync(chatId, userId, 0, ct);
                if (p.Param1 == "teacher")
                    await ShowTeacherSelectionAsync(chatId, userId, 0, ct);
                break;
            case "notify":
                await HandleNotifyToggleAsync(chatId, userId, ct);
                break;
            case "notifyday":
                await HandleNotifyDayToggleAsync(chatId, userId, int.Parse(p.Param1!), ct);
                break;
            case "notifysave":
                await _max.SendMessageAsync(chatId, "✅ Настройки уведомлений сохранены!", ct: ct);
                break;
            default:
                _logger.LogWarning("Unknown callback action {Action}", p.Action);
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
        }
```

Дополнительные приватные помощники (в тело класса):
```csharp
    private async Task ShowDayAfterParse(
        long chatId,
        long userId,
        string? dateText,
        int deltaDays,
        CancellationToken ct
    )
    {
        var date = CallbackPayload.TryParseDate(dateText);
        if (date is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }
        await ShowDayAsync(chatId, userId, date.Value.AddDays(deltaDays), ct);
    }

    private async Task ShowWeekAfterParse(
        long chatId,
        long userId,
        string? dateText,
        int deltaWeeks,
        CancellationToken ct
    )
    {
        var date = CallbackPayload.TryParseDate(dateText);
        if (date is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }
        var anchor = StudyWeek.MondayOf(date.Value).AddDays(deltaWeeks * 7);
        await ShowWeekAsync(chatId, userId, anchor, ct);
    }

    private async Task ShowCalendarAfterParse(
        long chatId,
        long userId,
        string? monthText,
        int deltaMonths,
        CancellationToken ct
    )
    {
        var month = CallbackPayload.TryParseMonth(monthText);
        if (month is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }
        await ShowCalendarAsync(chatId, userId, month.Value.AddMonths(deltaMonths), ct);
    }
```

- [ ] **Step 4: Добавить экраны навигации**

Добавить методы (заменить `SendScheduleAsync`, `SendWeekScheduleAsync`, их вызовы из `day:`/`week:` обработчиков):

```csharp
    private async Task<UserSettings?> GetSettingsAsync(long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        return await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
    }
```
Примечание: `UserSettings` — класс в `CollegeLMS.MaxBot.Models` (`using CollegeLMS.MaxBot.Models;` уже есть в файле).

```csharp
    private async Task ShowMainMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null)
        {
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        var roleLabel = settings.Role == "student" ? "Студент" : "Преподаватель";
        var entity = settings.GroupId.HasValue ? "группа выбрана" : "преподаватель выбран";
        if (settings.GroupId is null && settings.TeacherId is null)
            entity = "не выбран";

        var today = StudyWeek.Now(_tz);
        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new() { Type = "callback", Text = "📅 Сегодня", Payload = "today" },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = $"📆 Неделя", Payload = CallbackPayload.Week(today),
                },
                new()
                {
                    Type = "callback",
                    Text = $"🗓 Дата", Payload = CallbackPayload.Cal(today),
                },
            },
            new List<MaxButton>
            {
                new() { Type = "callback", Text = "⚙️ Настройки", Payload = "settings" },
            },
        };

        var text =
            $"🏠 *Главное меню*\n\n"
            + $"Роль: {roleLabel}\n"
            + $"Группа/Преподаватель: {entity}";

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private async Task ShowDayAsync(long chatId, long userId, DateTime date, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var entityName = settings.GroupId.HasValue ? "Группа" : "Преподаватель";

        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            var buttons = DayNavButtons(date, entityName);
            await _max.SendInlineKeyboardAsync(
                chatId,
                MessageFormatter.FormatDaySchedule([], date, entityName),
                buttons,
                ct: ct
            );
            return;
        }

        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            week: StudyWeek.ForDate(date),
            dayOfWeek: MessageFormatter.ToApiDay(date.DayOfWeek),
            ct: ct
        );

        var text = MessageFormatter.FormatDaySchedule(entries, date, entityName);
        var buttons = DayNavButtons(date, entityName);
        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private static List<List<MaxButton>> DayNavButtons(DateTime date, string entityName)
    {
        return
        [
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "← Пред. день",
                    Payload = CallbackPayload.DayPrev(date),
                },
                new()
                {
                    Type = "callback",
                    Text = "След. день →",
                    Payload = CallbackPayload.DayNext(date),
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "📆 Неделя",
                    Payload = CallbackPayload.Week(date),
                },
                new() { Type = "callback", Text = "🔙 Меню", Payload = "menu" },
            },
        ];
    }

    private async Task ShowWeekAsync(long chatId, long userId, DateTime weekStart, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var entityName = settings.GroupId.HasValue ? "Группа" : "Преподаватель";
        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            period: "week",
            week: StudyWeek.ForDate(weekStart),
            ct: ct
        );

        var text = MessageFormatter.FormatWeekSchedule(entries, weekStart, entityName);
        var buttons = WeekNavButtons(weekStart);

        if (text.Length > 4000)
        {
            var header = $"📅 *Неделя {MessageFormatter.FormatShortDate(weekStart)}–{MessageFormatter.FormatShortDate(weekStart.AddDays(6))}*";
            await _max.SendInlineKeyboardAsync(chatId, header, buttons, ct: ct);
            foreach (var group in entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key))
            {
                var date = MessageFormatter.DateForWeekDay(weekStart, group.Key);
                var dayText = MessageFormatter.FormatDaySchedule(group.ToList(), date, entityName);
                await _max.SendMessageAsync(chatId, dayText, ct: ct);
                await Task.Delay(500, ct);
            }
            return;
        }

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private static List<List<MaxButton>> WeekNavButtons(DateTime weekStart)
    {
        var firstRow = new List<MaxButton>();
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            firstRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = $"{MessageFormatter.DayAbbrForDate(date)} {MessageFormatter.FormatShortDate(date)}",
                    Payload = CallbackPayload.Day(date),
                }
            );
        }

        var navRow = new List<MaxButton>
        {
            new()
            {
                Type = "callback",
                Text = "← Неделя",
                Payload = CallbackPayload.WeekPrev(weekStart),
            },
            new()
            {
                Type = "callback",
                Text = "Неделя →",
                Payload = CallbackPayload.WeekNext(weekStart),
            },
        };

        var actionRow = new List<MaxButton>
        {
            new()
            {
                Type = "callback",
                Text = "🗓 Дата",
                Payload = CallbackPayload.Cal(weekStart),
            },
            new() { Type = "callback", Text = "📅 Сегодня", Payload = "today" },
            new() { Type = "callback", Text = "🔙 Меню", Payload = "menu" },
        };

        return [firstRow, navRow, actionRow];
    }

    private async Task ShowCalendarAsync(long chatId, long userId, DateTime month, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var maxMonth = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(16 * 7);
        var rows = CalendarFormatter.BuildGrid(month);

        var prevRow = new List<MaxButton>();
        if (CalendarFormatter.CanGoPrev(month))
            prevRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Пред. месяц",
                    Payload = CallbackPayload.CalPrev(month),
                }
            );
        if (CalendarFormatter.CanGoNext(month, maxMonth))
            prevRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "След. месяц →",
                    Payload = CallbackPayload.CalNext(month),
                }
            );

        var actionRow = new List<MaxButton>
        {
            new() { Type = "callback", Text = "📅 Сегодня", Payload = "today" },
            new() { Type = "callback", Text = "🔙 Меню", Payload = "menu" },
        };

        var buttons = new List<List<MaxButton>>();
        buttons.AddRange(rows);
        if (prevRow.Count > 0)
            buttons.Add(prevRow);
        buttons.Add(actionRow);

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"🗓 *{CalendarFormatter.MonthTitle(month)}*",
            buttons,
            ct: ct
        );
    }
```

- [ ] **Step 5: Обновить завершение выборов роли/группы/преподавателя и настройки**

- `HandleGroupSelectionAsync` / `HandleTeacherSelectionAsync`: заменить финальный `SendMessageAsync` со слэш-командами на `await ShowMainMenuAsync(chatId, userId, ct);`.
- `ShowSettingsAsync`: добавить кнопку
```csharp
buttons.Add(
    new List<MaxButton>
    {
        new() { Type = "callback", Text = "🔙 Меню", Payload = "menu" },
    }
);
```
в конец списка.

- [ ] **Step 6: Собрать проект и прогнать тесты**

Run: `dotnet build CollegeLMS.slnx -nologo`
Expected: Ошибок: 0. (Возможные предупреждения в `CollegeLMS.API` СУЩЕСТВУЮЩИЕ, не наши.)

Run: `dotnet test CollegeLMS.MaxBot.Tests -nologo`
Expected: все тесты PASS (существующие 27 + новые).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: maxbot menu navigation (day/week/calendar)"
```

---

### Task 6: Финальные проверки качества

**Files:**
- Никаких изменений кода.

- [ ] **Step 1: CSharpier**

Run: `dotnet csharpier format "CollegeLMS.MaxBot/Services/CallbackPayload.cs" "CollegeLMS.MaxBot/Services/CalendarFormatter.cs" "CollegeLMS.MaxBot/Services/MessageFormatter.cs" "CollegeLMS.MaxBot/Services/StudyWeek.cs" "CollegeLMS.MaxBot/Bot/MaxBotService.cs" "CollegeLMS.MaxBot.Tests/CallbackPayloadTests.cs" "CollegeLMS.MaxBot.Tests/CalendarFormatterTests.cs" "CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs" "CollegeLMS.MaxBot.Tests/StudyWeekTests.cs"`
Затем: `dotnet csharpier check "…те же файлы…"` → Expected: «no files necessary to be formatted» (без вывода ошибок).

- [ ] **Step 2: Полный прогон тестов бота**

Run: `dotnet test CollegeLMS.MaxBot.Tests -nologo`
Expected: все PASS.

- [ ] **Step 3: Обновить README бота**

`CollegeLMS.MaxBot/README.md`: заменить раздел про слэш-команды на описание кнопочного меню (и убрать упоминание `/schedule`, `/week`).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "chore: maxbot csharpier + readme"
```

---

## Self-Review заметки

- Payload-модель покрыта тестами (Task 1), форматтеры (Task 2–3), дата↔неделя (Task 4).
- `ScheduleNotifier` не затронут: старая сигнатура `FormatDaySchedule(entries, dayOfWeek, entityName)` сохранена перегрузкой.
- Удаление `/schedule`, `/week` из UI; `/help` обновлён.
- Воскресенье в `ShowDayAsync`: заголовок через `FormatDaySchedule([], date, entityName)` содержит дату, кнопки стрелок есть — соответствует спеку «воскресенье показывает выходной, навигация работает».
- Проверка границ календаря: младшая граница — первый месяц семестра, старшая — +16 недель (`maxMonth`).