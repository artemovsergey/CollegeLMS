# ScheduleService — Export, Import, CRUD Frontend

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Доделать ScheduleService: экспорт PDF/Excel, импорт XLSX, CRUD-интерфейс для диспетчера, календарный вид

**Architecture:** Backend: новые endpoint'ы `/api/schedule/export` и `/api/schedule/import` в существующем ScheduleController. ExportService генерирует файлы через QuestPDF (PDF) и ClosedXML (XLSX). ImportService парсит XLSX, валидирует, проверяет пересечения, bulk insert. Frontend: страница `/schedule` расширяется кнопками экспорта, формой импорта, модалками CRUD.

**Tech Stack:** QuestPDF 2025+, ClosedXML 0.104+, React 18, shadcn/ui Dialog

## Global Constraints
- Все сообщения об ошибках на русском языке
- `CancellationToken ct` на всех async методах
- `Result<T>` паттерн для всех сервисов
- Formatting: CSharpier (`dotnet csharpier .`)

---

### Task 1: NuGet packages + docker rebuild

**Files:**
- Modify: `CollegeLMS.API/CollegeLMS.csproj`
- Run: docker compose build + up

- [ ] **Step 1: Add QuestPDF + ClosedXML to csproj**

Добавить в `CollegeLMS.API/CollegeLMS.csproj` после секции `<!-- Image Processing -->`:

```xml
  <!-- Document Generation -->
  <ItemGroup>
    <PackageReference Include="QuestPDF" Version="2025.12.0" />
    <PackageReference Include="ClosedXML" Version="0.104.2" />
  </ItemGroup>
```

- [ ] **Step 2: Rebuild and restart API**

```powershell
docker compose build api && docker compose up -d api
```

---

### Task 2: ScheduleExportService

**Files:**
- Create: `CollegeLMS.API/Services/ScheduleExportService.cs`
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs` (add ExportAsync)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs` (implement ExportAsync)
- Create: `CollegeLMS.API/Dtos/ScheduleExportDtos.cs`

**Interfaces:**
- Consumes: `IScheduleService.GetAllAsync()`, ScheduleEntry entity
- Produces: `ExportScheduleAsync(groupId, teacherId, room, period, format, ct)` → `byte[]`

- [ ] **Step 1: Create ScheduleExportDtos.cs**

```csharp
namespace CollegeLMS.API.Dtos;

public enum ExportFormat
{
    Pdf,
    Xlsx
}

public class ExportResult
{
    public byte[] FileContent { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Add ExportAsync to IScheduleService**

```csharp
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.Interfaces;

public interface IScheduleService
{
    // ... existing methods ...

