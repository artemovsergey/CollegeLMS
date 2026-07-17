using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class ScheduleImportService(AppDbContext db)
{
    private static readonly Dictionary<string, DayOfWeek> DayMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["пн"] = DayOfWeek.Monday,
        ["понедельник"] = DayOfWeek.Monday,
        ["monday"] = DayOfWeek.Monday,
        ["вт"] = DayOfWeek.Tuesday,
        ["вторник"] = DayOfWeek.Tuesday,
        ["tuesday"] = DayOfWeek.Tuesday,
        ["ср"] = DayOfWeek.Wednesday,
        ["среда"] = DayOfWeek.Wednesday,
        ["wednesday"] = DayOfWeek.Wednesday,
        ["чт"] = DayOfWeek.Thursday,
        ["четверг"] = DayOfWeek.Thursday,
        ["thursday"] = DayOfWeek.Thursday,
        ["пт"] = DayOfWeek.Friday,
        ["пятница"] = DayOfWeek.Friday,
        ["friday"] = DayOfWeek.Friday,
        ["сб"] = DayOfWeek.Saturday,
        ["суббота"] = DayOfWeek.Saturday,
        ["saturday"] = DayOfWeek.Saturday,
        ["вс"] = DayOfWeek.Sunday,
        ["воскресенье"] = DayOfWeek.Sunday,
        ["sunday"] = DayOfWeek.Sunday,
    };

    private static readonly Dictionary<string, LessonType> LessonTypeMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["лекция"] = LessonType.Lecture,
        ["лек"] = LessonType.Lecture,
        ["lecture"] = LessonType.Lecture,
        ["практика"] = LessonType.Practice,
        ["пр"] = LessonType.Practice,
        ["practice"] = LessonType.Practice,
        ["лабораторная"] = LessonType.Lab,
        ["лаб"] = LessonType.Lab,
        ["lab"] = LessonType.Lab,
        ["экзамен"] = LessonType.Exam,
        ["экз"] = LessonType.Exam,
        ["exam"] = LessonType.Exam,
    };

    public async Task<Result<ScheduleImportResult>> ImportAsync(
        Stream fileStream,
        CancellationToken ct
    )
    {
        var result = new ScheduleImportResult();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return Result<ScheduleImportResult>.Fail(
                "Не удалось прочитать файл. Убедитесь, что это XLSX-файл.",
                400
            );
        }

        using (workbook)
        {
            var ws = workbook.Worksheet(1);
            if (ws is null)
                return Result<ScheduleImportResult>.Fail("Файл не содержит листов.", 400);

            var rows = ws.RowsUsed().Skip(1).ToList();
            if (rows.Count == 0)
                return Result<ScheduleImportResult>.Fail("Файл не содержит данных.", 400);

            var entriesToAdd = new List<ScheduleEntry>();

            foreach (var (row, index) in rows.Select((r, i) => (r, i)))
            {
                var rowNum = index + 2;
                var parsed = TryParseRow(row, rowNum, result);
                if (parsed is not null)
                    entriesToAdd.Add(parsed);
            }

            if (entriesToAdd.Count > 0)
            {
                db.ScheduleEntries.AddRange(entriesToAdd);
                await db.SaveChangesAsync(ct);
                result.Imported = entriesToAdd.Count;
            }
        }

        return Result<ScheduleImportResult>.Ok(result);
    }

    private static ScheduleEntry? TryParseRow(IXLRow row, int rowNum, ScheduleImportResult result)
    {
        var groupName = row.Cell(1).GetString().Trim();
        var numberPairStr = row.Cell(2).GetString().Trim();
        var subject = row.Cell(3).GetString().Trim();
        var room = row.Cell(4).GetString().Trim();
        var dayStr = row.Cell(5).GetString().Trim();
        var startStr = row.Cell(6).GetString().Trim();
        var endStr = row.Cell(7).GetString().Trim();
        var lessonTypeStr = row.Cell(8).GetString().Trim();
        var weeksStr = row.Cell(9).GetString().Trim();

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

        if (!int.TryParse(numberPairStr, out var numberPair) || numberPair < 1 || numberPair > 8)
            errors.Add($"Некорректный номер пары: '{numberPairStr}' (должен быть от 1 до 8)");

        var weeks = new List<int>();
        if (!string.IsNullOrEmpty(weeksStr))
        {
            foreach (var part in weeksStr.Split(',', StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var w) && w >= 1 && w <= 52)
                    weeks.Add(w);
                else
                    errors.Add($"Некорректная неделя: '{part}'");
            }
        }
        if (weeks.Count == 0)
            errors.Add("Укажите хотя бы одну неделю");

        if (errors.Count > 0)
        {
            result.Errors.Add(
                new ImportError { Row = rowNum, Message = string.Join("; ", errors) }
            );
            result.Skipped++;
            return null;
        }

        return new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Room = room,
            DayOfWeek = dayOfWeek,
            NumberPair = numberPair,
            StartTime = startTime,
            EndTime = endTime,
            Weeks = weeks,
            LessonType = lessonType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
