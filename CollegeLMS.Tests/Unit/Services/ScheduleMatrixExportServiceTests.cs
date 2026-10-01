using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Выгрузка расписания в формате файла импорта: главное свойство — файл должен
/// разбираться тем же парсером, что и исходный «Расписание.xlsx», без потерь.
/// </summary>
public class ScheduleMatrixExportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleMatrixExportService _sut;
    private readonly ScheduleImportService _parser;

    public ScheduleMatrixExportServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleMatrixExportService(_db);
        _parser = new ScheduleImportService(_db, new BellScheduleServiceStub());
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync(string name)
    {
        var now = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Course = 2,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    private async Task<Teacher> SeedTeacherAsync(string fullName)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@test.ru",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "Не указана",
            Position = "Преподаватель",
            CreatedAt = now,
            UpdatedAt = now,
            User = user,
        };
        _db.Users.Add(user);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return teacher;
    }

    private async Task<ScheduleEntry> SeedEntryAsync(
        Group group,
        Teacher? teacher,
        DayOfWeek day,
        int pair,
        string subject,
        string room,
        int[] weeks
    )
    {
        var now = DateTime.UtcNow;
        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            Group = group,
            TeacherId = teacher?.Id,
            Teacher = teacher,
            Subject = subject,
            Room = room,
            DayOfWeek = day,
            NumberPair = pair,
            StartTime = new TimeSpan(8, 30, 0),
            EndTime = new TimeSpan(10, 0, 0),
            Weeks = [.. weeks],
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    private static (
        string Group,
        int Pair,
        string Subject,
        string Room,
        string Teacher,
        string Weeks
    ) Parsed(string group, int pair, string subject, string room, string teacher, string weeks) =>
        (group, pair, subject, room, teacher, weeks);

    [Fact]
    public async Task ExportAsync_FailsWhenScheduleIsEmpty()
    {
        await SeedGroupAsync("РЭУ 252");

        var result = await _sut.ExportAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("не загружено");
    }

    [Fact]
    public async Task ExportAsync_FailsWhenThereAreNoGroups()
    {
        var now = DateTime.UtcNow;
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Subject = "Математика",
                Room = "233",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                Weeks = [1],
                CreatedAt = now,
                UpdatedAt = now,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ExportAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExportAsync_UsesTimestampedFileName()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Monday, 1, "Математика", "233", [1, 2]);

        var result = await _sut.ExportAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FileName.Should().MatchRegex(@"^Расписание_\d{6}_\d{4}\.xlsx$");
        result.Data.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportedFile_IsParsedBackWithoutLoss()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var other = await SeedGroupAsync("ПО 262");
        var math = await SeedTeacherAsync("Сапрыкина А.А.");
        var physics = await SeedTeacherAsync("Минаева Т.В.");

        var seeded = new List<ScheduleEntry>
        {
            await SeedEntryAsync(group, math, DayOfWeek.Monday, 1, "Математика", "233", [1, 2]),
            await SeedEntryAsync(group, math, DayOfWeek.Monday, 1, "Математика", "233", [3, 4]),
            await SeedEntryAsync(group, physics, DayOfWeek.Tuesday, 2, "Физика", "404", [1, 2, 3]),
            await SeedEntryAsync(other, math, DayOfWeek.Monday, 1, "Информатика", "301", [1]),
        };

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var (entries, errors) = _parser.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty("выгрузка должна разбираться без ошибок");
        entries.Should().HaveCount(seeded.Count);

        var expected = seeded
            .Select(e =>
                Parsed(
                    e.Group!.Name,
                    e.NumberPair,
                    e.Subject,
                    e.Room,
                    e.Teacher!.User.FullName,
                    string.Join(",", e.Weeks.OrderBy(w => w))
                )
            )
            .OrderBy(x => x, Comparer<(string, int, string, string, string, string)>.Default)
            .ToList();

        var actual = entries
            .Select(e =>
                Parsed(
                    e.GroupName,
                    e.Pair,
                    e.Subject,
                    e.Room,
                    e.TeacherName ?? string.Empty,
                    string.Join(",", e.Weeks.OrderBy(w => w))
                )
            )
            .OrderBy(x => x, Comparer<(string, int, string, string, string, string)>.Default)
            .ToList();

        actual.Should().Equal(expected);
    }

    [Fact]
    public async Task ExportedFile_OrderOfRoomsAndTeachersDoesNotDependOnRowOrder()
    {
        // Порядок строк в БД не гарантирован: выгрузка обязана быть одинаковой
        // при любом порядке, иначе аудитория разъезжается с преподавателем —
        // парсер импорта соединяет списки позиционно.
        var group = await SeedGroupAsync("РЭУ 252");
        var first = await SeedTeacherAsync("Степаненко О.А.");
        var second = await SeedTeacherAsync("Рахимова А.Л.");
        await SeedEntryAsync(group, first, DayOfWeek.Monday, 1, "Ин.язык", "413", [1, 2, 5]);
        await SeedEntryAsync(group, second, DayOfWeek.Monday, 1, "Ин.язык", "302", [1, 2, 5]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);
        var cell = sheet
            .Row(6)
            .CellsUsed()
            .First(c => c.GetString().Contains("Ин.язык", StringComparison.Ordinal));

        // Аудитории и преподаватели отсортированы по имени преподавателя,
        // поэтому пара «413 — Степаненко» не переворачивается.
        cell.GetString().Should().Be("302/413 Ин.язык (1-2,5) Рахимова А.Л./Степаненко О.А.");

        // Повторный разбор даёт те же пары, что и в базе.
        var (entries, errors) = _parser.ParseScheduleMatrix(workbook);
        errors.Should().BeEmpty();
        entries
            .Select(e => (e.Room, e.TeacherName))
            .Should()
            .BeEquivalentTo([("302", "Рахимова А.Л."), ("413", "Степаненко О.А.")]);
    }

    [Fact]
    public async Task ExportedFile_SameRoomWithTwoTeachersKeepsBothPairs()
    {
        // Одна аудитория, два преподавателя: парсер добивает более короткий
        // список первым элементом, поэтому пара должна восстановиться верно.
        var group = await SeedGroupAsync("РЭУ 252");
        var first = await SeedTeacherAsync("Рахимова А.Л.");
        var second = await SeedTeacherAsync("Степаненко О.А.");
        await SeedEntryAsync(group, first, DayOfWeek.Monday, 1, "Ин.язык", "302", [1, 2, 5]);
        await SeedEntryAsync(group, second, DayOfWeek.Monday, 1, "Ин.язык", "302", [1, 2, 5]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var (entries, errors) = _parser.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries
            .Select(e => (e.Room, e.TeacherName))
            .Should()
            .BeEquivalentTo([("302", "Рахимова А.Л."), ("302", "Степаненко О.А.")]);
    }

    [Fact]
    public async Task ExportedFile_PartiallyNamedTeachersAreNotWrittenAtAll()
    {
        // Часть пар без ФИО: список преподавателей сместил бы остальные пары,
        // поэтому в ячейке аудитории пишутся без преподавателей.
        var group = await SeedGroupAsync("РЭУ 252");
        var named = await SeedTeacherAsync("Рахимова А.Л.");
        await SeedEntryAsync(group, named, DayOfWeek.Monday, 1, "Ин.язык", "302", [1, 2, 5]);
        await SeedEntryAsync(group, null, DayOfWeek.Monday, 1, "Ин.язык", "413", [1, 2, 5]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);
        var cell = sheet
            .Row(6)
            .CellsUsed()
            .First(c => c.GetString().Contains("Ин.язык", StringComparison.Ordinal));

        cell.GetString().Should().Be("302/413 Ин.язык (1-2,5)");
    }

    [Fact]
    public async Task ExportedFile_CombinesSharedSubjectIntoOneCell()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var first = await SeedTeacherAsync("Рахимова А.Л.");
        var second = await SeedTeacherAsync("Степаненко О.А.");

        await SeedEntryAsync(group, first, DayOfWeek.Monday, 1, "Ин.язык", "302", [1, 2, 5]);
        await SeedEntryAsync(group, second, DayOfWeek.Monday, 1, "Ин.язык", "413", [1, 2, 5]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);
        var cell = sheet
            .Row(6)
            .CellsUsed()
            .First(c => c.GetString().Contains("Ин.язык", StringComparison.Ordinal));

        cell.GetString().Should().Be("302/413 Ин.язык (1-2,5) Рахимова А.Л./Степаненко О.А.");

        // Одна ячейка — две позиции после обратного разбора.
        var (entries, errors) = _parser.ParseScheduleMatrix(workbook);
        errors.Should().BeEmpty();
        entries.Should().HaveCount(2);
        entries
            .Select(e => (e.Room, e.TeacherName))
            .Should()
            .BeEquivalentTo([("302", "Рахимова А.Л."), ("413", "Степаненко О.А.")]);
    }

    [Fact]
    public async Task ExportedFile_FallsBackForLessonsWithoutRoomAndTeacher()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        await SeedEntryAsync(group, null, DayOfWeek.Monday, 1, "Математика", "", [1]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var (entries, errors) = _parser.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().ContainSingle();
        entries[0].Subject.Should().Be("Математика");
        // Прочерк в файле означает «аудитории нет» и не превращается в аудиторию «—».
        entries[0].Room.Should().BeEmpty();
        entries[0].TeacherName.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExportedFile_PutsGroupNamesInRowFiveAndDayInColumnA()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Wednesday, 3, "Математика", "233", [1]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);

        sheet.Cell(5, 3).GetString().Should().Be("РЭУ 252");
        sheet.Cell(6, 1).GetString().Should().Be("СРЕДА");
        sheet.Cell(6, 2).Value.IsNumber.Should().BeTrue();
        sheet.Cell(6, 2).GetDouble().Should().Be(3);
    }

    [Fact]
    public async Task ExportedFile_StylesHeaderAndDataRows()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Monday, 1, "Математика", "233", [1, 2]);
        await SeedEntryAsync(group, teacher, DayOfWeek.Monday, 2, "Физика", "404", [1, 2]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);

        sheet.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        sheet.Cell(5, 3).Style.Font.Bold.Should().BeTrue();
        sheet.Cell(6, 3).Style.Alignment.WrapText.Should().BeTrue();
        // Сверху тела таблицы — средняя рамка, внутри — тонкие линии.
        sheet.Cell(6, 3).Style.Border.TopBorder.Should().Be(XLBorderStyleValues.Medium);
        sheet.Cell(7, 3).Style.Border.TopBorder.Should().Be(XLBorderStyleValues.Thin);
    }

    [Fact]
    public async Task ExportedFile_UsesTimesNewRomanInsteadOfThemeCalibri()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Monday, 1, "Математика", "233", [1, 2]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);

        // Ни одна заполненная ячейка не должна остаться в Calibri из темы
        // ClosedXML: шрифт задаётся явно на всю область, включая пустые ячейки
        // строк и столбцов, которые иначе унаследовали бы шрифт по умолчанию.
        var used = sheet.RangeUsed();
        used.Should().NotBeNull();
        used!
            .Cells()
            .Select(cell => cell.Style.Font.FontName)
            .Distinct()
            .Should()
            .BeEquivalentTo(["Times New Roman"]);

        // Заголовок и данные сохраняют собственные размеры.
        sheet.Cell(1, 1).Style.Font.FontSize.Should().Be(14);
        sheet.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
    }

    [Fact]
    public async Task ExportedFile_FreezesDayAndPairColumns()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Monday, 1, "Математика", "233", [1, 2]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var view = workbook.Worksheet(1).SheetView;

        // Столбцы A (день) и B (номер пары) должны оставаться видимыми при
        // горизонтальной прокрутке, строки шапки — при вертикальной.
        view.SplitColumn.Should().Be(2);
        view.SplitRow.Should().Be(5);
    }

    [Fact]
    public async Task ExportedFile_OmitsDaysAndPairsWithoutLessons()
    {
        var group = await SeedGroupAsync("РЭУ 252");
        var teacher = await SeedTeacherAsync("Сапрыкина А.А.");
        await SeedEntryAsync(group, teacher, DayOfWeek.Friday, 4, "Математика", "233", [1]);

        var result = await _sut.ExportAsync(CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(result.Data!.Content));
        var sheet = workbook.Worksheet(1);

        var used = sheet
            .RowsUsed()
            .Where(r => r.RowNumber() > 5)
            .Select(r => r.Cell(1).GetString())
            .Where(v => v.Length > 0)
            .Distinct()
            .ToList();

        used.Should().BeEquivalentTo(["ПЯТНИЦА"]);
        sheet.Cell(6, 2).GetDouble().Should().Be(4);
    }
}
