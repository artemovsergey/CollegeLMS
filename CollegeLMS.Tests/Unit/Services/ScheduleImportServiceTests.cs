using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
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
    public void ParseScheduleMatrix_ParsesBasicSchedule()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        // Заголовок (строки 1-4 пропускаются)
        ws.Cell(2, 5).Value = "РАСПИСАНИЕ УЧЕБНЫХ ЗАНЯТИЙ";

        // Строка 5: группы
        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(5, 4).Value = "ПО 263";

        // Понедельник, пара 1 (строка 6)
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 3).Value = "232     История";
        ws.Cell(7, 3).Value = "(1-17)";
        ws.Cell(8, 3).Value = "Петренко В.Б.";
        ws.Cell(6, 4).Value = "408     Математика";
        ws.Cell(7, 4).Value = "(1-17)";
        ws.Cell(8, 4).Value = "Глебова Л.Н.";

        var result = _sut.ParseScheduleMatrix(workbook);

        result.Should().HaveCount(2);
        result[0].GroupName.Should().Be("ПО 262");
        result[0].Day.Should().Be("Monday");
        result[0].Pair.Should().Be(1);
        result[0].Subject.Should().Be("История");
        result[0].Room.Should().Be("232");
        result[0].TeacherName.Should().Be("Петренко В.Б.");
        result[0].Weeks.Should().BeEquivalentTo(Enumerable.Range(1, 17));

        result[1].GroupName.Should().Be("ПО 263");
        result[1].Subject.Should().Be("Математика");
        result[1].Room.Should().Be("408");
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesChitZal()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "РЭУ 263";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 3).Value = "ч.з      Рус.язык";
        ws.Cell(7, 3).Value = "(1-8,10-16)";
        ws.Cell(8, 3).Value = "Бекетова В.М.";

        var result = _sut.ParseScheduleMatrix(workbook);

        result.Should().HaveCount(1);
        result[0].Room.Should().Be("ч.з");
        result[0].Subject.Should().Be("Рус.язык");
        result[0].Weeks.Should().BeEquivalentTo(
            [1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16]);
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesLabSuffix()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ИП 252";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 3).Value = "307л        ИТ";
        ws.Cell(7, 3).Value = "(1-17)";
        ws.Cell(8, 3).Value = "Николаенко И.Д.";

        var result = _sut.ParseScheduleMatrix(workbook);

        result.Should().HaveCount(1);
        result[0].Room.Should().Be("307л");
        result[0].Subject.Should().Be("ИТ");
    }

    [Fact]
    public void ParseScheduleMatrix_SkipsEmptyCells()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        // Ячейка пустая — не должна добавляться

        var result = _sut.ParseScheduleMatrix(workbook);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseWeeks_ParsesRangeAndSingle()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(5, 4).Value = "ПО 263";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";

        ws.Cell(6, 3).Value = "232 История";
        ws.Cell(7, 3).Value = "(1-5,8,10-12)";
        ws.Cell(8, 3).Value = "Петренко В.Б.";

        ws.Cell(6, 4).Value = "408 Математика";
        ws.Cell(7, 4).Value = "(3)";
        ws.Cell(8, 4).Value = "Глебова Л.Н.";

        var result = _sut.ParseScheduleMatrix(workbook);

        result[0].Weeks.Should().BeEquivalentTo([1, 2, 3, 4, 5, 8, 10, 11, 12]);
        result[1].Weeks.Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task PreviewAsync_ReturnsError_WhenInvalidFile()
    {
        using var ms = new MemoryStream(new byte[] { 0, 1, 2, 3 });
        var result = await _sut.PreviewAsync(ms, default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Не удалось прочитать файл");
    }

    [Fact]
    public async Task PreviewAsync_ReturnsPreview_WhenValidFile()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 3).Value = "232     История";
        ws.Cell(7, 3).Value = "(1-17)";
        ws.Cell(8, 3).Value = "Петренко В.Б.";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.PreviewAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Preview!.TotalEntries.Should().Be(1);
        result.Preview.Entries.Should().HaveCount(1);
        result.Preview.Entries[0].Status.Should().Be("warning"); // группа не в БД
    }
}
