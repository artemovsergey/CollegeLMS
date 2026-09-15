# Нативный диспетчер в Max-боте — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Диспетчер создаёт корректировки расписания полностью из чата бота через пошаговый визард, без редиректа в mini-app.

**Architecture:** Чистая машина состояний `DispatcherCorrectionWizard` (без зависимостей) + метод клиента `ConfirmCorrectionAsync` (POST /api/schedule/correction/confirm с Bearer + Idempotency-Key) + клей в `MaxBotService` (словарь состояний per user, callback-роутинг, текстовый ввод).

**Tech Stack:** .NET 10, xUnit + FluentAssertions, существующий `CollegeLmsApiClient`/`MaxApiClient`.

## Global Constraints

- Все сообщения и комментарии в коде на русском
- Результат подтверждения: `POST /api/schedule/correction/confirm`, Bearer-токен диспетчера, заголовок `Idempotency-Key: Guid.NewGuid()`
- Кнопки-колбэки бота: payload-формат `wiz:<action>:<param>`
- CSharpier обязателен; тесты: `dotnet test CollegeLMS.MaxBot.Tests`
- Изменение типов: Add / Remove / Replace / Move (строки, как в API)

---

### Task 1: Машина состояний и сборка entry

**Files:**
- Create: `CollegeLMS.MaxBot/Services/DispatcherCorrectionWizard.cs`
- Test: `CollegeLMS.MaxBot.Tests/DispatcherCorrectionWizardTests.cs`

**Interfaces:**
- Produces: `DispatcherWizardState` (Step, ChangeType, Day, Week, GroupId, GroupName, RemovedNumberPair, RemovedSubject, RemovedTeacherId, RemovedTeacherName, NewPair, Subject, TeacherId, TeacherName, Note), `enum DispatcherWizardStep { None, Type, Day, Week, Group, RemovedPair, NewPair, Subject, Teacher, Note }`, `DispatcherCorrectionWizard.BuildEntry(state)` → `CorrectionEntryDto`, `DispatcherCorrectionWizard.BuildWeekGrid(week, totalWeeks)` → клавиатура.

- [ ] **Step 1: Пишем failing-тесты BuildEntry для всех 4 типов**

```csharp
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class DispatcherCorrectionWizardTests
{
    private static DispatcherWizardState State(string changeType) =>
        new()
        {
            Step = DispatcherWizardStep.Note,
            ChangeType = changeType,
            Day = 2, // Вторник
            Week = 3,
            GroupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            GroupName = "ИС-21",
            RemovedNumberPair = 2,
            RemovedSubject = "История",
            RemovedTeacherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            RemovedTeacherName = "Петров П.П.",
            NewPair = 4,
            Subject = "Информатика",
            TeacherId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            TeacherName = "Иванов И.И.",
        };

    [Fact]
    public void BuildEntry_Add_FillsNewLessonFields()
    {
        var state = State("Add");
        state.RemovedNumberPair = null;
        state.RemovedSubject = null;
        state.RemovedTeacherId = null;
        state.RemovedTeacherName = null;

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.ChangeType.Should().Be("Add");
        entry.DayOfWeek.Should().Be(2);
        entry.Week.Should().Be(3);
        entry.GroupId.Should().Be(state.GroupId);
        entry.GroupName.Should().Be("ИС-21");
        entry.NumberPair.Should().Be(4);
        entry.Subject.Should().Be("Информатика");
        entry.TeacherId.Should().Be(state.TeacherId);
        entry.TeacherName.Should().Be("Иванов И.И.");
        entry.RemovedNumberPair.Should().BeNull();
        entry.RemovedSubject.Should().BeNull();
        entry.Row.Should().Be(1);
    }

    [Fact]
    public void BuildEntry_Remove_HasNoNewSubject()
    {
        var state = State("Remove");
        state.Subject = null;
        state.TeacherId = null;
        state.TeacherName = null;
        state.NewPair = null;

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.ChangeType.Should().Be("Remove");
        entry.NumberPair.Should().Be(2);
        entry.Subject.Should().BeNull();
        entry.TeacherId.Should().BeNull();
        entry.RemovedSubject.Should().Be("История");
        entry.RemovedNumberPair.Should().Be(2);
        entry.RemovedTeacherName.Should().Be("Петров П.П.");
    }

    [Fact]
    public void BuildEntry_Replace_NewSubjectAtRemovedSlot()
    {
        var entry = DispatcherCorrectionWizard.BuildEntry(State("Replace"));

        entry.ChangeType.Should().Be("Replace");
        entry.NumberPair.Should().Be(2); // слот снимаемой пары
        entry.Subject.Should().Be("Информатика");
        entry.RemovedSubject.Should().Be("История");
        entry.RemovedNumberPair.Should().Be(2);
    }

    [Fact]
    public void BuildEntry_Move_NewSlotAndRemovedSlot()
    {
        var entry = DispatcherCorrectionWizard.BuildEntry(State("Move"));

        entry.ChangeType.Should().Be("Move");
        entry.NumberPair.Should().Be(4); // новая пара
        entry.RemovedNumberPair.Should().Be(2);
        entry.RemovedSubject.Should().Be("История");
    }

    [Fact]
    public void BuildEntry_NoteDash_BecomesNull()
    {
        var state = State("Add");
        state.Note = "—";

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.Note.Should().BeNull();
    }

    [Fact]
    public void BuildWeekGrid_RowsOfFour()
    {
        var buttons = DispatcherCorrectionWizard.BuildWeekGrid(1, 16);

        buttons.Should().HaveCount(4);
        buttons[0].Should().HaveCount(4);
        buttons.Sum(r => r.Count).Should().Be(16);
        buttons[0][0].Payload.Should().Be("wiz:week:1");
        buttons[3][3].Payload.Should().Be("wiz:week:16");
    }
}
```

