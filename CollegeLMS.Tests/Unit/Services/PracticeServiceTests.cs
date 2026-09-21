using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class PracticeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PracticeService _sut;

    public PracticeServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new PracticeService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync(string name = "ПО-262", int course = 2)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Course = course,
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

    private static List<PracticeDayRequest> UpDays(DateTime from, DateTime to)
    {
        var days = new List<PracticeDayRequest>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            days.Add(new PracticeDayRequest { Date = date, PairCount = 6 });
        }
        return days;
    }

    private static PracticeRequest Request(
        Guid groupId,
        Guid teacherId,
        PracticeKind kind = PracticeKind.Pp,
        DateTime? from = null,
        DateTime? to = null,
        string name = "УП 01",
        List<PracticeDayRequest>? days = null
    )
    {
        var dateFrom = from ?? new DateTime(2026, 9, 7);
        var dateTo = to ?? new DateTime(2026, 9, 11);
        return new()
        {
            Kind = kind,
            Name = name,
            GroupId = groupId,
            TeacherIds = [teacherId],
            DateFrom = dateFrom,
            DateTo = dateTo,
            Note = "примечание",
            Days = kind == PracticeKind.Up ? days ?? UpDays(dateFrom, dateTo) : null,
        };
    }

    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> fill)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Практики");
        ws.Cell(1, 1).Value = "Вид";
        ws.Cell(1, 2).Value = "Название";
        ws.Cell(1, 3).Value = "Группа";
        ws.Cell(1, 4).Value = "Дата начала";
        ws.Cell(1, 5).Value = "Дата окончания";
        ws.Cell(1, 6).Value = "Преподаватель";
        ws.Cell(1, 7).Value = "Примечание";
        fill(ws);

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        workbook.Dispose();
        return ms;
    }

    private static PracticeImportRow ValidRow(string groupName, string teacherName) =>
        new()
        {
            Row = 2,
            Kind = "УП",
            Name = "УП 01",
            GroupName = groupName,
            DateFrom = "07.09.2026",
            DateTo = "11.09.2026",
            TeacherName = teacherName,
            Note = "выезд",
        };

    // ---------- CRUD ----------

    [Fact]
    public async Task CreateAsync_ValidUp_ReturnsNameTeachersAndDays()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up, name: "  УП 01  "),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("УП 01");
        result.Data.GroupName.Should().Be(group.Name);
        result.Data.TeacherIds.Should().ContainSingle().Which.Should().Be(teacher.Id);
        result.Data.Teachers[0].Name.Should().Be(teacher.User.FullName);
        result.Data.Days.Should().NotBeEmpty();
        result.Data.Days.Should().OnlyContain(d => d.PairCount == 6);
        (await _db.Practices.CountAsync()).Should().Be(1);
        (await _db.PracticeTeachers.CountAsync()).Should().Be(1);
        (await _db.PracticeDays.CountAsync()).Should().Be(result.Data.Days.Count);
    }

    [Fact]
    public async Task CreateAsync_MultipleTeachers_ReturnsAll()
    {
        var group = await SeedGroupAsync();
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");

        var request = Request(group.Id, teacherA.Id, PracticeKind.Pp);
        request.TeacherIds = [teacherA.Id, teacherB.Id];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TeacherIds.Should().HaveCount(2).And.Contain([teacherA.Id, teacherB.Id]);
        result.Data.Teachers.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_NoTeachers_Returns400()
    {
        var group = await SeedGroupAsync();
        var request = Request(group.Id, Guid.NewGuid(), PracticeKind.Pp);
        request.TeacherIds = [];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("хотя бы одного преподавателя");
    }

    [Fact]
    public async Task CreateAsync_EmptyName_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Pp, name: "   "),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("название");
    }

    [Fact]
    public async Task CreateAsync_UpWithoutDays_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days = [];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("дни с числом пар");
    }

    [Fact]
    public async Task CreateAsync_DayOutsidePeriod_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days = [new PracticeDayRequest { Date = new DateTime(2026, 9, 20), PairCount = 6 }];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("вне периода");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task CreateAsync_InvalidPairCount_Returns400(int pairCount)
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairCount = pairCount },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("от 1 до 8");
    }

    [Fact]
    public async Task CreateAsync_DuplicateDays_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairCount = 6 },
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairCount = 4 },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("не должны повторяться");
    }

    [Fact]
    public async Task CreateAsync_GroupNotFound_Returns400()
    {
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(Guid.NewGuid(), teacher.Id),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Группа не найдена");
    }

    [Fact]
    public async Task CreateAsync_TeacherNotFound_Returns400()
    {
        var group = await SeedGroupAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Преподаватель не найден");
    }

    [Fact]
    public async Task CreateAsync_PeriodOutsideSemester_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 8, 1),
                to: new DateTime(2026, 8, 5)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("семестра");
    }

    [Fact]
    public async Task CreateAsync_DateFromAfterDateTo_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 9, 10),
                to: new DateTime(2026, 9, 5)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("позже даты окончания");
    }

    [Fact]
    public async Task CreateAsync_InvalidKind_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, kind: (PracticeKind)99),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("УП или ПП");
    }

    [Fact]
    public async Task CreateAsync_OverlappingPractice_Returns409()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 9, 9),
                to: new DateTime(2026, 9, 12)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("уже есть практика");
    }

    [Fact]
    public async Task CreateAsync_AnotherGroupSamePeriod_IsAllowed()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(Request(groupA.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.CreateAsync(Request(groupB.Id, teacher.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_SamePeriod_DoesNotConflictWithItselfAndReplacesDays()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var created = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );

        var update = Request(group.Id, teacher.Id, PracticeKind.Up, name: "УП 02");
        update.Days = [new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairCount = 3 }];

        var result = await _sut.UpdateAsync(created.Data!.Id, update, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("УП 02");
        result.Data.Days.Should().ContainSingle();
        result.Data.Days[0].PairCount.Should().Be(3);
        (await _db.PracticeDays.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Returns404()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.UpdateAsync(
            Guid.NewGuid(),
            Request(group.Id, teacher.Id),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesEntity()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var created = await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.DeleteAsync(created.Data!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Practices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Returns404()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByGroupAndKind()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(
            Request(groupA.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );
        await _sut.CreateAsync(
            Request(groupB.Id, teacher.Id, PracticeKind.Pp),
            CancellationToken.None
        );

        var result = await _sut.GetAllAsync(
            groupB.Id,
            null,
            PracticeKind.Pp,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].GroupName.Should().Be("ПО-263");
        result.Data.Items[0].Kind.Should().Be(PracticeKind.Pp);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByTeacherViaJoin()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");
        await _sut.CreateAsync(Request(groupA.Id, teacherA.Id), CancellationToken.None);
        await _sut.CreateAsync(Request(groupB.Id, teacherB.Id), CancellationToken.None);

        var result = await _sut.GetAllAsync(
            null,
            teacherA.Id,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].GroupName.Should().Be("ПО-262");
    }

    // ---------- Импорт XLSX ----------

    [Fact]
    public async Task PreviewImportAsync_ValidFile_ParsesRows()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "УП";
            ws.Cell(2, 2).Value = "УП 01";
            ws.Cell(2, 3).Value = group.Name;
            ws.Cell(2, 4).Value = "07.09.2026";
            ws.Cell(2, 5).Value = "11.09.2026";
            ws.Cell(2, 6).Value = teacher.User.FullName;
            ws.Cell(2, 7).Value = "выезд";
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalRows.Should().Be(1);
        result.Data.Errors.Should().BeEmpty();
        result.Data.Rows[0].Name.Should().Be("УП 01");
        result.Data.Rows[0].GroupName.Should().Be(group.Name);
        result.Data.Rows[0].Row.Should().Be(2);
    }

    [Fact]
    public async Task PreviewImportAsync_TeacherListWithSemicolon_ParsesAndValidates()
    {
        var group = await SeedGroupAsync();
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "ПП";
            ws.Cell(2, 2).Value = "ПП 09";
            ws.Cell(2, 3).Value = group.Name;
            ws.Cell(2, 4).Value = "07.09.2026";
            ws.Cell(2, 5).Value = "11.09.2026";
            ws.Cell(2, 6).Value = $"{teacherA.User.FullName}; {teacherB.User.FullName}";
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewImportAsync_UnknownTeacherInList_ReportsError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync("Марченко И.А.");

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "ПП";
            ws.Cell(2, 2).Value = "ПП 09";
            ws.Cell(2, 3).Value = group.Name;
            ws.Cell(2, 4).Value = "07.09.2026";
            ws.Cell(2, 5).Value = "11.09.2026";
            ws.Cell(2, 6).Value = $"{teacher.User.FullName}; Нет Такого";
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().ContainSingle();
        result.Data.Errors[0].Message.Should().Contain("«Нет Такого» не найден");
    }

    [Fact]
    public async Task PreviewImportAsync_InvalidRows_ReportsRowNumbers()
    {
        await SeedGroupAsync();
        await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "XX";
            ws.Cell(2, 2).Value = "";
            ws.Cell(2, 3).Value = "НЕТ-ТАКОЙ";
            ws.Cell(2, 4).Value = "не-дата";
            ws.Cell(2, 5).Value = "не-дата";
            ws.Cell(2, 6).Value = "Нет Такого";
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().NotBeEmpty();
        result.Data.Errors.Should().OnlyContain(e => e.Message.Contains("строка 2"));
    }

    [Fact]
    public async Task PreviewImportAsync_HeaderOnly_Returns400()
    {
        using var stream = BuildWorkbook(_ => { });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("нет данных");
    }

    [Fact]
    public async Task PreviewImportAsync_NotXlsx_Returns400()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("XLSX");
    }

    [Fact]
    public async Task ConfirmImportAsync_ValidRows_CreatesPracticesWithTeachers()
    {
        var group = await SeedGroupAsync();
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");
        var row = ValidRow(group.Name, $"{teacherA.User.FullName}; {teacherB.User.FullName}");
        row.Kind = "ПП";

        var result = await _sut.ConfirmImportAsync(
            new PracticeImportConfirmRequest { Rows = [row] },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(1);
        result.Data.Practices[0].GroupName.Should().Be(group.Name);
        result.Data.Practices[0].Name.Should().Be("УП 01");
        result.Data.Practices[0].Teachers.Should().HaveCount(2);
        result.Data.Practices[0].TeacherIds.Should().Contain([teacherA.Id, teacherB.Id]);
        (await _db.Practices.CountAsync()).Should().Be(1);
        (await _db.PracticeTeachers.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ConfirmImportAsync_UpRows_CreateDefaultDays()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.ConfirmImportAsync(
            new PracticeImportConfirmRequest
            {
                Rows = [ValidRow(group.Name, teacher.User.FullName)],
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var practice = result.Data!.Practices[0];
        practice.Days.Should().NotBeEmpty();
        practice.Days.Should().OnlyContain(d => d.PairCount == 6);
        (await _db.PracticeDays.CountAsync()).Should().Be(practice.Days.Count);
    }

    [Fact]
    public async Task ConfirmImportAsync_WithErrors_CreatesNothing()
    {
        await SeedGroupAsync();
        await SeedTeacherAsync();
        var invalid = ValidRow("НЕТ-ТАКОЙ", "Нет Такого");
        invalid.Kind = "XX";

        var result = await _sut.ConfirmImportAsync(
            new PracticeImportConfirmRequest { Rows = [invalid] },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        (await _db.Practices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmImportAsync_EmptyRows_Returns400()
    {
        var result = await _sut.ConfirmImportAsync(
            new PracticeImportConfirmRequest { Rows = [] },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Нет строк");
    }

    [Fact]
    public async Task PreviewImportAsync_OutsideSemester_ReportsError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "УП";
            ws.Cell(2, 2).Value = "УП 01";
            ws.Cell(2, 3).Value = group.Name;
            ws.Cell(2, 4).Value = "01.02.2027";
            ws.Cell(2, 5).Value = "05.02.2027";
            ws.Cell(2, 6).Value = teacher.User.FullName;
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().ContainSingle();
        result.Data.Errors[0].Message.Should().Contain("в пределах семестра");
    }

    [Fact]
    public async Task PreviewImportAsync_OverlappingRowsInFile_ReportsError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "УП";
            ws.Cell(2, 2).Value = "УП 01";
            ws.Cell(2, 3).Value = group.Name;
            ws.Cell(2, 4).Value = "07.09.2026";
            ws.Cell(2, 5).Value = "11.09.2026";
            ws.Cell(2, 6).Value = teacher.User.FullName;
            ws.Cell(3, 1).Value = "ПП";
            ws.Cell(3, 2).Value = "ПП 09";
            ws.Cell(3, 3).Value = group.Name;
            ws.Cell(3, 4).Value = "10.09.2026";
            ws.Cell(3, 5).Value = "14.09.2026";
            ws.Cell(3, 6).Value = teacher.User.FullName;
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Errors.Should().ContainSingle();
        result.Data.Errors[0].Message.Should().Contain("строка 3");
        result.Data.Errors[0].Message.Should().Contain("пересекается со строкой 2");
    }

    [Fact]
    public async Task ConfirmImportAsync_OverlapsExistingPractice_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        _db.Practices.Add(
            new Practice
            {
                Id = Guid.NewGuid(),
                Kind = PracticeKind.Pp,
                Name = "ПП 09",
                GroupId = group.Id,
                DateFrom = new DateTime(2026, 9, 7),
                DateTo = new DateTime(2026, 9, 11),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ConfirmImportAsync(
            new PracticeImportConfirmRequest
            {
                Rows = [ValidRow(group.Name, teacher.User.FullName)],
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("уже есть практика в этот период");
        (await _db.Practices.CountAsync()).Should().Be(1);
    }
}
