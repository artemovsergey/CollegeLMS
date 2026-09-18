using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CollegeLMS.API.Services;

public class ScheduleExportService(AppDbContext db, IBellScheduleService bells)
{
    private static readonly Dictionary<DayOfWeek, string> DaysMap = new()
    {
        [DayOfWeek.Monday] = "Понедельник",
        [DayOfWeek.Tuesday] = "Вторник",
        [DayOfWeek.Wednesday] = "Среда",
        [DayOfWeek.Thursday] = "Четверг",
        [DayOfWeek.Friday] = "Пятница",
    };

    private static readonly string[] DayOrder =
    [
        "Понедельник",
        "Вторник",
        "Среда",
        "Четверг",
        "Пятница",
    ];

    private static readonly Dictionary<string, string> DayShort = new()
    {
        ["Понедельник"] = "Пн",
        ["Вторник"] = "Вт",
        ["Среда"] = "Ср",
        ["Четверг"] = "Чт",
        ["Пятница"] = "Пт",
    };

    public async Task<Result<ExportResult>> ExportAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? period,
        string? scope,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        if (string.Equals(scope, "semester", StringComparison.OrdinalIgnoreCase))
            return await ExportSemesterAsync(groupId, teacherId, format, ct);

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
        {
            var nonWorking = await db
                .NonWorkingDays.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DateFrom <= today.Date && d.DateTo >= today.Date, ct);
            if (nonWorking is not null)
                return Result<ExportResult>.Fail($"Нерабочий день: {nonWorking.Title}", 400);

