# Plan: Парсер расписания из Excel

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Парсинг XLSX-файла расписания (матрица 49 групп × 5 дней × 7 пар) → валидация → превью → импорт в `schedule_entries`

**Architecture:** Новый `ScheduleImportService` парсит матричный формат Excel. Диспетчер загружает файл, видит превью с ошибками, подтверждает импорт. Группы и преподаватели сопоставляются с БД по имени.

**Tech Stack:** ClosedXML (чтение XLSX), ASP.NET Core, EF Core, PostgreSQL

## File Structure

| Файл | Действие | Ответственность |
|------|----------|-----------------|
| `Dtos/ScheduleImportDtos.cs` | Модификация | DTO для превью и подтверждения |
| `Services/ScheduleImportService.cs` | Полная переработка | Парсинг матричного формата |
| `Interfaces/IScheduleService.cs` | Модификация | Новые методы |
| `Services/ScheduleService.cs` | Модификация | Делегирование новым методам |
| `Controllers/ScheduleController.cs` | Модификация | Новые endpoints |

## Global Constraints

- .NET 10, C#, file-scoped namespaces
- `Result<T>` pattern — никаких try-catch в контроллерах
- PostgreSQL (Npgsql), EF Core, snake_case naming
- FluentValidation, Swashbuckle
- Все сообщения об ошибках на русском
- `ScheduleEntry` entity не изменяется
- `System.DayOfWeek` (не кастомный enum)
- `LessonType` по умолчанию = `Practice`
- Время пар: стандартные слоты (8:00-9:30, 9:40-11:10, 11:20-12:50, 13:30-15:00, 15:10-16:40, 16:50-18:20, 18:30-20:00)

---

### Task 1: DTOs для превью и подтверждения

**Files:**
- Modify: `CollegeLMS.API/Dtos/ScheduleImportDtos.cs`

**Interfaces:**
- Produces: `SchedulePreviewResponse`, `SchedulePreviewEntry`, `SchedulePreviewWarning`, `ConfirmImportRequest`, `ConfirmImportResult`

- [ ] **Step 1: Заменить содержимое ScheduleImportDtos.cs**

```csharp
namespace CollegeLMS.API.Dtos;

public class SchedulePreviewResponse
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int WarningsCount { get; set; }
    public List<SchedulePreviewWarning> Warnings { get; set; } = [];
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
}

public class SchedulePreviewEntry
{
    public string GroupName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public int Pair { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public List<int> Weeks { get; set; } = [];
    public string Status { get; set; } = "ok"; // ok | warning | error
    public string? StatusMessage { get; set; }
}

public class SchedulePreviewWarning
{
    public string Type { get; set; } = string.Empty; // group_not_found | teacher_not_found
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ConfirmImportRequest
{
    public List<SchedulePreviewEntry> Entries { get; set; } = [];
    public bool CreateMissingGroups { get; set; }
    public bool CreateMissingTeachers { get; set; }
}

public class ConfirmImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = [];
}

public class ImportError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Удалить старый ScheduleImportResult**

Удалить `ScheduleImportResult` (старый DTO) — он заменён на `ConfirmImportResult`.

- [ ] **Step 3: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS (новые DTO не используются пока нигде, старый `ScheduleImportResult` удалён — нужно обновить `ScheduleService` в следующем таске)

---

### Task 2: Интерфейс — новые методы

**Files:**
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs`

**Interfaces:**
- Consumes: DTOs из Task 1
- Produces: `PreviewScheduleAsync`, `ImportScheduleConfirmAsync`

- [ ] **Step 1: Добавить методы в IScheduleService**

```csharp
Task<Result<SchedulePreviewResponse>> PreviewScheduleAsync(
    Stream fileStream, CancellationToken ct);

Task<Result<ConfirmImportResult>> ImportScheduleConfirmAsync(
    ConfirmImportRequest request, CancellationToken ct);
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS (методы добавлены в интерфейс, реализация будет в Task 4)

---

### Task 3: Парсер матричного формата Excel

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs`

**Interfaces:**
- Consumes: поток XLSX
- Produces: `List<SchedulePreviewEntry>` (сырые данные из Excel)

**Ключевые константы:**
- Строка 5: группы (колонки C–AY)
- Строки 6–40: Понедельник (7 пар × ~5 строк)
- Строки 41–75: Вторник
- Строки 76–110: Среда
- Строки 111–145: Четверг
- Строки 146–179: Пятница
- Словарь дней: `{"ПОНЕДЕЛЬНИК": Monday, "ВТОРНИК": Tuesday, ...}`