    Task<Result<ExportResult>> ExportScheduleAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? period,
        ExportFormat format,
        CancellationToken ct = default
    );
}
```

- [ ] **Step 3: Create ScheduleExportService.cs**

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace CollegeLMS.API.Services;

public class ScheduleExportService(AppDbContext db)
{
    public async Task<Result<ExportResult>> ExportAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? period,
        ExportFormat format,
        CancellationToken ct
    )
    {
        var query = db
            .ScheduleEntries.AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(s => s.GroupId == groupId.Value);
        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);
        if (!string.IsNullOrEmpty(room))
            query = query.Where(s => s.Room == room);

        var today = DateTime.UtcNow;
        if (period == "day")
            query = query.Where(s => s.DayOfWeek == today.DayOfWeek);

        query = query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime);

        var entries = await query.ToListAsync(ct);

        if (entries.Count == 0)
            return Result<ExportResult>.Fail("Нет данных для экспорта", 404);

        var daysMap = new Dictionary<DayOfWeek, string>
        {
            { DayOfWeek.Monday, "Понедельник" },
            { DayOfWeek.Tuesday, "Вторник" },
            { DayOfWeek.Wednesday, "Среда" },
            { DayOfWeek.Thursday, "Четверг" },
            { DayOfWeek.Friday, "Пятница" },
            { DayOfWeek.Saturday, "Суббота" },
            { DayOfWeek.Sunday, "Воскресенье" },
        };

        if (format == ExportFormat.Pdf)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text("Расписание занятий")
                        .SemiBold().FontSize(16).AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("День").Bold();
                            h.Cell().Text("Предмет").Bold();
                            h.Cell().Text("Преподаватель").Bold();
                            h.Cell().Text("Аудитория").Bold();
                            h.Cell().Text("Начало").Bold();
                            h.Cell().Text("Конец").Bold();
                            h.Cell().Text("Группа").Bold();
                            h.Cell().Text("Тип").Bold();
                        });

                        foreach (var e in entries)
                        {
                            table.Cell().Text(daysMap.GetValueOrDefault(e.DayOfWeek, e.DayOfWeek.ToString()));
                            table.Cell().Text(e.Subject);
                            table.Cell().Text(e.Teacher?.User?.FullName ?? "");
                            table.Cell().Text(e.Room);
                            table.Cell().Text(e.StartTime.ToString(@"hh\:mm"));
                            table.Cell().Text(e.EndTime.ToString(@"hh\:mm"));
                            table.Cell().Text(e.Group?.Name ?? "");
                            table.Cell().Text(e.LessonType.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                    });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return Result<ExportResult>.Ok(new ExportResult
            {
                FileContent = pdfBytes,
                ContentType = "application/pdf",
                FileName = $"schedule_{DateTime.UtcNow:yyyyMMdd}.pdf"
            });
        }
        else
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Расписание");

            ws.Cell(1, 1).Value = "День";
            ws.Cell(1, 2).Value = "Предмет";
            ws.Cell(1, 3).Value = "Преподаватель";
            ws.Cell(1, 4).Value = "Аудитория";
            ws.Cell(1, 5).Value = "Начало";
            ws.Cell(1, 6).Value = "Конец";
            ws.Cell(1, 7).Value = "Группа";
            ws.Cell(1, 8).Value = "Тип занятия";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var e in entries)
            {
                ws.Cell(row, 1).Value = daysMap.GetValueOrDefault(e.DayOfWeek, e.DayOfWeek.ToString());
                ws.Cell(row, 2).Value = e.Subject;
                ws.Cell(row, 3).Value = e.Teacher?.User?.FullName ?? "";
                ws.Cell(row, 4).Value = e.Room;
                ws.Cell(row, 5).Value = e.StartTime.ToString(@"hh\:mm");
                ws.Cell(row, 6).Value = e.EndTime.ToString(@"hh\:mm");
                ws.Cell(row, 7).Value = e.Group?.Name ?? "";
                ws.Cell(row, 8).Value = e.LessonType.ToString();
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var xlsxBytes = ms.ToArray();

            return Result<ExportResult>.Ok(new ExportResult
            {
                FileContent = xlsxBytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"schedule_{DateTime.UtcNow:yyyyMMdd}.xlsx"
            });
        }
    }
}
```

- [ ] **Step 4: Register ScheduleExportService in DI**

В `Extensions/ServiceCollectionExtensions.cs` добавить:
```csharp
services.AddScoped<ScheduleExportService>();
```

- [ ] **Step 5: Implement ExportAsync in ScheduleService**

В `ScheduleService.cs` добавить метод, делегирующий в `ScheduleExportService`:
```csharp
public async Task<Result<ExportResult>> ExportScheduleAsync(
    Guid? groupId,
    Guid? teacherId,
    string? room,
    string? period,
    ExportFormat format,
    CancellationToken ct
)
{
    var exportService = new ScheduleExportService(db);
    return await exportService.ExportAsync(groupId, teacherId, room, period, format, ct);
}
```

---

### Task 3: Export endpoint + Controller