- [ ] **Step 2: Запуск — тесты падают (типы не существуют)**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter DispatcherCorrectionWizardTests`
Expected: FAIL (CS0246: DispatcherWizardState not found)

- [ ] **Step 3: Реализация**

```csharp
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Шаги визарда корректировки.</summary>
public enum DispatcherWizardStep
{
    None,
    Type,
    Day,
    Week,
    Group,
    RemovedPair,
    NewPair,
    Subject,
    Teacher,
    Note,
}

/// <summary>Состояние визарда одного диспетчера.</summary>
public class DispatcherWizardState
{
    public DispatcherWizardStep Step { get; set; } = DispatcherWizardStep.None;
    public string? ChangeType { get; set; }
    public int Day { get; set; }
    public int Week { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public int? RemovedNumberPair { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? NewPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Note { get; set; }
}

public static class DispatcherCorrectionWizard
{
    private static readonly string[] DayNames =
    [
        "",
        "Пн",
        "Вт",
        "Ср",
        "Чт",
        "Пт",
        "Сб",
    ];

    /// <summary>Собирает запись корректировки из состояния визарда.</summary>
    public static CorrectionEntryDto BuildEntry(DispatcherWizardState state)
    {
        var isRemove = state.ChangeType == "Remove";
        var hasRemoved = isRemove
            || state.ChangeType == "Replace"
            || state.ChangeType == "Move";

        var numberPair = state.ChangeType switch
        {
            "Add" => state.NewPair ?? 0,
            "Move" => state.NewPair ?? 0,
            _ => state.RemovedNumberPair ?? 0,
        };

        return new CorrectionEntryDto
        {
            Row = 1,
            GroupId = state.GroupId,
            GroupName = state.GroupName,
            ChangeType = state.ChangeType ?? "Add",
            DayOfWeek = state.Day,
            Week = state.Week,
            NumberPair = numberPair,
            Subject = isRemove ? null : state.Subject,
            TeacherId = isRemove ? null : state.TeacherId,
            TeacherName = isRemove ? null : state.TeacherName,
            RemovedNumberPair = hasRemoved ? state.RemovedNumberPair : null,
            RemovedSubject = hasRemoved ? state.RemovedSubject : null,
            RemovedTeacherId = hasRemoved ? state.RemovedTeacherId : null,
            RemovedTeacherName = hasRemoved ? state.RemovedTeacherName : null,
            Note = string.IsNullOrWhiteSpace(state.Note) || state.Note == "—" ? null : state.Note,
        };
    }

    /// <summary>Сетка недель по 4 кнопки в строке.</summary>
    public static List<List<MaxButton>> BuildWeekGrid(int currentWeek, int totalWeeks)
    {
        var buttons = Enumerable
            .Range(1, totalWeeks)
            .Select(w => new MaxButton
            {
                Type = "callback",
                Text = w == currentWeek ? $"• {w}" : w.ToString(),
                Payload = $"wiz:week:{w}",
            })
            .ToList();

        var rows = new List<List<MaxButton>>();
        for (var i = 0; i < buttons.Count; i += 4)
            rows.Add(buttons.Skip(i).Take(4).ToList());
        return rows;
    }

    /// <summary>Клавиатура выбора дня недели (Пн–Сб).</summary>
    public static List<List<MaxButton>> BuildDayKeyboard() =>
        [DayNames.Skip(1).Select((d, i) => new MaxButton
        {
            Type = "callback",
            Text = d,
            Payload = $"wiz:day:{i + 1}",
        }).ToList()];

    /// <summary>Клавиатура типов изменения.</summary>
    public static List<List<MaxButton>> BuildTypeKeyboard() =>
        [
            [new MaxButton { Type = "callback", Text = "🟢 Добавить", Payload = "wiz:type:Add" }],
            [new MaxButton { Type = "callback", Text = "🔴 Снять", Payload = "wiz:type:Remove" }],
            [new MaxButton { Type = "callback", Text = "🔵 Замена", Payload = "wiz:type:Replace" }],
            [new MaxButton { Type = "callback", Text = "🔄 Перенос", Payload = "wiz:type:Move" }],
        ];

    /// <summary>Кнопка отмены визарда.</summary>
    public static List<MaxButton> CancelRow() =>
        [new MaxButton { Type = "callback", Text = "❌ Отмена", Payload = "wiz:cancel" }];

    /// <summary>Название типа по-русски.</summary>
    public static string TypeLabel(string changeType) =>
        changeType switch
        {
            "Add" => "добавление",
            "Remove" => "снятие",
            "Replace" => "замена",
            "Move" => "перенос",
            _ => changeType,
        };
}
```

Примечание: `CorrectionEntryDto` появится в Task 2 — тесты Task 1 компилируются после Task 2 (порядок коммитов: Task 1+2 одним коммитом либо DTO-заглушка). Проще: выполнить Task 2 до запуска тестов Task 1.

- [ ] **Step 4: Тесты зелёные**

Run: `dotnet test CollegeLMS.MaxBot.Tests --filter DispatcherCorrectionWizardTests`
Expected: PASS (7 тестов)

- [ ] **Step 5: Commit** `feat: визард корректировок — машина состояний и сборка entry`

### Task 2: Клиент API — ConfirmCorrectionAsync

**Files:**
- Modify: `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs` — DTO entry/ответа
- Modify: `CollegeLMS.MaxBot/Clients/CollegeLmsApiClient.cs` — метод + GetScheduleMetaAsync
- Test: `CollegeLMS.MaxBot.Tests/MaxBotDispatcherFlowTests.cs` — тест отправки

**Interfaces:**
- Produces: `CorrectionEntryDto` (Row, GroupId, GroupName, ChangeType, DayOfWeek, Week, NumberPair, Subject?, TeacherId?, TeacherName?, RemovedSubject?, RemovedTeacherId?, RemovedTeacherName?, RemovedNumberPair?, Note?), `CorrectionConfirmResponse` (Applied), `ScheduleMetaDto` (TotalWeeks, CurrentWeek), `Task<CorrectionConfirmResponse?> ConfirmCorrectionAsync(CorrectionEntryDto entry, string token, CancellationToken ct)`, `Task<ScheduleMetaDto?> GetScheduleMetaAsync(CancellationToken ct)`.

- [ ] **Step 1: Failing-тест** — POST с Bearer + Idempotency-Key, парсинг applied:

```csharp
[Fact]
public async Task ConfirmCorrectionAsync_SendsBearerAndIdempotencyKey()
{
    string? auth = null;
    string? idem = null;
    var client = BuildClient(new StubHandler(request =>
    {
        auth = request.Headers.Authorization?.ToString();
        idem = request.Headers.Contains("Idempotency-Key")
            ? string.Join(",", request.Headers.GetValues("Idempotency-Key"))
            : "";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"isSuccess":true,"data":{"applied":1,"history":[]}}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
    }));

    var entry = new CorrectionEntryDto
    {
        Row = 1,
        GroupId = Guid.NewGuid(),
        GroupName = "ИС-21",
        ChangeType = "Add",
        DayOfWeek = 1,
        Week = 1,
        NumberPair = 1,
    };
    var result = await client.ConfirmCorrectionAsync(entry, "tok-1", CancellationToken.None);

    result.Should().NotBeNull();
    result!.Applied.Should().Be(1);
    auth.Should().Be(new AuthenticationHeaderValue("Bearer", "tok-1").ToString());
    Guid.TryParse(idem, out _).Should().BeTrue();
}

[Fact]
public async Task GetScheduleMetaAsync_ParsesTotalWeeks()
{
    var client = BuildClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            """{"isSuccess":true,"data":{"semesterStart":"2026-09-01","totalWeeks":16,"currentWeek":2,"currentDate":"2026-09-15"}}""",
            Encoding.UTF8,
            "application/json"
        ),
    }));

    var meta = await client.GetScheduleMetaAsync(CancellationToken.None);

    meta.Should().NotBeNull();
    meta!.TotalWeeks.Should().Be(16);
    meta.CurrentWeek.Should().Be(2);
}
```

- [ ] **Step 2: Запуск — FAIL**

- [ ] **Step 3: DTO в `CollegeLmsApiDtos.cs`:**

```csharp
public class CorrectionEntryDto
{
    public int Row { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public string ChangeType { get; set; } = "Add";
    public int DayOfWeek { get; set; }
    public int Week { get; set; }
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
}

public class CorrectionConfirmResponse
{
    public int Applied { get; set; }
}

public class ScheduleMetaDto
{
    public string? SemesterStart { get; set; }
    public int TotalWeeks { get; set; }
    public int CurrentWeek { get; set; }
}
```

- [ ] **Step 4: Методы клиента:**

```csharp
public async Task<ScheduleMetaDto?> GetScheduleMetaAsync(CancellationToken ct = default)
{
    try
    {
        var resp = await _http.GetAsync("/api/schedule/meta", ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        var wrapper = JsonSerializer.Deserialize<ResultWrapper<ScheduleMetaDto>>(json, JsonOpts);
        return wrapper?.Data;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch schedule meta");
        return null;
    }
}

public async Task<CorrectionConfirmResponse?> ConfirmCorrectionAsync(
    CorrectionEntryDto entry,
    string token,
    CancellationToken ct
)
{
    try
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/schedule/correction/confirm"
        )
        {
            Content = JsonContent.Create(new { entries = new[] { entry } }, JsonOpts),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var resp = await _http.SendAsync(request, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Correction confirm returned {Code}: {Body}",
                resp.StatusCode,
                json
            );
            return null;
        }
        var wrapper = JsonSerializer.Deserialize<ResultWrapper<CorrectionConfirmResponse>>(
            json,
            JsonOpts
        );
        return wrapper?.Data;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to confirm correction");
        return null;
    }
}
```

- [ ] **Step 5: Тесты зелёные, commit** `feat: клиент API — применение корректировки из бота`

### Task 3: Клей в MaxBotService — флоу визарда

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs`
- Modify: `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs` (если нужны поля GroupResponse/TeacherResponse для payload — уже есть Id+Name)