- [ ] **Step 1: Написать метод ParseScheduleMatrix**

```csharp
using ClosedXML.Excel;

namespace CollegeLMS.API.Services;

public class ScheduleImportService(AppDbContext db)
{
    private static readonly Dictionary<string, DayOfWeek> DayMap = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["понедельник"] = DayOfWeek.Monday,
        ["вторник"] = DayOfWeek.Tuesday,
        ["среда"] = DayOfWeek.Wednesday,
        ["четверг"] = DayOfWeek.Thursday,
        ["пятница"] = DayOfWeek.Friday,
        ["суббота"] = DayOfWeek.Saturday,
    };

    private static readonly TimeSpan[] DefaultSlots =
    [
        new(8, 0, 0), new(9, 30, 0),   // 1 пара
        new(9, 40, 0), new(11, 10, 0),  // 2 пара
        new(11, 20, 0), new(12, 50, 0), // 3 пара
        new(13, 30, 0), new(15, 0, 0),  // 4 пара
        new(15, 10, 0), new(16, 40, 0), // 5 пара
        new(16, 50, 0), new(18, 20, 0), // 6 пара
        new(18, 30, 0), new(20, 0, 0),  // 7 пара
    ];

    public List<SchedulePreviewEntry> ParseScheduleMatrix(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheet(1);
        var entries = new List<SchedulePreviewEntry>();

        // Строка 5: маппинг колонок → группы
        var groupColumns = new Dictionary<int, string>();
        for (int col = 3; col <= ws.ColumnUsed().ColumnNumber(); col++)
        {
            var name = ws.Cell(5, col).GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                groupColumns[col] = name;
        }

        // Блоки дней: (начальная_строка_потока, день_недели)
        var dayBlocks = new List<(int StartRow, DayOfWeek Day)>();
        for (int row = 1; row <= ws.LastRowUsed().RowNumber(); row++)
        {
            var aVal = ws.Cell(row, 1).GetString().Trim().ToUpperInvariant();
            if (DayMap.TryGetValue(aVal, out var day))
                dayBlocks.Add((row, day));
        }

        foreach (var (startRow, day) in dayBlocks)
        {
            for (int pair = 1; pair <= 7; pair++)
            {
                int pairRow = startRow + (pair - 1) * 5;

                foreach (var (col, groupName) in groupColumns)
                {
                    var cellText = ws.Cell(pairRow, col).GetString().Trim();
                    if (string.IsNullOrEmpty(cellText))
                        continue;

                    // Парсим ячейку: "232     История" или "ч.з      Рус.язык"
                    var (room, subject) = ParseRoomSubject(cellText);
                    if (string.IsNullOrEmpty(subject))
                        continue;

                    // Недели: строка на 1 ниже
                    var weeksText = ws.Cell(pairRow + 1, col).GetString().Trim();
                    var weeks = ParseWeeks(weeksText);

                    // Преподаватель: строка на 2 ниже
                    var teacherName = ws.Cell(pairRow + 2, col).GetString().Trim();

                    entries.Add(new SchedulePreviewEntry
                    {
                        GroupName = groupName,
                        Day = day.ToString(),
                        Pair = pair,
                        Subject = subject,
                        Room = room,
                        TeacherName = teacherName,
                        Weeks = weeks,
                        Status = "ok",
                    });
                }
            }
        }

        return entries;
    }

    private static (string Room, string Subject) ParseRoomSubject(string cell)
    {
        // Форматы: "232     История", "ч.з      Рус.язык", "307л        ИТ"
        var text = cell.Trim();

        // Если начинается с "ч.з" — читальный зал
        if (text.StartsWith("ч.з", StringComparison.OrdinalIgnoreCase))
        {
            var subject = text[3..].Trim();
            return ("ч.з", subject);
        }

        // Ищем разделитель: пробелы после номера/буквы
        // "307л" → Room="307л", остальное — Subject
        // "232     История" → Room="232", Subject="История"
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"^([\d]+[л]?)\s{2,}(.+)$");

        if (match.Success)
            return (match.Groups[1].Value, match.Groups[2].Value.Trim());

        // Fallback: пробел как разделитель
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
            return (parts[0], parts[1]);

        return (string.Empty, text);
    }

    private static List<int> ParseWeeks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        // Убираем скобки: "(1-17)" → "1-17"
        var clean = text.Trim('(', ')', ' ');
        var weeks = new List<int>();

        foreach (var part in clean.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (int.TryParse(range[0], out var from) && int.TryParse(range[1], out var to))
                {
                    for (int i = from; i <= to; i++)
                        weeks.Add(i);
                }
            }
            else if (int.TryParse(part, out var w))
            {
                weeks.Add(w);
            }
        }

        return weeks.Distinct().OrderBy(x => x).ToList();
    }
}
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 3: Закоммитить**

```bash
git add CollegeLMS.API/Services/ScheduleImportService.cs
git commit -m "feat: add matrix schedule parser"
```

---

### Task 4: Preview — валидация + маппинг с БД

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs`