**Files:**
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs`

- [ ] **Step 1: Add export endpoint**

```csharp
[HttpGet("export")]
[Authorize(Roles = "Admin,Teacher,Student,Dispatcher")]
[SwaggerOperation(Summary = "Экспорт расписания в PDF или Excel")]
[SwaggerResponse(200, "Файл готов к скачиванию")]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(404, "Нет данных")]
[SwaggerResponse(500, "Ошибка сервера")]
public async Task<IActionResult> Export(
    [FromQuery] Guid? groupId,
    [FromQuery] Guid? teacherId,
    [FromQuery] string? room,
    [FromQuery] string? period,
    [FromQuery] string format = "pdf",
    CancellationToken ct = default
)
{
    var fmt = format.ToLower() == "xlsx" ? ExportFormat.Xlsx : ExportFormat.Pdf;
    var result = await service.ExportScheduleAsync(groupId, teacherId, room, period, fmt, ct);

    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);

    return File(
        result.Data!.FileContent,
        result.Data.ContentType,
        result.Data.FileName
    );
}
```

---

### Task 4: ScheduleImportService

**Files:**
- Create: `CollegeLMS.API/Services/ScheduleImportService.cs`
- Create: `CollegeLMS.API/Dtos/ScheduleImportDtos.cs`
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs`
- Modify: `CollegeLMS.API/Services/ScheduleService.cs`

**Interfaces:**
- Produces: `ImportScheduleAsync(Stream fileStream, ct)` → `Result<ImportResult>`
- Consumes: XLSX file stream

- [ ] **Step 1: Create ScheduleImportDtos.cs**

