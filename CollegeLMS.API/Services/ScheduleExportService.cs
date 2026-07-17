using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CollegeLMS.API.Services;

public class ScheduleExportService(AppDbContext db)
{
    private static readonly Dictionary<DayOfWeek, string> DaysMap = new()
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

        query = query.OrderBy(s => s.DayOfWeek).ThenBy(s => s.NumberPair).ThenBy(s => s.StartTime);
        var entries = await query.ToListAsync(ct);

        if (entries.Count == 0)
            return Result<ExportResult>.Fail("Нет данных для экспорта", 404);

        return format == ExportFormat.Pdf ? ExportPdf(entries) : ExportXlsx(entries);
    }

    private static Result<ExportResult> ExportPdf(IReadOnlyList<Entities.ScheduleEntry> entries)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Расписание занятий").SemiBold().FontSize(16).AlignCenter();

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(0.7f);
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("День").Bold();
                                h.Cell().Text("Предмет").Bold();
                                h.Cell().Text("Преподаватель").Bold();
                                h.Cell().Text("Пара").Bold();
                                h.Cell().Text("Ауд.").Bold();
                                h.Cell().Text("Начало").Bold();
                                h.Cell().Text("Конец").Bold();
                                h.Cell().Text("Группа").Bold();
                                h.Cell().Text("Тип").Bold();
                                h.Cell().Text("Недели").Bold();
                            });

                            foreach (var e in entries)
                            {
                                table
                                    .Cell()
                                    .Text(
                                        DaysMap.GetValueOrDefault(e.DayOfWeek, e.DayOfWeek.ToString())
                                    );
                                table.Cell().Text(e.Subject);
                                table.Cell().Text(e.Teacher?.User?.FullName ?? "");
                                table.Cell().Text(e.NumberPair.ToString());
                                table.Cell().Text(e.Room);
                                table.Cell().Text(e.StartTime.ToString(@"hh\:mm"));
                                table.Cell().Text(e.EndTime.ToString(@"hh\:mm"));
                                table.Cell().Text(e.Group?.Name ?? "");
                                table.Cell().Text(e.LessonType.ToString());
                                table.Cell().Text(string.Join(", ", e.Weeks));
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
                FileName = $"schedule_{DateTime.UtcNow:yyyyMMdd}.pdf",
            }
        );
    }

    private static Result<ExportResult> ExportXlsx(IReadOnlyList<Entities.ScheduleEntry> entries)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(1, 1).Value = "День";
        ws.Cell(1, 2).Value = "Предмет";
        ws.Cell(1, 3).Value = "Преподаватель";
        ws.Cell(1, 4).Value = "Пара";
        ws.Cell(1, 5).Value = "Аудитория";
        ws.Cell(1, 6).Value = "Начало";
        ws.Cell(1, 7).Value = "Конец";
        ws.Cell(1, 8).Value = "Группа";
        ws.Cell(1, 9).Value = "Тип занятия";
        ws.Cell(1, 10).Value = "Недели";

        var headerRange = ws.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var e in entries)
        {
            ws.Cell(row, 1).Value = DaysMap.GetValueOrDefault(e.DayOfWeek, e.DayOfWeek.ToString());
            ws.Cell(row, 2).Value = e.Subject;
            ws.Cell(row, 3).Value = e.Teacher?.User?.FullName ?? "";
            ws.Cell(row, 4).Value = e.NumberPair;
            ws.Cell(row, 5).Value = e.Room;
            ws.Cell(row, 6).Value = e.StartTime.ToString(@"hh\:mm");
            ws.Cell(row, 7).Value = e.EndTime.ToString(@"hh\:mm");
            ws.Cell(row, 8).Value = e.Group?.Name ?? "";
            ws.Cell(row, 9).Value = e.LessonType.ToString();
            ws.Cell(row, 10).Value = string.Join(", ", e.Weeks);
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return Result<ExportResult>.Ok(
            new ExportResult
            {
                FileContent = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"schedule_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            }
        );
    }
}