**Interfaces:**
- Consumes: `List<SchedulePreviewEntry>` из Task 3
- Produces: `SchedulePreviewResponse`

- [ ] **Step 1: Добавить метод PreviewAsync**

```csharp
public async Task<Result<SchedulePreviewResponse>> PreviewAsync(
    Stream fileStream, CancellationToken ct)
{
    XLWorkbook workbook;
    try
    {
        workbook = new XLWorkbook(fileStream);
    }
    catch
    {
        return Result<SchedulePreviewResponse>.Fail(
            "Не удалось прочитать файл. Убедитесь, что это XLSX-файл.", 400);
    }

    using (workbook)
    {
        var entries = ParseScheduleMatrix(workbook);

        // Загружаем группы и преподавателей из БД
        var groupNames = entries.Select(e => e.GroupName).Distinct().ToList();
        var teacherNames = entries
            .Where(e => !string.IsNullOrEmpty(e.TeacherName))
            .Select(e => e.TeacherName).Distinct().ToList();

        var groupsInDb = await db.Groups
            .Where(g => groupNames.Contains(g.Name))
            .Select(g => g.Name)
            .ToListAsync(ct);

        var teachersInDb = await db.Teachers
            .Include(t => t.User)
            .Where(t => teacherNames.Contains(t.User.FullName))
            .Select(t => t.User.FullName)
            .ToListAsync(ct);

        var warnings = new List<SchedulePreviewWarning>();

        // Валидация групп
        var missingGroups = groupNames.Except(groupsInDb).ToList();
        if (missingGroups.Count > 0)
        {
            var count = entries.Count(e => missingGroups.Contains(e.GroupName));
            warnings.Add(new SchedulePreviewWarning
            {
                Type = "group_not_found",
                Value = string.Join(", ", missingGroups),
                Count = count,
            });
        }

        // Валидация преподавателей
        var missingTeachers = teacherNames.Except(teachersInDb).ToList();
        if (missingTeachers.Count > 0)
        {
            var count = entries.Count(e =>
                !string.IsNullOrEmpty(e.TeacherName) &&
                missingTeachers.Contains(e.TeacherName));
            warnings.Add(new SchedulePreviewWarning
            {
                Type = "teacher_not_found",
                Value = string.Join(", ", missingTeachers),
                Count = count,
            });
        }

        // Помечаем статусы
        foreach (var entry in entries)
        {
            if (missingGroups.Contains(entry.GroupName))
            {
                entry.Status = "warning";
                entry.StatusMessage = $"Группа '{entry.GroupName}' не найдена в БД";
            }
            else if (!string.IsNullOrEmpty(entry.TeacherName) &&
                     missingTeachers.Contains(entry.TeacherName))
            {
                entry.Status = "warning";
                entry.StatusMessage = $"Преподаватель '{entry.TeacherName}' не найден в БД";
            }
        }

        var validCount = entries.Count(e => e.Status == "ok");

        return Result<SchedulePreviewResponse>.Ok(new SchedulePreviewResponse
        {
            TotalEntries = entries.Count,
            ValidEntries = validCount,
            WarningsCount = warnings.Sum(w => w.Count),
            Warnings = warnings,
            Entries = entries,
        });
    }
}
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 3: Закоммитить**

```bash
git add CollegeLMS.API/Services/ScheduleImportService.cs
git commit -m "feat: add schedule preview with DB validation"
```

---

### Task 5: Confirm — импорт в БД

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs`

**Interfaces:**
- Consumes: `ConfirmImportRequest` (список записей + флаги создания)
- Produces: `ConfirmImportResult`