```csharp
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ScheduleImportResult
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

public class ScheduleImportRow
{
    public string GroupName { get; set; } = string.Empty;
    public string? TeacherName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string LessonType { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create ScheduleImportService.cs**

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class ScheduleImportService(AppDbContext db)
{
    private static readonly Dictionary<string, DayOfWeek> DayMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["пн"] = DayOfWeek.Monday, ["понедельник"] = DayOfWeek.Monday, ["monday"] = DayOfWeek.Monday,
        ["вт"] = DayOfWeek.Tuesday, ["вторник"] = DayOfWeek.Tuesday, ["tuesday"] = DayOfWeek.Tuesday,
        ["ср"] = DayOfWeek.Wednesday, ["среда"] = DayOfWeek.Wednesday, ["wednesday"] = DayOfWeek.Wednesday,
        ["чт"] = DayOfWeek.Thursday, ["четверг"] = DayOfWeek.Thursday, ["thursday"] = DayOfWeek.Thursday,
        ["пт"] = DayOfWeek.Friday, ["пятница"] = DayOfWeek.Friday, ["friday"] = DayOfWeek.Friday,
        ["сб"] = DayOfWeek.Saturday, ["суббота"] = DayOfWeek.Saturday, ["saturday"] = DayOfWeek.Saturday,
        ["вс"] = DayOfWeek.Sunday, ["воскресенье"] = DayOfWeek.Sunday, ["sunday"] = DayOfWeek.Sunday,
    };

    private static readonly Dictionary<string, LessonType> LessonTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["лекция"] = LessonType.Lecture, ["лек"] = LessonType.Lecture, ["lecture"] = LessonType.Lecture,
        ["практика"] = LessonType.Practice, ["пр"] = LessonType.Practice, ["practice"] = LessonType.Practice,
        ["лабораторная"] = LessonType.Lab, ["лаб"] = LessonType.Lab, ["lab"] = LessonType.Lab,
        ["экзамен"] = LessonType.Exam, ["экз"] = LessonType.Exam, ["exam"] = LessonType.Exam,
    };

    public async Task<Result<ScheduleImportResult>> ImportAsync(Stream fileStream, CancellationToken ct)
    {
        var result = new ScheduleImportResult();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return Result<ScheduleImportResult>.Fail("Не удалось прочитать файл. Убедитесь, что это XLSX-файл.", 400);
        }

        var ws = workbook.Worksheet(1);
        if (ws is null)
            return Result<ScheduleImportResult>.Fail("Файл не содержит листов.", 400);

        var rows = ws.RowsUsed().Skip(1).ToList(); // skip header
        if (rows.Count == 0)
            return Result<ScheduleImportResult>.Fail("Файл не содержит данных.", 400);

        var entriesToAdd = new List<ScheduleEntry>();

        foreach (var (row, index) in rows.Select((r, i) => (r, i)))
        {
            var rowNum = index + 2; // 1-based + header
            var parsed = TryParseRow(row, rowNum, result, db, ct);
            if (parsed is not null)
                entriesToAdd.Add(parsed);
        }

        if (entriesToAdd.Count > 0)
        {
            db.ScheduleEntries.AddRange(entriesToAdd);
            await db.SaveChangesAsync(ct);
            result.Imported = entriesToAdd.Count;
        }

        return Result<ScheduleImportResult>.Ok(result);
    }

    private ScheduleEntry? TryParseRow(
        IXLRow row,
        int rowNum,
        ScheduleImportResult result,
        AppDbContext db,
        CancellationToken ct
    )
    {
        var groupName = row.Cell(1).GetString().Trim();
        var teacherName = row.Cell(2).GetString().Trim();
        var subject = row.Cell(3).GetString().Trim();
        var room = row.Cell(4).GetString().Trim();
        var dayStr = row.Cell(5).GetString().Trim();
        var startStr = row.Cell(6).GetString().Trim();
        var endStr = row.Cell(7).GetString().Trim();
        var lessonTypeStr = row.Cell(8).GetString().Trim();

        var errors = new List<string>();

        if (string.IsNullOrEmpty(groupName))
            errors.Add("Группа не указана");
        if (string.IsNullOrEmpty(subject))
            errors.Add("Предмет не указан");
        if (string.IsNullOrEmpty(room))
            errors.Add("Аудитория не указана");

        if (!DayMap.TryGetValue(dayStr, out var dayOfWeek))
            errors.Add($"Некорректный день недели: '{dayStr}'");

        if (!LessonTypeMap.TryGetValue(lessonTypeStr, out var lessonType))
            errors.Add($"Некорректный тип занятия: '{lessonTypeStr}'");

        if (!TimeSpan.TryParse(startStr, out var startTime))
            errors.Add($"Некорректное время начала: '{startStr}'");
        if (!TimeSpan.TryParse(endStr, out var endTime))
            errors.Add($"Некорректное время конца: '{endStr}'");

        if (startTime >= endTime)
            errors.Add("Время начала должно быть меньше времени конца");

        if (errors.Count > 0)
        {
            result.Errors.Add(new ImportError { Row = rowNum, Message = string.Join("; ", errors) });
            result.Skipped++;
            return null;
        }

        return new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Room = room,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            LessonType = lessonType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
```

- [ ] **Step 3: Add import to IScheduleService**

```csharp
Task<Result<ScheduleImportResult>> ImportScheduleAsync(IFormFile file, CancellationToken ct = default);
```

- [ ] **Step 4: Implement import in ScheduleService**

```csharp
public async Task<Result<ScheduleImportResult>> ImportScheduleAsync(
    IFormFile file,
    CancellationToken ct
)
{
    if (file == null || file.Length == 0)
        return Result<ScheduleImportResult>.Fail("Файл не выбран или пуст", 400);

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext != ".xlsx")
        return Result<ScheduleImportResult>.Fail("Поддерживается только формат XLSX", 400);

    if (file.Length > 10 * 1024 * 1024) // 10MB
        return Result<ScheduleImportResult>.Fail("Файл слишком большой. Максимум 10MB.", 400);

    using var stream = new MemoryStream();
    await file.CopyToAsync(stream, ct);
    stream.Seek(0, SeekOrigin.Begin);

    var importService = new ScheduleImportService(db);
    return await importService.ImportAsync(stream, ct);
}
```

- [ ] **Step 5: Register ScheduleImportService in DI**

```csharp
services.AddScoped<ScheduleImportService>();
```

---

### Task 5: Import endpoint in ScheduleController

