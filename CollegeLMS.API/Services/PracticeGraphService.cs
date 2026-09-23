using System.Globalization;
using System.Text.RegularExpressions;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

/// <summary>Импорт и экспорт графика учебной практики (УП) в формате DOCX.</summary>
public class PracticeGraphService(AppDbContext db, IConfiguration config) : IPracticeGraphService
{
    private const int MinPairNumber = 1;
    private const int MaxPairNumber = 8;
    private const int MaxRoomLength = 20;
    private const string Sheet = "График УП";
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private static readonly Regex DateRegex = new(@"\d{2}\.\d{2}\.\d{4}", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"\d+", RegexOptions.Compiled);

    private readonly string _templatePath =
        config["PracticeGraphTemplatePath"]
        ?? Path.Combine("..", "import", "templates", "Шаблон графика УП.docx");

    public async Task<Result<PracticeGraphPreviewResponse>> PreviewImportAsync(
        Stream docx,
        CancellationToken ct
    )
    {
        PracticeGraphPreviewResponse preview;
        using (var document = WordprocessingDocument.Open(docx, false))
        {
            preview = Parse(document);
        }

        if (preview.Rows.Count == 0 && preview.Errors.Count == 0)
            preview.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = "В файле не найдено ни одной строки графика.",
                }
            );

        await ValidatePreviewAsync(preview, ct);
        preview.TotalRows = preview.Rows.Count;

        return Result<PracticeGraphPreviewResponse>.Ok(preview);
    }

    public async Task<Result<PracticeGraphConfirmResponse>> ConfirmImportAsync(
        PracticeGraphConfirmRequest request,
        CancellationToken ct
    )
    {
        if (request.Rows.Count == 0)
            return Result<PracticeGraphConfirmResponse>.Fail("Нет строк для импорта.", 400);

        if (request.DateFrom == default || request.DateTo == default)
            return Result<PracticeGraphConfirmResponse>.Fail("Укажите период практики.", 400);

        if (request.DateFrom.Date > request.DateTo.Date)
            return Result<PracticeGraphConfirmResponse>.Fail(
                "Дата начала практики не может быть позже даты окончания.",
                400
            );

        if (!StudyWeek.IsInSemester(request.DateFrom) || !StudyWeek.IsInSemester(request.DateTo))
            return Result<PracticeGraphConfirmResponse>.Fail(
                "Период практики должен быть в пределах семестра.",
                400
            );

        var lookups = await BuildLookupsAsync(ct);
        var group = lookups.Groups.FirstOrDefault(g =>
            ScheduleImportService.GroupLookupKey(g.Name)
            == ScheduleImportService.GroupLookupKey(request.GroupName)
        );
        if (group is null)
            return Result<PracticeGraphConfirmResponse>.Fail("Группа не найдена.", 400);

        var now = DateTime.UtcNow;
        var errors = new List<string>();
        var practices = new List<Practice>();

        foreach (var row in request.Rows)
        {
            var teacherId = ScheduleImportService.ResolveTeacherId(
                row.TeacherName,
                lookups.TeachersByFullName,
                lookups.TeachersBySurname
            );
            if (teacherId is null)
            {
                errors.Add($"Преподаватель «{row.TeacherName}» не найден (строка {row.Row}).");
                continue;
            }

            var name = string.IsNullOrWhiteSpace(row.Name) ? request.Name : row.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"В строке {row.Row} не указана тема УП.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.Room) && row.Room.Trim().Length > MaxRoomLength)
            {
                errors.Add(
                    $"Номер кабинета не должен превышать {MaxRoomLength} символов (строка {row.Row})."
                );
                continue;
            }

            if (row.Days.Count == 0)
            {
                errors.Add($"В строке {row.Row} нет учебных дней.");
                continue;
            }

            var days = new List<PracticeDay>();
            var seenDates = new HashSet<DateTime>();
            var rowValid = true;
            foreach (var day in row.Days)
            {
                var date = day.Date.Date;
                if (date < request.DateFrom.Date || date > request.DateTo.Date)
                {
                    errors.Add($"Дата {date:dd.MM.yyyy} вне периода практики (строка {row.Row}).");
                    rowValid = false;
                    break;
                }

                var numbers = (day.PairNumbers ?? [])
                    .Where(n => n > 0)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToArray();
                if (numbers.Length == 0)
                {
                    errors.Add($"В строке {row.Row} не указаны пары на {date:dd.MM.yyyy}.");
                    rowValid = false;
                    break;
                }

                if (numbers.Any(n => n < MinPairNumber || n > MaxPairNumber))
                {
                    errors.Add($"Номера пар должны быть от 1 до 8 (строка {row.Row}).");
                    rowValid = false;
                    break;
                }

                if (!seenDates.Add(date))
                {
                    errors.Add($"Дата {date:dd.MM.yyyy} повторяется в строке {row.Row}.");
                    rowValid = false;
                    break;
                }

                days.Add(
                    new PracticeDay
                    {
                        Id = Guid.NewGuid(),
                        Date = date,
                        PairNumbers = numbers,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }
                );
            }

            if (!rowValid)
                continue;

            var practice = new Practice
            {
                Id = Guid.NewGuid(),
                Kind = PracticeKind.Up,
                Name = name.Trim(),
                GroupId = group.Id,
                DateFrom = request.DateFrom.Date,
                DateTo = request.DateTo.Date,
                Note = null,
                Room = string.IsNullOrWhiteSpace(row.Room) ? null : row.Room.Trim(),
                Subgroup = row.Subgroup,
                CreatedAt = now,
                UpdatedAt = now,
            };
            practice.Teachers =
            [
                new PracticeTeacher
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId.Value,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            ];
            practice.Days = days;
            practices.Add(practice);
        }

        if (errors.Count > 0)
            return Result<PracticeGraphConfirmResponse>.Fail(string.Join(" ", errors), 400);

        var conflict = await db
            .Practices.AsNoTracking()
            .AnyAsync(
                p =>
                    p.GroupId == group.Id
                    && p.DateFrom <= request.DateTo.Date
                    && p.DateTo >= request.DateFrom.Date
                    && !(
                        p.Kind == PracticeKind.Up
                        && p.DateFrom == request.DateFrom.Date
                        && p.DateTo == request.DateTo.Date
                    ),
                ct
            );
        if (conflict)
            return Result<PracticeGraphConfirmResponse>.Fail(
                "У группы уже есть практика в этот период.",
                409
            );

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var existing = await db
            .Practices.Where(p =>
                p.GroupId == group.Id
                && p.Kind == PracticeKind.Up
                && p.DateFrom == request.DateFrom.Date
                && p.DateTo == request.DateTo.Date
            )
            .ToListAsync(ct);
        db.Practices.RemoveRange(existing);
        db.Practices.AddRange(practices);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var ids = practices.Select(p => p.Id).ToList();
        var created = await db
            .Practices.AsNoTracking()
            .Include(p => p.Group)
            .Include(p => p.Teachers)
                .ThenInclude(t => t.Teacher!)
                    .ThenInclude(t => t.User)
            .Include(p => p.Days)
            .Where(p => ids.Contains(p.Id))
            .OrderBy(p => p.Subgroup)
            .ToListAsync(ct);

        return Result<PracticeGraphConfirmResponse>.Ok(
            new PracticeGraphConfirmResponse
            {
                Imported = created.Count,
                Practices = created.Select(p => p.ToDto()).ToList(),
            }
        );
    }

    public async Task<Result<DocumentDownloadResult>> ExportAsync(
        Guid groupId,
        CancellationToken ct
    )
    {
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return Result<DocumentDownloadResult>.Fail("Группа не найдена.", 404);

        var practices = await db
            .Practices.AsNoTracking()
            .Include(p => p.Teachers)
                .ThenInclude(t => t.Teacher!)
                    .ThenInclude(t => t.User)
            .Include(p => p.Days)
            .Where(p => p.GroupId == groupId && p.Kind == PracticeKind.Up)
            .OrderBy(p => p.Subgroup)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
        if (practices.Count == 0)
            return Result<DocumentDownloadResult>.Fail(
                "У группы нет учебных практик для выгрузки.",
                404
            );

        if (!File.Exists(_templatePath))
            return Result<DocumentDownloadResult>.Fail("Файл шаблона отсутствует на сервере.", 404);

        var dateFrom = practices.Min(p => p.DateFrom);
        var dateTo = practices.Max(p => p.DateTo);
        var practiceName =
            practices.Select(p => p.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? string.Empty;

        using var output = new MemoryStream();
        using (var template = File.OpenRead(_templatePath))
            await template.CopyToAsync(output, ct);
        output.Position = 0;

        using (var document = WordprocessingDocument.Open(output, true))
        {
            var body = document.MainDocumentPart!.Document.Body!;

            foreach (var paragraph in body.Elements<Paragraph>().ToList())
            {
                var text = ParagraphText(paragraph);
                if (text.Contains("в период с", StringComparison.OrdinalIgnoreCase))
                    SetParagraphText(
                        paragraph,
                        $"График проведения занятий в период с {dateFrom:dd.MM.yyyy} г. по {dateTo:dd.MM.yyyy} г."
                    );
                else if (text.StartsWith("по ", StringComparison.OrdinalIgnoreCase))
                    SetParagraphText(paragraph, $"по {practiceName}");
                else if (text.Contains("в группе", StringComparison.OrdinalIgnoreCase))
                    SetParagraphText(paragraph, $"в группе {group.Name}");
            }

            var table = body.Elements<Table>().FirstOrDefault();
            if (table is not null)
                FillTable(table, practices);

            document.MainDocumentPart.Document.Save();
        }

        return Result<DocumentDownloadResult>.Ok(
            new DocumentDownloadResult
            {
                Content = output.ToArray(),
                ContentType = DocxContentType,
                FileName = $"График УП {group.Name} {DateTime.UtcNow:dd.MM.yyyy_HH-mm-ss}.docx",
            }
        );
    }

    private static PracticeGraphPreviewResponse Parse(WordprocessingDocument document)
    {
        var result = new PracticeGraphPreviewResponse();
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            result.Errors.Add(
                new ScheduleValidationError { Sheet = Sheet, Message = "Документ пуст." }
            );
            return result;
        }

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = ParagraphText(paragraph);
            if (text.Contains("в период с", StringComparison.OrdinalIgnoreCase))
            {
                var dates = DateRegex
                    .Matches(text)
                    .Select(m => ParseDate(m.Value))
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .ToList();
                if (dates.Count >= 2)
                {
                    result.DateFrom = dates[0];
                    result.DateTo = dates[^1];
                }
            }
            else if (
                result.PracticeName is null
                && text.StartsWith("по ", StringComparison.OrdinalIgnoreCase)
            )
            {
                result.PracticeName = text[3..].Trim();
            }
            else if (
                result.GroupName is null
                && text.Contains("в группе", StringComparison.OrdinalIgnoreCase)
            )
            {
                var index = text.IndexOf("в группе", StringComparison.OrdinalIgnoreCase);
                result.GroupName = text[(index + "в группе".Length)..].Trim();
            }
        }

        var table = body.Elements<Table>().FirstOrDefault();
        if (table is null)
        {
            result.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = "В документе не найдена таблица графика.",
                }
            );
            return result;
        }

        var rows = table.Elements<TableRow>().Skip(1).ToList();
        var rowNumber = 0;
        foreach (var row in rows)
        {
            rowNumber++;
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count < 6)
            {
                result.Errors.Add(
                    new ScheduleValidationError
                    {
                        Sheet = Sheet,
                        Row = rowNumber,
                        Message = "Строка таблицы содержит меньше 6 колонок.",
                    }
                );
                continue;
            }

            var subgroupText = CellText(cells[0]);
            var dates = CellLines(cells[3]).Select(ParseDate).ToList();
            var pairs = CellLines(cells[4]).Select(ParsePairNumbers).ToList();

            var graphRow = new PracticeGraphRow
            {
                Row = rowNumber,
                Subgroup = int.TryParse(subgroupText, out var subgroup) ? subgroup : null,
                Name = CellText(cells[1]),
                Room = Normalize(CellText(cells[2])),
                TeacherName = CellText(cells[5]),
                Days = [],
            };

            if (dates.Count != pairs.Count)
            {
                result.Errors.Add(
                    new ScheduleValidationError
                    {
                        Sheet = Sheet,
                        Row = rowNumber,
                        Message = "Количество дат и строк с парами не совпадает.",
                    }
                );
            }
            else
            {
                for (var i = 0; i < dates.Count; i++)
                {
                    if (dates[i] is null)
                    {
                        result.Errors.Add(
                            new ScheduleValidationError
                            {
                                Sheet = Sheet,
                                Row = rowNumber,
                                Message = $"Не удалось разобрать дату «{CellLines(cells[3])[i]}».",
                            }
                        );
                        continue;
                    }

                    graphRow.Days.Add(
                        new PracticeGraphDay
                        {
                            Date = dates[i]!.Value,
                            PairNumbers = pairs[i] ?? [],
                        }
                    );
                }
            }

            result.Rows.Add(graphRow);
        }

        if (result.DateFrom is null && result.Rows.SelectMany(r => r.Days).Any())
        {
            var allDates = result.Rows.SelectMany(r => r.Days).Select(d => d.Date).ToList();
            result.DateFrom = allDates.Min();
            result.DateTo = allDates.Max();
        }

        return result;
    }

    private async Task ValidatePreviewAsync(
        PracticeGraphPreviewResponse preview,
        CancellationToken ct
    )
    {
        var lookups = await BuildLookupsAsync(ct);

        if (string.IsNullOrWhiteSpace(preview.GroupName))
            preview.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = "Не удалось определить группу из шапки графика.",
                }
            );
        else if (
            !lookups.Groups.Any(g =>
                ScheduleImportService.GroupLookupKey(g.Name)
                == ScheduleImportService.GroupLookupKey(preview.GroupName)
            )
        )
            preview.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = $"Группа «{preview.GroupName}» не найдена.",
                }
            );

        if (preview.DateFrom is null || preview.DateTo is null)
            preview.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = "Не удалось определить период практики.",
                }
            );
        else if (
            !StudyWeek.IsInSemester(preview.DateFrom.Value)
            || !StudyWeek.IsInSemester(preview.DateTo.Value)
        )
            preview.Errors.Add(
                new ScheduleValidationError
                {
                    Sheet = Sheet,
                    Message = "Период практики должен быть в пределах семестра.",
                }
            );

        foreach (var row in preview.Rows)
        {
            if (
                string.IsNullOrWhiteSpace(row.TeacherName)
                || ScheduleImportService.ResolveTeacherId(
                    row.TeacherName,
                    lookups.TeachersByFullName,
                    lookups.TeachersBySurname
                )
                    is null
            )
                preview.Errors.Add(
                    new ScheduleValidationError
                    {
                        Sheet = Sheet,
                        Row = row.Row,
                        Message = $"Преподаватель «{row.TeacherName}» не найден.",
                    }
                );

            if (row.Days.Count == 0)
                preview.Errors.Add(
                    new ScheduleValidationError
                    {
                        Sheet = Sheet,
                        Row = row.Row,
                        Message = "В строке нет учебных дней.",
                    }
                );

            var seenDates = new HashSet<DateTime>();
            foreach (var day in row.Days)
            {
                if (preview.DateFrom is not null && preview.DateTo is not null)
                {
                    if (
                        day.Date < preview.DateFrom.Value.Date
                        || day.Date > preview.DateTo.Value.Date
                    )
                        preview.Errors.Add(
                            new ScheduleValidationError
                            {
                                Sheet = Sheet,
                                Row = row.Row,
                                Message = $"Дата {day.Date:dd.MM.yyyy} вне периода практики.",
                            }
                        );
                }

                var numbers = day.PairNumbers ?? [];
                if (numbers.Count == 0)
                    preview.Errors.Add(
                        new ScheduleValidationError
                        {
                            Sheet = Sheet,
                            Row = row.Row,
                            Message = $"Не указаны пары на {day.Date:dd.MM.yyyy}.",
                        }
                    );
                else if (numbers.Any(n => n < MinPairNumber || n > MaxPairNumber))
                    preview.Errors.Add(
                        new ScheduleValidationError
                        {
                            Sheet = Sheet,
                            Row = row.Row,
                            Message = "Номера пар должны быть от 1 до 8.",
                        }
                    );

                if (!seenDates.Add(day.Date.Date))
                    preview.Errors.Add(
                        new ScheduleValidationError
                        {
                            Sheet = Sheet,
                            Row = row.Row,
                            Message = $"Дата {day.Date:dd.MM.yyyy} повторяется.",
                        }
                    );
            }
        }
    }

    private async Task<GraphLookups> BuildLookupsAsync(CancellationToken ct)
    {
        var groups = await db
            .Groups.AsNoTracking()
            .Select(g => new GroupRef(g.Id, g.Name))
            .ToListAsync(ct);

        var teachers = await db
            .Teachers.AsNoTracking()
            .Select(t => new TeacherRef(t.Id, t.User!.FullName))
            .ToListAsync(ct);

        var byFullName = teachers
            .GroupBy(t => ScheduleImportService.TeacherLookupKey(t.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);
        var bySurname = teachers
            .GroupBy(t => ScheduleImportService.SurnameKey(t.Name))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        return new GraphLookups(groups, byFullName, bySurname);
    }

    private static void FillTable(Table table, List<Practice> practices)
    {
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0)
            return;

        var template = rows.Skip(1).FirstOrDefault();
        if (template is null)
            return;

        var dataRows = rows.Skip(1).ToList();
        while (dataRows.Count > practices.Count)
        {
            dataRows[^1].Remove();
            dataRows.RemoveAt(dataRows.Count - 1);
        }
        while (dataRows.Count < practices.Count)
        {
            var clone = (TableRow)template.CloneNode(true);
            table.AppendChild(clone);
            dataRows.Add(clone);
        }

        for (var i = 0; i < practices.Count; i++)
        {
            var practice = practices[i];
            var cells = dataRows[i].Elements<TableCell>().ToList();
            if (cells.Count < 6)
                continue;

            var days = practice.Days.OrderBy(d => d.Date).ToList();
            SetCellLines(cells[0], [(practice.Subgroup ?? (i + 1)).ToString()]);
            SetCellLines(cells[1], [practice.Name]);
            SetCellLines(cells[2], [practice.Room ?? string.Empty]);
            SetCellLines(cells[3], days.Select(d => d.Date.ToString("dd.MM.yyyy")).ToList());
            SetCellLines(cells[4], days.Select(d => FormatPairs(d.PairNumbers)).ToList());
            SetCellLines(
                cells[5],
                [
                    string.Join(
                        " / ",
                        practice
                            .Teachers.OrderBy(t => t.Teacher?.User?.FullName ?? string.Empty)
                            .Select(t => t.Teacher?.User?.FullName ?? string.Empty)
                            .Where(n => n.Length > 0)
                    ),
                ]
            );
        }
    }

    private static string FormatPairs(int[] numbers) =>
        numbers.Length == 0
            ? "—"
            : $"{string.Join(",", numbers)} {(numbers.Length == 1 ? "пара" : "пары")}";

    private static void SetCellLines(TableCell cell, IReadOnlyList<string> lines)
    {
        var paragraphs = cell.Elements<Paragraph>().ToList();
        var template = paragraphs.FirstOrDefault() ?? new Paragraph();
        var runProps =
            template.Elements<Run>().FirstOrDefault()?.RunProperties?.CloneNode(true)
            as RunProperties;

        foreach (var paragraph in paragraphs)
            paragraph.Remove();

        var content = lines.Count == 0 ? [string.Empty] : lines;
        foreach (var line in content)
        {
            var paragraph = (Paragraph)template.CloneNode(true);
            ReplaceParagraphText(paragraph, line, runProps);
            cell.AppendChild(paragraph);
        }
    }

    private static void SetParagraphText(Paragraph paragraph, string text)
    {
        var runProps =
            paragraph.Elements<Run>().FirstOrDefault()?.RunProperties?.CloneNode(true)
            as RunProperties;
        ReplaceParagraphText(paragraph, text, runProps);
    }

    private static void ReplaceParagraphText(
        Paragraph paragraph,
        string text,
        RunProperties? runProps
    )
    {
        foreach (var run in paragraph.Elements<Run>().ToList())
            run.Remove();

        var newRun = new Run();
        if (runProps is not null)
            newRun.AppendChild(runProps.CloneNode(true));
        newRun.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        paragraph.AppendChild(newRun);
    }

    private static string ParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));

    private static List<string> CellLines(TableCell cell) =>
        cell.Elements<Paragraph>().Select(p => ParagraphText(p).Trim()).ToList();

    private static string CellText(TableCell cell) =>
        string.Join(" ", CellLines(cell).Where(line => line.Length > 0)).Trim();

    private static List<int> ParsePairNumbers(string text) =>
        NumberRegex
            .Matches(text)
            .Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture))
            .ToList();

    private static DateTime? ParseDate(string text)
    {
        var match = DateRegex.Match(text);
        if (!match.Success)
            return null;

        return DateTime.TryParseExact(
            match.Value,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
        )
            ? date.Date
            : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record GroupRef(Guid Id, string Name);

    private sealed record TeacherRef(Guid Id, string Name);

    private sealed record GraphLookups(
        List<GroupRef> Groups,
        Dictionary<string, Guid> TeachersByFullName,
        Dictionary<string, List<Guid>> TeachersBySurname
    );
}
