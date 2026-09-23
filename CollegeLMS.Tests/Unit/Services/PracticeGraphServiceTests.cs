using System.Text;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CollegeLMS.Tests.Unit.Services;

public class PracticeGraphServiceTests : IDisposable
{
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly AppDbContext _db;
    private readonly PracticeGraphService _sut;

    public PracticeGraphServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new PracticeGraphService(_db, BuildConfig(TemplatePath()));
    }

    public void Dispose() => _db.Dispose();

    // ---------- Инфраструктура ----------

    private static string TemplatePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "import",
                "templates",
                "Шаблон графика УП.docx"
            );
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Не найден шаблон import/templates/Шаблон графика УП.docx."
        );
    }

    private static IConfiguration BuildConfig(string? templatePath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["PracticeGraphTemplatePath"] = templatePath }
            )
            .Build();

    private async Task<Group> SeedGroupAsync(string name = "ПО-262")
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Course = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    private async Task<Teacher> SeedTeacherAsync(string fullName = "Марченко И.А.")
    {
        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@collegelms.ru",
            FullName = fullName,
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "ЦК",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = user,
        };
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return teacher;
    }

    private async Task<Practice> SeedUpPracticeAsync(
        Group group,
        Teacher teacher,
        string name = "УП.01 Модуль",
        string? room = "202л",
        int? subgroup = 1,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int[]? pairNumbers = null
    )
    {
        var utcNow = DateTime.UtcNow;
        var from = (dateFrom ?? new DateTime(2026, 9, 7)).Date;
        var to = (dateTo ?? new DateTime(2026, 9, 11)).Date;
        var practice = new Practice
        {
            Id = Guid.NewGuid(),
            Kind = PracticeKind.Up,
            Name = name,
            GroupId = group.Id,
            DateFrom = from,
            DateTo = to,
            Room = room,
            Subgroup = subgroup,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Teachers =
            [
                new PracticeTeacher
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacher.Id,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
            ],
            Days =
            [
                new PracticeDay
                {
                    Id = Guid.NewGuid(),
                    Date = from,
                    PairNumbers = pairNumbers ?? [1, 2, 3],
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
            ],
        };
        _db.Practices.Add(practice);
        await _db.SaveChangesAsync();
        return practice;
    }

    private static MemoryStream BuildGraphDocx(
        string period,
        string practiceName,
        string groupName,
        (
            string Subgroup,
            string Name,
            string Room,
            string[] Dates,
            string[] Pairs,
            string Teacher
        )[] rows,
        bool includeTable = true
    )
    {
        var stream = new MemoryStream();
        using (
            var document = WordprocessingDocument.Create(
                stream,
                WordprocessingDocumentType.Document
            )
        )
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());

            body.AppendChild(TextParagraph(period));
            body.AppendChild(TextParagraph(practiceName));
            body.AppendChild(TextParagraph(groupName));

            if (includeTable)
            {
                var table = new Table();
                var header = new TableRow();
                foreach (
                    var text in new[]
                    {
                        "№ подгруппы",
                        "Наименование темы УП",
                        "Номер кабинета",
                        "Даты",
                        "Время занятий (пары)",
                        "преподаватель",
                    }
                )
                    header.AppendChild(TextCell(text));
                table.AppendChild(header);

                foreach (var row in rows)
                    table.AppendChild(
                        DataRow(row.Subgroup, row.Name, row.Room, row.Dates, row.Pairs, row.Teacher)
                    );

                body.AppendChild(table);
            }

            main.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static TableRow DataRow(
        string subgroup,
        string name,
        string room,
        string[] dates,
        string[] pairs,
        string teacher
    )
    {
        var row = new TableRow();
        row.AppendChild(TextCell(subgroup));
        row.AppendChild(TextCell(name));
        row.AppendChild(TextCell(room));
        row.AppendChild(TextCell(dates));
        row.AppendChild(TextCell(pairs));
        row.AppendChild(TextCell(teacher));
        return row;
    }

    private static Paragraph TextParagraph(string text) =>
        new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

    private static TableCell TextCell(params string[] lines)
    {
        var cell = new TableCell();
        var content = lines.Length == 0 ? [string.Empty] : lines;
        foreach (var line in content)
            cell.AppendChild(TextParagraph(line));
        return cell;
    }

    private static PracticeGraphConfirmRequest ConfirmRequest(
        string groupName,
        string name,
        DateTime from,
        DateTime to,
        params PracticeGraphRow[] rows
    ) =>
        new()
        {
            GroupName = groupName,
            Name = name,
            DateFrom = from,
            DateTo = to,
            Rows = rows.ToList(),
        };

    private static PracticeGraphRow ConfirmRow(
        int row,
        int subgroup,
        string name,
        string room,
        string teacher,
        DateTime date,
        int[] pairNumbers
    ) =>
        new()
        {
            Row = row,
            Subgroup = subgroup,
            Name = name,
            Room = room,
            TeacherName = teacher,
            Days = [new PracticeGraphDay { Date = date, PairNumbers = pairNumbers.ToList() }],
        };

    // ---------- Preview: разбор ----------

    [Fact]
    public async Task PreviewImportAsync_ValidDocx_ParsesHeaderRowsSubgroupsRoomsAndPairs()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Разработка программных модулей",
            "в группе ПО-262",
            [
                (
                    "1",
                    "УП.01 Модуль 1",
                    "202л",
                    ["07.09.2026", "08.09.2026"],
                    ["1,2,3 пары", "1,2 пары"],
                    teacher.User.FullName
                ),
                (
                    "2",
                    "УП.01 Модуль 2",
                    "203л",
                    ["09.09.2026"],
                    ["3,4,5 пары"],
                    teacher.User.FullName
                ),
            ]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var preview = result.Data!;
        preview.Errors.Should().BeEmpty();
        preview.GroupName.Should().Be(group.Name);
        preview.PracticeName.Should().Be("УП.01 Разработка программных модулей");
        preview.DateFrom.Should().Be(new DateTime(2026, 9, 7));
        preview.DateTo.Should().Be(new DateTime(2026, 9, 11));
        preview.TotalRows.Should().Be(2);
        preview.Rows.Should().HaveCount(2);

        var first = preview.Rows[0];
        first.Subgroup.Should().Be(1);
        first.Room.Should().Be("202л");
        first.Name.Should().Be("УП.01 Модуль 1");
        first.TeacherName.Should().Be("Марченко И.А.");
        first.Days.Should().HaveCount(2);
        first.Days[0].Date.Should().Be(new DateTime(2026, 9, 7));
        first.Days[0].PairNumbers.Should().Equal(1, 2, 3);
        first.Days[1].PairNumbers.Should().Equal(1, 2);

        var second = preview.Rows[1];
        second.Subgroup.Should().Be(2);
        second.Room.Should().Be("203л");
        second.Days.Should().ContainSingle();
        second.Days[0].PairNumbers.Should().Equal(3, 4, 5);
    }

    [Fact]
    public async Task PreviewImportAsync_DateAndPairCountMismatch_ReportsError()
    {
        await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Модуль",
            "в группе ПО-262",
            [
                (
                    "1",
                    "УП.01 Модуль",
                    "202л",
                    ["07.09.2026", "08.09.2026"],
                    ["1,2 пары"],
                    teacher.User.FullName
                ),
            ]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Errors.Should()
            .Contain(e => e.Message.Contains("Количество дат и строк с парами не совпадает"));
    }

    [Theory]
    [InlineData("0 пара")]
    [InlineData("9 пара")]
    public async Task PreviewImportAsync_PairNumberOutsideRange_ReportsError(string pairText)
    {
        await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Модуль",
            "в группе ПО-262",
            [("1", "УП.01 Модуль", "202л", ["07.09.2026"], [pairText], teacher.User.FullName)]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Errors.Should()
            .Contain(e => e.Message.Contains("Номера пар должны быть от 1 до 8"));
    }

    [Fact]
    public async Task PreviewImportAsync_UnknownGroup_ReportsError()
    {
        await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Модуль",
            "в группе НЕТ-ТАКОЙ",
            [("1", "УП.01 Модуль", "202л", ["07.09.2026"], ["1,2 пары"], teacher.User.FullName)]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Errors.Should()
            .Contain(e => e.Message.Contains("«НЕТ-ТАКОЙ»") && e.Message.Contains("не найдена"));
    }

    [Fact]
    public async Task PreviewImportAsync_UnknownTeacher_ReportsError()
    {
        await SeedGroupAsync("ПО-262");
        await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Модуль",
            "в группе ПО-262",
            [("1", "УП.01 Модуль", "202л", ["07.09.2026"], ["1,2 пары"], "Нет Такого")]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Errors.Should()
            .Contain(e => e.Message.Contains("«Нет Такого»") && e.Message.Contains("не найден"));
    }

    [Fact]
    public async Task PreviewImportAsync_NoTable_ReportsError()
    {
        await SeedGroupAsync("ПО-262");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 07.09.2026 г. по 11.09.2026 г.",
            "по УП.01 Модуль",
            "в группе ПО-262",
            [],
            includeTable: false
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().Contain(e => e.Message.Contains("не найдена таблица графика"));
    }

    [Fact]
    public async Task PreviewImportAsync_OutsideSemester_ReportsError()
    {
        await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildGraphDocx(
            "График проведения занятий в период с 01.02.2027 г. по 05.02.2027 г.",
            "по УП.01 Модуль",
            "в группе ПО-262",
            [("1", "УП.01 Модуль", "202л", ["01.02.2027"], ["1,2 пары"], teacher.User.FullName)]
        );

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Errors.Should()
            .Contain(e => e.Message.Contains("Период практики должен быть в пределах семестра"));
    }

    // ---------- Confirm: создание ----------

    [Fact]
    public async Task ConfirmImportAsync_ValidRows_CreatesPracticesWithSubgroupsRoomsAndDays()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль 1",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 7),
                [1, 2, 3]
            ),
            ConfirmRow(
                2,
                2,
                "УП.01 Модуль 2",
                "203л",
                teacher.User.FullName,
                new DateTime(2026, 9, 8),
                [3, 4]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(2);
        result.Data.Practices.Select(p => p.Subgroup).Should().Equal(1, 2);
        result.Data.Practices[0].GroupName.Should().Be(group.Name);
        result.Data.Practices[0].Room.Should().Be("202л");
        result.Data.Practices[0].Days.Should().ContainSingle();
        result.Data.Practices[0].Days[0].PairNumbers.Should().Equal(1, 2, 3);
        result.Data.Practices[1].Room.Should().Be("203л");
        result.Data.Practices[1].Days[0].PairNumbers.Should().Equal(3, 4);

        (await _db.Practices.CountAsync()).Should().Be(2);
        (await _db.PracticeDays.CountAsync()).Should().Be(2);
        (await _db.PracticeTeachers.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ConfirmImportAsync_ReplacesExistingIdenticalPeriodUp()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var existing = await SeedUpPracticeAsync(group, teacher, name: "Старая УП", subgroup: 9);

        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 7),
                [1, 2]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(1);
        var ids = await _db.Practices.Select(p => p.Id).ToListAsync();
        ids.Should().NotContain(existing.Id);
        ids.Should().ContainSingle();
    }

    [Fact]
    public async Task ConfirmImportAsync_ConflictsWithPp_Returns409()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var utcNow = DateTime.UtcNow;
        _db.Practices.Add(
            new Practice
            {
                Id = Guid.NewGuid(),
                Kind = PracticeKind.Pp,
                Name = "ПП 09",
                GroupId = group.Id,
                DateFrom = new DateTime(2026, 9, 7),
                DateTo = new DateTime(2026, 9, 11),
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();

        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 7),
                [1, 2]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("уже есть практика");
        (await _db.Practices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ConfirmImportAsync_UnknownTeacher_Returns400AndCreatesNothing()
    {
        var group = await SeedGroupAsync("ПО-262");
        await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(1, 1, "УП.01 Модуль", "202л", "Нет Такого", new DateTime(2026, 9, 7), [1, 2])
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("«Нет Такого» не найден");
        (await _db.Practices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmImportAsync_DayOutsidePeriod_Returns400()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 20),
                [1, 2]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("вне периода практики");
    }

    [Fact]
    public async Task ConfirmImportAsync_PairNumberOutsideRange_Returns400()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 7),
                [9]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("от 1 до 8");
    }

    [Fact]
    public async Task ConfirmImportAsync_UnknownGroup_Returns400()
    {
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            "НЕТ-ТАКОЙ",
            "УП.01 Модуль",
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 11),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 9, 7),
                [1, 2]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Группа не найдена");
    }

    [Fact]
    public async Task ConfirmImportAsync_OutsideSemester_Returns400()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        var request = ConfirmRequest(
            group.Name,
            "УП.01 Модуль",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 5),
            ConfirmRow(
                1,
                1,
                "УП.01 Модуль",
                "202л",
                teacher.User.FullName,
                new DateTime(2026, 8, 1),
                [1, 2]
            )
        );

        var result = await _sut.ConfirmImportAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("в пределах семестра");
    }

    [Fact]
    public async Task ConfirmImportAsync_EmptyRows_Returns400()
    {
        var result = await _sut.ConfirmImportAsync(
            new PracticeGraphConfirmRequest
            {
                GroupName = "ПО-262",
                Name = "УП.01 Модуль",
                DateFrom = new DateTime(2026, 9, 7),
                DateTo = new DateTime(2026, 9, 11),
                Rows = [],
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Нет строк");
    }

    // ---------- Export ----------

    [Fact]
    public async Task ExportAsync_ExistingUpPractice_ReturnsDocx()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        await SeedUpPracticeAsync(group, teacher);

        var result = await _sut.ExportAsync(group.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ContentType.Should().Be(DocxContentType);
        result.Data.Content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(result.Data.Content, 0, 2).Should().Be("PK");
        result.Data.FileName.Should().Contain(group.Name).And.EndWith(".docx");
    }

    [Fact]
    public async Task ExportAsync_GroupNotFound_Returns404()
    {
        var result = await _sut.ExportAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Группа не найдена");
    }

    [Fact]
    public async Task ExportAsync_NoUpPractices_Returns404()
    {
        var group = await SeedGroupAsync("ПО-262");

        var result = await _sut.ExportAsync(group.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("нет учебных практик");
    }

    [Fact]
    public async Task ExportAsync_TemplateMissing_Returns404()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        await SeedUpPracticeAsync(group, teacher);
        var sut = new PracticeGraphService(_db, BuildConfig("Z:\\нет-такого-шаблона.docx"));

        var result = await sut.ExportAsync(group.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("шаблона");
    }

    [Fact]
    public async Task ExportAsync_ThenPreview_RoundTripsGraph()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        await SeedUpPracticeAsync(
            group,
            teacher,
            name: "УП.01 Разработка программных модулей",
            room: "202л",
            subgroup: 1,
            pairNumbers: [1, 2, 3]
        );

        var exported = await _sut.ExportAsync(group.Id, CancellationToken.None);
        exported.IsSuccess.Should().BeTrue();

        using var stream = new MemoryStream(exported.Data!.Content);
        var preview = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        preview.IsSuccess.Should().BeTrue();
        var data = preview.Data!;
        data.Errors.Should().BeEmpty();
        data.GroupName.Should().Be(group.Name);
        data.PracticeName.Should().Be("УП.01 Разработка программных модулей");
        data.DateFrom.Should().Be(new DateTime(2026, 9, 7));
        data.DateTo.Should().Be(new DateTime(2026, 9, 11));
        data.Rows.Should().ContainSingle();

        var row = data.Rows[0];
        row.Subgroup.Should().Be(1);
        row.Room.Should().Be("202л");
        row.TeacherName.Should().Be("Марченко И.А.");
        row.Days.Should().ContainSingle();
        row.Days[0].PairNumbers.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ExportAsync_TwoSubgroups_WritesBothRows()
    {
        var group = await SeedGroupAsync("ПО-262");
        var teacher = await SeedTeacherAsync("Марченко И.А.");
        await SeedUpPracticeAsync(
            group,
            teacher,
            name: "УП.01 Модуль",
            room: "202л",
            subgroup: 1,
            pairNumbers: [1, 2]
        );
        await SeedUpPracticeAsync(
            group,
            teacher,
            name: "УП.01 Модуль",
            room: "203л",
            subgroup: 2,
            dateFrom: new DateTime(2026, 9, 8),
            dateTo: new DateTime(2026, 9, 9),
            pairNumbers: [3, 4]
        );

        var exported = await _sut.ExportAsync(group.Id, CancellationToken.None);
        exported.IsSuccess.Should().BeTrue();

        using var stream = new MemoryStream(exported.Data!.Content);
        var preview = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        preview.IsSuccess.Should().BeTrue();
        preview.Data!.Errors.Should().BeEmpty();
        preview.Data.Rows.Should().HaveCount(2);
        preview.Data.Rows.Select(r => r.Subgroup).Should().Equal(1, 2);
        preview.Data.Rows[1].Days[0].PairNumbers.Should().Equal(3, 4);
    }
}