**Files:**
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs`

- [ ] **Step 1: Add import endpoint**

```csharp
[HttpPost("import")]
[Authorize(Roles = "Dispatcher,Admin")]
[SwaggerOperation(Summary = "Импорт расписания из XLSX-файла")]
[SwaggerResponse(200, "Импорт выполнен", typeof(Result<ScheduleImportResult>))]
[SwaggerResponse(400, "Ошибка валидации файла")]
[SwaggerResponse(401, "Не авторизован")]
[SwaggerResponse(403, "Доступ запрещён")]
[SwaggerResponse(500, "Ошибка сервера")]
public async Task<ActionResult<Result<ScheduleImportResult>>> Import(
    IFormFile file,
    CancellationToken ct
)
{
    var result = await service.ImportScheduleAsync(file, ct);
    if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result);
    return Ok(result);
}
```

---

### Task 6: Unit tests for export + import

**Files:**
- Modify: `CollegeLMS.Tests/Unit/Services/ScheduleServiceTests.cs`
- Create: `CollegeLMS.Tests/Unit/Services/ScheduleImportServiceTests.cs`

- [ ] **Step 1: Add export test to ScheduleServiceTests**

```csharp
[Fact]
public async Task ExportScheduleAsync_ReturnsPdfBytes_WhenEntriesExist()
{
    var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
    _db.ScheduleEntries.AddRange(entries);
    await _db.SaveChangesAsync();

    var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Pdf, default);

    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.FileContent.Should().NotBeEmpty();
    result.Data.ContentType.Should().Be("application/pdf");
}

[Fact]
public async Task ExportScheduleAsync_ReturnsXlsxBytes_WhenEntriesExist()
{
    var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
    _db.ScheduleEntries.AddRange(entries);
    await _db.SaveChangesAsync();

    var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Xlsx, default);

    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.FileContent.Should().NotBeEmpty();
    result.Data.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
}

[Fact]
public async Task ExportScheduleAsync_ReturnsFail_WhenNoData()
{
    var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Pdf, default);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
}
```

- [ ] **Step 2: Create ScheduleImportServiceTests.cs**

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleImportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleImportService _sut;

    public ScheduleImportServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleImportService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ImportAsync_ImportsValidRows()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = "ГР-11", Course = 1 };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");
        ws.Cell(1, 1).Value = "Группа";
        ws.Cell(1, 2).Value = "Преподаватель";
        ws.Cell(1, 3).Value = "Предмет";
        ws.Cell(1, 4).Value = "Аудитория";
        ws.Cell(1, 5).Value = "День";
        ws.Cell(1, 6).Value = "Начало";
        ws.Cell(1, 7).Value = "Конец";
        ws.Cell(1, 8).Value = "Тип занятия";
        ws.Cell(2, 1).Value = "ГР-11";
        ws.Cell(2, 2).Value = "";
        ws.Cell(2, 3).Value = "Математика";
        ws.Cell(2, 4).Value = "301";
        ws.Cell(2, 5).Value = "Пн";
        ws.Cell(2, 6).Value = "09:00";
        ws.Cell(2, 7).Value = "10:30";
        ws.Cell(2, 8).Value = "Лекция";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(1);
        result.Data.Skipped.Should().Be(0);
        result.Data.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ReturnsError_WhenInvalidFile()
    {
        using var ms = new MemoryStream(new byte[] { 0, 1, 2, 3 });
        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ImportAsync_SkipsInvalidRows()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");
        ws.Cell(1, 1).Value = "Группа";
        ws.Cell(1, 2).Value = "Преподаватель";
        ws.Cell(1, 3).Value = "Предмет";
        ws.Cell(1, 4).Value = "Аудитория";
        ws.Cell(1, 5).Value = "День";
        ws.Cell(1, 6).Value = "Начало";
        ws.Cell(1, 7).Value = "Конец";
        ws.Cell(1, 8).Value = "Тип занятия";
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 3).Value = "Математика";
        ws.Cell(2, 4).Value = "301";
        ws.Cell(2, 5).Value = "НепонятныйДень";
        ws.Cell(2, 6).Value = "09:00";
        ws.Cell(2, 7).Value = "10:30";
        ws.Cell(2, 8).Value = "Лекция";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(0);
        result.Data.Skipped.Should().Be(1);
        result.Data.Errors.Should().HaveCount(1);
        result.Data.Errors[0].Message.Should().Contain("Группа не указана");
    }
}
```