            query = query.Where(s => s.DayOfWeek == today.DayOfWeek);
        }

        query = query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.NumberPair).ThenBy(s => s.StartTime);
        var entries = await query.ToListAsync(ct);

        // Время пар — из справочника звонков (единый источник).
        var bellTimes = await bells.GetTimeMapAsync(ct);
        foreach (var entry in entries)
        {
            if (bellTimes.TryGetValue(entry.NumberPair, out var time))
            {
                entry.StartTime = time.Start;
                entry.EndTime = time.End;
            }
        }

        if (entries.Count == 0)
            return Result<ExportResult>.Fail("Нет данных для экспорта", 404);

        return format switch
        {
            ExportFormat.Pdf when layout == ExportLayout.DayCards => ExportPdfDayCards(entries),
            ExportFormat.Pdf => ExportPdfGrid(entries),
            ExportFormat.Xlsx when layout == ExportLayout.DayCards => ExportXlsxDayCards(entries),
            _ => ExportXlsxGrid(entries),
        };
    }

    // ──────────────────────── SEMESTER EXPORT ────────────────────────

    private async Task<Result<ExportResult>> ExportSemesterAsync(
        Guid? groupId,
        Guid? teacherId,
        ExportFormat format,
        CancellationToken ct
    )
    {
        if (groupId.HasValue == teacherId.HasValue)
            return Result<ExportResult>.Fail("Укажите одну группу или одного преподавателя.", 400);

        var entriesQuery = db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();
        if (groupId.HasValue)
            entriesQuery = entriesQuery.Where(e => e.GroupId == groupId.Value);
        if (teacherId.HasValue)
            entriesQuery = entriesQuery.Where(e => e.TeacherId == teacherId.Value);

        var entries = await entriesQuery.ToListAsync(ct);

        var bellTimes = await bells.GetTimeMapAsync(ct);
        foreach (var entry in entries)
        {
            if (bellTimes.TryGetValue(entry.NumberPair, out var time))
            {
                entry.StartTime = time.Start;
                entry.EndTime = time.End;
            }
        }

        var inserts = await db
            .ScheduleInserts.AsNoTracking()
            .Where(i => i.IsActive)
            .ToListAsync(ct);

        var practicesQuery = db
            .Practices.AsNoTracking()
            .Include(p => p.Group)
            .Include(p => p.Teacher!)
                .ThenInclude(t => t.User)
            .AsQueryable();
        if (groupId.HasValue)
            practicesQuery = practicesQuery.Where(p => p.GroupId == groupId.Value);
        if (teacherId.HasValue)
            practicesQuery = practicesQuery.Where(p => p.TeacherId == teacherId.Value);
        var practices = await practicesQuery.ToListAsync(ct);

        var monday = StudyWeek.MondayOf(StudyWeek.SemesterStart);
        var semesterStart = StudyWeek.SemesterStart.Date;
        var semesterEnd = monday.AddDays(StudyWeek.TotalWeeks * 7 - 1);
        var nonWorking = await db
            .NonWorkingDays.AsNoTracking()
            .Where(d => d.DateFrom <= semesterEnd && d.DateTo >= semesterStart)
            .ToListAsync(ct);

        var historyQuery = db.ScheduleHistory.AsNoTracking().AsQueryable();
        if (groupId.HasValue)
            historyQuery = historyQuery.Where(h => h.GroupId == groupId.Value);
        if (teacherId.HasValue)
            historyQuery = historyQuery.Where(h =>
                h.TeacherId == teacherId.Value || h.RemovedTeacherId == teacherId.Value
            );
        var history = await historyQuery.ToListAsync(ct);

        var dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
        var cells = new string[StudyWeek.TotalWeeks, 6];
        for (var week = 1; week <= StudyWeek.TotalWeeks; week++)
        {
            for (var dayIndex = 0; dayIndex < 6; dayIndex++)
            {
                var day = (DayOfWeek)(dayIndex + 1);
                var date = monday.AddDays((week - 1) * 7 + dayIndex);
                cells[week - 1, dayIndex] = BuildSemesterCell(
                    date,
                    day,
                    week,
                    entries,
                    inserts,
                    practices,
                    nonWorking,
                    history
                );
            }
        }

        return format == ExportFormat.Xlsx
            ? ExportSemesterXlsx(cells, dayNames)
            : ExportSemesterPdf(cells, dayNames);
    }

    private static string BuildSemesterCell(
        DateTime date,
        DayOfWeek day,
        int week,
        List<Entities.ScheduleEntry> entries,
        List<Entities.ScheduleInsert> inserts,
        List<Entities.Practice> practices,
        List<Entities.NonWorkingDay> nonWorking,
        List<Entities.ScheduleHistory> history
    )
    {
        var nwd = nonWorking.FirstOrDefault(d => d.DateFrom <= date && d.DateTo >= date);
        if (nwd is not null)
            return $"Не работает: {nwd.Title}";

        var practice = practices.FirstOrDefault(p => p.DateFrom <= date && p.DateTo >= date);
        if (practice is not null)
        {
            var kind = practice.Kind == Entities.Enums.PracticeKind.Up ? "УП" : "ПП";
            var parts = new List<string> { $"{kind}: {practice.Group?.Name ?? string.Empty}" };
            if (!string.IsNullOrEmpty(practice.Teacher?.User?.FullName))
                parts.Add(practice.Teacher.User.FullName);
            if (!string.IsNullOrEmpty(practice.Organization))
                parts.Add(practice.Organization);
            return string.Join("\n", parts);
        }

        var lines = new List<string>();
        foreach (var insert in inserts.Where(i => i.DayOfWeek == day).OrderBy(i => i.StartTime))
            lines.Add($"{insert.StartTime:hh\\:mm}–{insert.EndTime:hh\\:mm} {insert.Title}");

        foreach (
            var entry in entries
                .Where(e => e.DayOfWeek == day && e.Weeks.Contains(week))
                .OrderBy(e => e.NumberPair)
        )
        {
            var line = $"{entry.NumberPair}. {entry.Subject}";
            if (!string.IsNullOrEmpty(entry.Group?.Name))
                line += $" ({entry.Group.Name})";
            if (!string.IsNullOrEmpty(entry.Room))
                line += $" ауд. {entry.Room}";
            if (!string.IsNullOrEmpty(entry.Teacher?.User?.FullName))
                line += $" {entry.Teacher.User.FullName}";

            var marks = BuildSemesterMarks(history, entry.GroupId, day, week, entry.NumberPair);
            if (marks.Length > 0)
                line += $" · {marks}";

            lines.Add(line);
        }

        return lines.Count == 0 ? "—" : string.Join("\n", lines);
    }

    private static string BuildSemesterMarks(
        List<Entities.ScheduleHistory> history,
        Guid groupId,
        DayOfWeek day,
        int week,
        int numberPair
    )
    {
        var labels = new List<string>();
        foreach (
            var h in history.Where(h =>
                h.GroupId == groupId
                && h.DayOfWeek == day
                && h.Week == week
                && h.NumberPair == numberPair
            )
        )
        {
            var label = h.ChangeType switch
            {
                Entities.Enums.ScheduleChangeType.Add => "добавлено",
                Entities.Enums.ScheduleChangeType.Remove => string.Equals(
                    h.Note?.Trim(),
                    "сам.р.",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "сам.р."
                    : "снято",
                Entities.Enums.ScheduleChangeType.Replace => "замена",
                Entities.Enums.ScheduleChangeType.Move => "перенос",
                _ => null,
            };
            if (label is not null && !labels.Contains(label))
                labels.Add(label);
        }
        return string.Join(", ", labels);
    }

    private static Result<ExportResult> ExportSemesterXlsx(string[,] cells, string[] dayNames)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Семестр");

        ws.Cell(1, 1).Value = "Неделя";
        for (var day = 0; day < dayNames.Length; day++)
            ws.Cell(1, day + 2).Value = dayNames[day];

        var header = ws.Range(1, 1, 1, 7);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var weeks = cells.GetLength(0);
        for (var week = 0; week < weeks; week++)
        {
            ws.Cell(week + 2, 1).Value = week + 1;
            ws.Cell(week + 2, 1).Style.Font.Bold = true;
            ws.Cell(week + 2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            for (var day = 0; day < 6; day++)
            {
                var cell = ws.Cell(week + 2, day + 2);
                cell.Value = cells[week, day];
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }

        ws.Column(1).Width = 8;
        for (var day = 2; day <= 7; day++)
            ws.Column(day).Width = 30;

        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var stamp = DateTime.UtcNow.ToString("dd.MM.yyyy_HH-mm-ss");
        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = stream.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"Расписание_семестр_{stamp}.xlsx",
            }
        );
    }

    private static Result<ExportResult> ExportSemesterPdf(string[,] cells, string[] dayNames)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var weeks = cells.GetLength(0);
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontSize(6));

                page.Header()
                    .PaddingBottom(6)
                    .Text("Расписание на семестр")
                    .SemiBold()
                    .FontSize(13)
                    .AlignCenter();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            for (var day = 0; day < 6; day++)
                                columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header
                                .Cell()
                                .Background("#1e3a5f")
                                .Padding(3)
                                .Text("Неделя")
                                .FontColor(Colors.White)
                                .SemiBold()
                                .FontSize(7)
                                .AlignCenter();
                            foreach (var day in dayNames)
                            {
                                header
                                    .Cell()
                                    .Background("#1e3a5f")
                                    .Padding(3)
                                    .Text(day)
                                    .FontColor(Colors.White)
                                    .SemiBold()
                                    .FontSize(7)
                                    .AlignCenter();
                            }
                        });

                        for (var week = 0; week < weeks; week++)
                        {
                            table
                                .Cell()
                                .BorderBottom(0.5f)
                                .BorderColor("#e5e7eb")
                                .Padding(3)
                                .Text((week + 1).ToString())
                                .SemiBold();

                            for (var day = 0; day < 6; day++)
                            {
                                table
                                    .Cell()
                                    .BorderBottom(0.5f)
                                    .BorderColor("#e5e7eb")
                                    .Padding(3)
                                    .Text(cells[week, day]);
                            }
                        }
                    });
            });
        });

        var bytes = document.GeneratePdf();
        var stamp = DateTime.UtcNow.ToString("dd.MM.yyyy_HH-mm-ss");
        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = bytes,
                ContentType = "application/pdf",
                FileName = $"Расписание_семестр_{stamp}.pdf",
            }
        );
    }

    private static string CellText(Entities.ScheduleEntry e)
    {
        var parts = new List<string> { e.Subject };
        if (!string.IsNullOrEmpty(e.Group?.Name))
            parts.Add(e.Group.Name);
        if (!string.IsNullOrEmpty(e.Room))
            parts.Add($"ауд. {e.Room}");
        if (!string.IsNullOrEmpty(e.Teacher?.User?.FullName))
            parts.Add(e.Teacher.User.FullName);
        return string.Join("\n", parts);
    }

    // ──────────────────────── PDF GRID ────────────────────────

    private static Result<ExportResult> ExportPdfGrid(IReadOnlyList<Entities.ScheduleEntry> entries)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var grouped = entries
            .GroupBy(e => e.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(e => e.NumberPair).ToDictionary(g2 => g2.Key, g2 => g2.ToList())
            );

        var allPairs = entries.Select(e => e.NumberPair).Distinct().OrderBy(n => n).ToList();
        var days = DaysMap.Keys.Where(DaysMap.ContainsKey).ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header()
                    .PaddingBottom(8)
                    .Text("Расписание занятий")
                    .SemiBold()
                    .FontSize(14)
                    .AlignCenter();

                page.Content()
                    .Table(table =>
                    {
                        // Columns: Пара | Время | Пн | Вт | Ср | Чт | Пт
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30); // №
                            c.ConstantColumn(50); // Время
                            foreach (var _ in days)
                                c.RelativeColumn(1);
                        });

                        // Header row
                        table.Header(h =>
                        {
                            h.Cell()
                                .Background(Color.FromHex("#1e3a5f"))
                                .Padding(4)
                                .Text("№")
                                .FontColor(Colors.White)
                                .SemiBold()
                                .AlignCenter();
                            h.Cell()
                                .Background(Color.FromHex("#1e3a5f"))
                                .Padding(4)
                                .Text("Время")
                                .FontColor(Colors.White)
                                .SemiBold()
                                .AlignCenter();
                            foreach (var day in days)
                            {
                                h.Cell()
                                    .Background(Color.FromHex("#1e3a5f"))
                                    .Padding(4)
                                    .Text(DaysMap[day])
                                    .FontColor(Colors.White)
                                    .SemiBold()
                                    .AlignCenter();
                            }
                        });

                        foreach (var pairNum in allPairs)
                        {
                            // Pair number + time row
                            var firstEntry = entries.FirstOrDefault(e => e.NumberPair == pairNum);
                            var timeStr =
                                firstEntry != null
                                    ? $"{firstEntry.StartTime:hh\\:mm}–{firstEntry.EndTime:hh\\:mm}"
                                    : "";

                            var isEven = pairNum % 2 == 0;
                            var bgColor = isEven ? Color.FromHex("#f0f4f8") : Colors.White;

                            table
                                .Cell()
                                .Background(bgColor)
                                .Padding(3)
                                .Text(pairNum.ToString())
                                .SemiBold()
                                .AlignCenter();
                            table
                                .Cell()
                                .Background(bgColor)
                                .Padding(3)
                                .Text(timeStr)
                                .FontSize(7)
                                .AlignCenter();

                            foreach (var day in days)
                            {
                                var cellText = "";
                                if (
                                    grouped.TryGetValue(day, out var dayPairs)
                                    && dayPairs.TryGetValue(pairNum, out var pairEntries)
                                )
                                {
                                    cellText = string.Join("\n\n", pairEntries.Select(CellText));
                                }

                                table
                                    .Cell()
                                    .Background(bgColor)
                                    .Padding(3)
                                    .Text(cellText)
                                    .FontSize(7);
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                    });
            });
        });

        var bytes = doc.GeneratePdf();
        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = bytes,
                ContentType = "application/pdf",
                FileName = $"schedule_grid_{DateTime.UtcNow:yyyyMMdd}.pdf",
            }
        );
    }

    // ──────────────────── PDF DAY CARDS ───────────────────────

    private static Result<ExportResult> ExportPdfDayCards(
        IReadOnlyList<Entities.ScheduleEntry> entries
    )
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var byDay = entries
            .Where(e => DaysMap.ContainsKey(e.DayOfWeek))
            .GroupBy(e => e.DayOfWeek)
            .OrderBy(g => Array.IndexOf(DayOrder, DaysMap[g.Key]));

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .PaddingBottom(6)
                    .Text("Расписание занятий")
                    .SemiBold()
                    .FontSize(14)
                    .AlignCenter();

                page.Content()
                    .Column(col =>
                    {
                        bool first = true;
                        foreach (var dayGroup in byDay)
                        {
                            if (!first)
                                col.Item().PaddingTop(10);
                            first = false;

                            var dayName = DaysMap[dayGroup.Key];

                            // Day header
                            col.Item()
                                .Background(Color.FromHex("#1e3a5f"))
                                .Padding(6)
                                .Text(dayName)
                                .FontColor(Colors.White)
                                .SemiBold()
                                .FontSize(11);

                            // Pairs table
                            col.Item()
                                .PaddingTop(2)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(25); // Пара
                                        c.ConstantColumn(55); // Время
                                        c.RelativeColumn(2); // Предмет
                                        c.ConstantColumn(55); // Группа
                                        c.ConstantColumn(50); // Ауд.
                                        c.RelativeColumn(1.5f); // Преподаватель
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Пара")
                                            .SemiBold()
                                            .FontSize(8)
                                            .AlignCenter();
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Время")
                                            .SemiBold()
                                            .FontSize(8)
                                            .AlignCenter();
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Предмет")
                                            .SemiBold()
                                            .FontSize(8);
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Группа")
                                            .SemiBold()
                                            .FontSize(8)
                                            .AlignCenter();
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Ауд.")
                                            .SemiBold()
                                            .FontSize(8)
                                            .AlignCenter();
                                        h.Cell()
                                            .Background(Color.FromHex("#e8edf2"))
                                            .Padding(3)
                                            .Text("Преподаватель")
                                            .SemiBold()
                                            .FontSize(8);
                                    });

                                    foreach (var e in dayGroup.OrderBy(e => e.NumberPair))
                                    {
                                        var bg =
                                            e.NumberPair % 2 == 0
                                                ? Color.FromHex("#f8f9fa")
                                                : Colors.White;

                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text(e.NumberPair.ToString())
                                            .AlignCenter();
                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text($"{e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}")
                                            .FontSize(7)
                                            .AlignCenter();
                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text(e.Subject)
                                            .FontSize(8);
                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text(e.Group?.Name ?? "")
                                            .FontSize(8)
                                            .AlignCenter();
                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text(e.Room)
                                            .FontSize(8)
                                            .AlignCenter();
                                        table
                                            .Cell()
                                            .Background(bg)
                                            .Padding(3)
                                            .Text(e.Teacher?.User?.FullName ?? "")
                                            .FontSize(8);
                                    }
                                });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                    });
            });
        });

        var bytes = doc.GeneratePdf();
        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = bytes,
                ContentType = "application/pdf",
                FileName = $"schedule_days_{DateTime.UtcNow:yyyyMMdd}.pdf",
            }
        );
    }

    // ──────────────────── XLSX GRID ───────────────────────────

    private static Result<ExportResult> ExportXlsxGrid(
        IReadOnlyList<Entities.ScheduleEntry> entries
    )
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        var grouped = entries
            .GroupBy(e => e.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(e => e.NumberPair).ToDictionary(g2 => g2.Key, g2 => g2.ToList())
            );

        var allPairs = entries.Select(e => e.NumberPair).Distinct().OrderBy(n => n).ToList();
        var days = DaysMap.Keys.Where(DaysMap.ContainsKey).ToList();

        // Title
        ws.Range(1, 1, 1, 2 + days.Count).Merge();
        ws.Cell(1, 1).Value = "Расписание занятий";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Header row (row 3)
        int headerRow = 3;
        ws.Cell(headerRow, 1).Value = "№";
        ws.Cell(headerRow, 2).Value = "Время";
        for (int i = 0; i < days.Count; i++)
            ws.Cell(headerRow, 3 + i).Value = DaysMap[days[i]];

        var headerRange = ws.Range(headerRow, 1, headerRow, 2 + days.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int row = headerRow + 1;
        foreach (var pairNum in allPairs)
        {
            var firstEntry = entries.FirstOrDefault(e => e.NumberPair == pairNum);
            var timeStr =
                firstEntry != null
                    ? $"{firstEntry.StartTime:hh\\:mm}–{firstEntry.EndTime:hh\\:mm}"
                    : "";

            ws.Cell(row, 1).Value = pairNum;
            ws.Cell(row, 2).Value = timeStr;

            for (int i = 0; i < days.Count; i++)
            {
                var text = "";
                if (
                    grouped.TryGetValue(days[i], out var dayPairs)
                    && dayPairs.TryGetValue(pairNum, out var pairEntries)
                )
                {
                    text = string.Join("\n", pairEntries.Select(CellTextXlsx));
                }
                ws.Cell(row, 3 + i).Value = text;
            }

            var isEven = pairNum % 2 == 0;
            var rowRange = ws.Range(row, 1, row, 2 + days.Count);
            rowRange.Style.Fill.BackgroundColor = isEven
                ? XLColor.FromHtml("#f0f4f8")
                : XLColor.White;
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            rowRange.Style.Alignment.WrapText = true;

            ws.Row(row).Height = 60;
            row++;
        }

        // Auto-fit first 2 columns, fixed widths for day columns
        ws.Column(1).Width = 5;
        ws.Column(2).Width = 12;
        for (int i = 0; i < days.Count; i++)
            ws.Column(3 + i).Width = 28;

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.Margins.Left = 0.5;
        ws.PageSetup.Margins.Right = 0.5;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"schedule_grid_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            }
        );
    }

    // ────────────────── XLSX DAY CARDS ────────────────────────

    private static Result<ExportResult> ExportXlsxDayCards(
        IReadOnlyList<Entities.ScheduleEntry> entries
    )
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        var byDay = entries
            .Where(e => DaysMap.ContainsKey(e.DayOfWeek))
            .GroupBy(e => e.DayOfWeek)
            .OrderBy(g => Array.IndexOf(DayOrder, DaysMap[g.Key]));

        // Title
        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Value = "Расписание занятий";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 3;
        foreach (var dayGroup in byDay)
        {
            var dayName = DaysMap[dayGroup.Key];

            // Day header
            ws.Range(row, 1, row, 6).Merge();
            ws.Cell(row, 1).Value = dayName;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            // Column headers
            ws.Cell(row, 1).Value = "Пара";
            ws.Cell(row, 2).Value = "Время";
            ws.Cell(row, 3).Value = "Предмет";
            ws.Cell(row, 4).Value = "Группа";
            ws.Cell(row, 5).Value = "Ауд.";
            ws.Cell(row, 6).Value = "Преподаватель";

            var colRange = ws.Range(row, 1, row, 6);
            colRange.Style.Font.Bold = true;
            colRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8edf2");
            colRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;

            // Data
            foreach (var e in dayGroup.OrderBy(e => e.NumberPair))
            {
                ws.Cell(row, 1).Value = e.NumberPair;
                ws.Cell(row, 2).Value = $"{e.StartTime:hh\\:mm}–{e.EndTime:hh\\:mm}";
                ws.Cell(row, 3).Value = e.Subject;
                ws.Cell(row, 4).Value = e.Group?.Name ?? "";
                ws.Cell(row, 5).Value = e.Room;
                ws.Cell(row, 6).Value = e.Teacher?.User?.FullName ?? "";

                var bg = e.NumberPair % 2 == 0 ? XLColor.FromHtml("#f8f9fa") : XLColor.White;
                var dataRange = ws.Range(row, 1, row, 6);
                dataRange.Style.Fill.BackgroundColor = bg;
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Alignment.WrapText = true;
                row++;
            }

            row++; // gap between days
        }

        ws.Column(1).Width = 6;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 10;
        ws.Column(6).Width = 24;

        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.Margins.Left = 0.7;
        ws.PageSetup.Margins.Right = 0.7;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"schedule_days_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            }
        );
    }

    private static string CellTextXlsx(Entities.ScheduleEntry e)
    {
        var parts = new List<string> { e.Subject };
        if (!string.IsNullOrEmpty(e.Group?.Name))
            parts.Add(e.Group.Name);
        if (!string.IsNullOrEmpty(e.Room))
            parts.Add($"ауд. {e.Room}");
        if (!string.IsNullOrEmpty(e.Teacher?.User?.FullName))
            parts.Add(e.Teacher.User.FullName);
        return string.Join(" | ", parts);
    }
}
