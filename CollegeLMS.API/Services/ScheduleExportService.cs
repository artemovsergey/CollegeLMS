using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CollegeLMS.API.Services;

public class ScheduleExportService(
    AppDbContext db,
    IBellScheduleService bells,
    IScheduleViewService views
)
{
    private const string DayFilePrefix = "Расписание_день";
    private const string WeekFilePrefix = "Расписание_неделя";
    private const string SemesterFilePrefix = "Расписание_семестр";
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    private static readonly Dictionary<DayOfWeek, string> DayNames = new()
    {
        [DayOfWeek.Monday] = "Понедельник",
        [DayOfWeek.Tuesday] = "Вторник",
        [DayOfWeek.Wednesday] = "Среда",
        [DayOfWeek.Thursday] = "Четверг",
        [DayOfWeek.Friday] = "Пятница",
        [DayOfWeek.Saturday] = "Суббота",
        [DayOfWeek.Sunday] = "Воскресенье",
    };

    public async Task<Result<ExportResult>> ExportAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        string? scope,
        DateTime? date,
        int? week,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        if (string.Equals(scope, "semester", StringComparison.OrdinalIgnoreCase))
            return await ExportSemesterAsync(groupId, teacherId, format, ct);
        if (string.Equals(scope, "day", StringComparison.OrdinalIgnoreCase))
            return await ExportDayViewAsync(groupId, teacherId, room, date, format, layout, ct);
        if (string.Equals(scope, "week", StringComparison.OrdinalIgnoreCase))
            return await ExportWeekViewAsync(
                groupId,
                teacherId,
                room,
                week,
                date,
                format,
                layout,
                ct
            );

        return Result<ExportResult>.Fail("Укажите scope: day, week или semester.", 400);
    }

    // ──────────────────────── DAY / WEEK EXPORT ────────────────────────

    private async Task<Result<ExportResult>> ExportDayViewAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        DateTime? date,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        var viewResult = await views.GetDayAsync(groupId, teacherId, room, date, ct);
        if (!viewResult.IsSuccess)
            return Result<ExportResult>.Fail(
                viewResult.ErrorMessage ?? "Нет данных для экспорта",
                viewResult.StatusCode
            );

        var day = viewResult.Data!;
        if (day.IsNonWorking)
            return Result<ExportResult>.Fail($"Нерабочий день: {day.NonWorkingTitle}", 400);
        if (day.IsSunday || IsEmptyDay(day))
            return Result<ExportResult>.Fail("Нет данных для экспорта", 404);

        var title = $"Расписание на день · {DayNames[day.Date.DayOfWeek]}, {day.Date:dd.MM.yyyy}";
        return RenderViews([day], title, DayFilePrefix, format, layout);
    }

    private async Task<Result<ExportResult>> ExportWeekViewAsync(
        Guid? groupId,
        Guid? teacherId,
        string? room,
        int? week,
        DateTime? date,
        ExportFormat format,
        ExportLayout layout,
        CancellationToken ct
    )
    {
        var viewResult = await views.GetWeekAsync(groupId, teacherId, room, week, date, ct);
        if (!viewResult.IsSuccess)
            return Result<ExportResult>.Fail(
                viewResult.ErrorMessage ?? "Нет данных для экспорта",
                viewResult.StatusCode
            );

        var data = viewResult.Data!;
        var days = data.Days.Where(d => !d.IsSunday).ToList();
        if (days.Count == 0 || days.All(IsEmptyDay))
            return Result<ExportResult>.Fail("Нет данных для экспорта", 404);

        var lastDay = days[^1].Date;
        var title =
            $"Расписание на неделю {data.Week} · {data.WeekStart:dd.MM.yyyy}–{lastDay:dd.MM.yyyy}";
        return RenderViews(days, title, WeekFilePrefix, format, layout);
    }

    private static Result<ExportResult> RenderViews(
        IReadOnlyList<ScheduleDayViewResponse> days,
        string title,
        string filePrefix,
        ExportFormat format,
        ExportLayout layout
    ) =>
        (format, layout) switch
        {
            (ExportFormat.Xlsx, ExportLayout.DayCards) => ExportDayCardsXlsx(
                days,
                title,
                filePrefix
            ),
            (ExportFormat.Xlsx, _) => ExportGridXlsx(days, title, filePrefix),
            (ExportFormat.Pdf, ExportLayout.DayCards) => ExportDayCardsPdf(days, title, filePrefix),
            _ => ExportGridPdf(days, title, filePrefix),
        };

    private static bool IsEmptyDay(ScheduleDayViewResponse day) =>
        !day.IsNonWorking
        && day.Practices.Count == 0
        && day.Inserts.Count == 0
        && day.Entries.Count == 0;

    /// <summary>Строки слоёв дня: вставки, практика (заменяет пары), пары с бейджами.</summary>
    private static List<string> DayLines(ScheduleDayViewResponse day)
    {
        if (day.IsNonWorking)
            return [$"Не работает: {day.NonWorkingTitle}"];
        if (day.Practices.Count > 0)
            return day.Practices.Select(PracticeLine).ToList();

        var lines = day
            .Inserts.OrderBy(i => i.StartTime)
            .Select(i => $"{i.StartTime:hh\\:mm}–{i.EndTime:hh\\:mm} {i.Title}")
            .ToList();
        lines.AddRange(day.Entries.OrderBy(e => e.NumberPair).Select(EntryLine));
        return lines;
    }

    /// <summary>Строки без пар: вставки, практики и пометка нерабочего дня.</summary>
    private static string SpecialLines(ScheduleDayViewResponse day)
    {
        var lines = new List<string>();
        if (day.IsNonWorking)
            lines.Add($"Не работает: {day.NonWorkingTitle}");
        lines.AddRange(day.Practices.Select(PracticeLine));
        lines.AddRange(
            day.Inserts.OrderBy(i => i.StartTime)
                .Select(i => $"{i.StartTime:hh\\:mm}–{i.EndTime:hh\\:mm} {i.Title}")
        );
        return string.Join("\n", lines);
    }

    private static string PracticeLine(PracticeResponse practice)
    {
        var kind = practice.Kind == PracticeKind.Up ? "УП" : "ПП";
        var parts = new List<string> { $"{kind}: {practice.GroupName}" };
        if (!string.IsNullOrWhiteSpace(practice.TeacherName))
            parts.Add(practice.TeacherName);
        if (!string.IsNullOrWhiteSpace(practice.Organization))
            parts.Add(practice.Organization);
        return string.Join(" · ", parts);
    }

    private static string EntryLine(ScheduleResponse entry)
    {
        var parts = new List<string> { $"{entry.NumberPair}. {entry.Subject}" };
        if (!string.IsNullOrWhiteSpace(entry.Room))
            parts.Add($"ауд. {entry.Room}");
        if (!string.IsNullOrWhiteSpace(entry.TeacherName))
            parts.Add(entry.TeacherName);

        var marks = BuildMarks(entry.ChangeTags);
        if (marks.Length > 0)
            parts.Add(marks);

        return string.Join(" · ", parts);
    }

    private static string BuildMarks(IReadOnlyList<ChangeTag> tags)
    {
        var labels = new List<string>();
        foreach (var tag in tags)
        {
            var label = tag.ChangeType switch
            {
                ScheduleChangeType.Add => "добавлено",
                ScheduleChangeType.Remove => string.Equals(
                    tag.Note?.Trim(),
                    "сам.р.",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "сам.р."
                    : "снято",
                ScheduleChangeType.Replace => "замена",
                ScheduleChangeType.Move => "перенос",
                _ => null,
            };
            if (label is not null && !labels.Contains(label))
                labels.Add(label);
        }
        return string.Join(", ", labels);
    }

    private static string DayHeader(ScheduleDayViewResponse day) =>
        $"{DayNames[day.Date.DayOfWeek]}, {day.Date:dd.MM.yyyy}";

    private static Result<ExportResult> ExportGridXlsx(
        IReadOnlyList<ScheduleDayViewResponse> days,
        string title,
        string filePrefix
    )
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Range(1, 1, 1, 1 + days.Count).Merge();
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        const int headerRow = 3;
        ws.Cell(headerRow, 1).Value = "№";
        for (var i = 0; i < days.Count; i++)
            ws.Cell(headerRow, 2 + i).Value = DayHeader(days[i]);

        var headerRange = ws.Range(headerRow, 1, headerRow, 1 + days.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var row = headerRow + 1;
        ws.Cell(row, 1).Value = "Вставки";
        for (var i = 0; i < days.Count; i++)
            ws.Cell(row, 2 + i).Value = SpecialLines(days[i]);
        var layerRange = ws.Range(row, 1, row, 1 + days.Count);
        layerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8edf2");
        layerRange.Style.Alignment.WrapText = true;
        layerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        layerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row++;

        var pairs = days.SelectMany(d => d.Entries.Select(e => e.NumberPair))
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        foreach (var pair in pairs)
        {
            ws.Cell(row, 1).Value = pair;
            for (var i = 0; i < days.Count; i++)
            {
                ws.Cell(row, 2 + i).Value = string.Join(
                    "\n",
                    days[i].Entries.Where(e => e.NumberPair == pair).Select(EntryLine)
                );
            }

            var pairRange = ws.Range(row, 1, row, 1 + days.Count);
            pairRange.Style.Fill.BackgroundColor =
                pair % 2 == 0 ? XLColor.FromHtml("#f0f4f8") : XLColor.White;
            pairRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            pairRange.Style.Alignment.WrapText = true;
            pairRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(row).Height = 60;
            row++;
        }

        ws.Column(1).Width = 10;
        for (var i = 0; i < days.Count; i++)
            ws.Column(2 + i).Width = 30;

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return OkFile(stream.ToArray(), XlsxContentType, filePrefix, "xlsx");
    }

    private static Result<ExportResult> ExportGridPdf(
        IReadOnlyList<ScheduleDayViewResponse> days,
        string title,
        string filePrefix
    )
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pairs = days.SelectMany(d => d.Entries.Select(e => e.NumberPair))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().PaddingBottom(8).Text(title).SemiBold().FontSize(14).AlignCenter();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(45);
                            foreach (var _ in days)
                                c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell()
                                .Background(Color.FromHex("#1e3a5f"))
                                .Padding(4)
                                .Text("№")
                                .FontColor(Colors.White)
                                .SemiBold()
                                .AlignCenter();
                            foreach (var day in days)
                            {
                                h.Cell()
                                    .Background(Color.FromHex("#1e3a5f"))
                                    .Padding(4)
                                    .Text(DayHeader(day))
                                    .FontColor(Colors.White)
                                    .SemiBold()
                                    .FontSize(7)
                                    .AlignCenter();
                            }
                        });

                        table
                            .Cell()
                            .Background(Color.FromHex("#e8edf2"))
                            .Padding(3)
                            .Text("Вставки")
                            .SemiBold()
                            .FontSize(7);
                        foreach (var day in days)
                        {
                            table
                                .Cell()
                                .Background(Color.FromHex("#e8edf2"))
                                .Padding(3)
                                .Text(SpecialLines(day))
                                .FontSize(7);
                        }

                        foreach (var pair in pairs)
                        {
                            var isEven = pair % 2 == 0;
                            var bg = isEven ? Color.FromHex("#f0f4f8") : Colors.White;

                            table
                                .Cell()
                                .Background(bg)
                                .Padding(3)
                                .Text(pair.ToString())
                                .SemiBold()
                                .AlignCenter();
                            foreach (var day in days)
                            {
                                var text = string.Join(
                                    "\n",
                                    day.Entries.Where(e => e.NumberPair == pair).Select(EntryLine)
                                );
                                table.Cell().Background(bg).Padding(3).Text(text).FontSize(7);
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

        return OkFile(document.GeneratePdf(), PdfContentType, filePrefix, "pdf");
    }

    private static Result<ExportResult> ExportDayCardsXlsx(
        IReadOnlyList<ScheduleDayViewResponse> days,
        string title,
        string filePrefix
    )
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var row = 3;
        foreach (var day in days)
        {
            ws.Range(row, 1, row, 6).Merge();
            ws.Cell(row, 1).Value = $"{DayNames[day.Date.DayOfWeek]}, {day.Date:dd.MM.yyyy}";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            var lines = DayLines(day);
            if (lines.Count == 0)
                lines.Add("Нет пар");

            foreach (var line in lines)
            {
                ws.Range(row, 1, row, 6).Merge();
                ws.Cell(row, 1).Value = line;
                ws.Cell(row, 1).Style.Alignment.WrapText = true;
                ws.Cell(row, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, 1).Style.Border.BottomBorderColor = XLColor.FromHtml("#e5e7eb");
                row++;
            }

            row++;
        }

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 40;
        ws.Column(6).Width = 24;

        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.Margins.Left = 0.7;
        ws.PageSetup.Margins.Right = 0.7;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return OkFile(stream.ToArray(), XlsxContentType, filePrefix, "xlsx");
    }

    private static Result<ExportResult> ExportDayCardsPdf(
        IReadOnlyList<ScheduleDayViewResponse> days,
        string title,
        string filePrefix
    )
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().PaddingBottom(6).Text(title).SemiBold().FontSize(14).AlignCenter();

                page.Content()
                    .Column(col =>
                    {
                        var first = true;
                        foreach (var day in days)
                        {
                            if (!first)
                                col.Item().PaddingTop(10);
                            first = false;

                            col.Item()
                                .Background(Color.FromHex("#1e3a5f"))
                                .Padding(6)
                                .Text($"{DayNames[day.Date.DayOfWeek]}, {day.Date:dd.MM.yyyy}")
                                .FontColor(Colors.White)
                                .SemiBold()
                                .FontSize(11);

                            var lines = DayLines(day);
                            if (lines.Count == 0)
                                lines.Add("Нет пар");

                            foreach (var line in lines)
                                col.Item().PaddingTop(2).Text(line);
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

        return OkFile(document.GeneratePdf(), PdfContentType, filePrefix, "pdf");
    }

    private static Result<ExportResult> OkFile(
        byte[] content,
        string contentType,
        string filePrefix,
        string extension
    ) =>
        Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = content,
                ContentType = contentType,
                FileName = BuildFileName(filePrefix, extension),
            }
        );

    private static string BuildFileName(string prefix, string extension) =>
        $"{prefix}_{DateTime.UtcNow:dd.MM.yyyy_HH-mm-ss}.{extension}";

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
        return OkFile(stream.ToArray(), XlsxContentType, SemesterFilePrefix, "xlsx");
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

        return OkFile(document.GeneratePdf(), PdfContentType, SemesterFilePrefix, "pdf");
    }
}
