using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

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
        new(8, 0, 0), new(9, 30, 0),
        new(9, 40, 0), new(11, 10, 0),
        new(11, 20, 0), new(12, 50, 0),
        new(13, 30, 0), new(15, 0, 0),
        new(15, 10, 0), new(16, 40, 0),
        new(16, 50, 0), new(18, 20, 0),
        new(18, 30, 0), new(20, 0, 0),
    ];

    public List<SchedulePreviewEntry> ParseScheduleMatrix(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheet(1);
        var entries = new List<SchedulePreviewEntry>();

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 3;
        var groupColumns = new Dictionary<int, string>();
        for (int col = 3; col <= lastCol; col++)
        {
            var name = ws.Cell(5, col).GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                groupColumns[col] = name;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var dayBlocks = new List<(int StartRow, DayOfWeek Day)>();
        for (int row = 1; row <= lastRow; row++)
        {
            var aVal = ws.Cell(row, 1).GetString().Trim().ToUpperInvariant();
            if (DayMap.TryGetValue(aVal, out var day))
                dayBlocks.Add((row, day));
        }

        for (int bi = 0; bi < dayBlocks.Count; bi++)
        {
            var (dayStart, day) = dayBlocks[bi];
            int dayEnd = bi + 1 < dayBlocks.Count
                ? dayBlocks[bi + 1].StartRow
                : lastRow + 1;

            var pairRows = new List<int>();
            for (int r = dayStart; r < dayEnd; r++)
            {
                var bVal = ws.Cell(r, 2).Value;
                if (bVal.IsNumber)
                {
                    var num = bVal.GetNumber();
                    if (num >= 1 && num <= 7)
                        pairRows.Add(r);
                }
            }

            for (int pi = 0; pi < pairRows.Count; pi++)
            {
                int pairRow = pairRows[pi];
                int pairNum = (int)ws.Cell(pairRow, 2).GetDouble();
                int nextPairRow = pi + 1 < pairRows.Count
                    ? pairRows[pi + 1]
                    : dayEnd;

                foreach (var (col, groupName) in groupColumns)
                {
                    for (int r = pairRow; r < nextPairRow; r++)
                    {
                        var cellText = ws.Cell(r, col).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(cellText))
                            continue;

                        var parsed = ParseSubjectCell(cellText);
                        if (string.IsNullOrEmpty(parsed.Subject))
                            continue;

                        entries.Add(new SchedulePreviewEntry
                        {
                            GroupName = groupName,
                            Day = day.ToString(),
                            Pair = pairNum,
                            Subject = parsed.Subject,
                            Room = parsed.Room,
                            TeacherName = parsed.Teacher,
                            Weeks = parsed.Weeks,
                            Status = "ok",
                        });
                    }
                }
            }
        }

        return entries;
    }

    private static readonly Regex SubjectCellRegex = new(
        @"^(?<room>[^\s]+)\s+(?<subject>[^(]+?)(?:\s*\((?<weeks>[^)]+)\))?\s*(?<teacher>[А-Яа-яёЁ][А-Яа-яёЁ.\s]*)?$",
        RegexOptions.Compiled);

    private static (string Room, string Subject, List<int> Weeks, string Teacher) ParseSubjectCell(string cell)
    {
        var text = cell.Trim();
        if (string.IsNullOrEmpty(text) || text == ".")
            return (string.Empty, string.Empty, [], string.Empty);

        if (text.StartsWith("ч.з", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("с.з", StringComparison.OrdinalIgnoreCase))
        {
            var roomPart = text.Split(' ', 2)[0].Trim();
            var rest = text[roomPart.Length..].Trim();
            var weeksMatch = Regex.Match(rest, @"\(([^)]+)\)");
            var weeks = weeksMatch.Success ? ParseWeeks(weeksMatch.Groups[1].Value) : new List<int>();
            var subject = Regex.Replace(rest, @"\([^)]*\)", "").Trim();
            var teacher = ExtractTrailingTeacher(subject);
            if (!string.IsNullOrEmpty(teacher))
                subject = subject[..^teacher.Length].TrimEnd();
            return (roomPart, subject, weeks, teacher);
        }

        var match = SubjectCellRegex.Match(text);
        if (match.Success)
        {
            var room = match.Groups["room"].Value;
            var subject = match.Groups["subject"].Value.Trim();
            var weeks = match.Groups["weeks"].Success
                ? ParseWeeks(match.Groups["weeks"].Value)
                : new List<int>();
            var teacher = match.Groups["teacher"].Value.Trim();
            if (string.IsNullOrEmpty(teacher))
                teacher = ExtractTrailingTeacher(subject);
            if (!string.IsNullOrEmpty(teacher) && subject.EndsWith(teacher))
                subject = subject[..^teacher.Length].TrimEnd();
            return (room, subject, weeks, teacher);
        }

        return (string.Empty, text, [], string.Empty);
    }

    private static string ExtractTrailingTeacher(string text)
    {
        var match = Regex.Match(text, @"([А-Яа-яёЁ][А-Яа-яёЁ]+\s+[А-Яа-яёЁ]\.[А-Яа-яёЁ]\.?)$");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static List<int> ParseWeeks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

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

    public async Task<PreviewResult> PreviewAsync(
        Stream fileStream, CancellationToken ct)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            return new PreviewResult
            {
                IsSuccess = false,
                ErrorMessage = "Не удалось прочитать файл. Убедитесь, что это XLSX-файл.",
            };
        }

        using (workbook)
        {
            var entries = ParseScheduleMatrix(workbook);

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

            return new PreviewResult
            {
                IsSuccess = true,
                Preview = new SchedulePreviewResponse
                {
                    TotalEntries = entries.Count,
                    ValidEntries = validCount,
                    WarningsCount = warnings.Sum(w => w.Count),
                    Warnings = warnings,
                    Entries = entries,
                },
            };
        }
    }

    public async Task<ConfirmResult> ConfirmAsync(
        ConfirmImportRequest request, CancellationToken ct)
    {
        var result = new ConfirmResult { IsSuccess = true };

        var existingGroups = await db.Groups
            .ToDictionaryAsync(g => g.Name, g => g.Id, ct);

        var existingTeachers = await db.Teachers
            .Include(t => t.User)
            .ToDictionaryAsync(t => t.User.FullName, t => t.Id, ct);

        var entriesToAdd = new List<ScheduleEntry>();

        foreach (var entry in request.Entries)
        {
            if (!existingGroups.TryGetValue(entry.GroupName, out var groupId))
            {
                if (request.CreateMissingGroups)
                {
                    var newGroup = new Entities.Group
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

            Guid? teacherId = null;
            if (!string.IsNullOrEmpty(entry.TeacherName))
            {
                if (!existingTeachers.TryGetValue(entry.TeacherName, out var tid))
                {
                    if (request.CreateMissingTeachers)
                    {
                        var user = new User
                        {
                            Id = Guid.NewGuid(),
                            FullName = entry.TeacherName,
                            Login = entry.TeacherName.Replace(" ", ".").ToLowerInvariant(),
                            Email = $"{entry.TeacherName.Replace(" ", ".").ToLowerInvariant()}@temp.local",
                            PasswordHash = "",
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

        return result;
    }
}

public class PreviewResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public SchedulePreviewResponse? Preview { get; set; }
}

public class ConfirmResult
{
    public bool IsSuccess { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = [];
}