- [ ] **Step 1: Добавить метод ConfirmAsync**

```csharp
public async Task<Result<ConfirmImportResult>> ConfirmAsync(
    ConfirmImportRequest request, CancellationToken ct)
{
    var result = new ConfirmImportResult();

    // Загружаем существующие группы
    var existingGroups = await db.Groups
        .ToDictionaryAsync(g => g.Name, g => g.Id, ct);

    // Загружаем существующих преподавателей
    var existingTeachers = await db.Teachers
        .Include(t => t.User)
        .ToDictionaryAsync(t => t.User.FullName, t => t.Id, ct);

    var entriesToAdd = new List<ScheduleEntry>();

    foreach (var entry in request.Entries)
    {
        // Маппинг группы
        if (!existingGroups.TryGetValue(entry.GroupName, out var groupId))
        {
            if (request.CreateMissingGroups)
            {
                var newGroup = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = entry.GroupName,
                    Course = int.TryParse(
                        new string(entry.GroupName.Where(char.IsDigit).ToArray()),
                        out var c) ? c : 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                db.Groups.Add(newGroup);
                groupId = newGroup.Id;
                existingGroups[entry.GroupName] = groupId;
            }
            else
            {
                result.Skipped++;
                result.Errors.Add(new ImportError
                {
                    Row = result.Skipped,
                    Message = $"Группа '{entry.GroupName}' не найдена",
                });
                continue;
            }
        }

        // Маппинг преподавателя
        Guid? teacherId = null;
        if (!string.IsNullOrEmpty(entry.TeacherName))
        {
            if (!existingTeachers.TryGetValue(entry.TeacherName, out var tid))
            {
                if (request.CreateMissingTeachers)
                {
                    // Создаём User + Teacher
                    var nameParts = entry.TeacherName.Split(' ',
                        StringSplitOptions.RemoveEmptyEntries);
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = nameParts.Length > 1 ? nameParts[1] : "",
                        LastName = nameParts.Length > 0 ? nameParts[0] : "",
                        Patronymic = nameParts.Length > 2 ? nameParts[2] : "",
                        FullName = entry.TeacherName,
                        Role = UserRole.Teacher,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.Users.Add(user);

                    var teacher = new Teacher
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.Teachers.Add(teacher);
                    tid = teacher.Id;
                    existingTeachers[entry.TeacherName] = tid;
                }
                else
                {
                    result.Skipped++;
                    result.Errors.Add(new ImportError
                    {
                        Row = result.Skipped,
                        Message = $"Преподаватель '{entry.TeacherName}' не найден",
                    });
                    continue;
                }
            }
            teacherId = tid;
        }

        // Маппинг дня недели
        if (!Enum.TryParse<DayOfWeek>(entry.Day, true, out var dayOfWeek))
        {
            result.Skipped++;
            result.Errors.Add(new ImportError
            {
                Row = result.Skipped,
                Message = $"Некорректный день: '{entry.Day}'",
            });
            continue;
        }

        // Время по умолчанию
        var slotIndex = Math.Clamp(entry.Pair - 1, 0, 6);
        var startTime = DefaultSlots[slotIndex * 2];
        var endTime = DefaultSlots[slotIndex * 2 + 1];

        entriesToAdd.Add(new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = teacherId,
            Subject = entry.Subject,
            Room = entry.Room,
            DayOfWeek = dayOfWeek,
            NumberPair = entry.Pair,
            StartTime = startTime,
            EndTime = endTime,
            Weeks = entry.Weeks.Count > 0 ? entry.Weeks : [1],
            LessonType = LessonType.Practice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
    }

    if (entriesToAdd.Count > 0)
    {
        db.ScheduleEntries.AddRange(entriesToAdd);
        await db.SaveChangesAsync(ct);
    }

    result.Imported = entriesToAdd.Count;

    return Result<ConfirmImportResult>.Ok(result);
}
```

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 3: Закоммитить**

```bash
git add CollegeLMS.API/Services/ScheduleImportService.cs
git commit -m "feat: add schedule import confirm with DB write"
```

---

### Task 6: ScheduleService — делегирование

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleService.cs`

**Interfaces:**
- Consumes: `ScheduleImportService.PreviewAsync`, `ScheduleImportService.ConfirmAsync`
- Produces: `PreviewScheduleAsync`, `ImportScheduleConfirmAsync`

- [ ] **Step 1: Обновить метод ImportScheduleAsync и добавить новые**

Заменить существующий `ImportScheduleAsync` и добавить два новых метода:

```csharp
public async Task<Result<SchedulePreviewResponse>> PreviewScheduleAsync(
    Stream fileStream, CancellationToken ct)
{
    return await importService.PreviewAsync(fileStream, ct);
}

