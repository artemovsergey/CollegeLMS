using System.Text;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public interface IScheduleMatrixExportService
{
    Task<Result<DocumentDownloadResult>> ExportAsync(CancellationToken ct);
}

/// <summary>
/// Выгрузка расписания в формате файла импорта «Расписание.xlsx».
///
/// Источник — таблица `schedule_entries`, то есть уже применённые корректировки.
/// Собирается та же матрица, которую понимает `ScheduleImportService`, поэтому
/// выгрузку можно загрузить обратно без ручной правки.
/// </summary>
public class ScheduleMatrixExportService(AppDbContext db) : IScheduleMatrixExportService
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Номер строки с названиями групп — как в исходном файле.</summary>
    private const int HeaderRow = 5;

    /// <summary>Первый столбец группы: A — день, B — номер пары.</summary>
    private const int FirstGroupColumn = 3;

    private const int MaxPair = 8;

    /// <summary>
    /// Токен занятия без аудитории. Формат файла требует его перед предметом,
    /// а парсер импорта трактует его как «аудитории нет», поэтому в базу он
    /// не попадает и при обратном импорте выгрузки.
    /// </summary>
    private const string RoomPlaceholder = ScheduleImportService.NoRoomPlaceholder;

    private static readonly string[] DayNames =
    [
        "ПОНЕДЕЛЬНИК",
        "ВТОРНИК",
        "СРЕДА",
        "ЧЕТВЕРГ",
        "ПЯТНИЦА",
        "СУББОТА",
    ];

    public async Task<Result<DocumentDownloadResult>> ExportAsync(CancellationToken ct)
    {
        var groups = await db
            .Groups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct);

        if (groups.Count == 0)
            return Result<DocumentDownloadResult>.Fail(
                "В системе нет групп — расписание выгружать нечего.",
                400
            );

        var entries = await db
            .ScheduleEntries.AsNoTracking()
            .Include(e => e.Teacher!)
                .ThenInclude(t => t.User)
            .Where(e => e.NumberPair >= 1 && e.NumberPair <= MaxPair)
            .ToListAsync(ct);

        if (entries.Count == 0)
            return Result<DocumentDownloadResult>.Fail(
                "Расписание не загружено: нет ни одной пары. Импортируйте файл расписания.",
                400
            );

        var columnByGroup = new Dictionary<Guid, int>();
        for (var i = 0; i < groups.Count; i++)
            columnByGroup[groups[i].Id] = FirstGroupColumn + i;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Расписание");
        var lastColumn = FirstGroupColumn + groups.Count - 1;

        WriteTitle(sheet, lastColumn);
        for (var i = 0; i < groups.Count; i++)
            sheet.Cell(HeaderRow, FirstGroupColumn + i).Value = groups[i].Name;

        var row = HeaderRow + 1;
        for (var dayIndex = 0; dayIndex < DayNames.Length; dayIndex++)
        {
            var day = (DayOfWeek)(dayIndex + 1);
            var dayEntries = entries.Where(e => e.DayOfWeek == day).ToList();
            if (dayEntries.Count == 0)
                continue;

            var dayStartRow = row;
            for (var pair = 1; pair <= MaxPair; pair++)
            {
                var pairEntries = dayEntries.Where(e => e.NumberPair == pair).ToList();
                if (pairEntries.Count == 0)
                    continue;

                // Объединённые компоненты по группам: одинаковые предмет с
                // одинаковыми неделями — одна ячейка с перечислением через слеш.
                var cells = new Dictionary<Guid, List<string>>();
                foreach (var group in groups)
                {
                    var parts = Combine(pairEntries.Where(e => e.GroupId == group.Id).ToList());
                    if (parts.Count > 0)
                        cells[group.Id] = parts;
                }

                var lines = cells.Count == 0 ? 1 : cells.Values.Max(v => v.Count);
                var pairStartRow = row;

                for (var line = 0; line < lines; line++)
                {
                    foreach (var group in groups)
                    {
                        if (!cells.TryGetValue(group.Id, out var parts) || line >= parts.Count)
                            continue;
                        var column = columnByGroup[group.Id];
                        sheet.Cell(row + line, column).Value = parts[line];
                    }
                }

                sheet.Cell(pairStartRow, 2).Value = pair;
                row += lines;
            }

            // День занимает весь свой блок: объединяем и центрируем по вертикали.
            if (row - 1 > dayStartRow)
                sheet.Range(dayStartRow, 1, row - 1, 1).Merge();
            sheet.Cell(dayStartRow, 1).Value = DayNames[dayIndex];
        }

        ApplyLayout(sheet, lastColumn, HeaderRow);
        StyleData(sheet, HeaderRow, row - 1, lastColumn);

        using var output = new MemoryStream();
        workbook.SaveAs(output);

        // FILE-3: сначала дата, потом время.
        var stamp = DateTime.Now.ToString("ddMMyy_HHmm");
        return Result<DocumentDownloadResult>.Ok(
            new DocumentDownloadResult
            {
                Content = output.ToArray(),
                ContentType = XlsxContentType,
                FileName = $"Расписание_{stamp}.xlsx",
            }
        );
    }

    /// <summary>
    /// Схлопывает занятия одной пары в компоненты: одинаковый предмет с одинаковыми
    /// неделями становится одной ячейкой «ауд/ауд Предмет (недели) Преп./Преп.».
    /// </summary>
    private static List<string> Combine(List<ScheduleEntry> entries)
    {
        var groups = new Dictionary<string, List<ScheduleEntry>>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var key =
                $"{ScheduleImportService.SubjectLookupKey(entry.Subject)}|{WeeksKey(entry.Weeks)}";
            if (!groups.TryGetValue(key, out var bucket))
            {
                bucket = [];
                groups[key] = bucket;
            }
            bucket.Add(entry);
        }

        return groups
            .Values.Select(bucket =>
            {
                var rooms = new List<string>();
                foreach (var entry in bucket)
                {
                    var room = string.IsNullOrWhiteSpace(entry.Room)
                        ? RoomPlaceholder
                        : entry.Room.Trim();
                    if (!rooms.Contains(room))
                        rooms.Add(room);
                }

                var teachers = new List<string>();
                foreach (var entry in bucket)
                {
                    var name = entry.Teacher?.User.FullName;
                    if (string.IsNullOrWhiteSpace(name) || teachers.Contains(name))
                        continue;
                    teachers.Add(name);
                }

                var weeks = bucket[0].Weeks;
                var text = new StringBuilder();
                text.Append(string.Join('/', rooms));
                text.Append(' ').Append(ScheduleImportService.NormalizeSubject(bucket[0].Subject));
                if (weeks.Count > 0)
                    text.Append(" (").Append(FormatWeeks(weeks)).Append(')');
                // Преподавателя без ФИО не пишем: парсер импорта опознаёт преподавателя
                // только по шаблону «Фамилия И.О.», и подстановка попала бы в предмет.
                if (teachers.Count > 0)
                    text.Append(' ').Append(string.Join('/', teachers));
                return text.ToString();
            })
            .OrderBy(FirstWeek)
            .ThenBy(text => text, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int FirstWeek(string cellText)
    {
        var open = cellText.IndexOf('(');
        if (open < 0)
            return int.MaxValue;
        var close = cellText.IndexOf(')', open);
        if (close < 0)
            return int.MaxValue;
        var first = cellText[(open + 1)..close].Split(',')[0].Split('-')[0];
        return int.TryParse(first, out var week) ? week : int.MaxValue;
    }

    private static string WeeksKey(IEnumerable<int> weeks) =>
        string.Join(",", weeks.Distinct().OrderBy(w => w));

    /// <summary>Недели в компактной форме: 1–6, 8, 10 → «1-6,8,10».</summary>
    private static string FormatWeeks(IEnumerable<int> source)
    {
        var weeks = source.Distinct().OrderBy(w => w).ToList();
        if (weeks.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        var start = weeks[0];
        var previous = weeks[0];
        foreach (var week in weeks.Skip(1))
        {
            if (week == previous + 1)
            {
                previous = week;
                continue;
            }

            parts.Add(FormatRun(start, previous));
            start = previous = week;
        }
        parts.Add(FormatRun(start, previous));
        return string.Join(",", parts);
    }

    private static string FormatRun(int start, int end) =>
        start == end ? $"{start}" : $"{start}-{end}";

    private static void WriteTitle(IXLWorksheet sheet, int lastColumn)
    {
        var semesterEnd = StudyWeek
            .SemesterStart.AddDays(StudyWeek.TotalWeeks * 7 - 3)
            .ToString("dd.MM.yyyy");

        sheet.Range(1, 1, 1, lastColumn).Merge();
        sheet.Cell(1, 1).Value = "РАСПИСАНИЕ УЧЕБНЫХ ЗАНЯТИЙ";

        sheet.Range(2, 1, 2, lastColumn).Merge();
        sheet.Cell(2, 1).Value =
            $"на период с {StudyWeek.SemesterStart:dd.MM.yyyy} по {semesterEnd} "
            + $"({StudyWeek.TotalWeeks} недель)";

        sheet.Range(3, 1, 3, lastColumn).Merge();
        sheet.Cell(3, 1).Value =
            $"сформировано {DateTime.Now:dd.MM.yyyy HH:mm} из данных системы "
            + "с учётом применённых корректировок";

        var title = sheet.Range(1, 1, 3, lastColumn);
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        title.Style.Font.FontName = "Times New Roman";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        sheet.Row(1).Height = 24;
        sheet.Row(2).Height = 20;
        sheet.Row(3).Height = 20;
        sheet.Row(4).Height = 8;
    }

    private static void ApplyLayout(IXLWorksheet sheet, int lastColumn, int headerRow)
    {
        sheet.Column(1).Width = 14;
        sheet.Column(2).Width = 6;
        for (var column = FirstGroupColumn; column <= lastColumn; column++)
            sheet.Column(column).Width = 26;

        var header = sheet.Range(headerRow, FirstGroupColumn, headerRow, lastColumn);
        header.Style.Font.Bold = true;
        header.Style.Font.FontSize = 12;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Alignment.WrapText = true;
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Row(headerRow).Height = 26;

        sheet.SheetView.FreezeRows(headerRow);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
    }

    /// <summary>Оформление блоков данных: рамка, выравнивание, перенос, ориентация.</summary>
    private static void StyleData(IXLWorksheet sheet, int headerRow, int lastRow, int lastColumn)
    {
        if (lastRow <= headerRow)
            return;

        var body = sheet.Range(headerRow + 1, 1, lastRow, lastColumn);
        body.Style.Font.FontName = "Times New Roman";
        body.Style.Font.FontSize = 10;
        body.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        body.Style.Alignment.WrapText = true;
        body.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        body.Style.Border.InsideBorderColor = XLColor.Black;
        body.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        body.Style.Border.OutsideBorderColor = XLColor.Black;

        var dayColumn = sheet.Range(headerRow + 1, 1, lastRow, 1);
        dayColumn.Style.Font.Bold = true;
        dayColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        dayColumn.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        dayColumn.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8edf2");

        var pairColumn = sheet.Range(headerRow + 1, 2, lastRow, 2);
        pairColumn.Style.Font.Bold = true;
        pairColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        pairColumn.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        for (var row = headerRow + 1; row <= lastRow; row++)
            sheet.Row(row).Height = 15.75;
    }
}
