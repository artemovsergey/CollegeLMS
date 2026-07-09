using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using ClosedXML.Excel;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleImportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleImportService _sut;

    public ScheduleImportServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleImportService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ImportAsync_ImportsValidRows()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");
        ws.Cell(1, 1).Value = "Группа";
        ws.Cell(1, 2).Value = "Преподаватель";
        ws.Cell(1, 3).Value = "Предмет";
        ws.Cell(1, 4).Value = "Аудитория";
        ws.Cell(1, 5).Value = "День";
        ws.Cell(1, 6).Value = "Начало";
        ws.Cell(1, 7).Value = "Конец";
        ws.Cell(1, 8).Value = "Тип занятия";
        ws.Cell(2, 1).Value = "ГР-11";
        ws.Cell(2, 2).Value = "";
        ws.Cell(2, 3).Value = "Математика";
        ws.Cell(2, 4).Value = "301";
        ws.Cell(2, 5).Value = "Пн";
        ws.Cell(2, 6).Value = "09:00";
        ws.Cell(2, 7).Value = "10:30";
        ws.Cell(2, 8).Value = "Лекция";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(1);
        result.Data.Skipped.Should().Be(0);
        result.Data.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ReturnsError_WhenInvalidFile()
    {
        using var ms = new MemoryStream(new byte[] { 0, 1, 2, 3 });
        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ImportAsync_SkipsInvalidRows()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");
        ws.Cell(1, 1).Value = "Группа";
        ws.Cell(1, 2).Value = "Преподаватель";
        ws.Cell(1, 3).Value = "Предмет";
        ws.Cell(1, 4).Value = "Аудитория";
        ws.Cell(1, 5).Value = "День";
        ws.Cell(1, 6).Value = "Начало";
        ws.Cell(1, 7).Value = "Конец";
        ws.Cell(1, 8).Value = "Тип занятия";
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 3).Value = "Математика";
        ws.Cell(2, 4).Value = "301";
        ws.Cell(2, 5).Value = "НепонятныйДень";
        ws.Cell(2, 6).Value = "09:00";
        ws.Cell(2, 7).Value = "10:30";
        ws.Cell(2, 8).Value = "Лекция";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Imported.Should().Be(0);
        result.Data.Skipped.Should().Be(1);
        result.Data.Errors.Should().HaveCount(1);
        result.Data.Errors[0].Message.Should().Contain("Группа не указана");
    }

    [Fact]
    public async Task ImportAsync_ReturnsFail_WhenEmptyFile()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");
        ws.Cell(1, 1).Value = "Header";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.ImportAsync(ms, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }
}