public async Task<Result<ConfirmImportResult>> ImportScheduleConfirmAsync(
    ConfirmImportRequest request, CancellationToken ct)
{
    return await importService.ConfirmAsync(request, ct);
}
```

Удалить старый `ImportScheduleAsync` из `ScheduleService` (он больше не нужен — заменён на Preview + Confirm).

- [ ] **Step 2: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 3: Закоммитить**

```bash
git add CollegeLMS.API/Services/ScheduleService.cs
git commit -m "feat: wire up schedule import preview and confirm"
```

---

### Task 7: Контроллер — новые endpoints

**Files:**
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs`

**Interfaces:**
- Consumes: `IScheduleService.PreviewScheduleAsync`, `IScheduleService.ImportScheduleConfirmAsync`

- [ ] **Step 1: Заменить старый POST /import на два новых endpoint**

```csharp
[HttpPost("import/preview")]
[Authorize(Roles = "Dispatcher,Admin")]
[SwaggerOperation(Summary = "Превью импорта расписания из XLSX")]
[ProducesResponseType(typeof(Result<SchedulePreviewResponse>), StatusCodes.Status200OK)]
public async Task<IActionResult> PreviewImport(
    IFormFile file,
    CancellationToken ct)
{
    if (file == null || file.Length == 0)
        return BadRequest(Result<SchedulePreviewResponse>.Fail("Файл не выбран", 400));

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext != ".xlsx")
        return BadRequest(Result<SchedulePreviewResponse>.Fail(
            "Поддерживается только формат XLSX", 400));

    if (file.Length > 10 * 1024 * 1024)
        return BadRequest(Result<SchedulePreviewResponse>.Fail(
            "Файл слишком большой. Максимум 10MB.", 400));

    using var stream = new MemoryStream();
    await file.CopyToAsync(stream, ct);
    stream.Seek(0, SeekOrigin.Begin);

    var result = await scheduleService.PreviewScheduleAsync(stream, ct);
    return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
}

[HttpPost("import/confirm")]
[Authorize(Roles = "Dispatcher,Admin")]
[SwaggerOperation(Summary = "Подтвердить импорт расписания")]
[ProducesResponseType(typeof(Result<ConfirmImportResult>), StatusCodes.Status200OK)]
public async Task<IActionResult> ConfirmImport(
    ConfirmImportRequest request,
    CancellationToken ct)
{
    var result = await scheduleService.ImportScheduleConfirmAsync(request, ct);
    return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
}
```

Удалить старый endpoint `POST /import`.

- [ ] **Step 2: Убрать старый using если нужен**

Убедиться что `CollegeLMS.API.Dtos` импортируется (для новых DTO).

- [ ] **Step 3: Проверить сборку**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 4: Закоммитить**

```bash
git add CollegeLMS.API/Controllers/ScheduleController.cs
git commit -m "feat: add schedule import preview and confirm endpoints"
```

---

### Task 8: Финальная проверка и чистка

**Files:**
- Modify: `CollegeLMS.API/Services/ScheduleImportService.cs` (проверить что старый метод `ImportAsync` удалён)
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs` (проверить что старый `ImportScheduleAsync` удалён)

- [ ] **Step 1: Удалить старые методы**

Убедиться что:
- `ScheduleImportService.ImportAsync` (старый метод) удалён
- `IScheduleService.ImportScheduleAsync` удалён
- `ScheduleService.ImportScheduleAsync` удалён
- Старый DTO `ScheduleImportResult` удалён из `ScheduleImportDtos.cs`

- [ ] **Step 2: Финальная сборка**

Run: `dotnet build CollegeLMS.API`
Expected: PASS

- [ ] **Step 3: Запустить тесты**

Run: `dotnet test CollegeLMS.Tests`
Expected: PASS (или исправить упавшие)

- [ ] **Step 4: Форматирование**

Run: `dotnet csharpier format .`
Expected: Форматирование применено

- [ ] **Step 5: Финальный коммит**

```bash
git add -A
git commit -m "feat: schedule import from Excel - complete parser with preview and confirm"
```