**Interfaces:**
- Consumes: `DispatcherCorrectionWizard`, `DispatcherWizardState`, `CollegeLmsApiClient.ConfirmCorrectionAsync/GetScheduleMetaAsync/GetScheduleAsync/GetTeachersAsync`, `_dispatcherTokens[userId]`.
- Produces: callback-акции `wiz:type|day|week|group|rem|pair|teacher|apply|cancel`; текстовый ввод в шагах Subject/Note; меню диспетчера с «➕ Новая корректировка» (payload `dispatcher:wizard`) вместо open_app.

- [ ] **Step 1: Меню диспетчера** — заменить open_app-кнопку на callback:

```csharp
var buttons = new List<List<MaxButton>>
{
    new()
    {
        new()
        {
            Type = "callback",
            Text = "➕ Новая корректировка",
            Payload = "dispatcher:wizard",
        },
    },
};
```
(open_app «Создать корректировку» удалить; XLSX и «Меню» остаются.)

- [ ] **Step 2: Состояние и хелперы** — поле `_wizardStates = new Dictionary<long, DispatcherWizardState>();` + методы: `StartWizardAsync` (Type-шаг), `ShowWizardRemovedPairAsync` (пары дня из `_api.GetScheduleAsync(groupId, week, dayOfWeek)`), `ShowWizardTeacherKeyboardAsync`, `ShowWizardConfirmAsync` (предпросмотр-текст + «✅ Применить»/«❌ Отмена»), `ApplyWizardAsync` (`ConfirmCorrectionAsync`; guard `_dispatcherTokens`), `CancelWizard`, `BuildWizardPreview(state)` — текст предпросмотра.