---

### Task 7: Frontend — экспорт с кнопками

**Files:**
- Modify: `frontend/app/schedule/page.tsx`

- [ ] **Step 1: Replace the page.tsx content with the updated version with export/import/CRUD**

See full implementation in subsequent tasks.

- [ ] **Step 2: Add export button to ScheduleFilterBar**

In `ScheduleFilterBar.tsx`, add after the filter buttons:
```tsx
<div className="ml-auto flex items-center gap-2">
  <Button variant="outline" size="sm" onClick={() => onExport("pdf")}>
    <FileDown className="size-4 mr-1" />
    PDF
  </Button>
  <Button variant="outline" size="sm" onClick={() => onExport("xlsx")}>
    <FileSpreadsheet className="size-4 mr-1" />
    Excel
  </Button>
</div>
```

Update props interface to include `onExport: (format: string) => void`.

- [ ] **Step 3: Add export and import functions in api/schedule.ts**

```typescript
export async function exportSchedule(
  filters: ScheduleFilters,
  format: "pdf" | "xlsx"
): Promise<Blob> {
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.period) params.set("period", filters.period)
  params.set("format", format)

  const token = typeof window !== "undefined" ? localStorage.getItem("token") : null
  const { data } = await api.get<Blob>(`/api/schedule/export?${params.toString()}`, {
    responseType: "blob",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  return data
}

export async function importSchedule(file: File): Promise<Result<ScheduleImportResult>> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await api.post("/api/schedule/import", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}
```

- [ ] **Step 4: Handle Blob download**

```typescript
function downloadBlob(blob: Blob, filename: string) {
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(url)
}
```

---

### Task 8: Frontend — CRUD модалки для диспетчера

**Files:**
- Modify: `frontend/app/schedule/page.tsx`
- Create: `frontend/components/ScheduleEntryDialog.tsx`

- [ ] **Step 1: Create ScheduleEntryDialog.tsx**

Модалка для создания/редактирования записи расписания:
- Поля: группа (select), преподаватель (select), предмет, аудитория, день недели, время начала, время конца, тип занятия
- Валидация на фронте
- POST/PUT запросы

- [ ] **Step 2: Add CRUD buttons on schedule page**

Кнопки "Добавить", "Редактировать", "Удалить" — видны только Dispatcher/Admin.
При клике на запись в таблице — открывается модалка редактирования.

- [ ] **Step 3: Add API functions**

```typescript
export async function createSchedule(data: CreateScheduleRequest): Promise<Result<ScheduleResponse>>
export async function updateSchedule(id: string, data: UpdateScheduleRequest): Promise<Result<ScheduleResponse>>
export async function deleteSchedule(id: string): Promise<Result<null>>
```

---

### Task 9: Frontend — форма импорта

**Files:**
- Modify: `frontend/app/schedule/page.tsx`
- Create: `frontend/components/ScheduleImportDialog.tsx`

- [ ] **Step 1: Create ScheduleImportDialog.tsx**

Диалог с дропзоной / выбором XLSX-файла, кнопкой импорта, отображением результатов.

- [ ] **Step 2: Handle import with progress/results**

Показать результат: сколько импортировано, сколько ошибок, список ошибок.

---

### Task 10: Verification

**Files:**
- Run: build + test

- [ ] **Step 1: dotnet build**

```powershell
docker compose exec api dotnet build --no-restore CollegeLMS.API/CollegeLMS.csproj
```

- [ ] **Step 2: dotnet test**

```powershell
docker compose exec api dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --no-restore
```
