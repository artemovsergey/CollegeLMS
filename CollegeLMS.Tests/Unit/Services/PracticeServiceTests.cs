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

    private static PracticeRequest Request(
        Guid groupId,
        Guid teacherId,
        PracticeKind kind = PracticeKind.Up,
        DateTime? from = null,
        DateTime? to = null,
        string? organization = null
    ) =>
        new()
        {
            Kind = kind,
            GroupId = groupId,
            TeacherId = teacherId,
            DateFrom = from ?? new DateTime(2026, 9, 7),
            DateTo = to ?? new DateTime(2026, 9, 11),
            Organization = organization,
            Note = "примечание",
        };

    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> fill)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Практики");
        ws.Cell(1, 1).Value = "Вид";
        ws.Cell(1, 2).Value = "Группа";
        ws.Cell(1, 3).Value = "Дата с";
        ws.Cell(1, 4).Value = "Дата по";
        ws.Cell(1, 5).Value = "Преподаватель";
        ws.Cell(1, 6).Value = "Организация";
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
            GroupName = groupName,
            DateFrom = "07.09.2026",
            DateTo = "11.09.2026",
            TeacherName = teacherName,
            Organization = "ООО Ромашка",
            Note = "выезд",
        };

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccess()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, organization: "  ООО Ромашка  "),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.GroupName.Should().Be(group.Name);
        result.Data.TeacherName.Should().Be(teacher.User.FullName);
        result.Data.Organization.Should().Be("ООО Ромашка");
        (await _db.Practices.CountAsync()).Should().Be(1);
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
    public async Task UpdateAsync_SamePeriod_DoesNotConflictWithItself()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var created = await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.UpdateAsync(
            created.Data!.Id,
            Request(group.Id, teacher.Id, organization: "Обновлено"),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Organization.Should().Be("Обновлено");
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
    public async Task PreviewImportAsync_ValidFile_ParsesRows()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "УП";
            ws.Cell(2, 2).Value = group.Name;
            ws.Cell(2, 3).Value = "07.09.2026";
            ws.Cell(2, 4).Value = "11.09.2026";
            ws.Cell(2, 5).Value = teacher.User.FullName;
            ws.Cell(2, 6).Value = "ООО Ромашка";
            ws.Cell(2, 7).Value = "выезд";
        });

        var result = await _sut.PreviewImportAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalRows.Should().Be(1);
        result.Data.Errors.Should().BeEmpty();
        result.Data.Rows[0].GroupName.Should().Be(group.Name);
        result.Data.Rows[0].Row.Should().Be(2);
    }

    [Fact]
    public async Task PreviewImportAsync_InvalidRows_ReportsRowNumbers()
    {
        await SeedGroupAsync();
        await SeedTeacherAsync();

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "XX";
            ws.Cell(2, 2).Value = "НЕТ-ТАКОЙ";
            ws.Cell(2, 3).Value = "не-дата";
            ws.Cell(2, 4).Value = "не-дата";
            ws.Cell(2, 5).Value = "Нет Такого";
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
    public async Task ConfirmImportAsync_ValidRows_CreatesPractices()
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
        result.Data!.Imported.Should().Be(1);
        result.Data.Practices[0].GroupName.Should().Be(group.Name);
        result.Data.Practices[0].TeacherName.Should().Be(teacher.User.FullName);
        (await _db.Practices.CountAsync()).Should().Be(1);
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
}