- [ ] **Step 3: Callback-кейсы** `wiz:*` в `HandleCallbackAsync` (type/day/week/group/rem/pair/teacher/apply/cancel; week: `GetScheduleMetaAsync` → totalWeeks, fallback 16).

- [ ] **Step 4: Текстовый ввод** — в `HandleMessageAsync` перед прочими проверками:

```csharp
if (_wizardStates.TryGetValue(userId, out var ws)
    && ws.Step is DispatcherWizardStep.Subject or DispatcherWizardStep.Note)
{
    await HandleWizardTextInputAsync(chatId, userId, text.Trim(), ct);
    return;
}
```
Subject: непустой текст → `ws.Subject`, шаг Teacher. Note: «—» или текст → `ws.Note`, шаг Confirm.

- [ ] **Step 5: Сборка, тесты бота, csharpier** — Run: `dotnet build CollegeLMS.slnx && dotnet test CollegeLMS.MaxBot.Tests && dotnet csharpier check CollegeLMS.MaxBot` — PASS.

- [ ] **Step 6: Commit** `feat: нативный визард корректировок диспетчера в боте`

## Self-Review

- Спека покрыта: меню (Task 3.1), шаги 1–9 (Task 3.2–3.4), сборка entry (Task 1), применение (Task 2), тесты (Tasks 1–2).
- Типы согласованы: `CorrectionEntryDto` создаётся в Task 2, потребляется Task 1 тестами и Task 3.
- Guard'ы: нет токена → предложение `/dispatcher`; нет пар на день → подсказка Add/отмена; note «—» → null.
