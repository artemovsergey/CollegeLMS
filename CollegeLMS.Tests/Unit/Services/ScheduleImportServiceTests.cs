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
    private readonly BellScheduleServiceStub _bells;
    private readonly ScheduleImportService _sut;

    public ScheduleImportServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _bells = new BellScheduleServiceStub();
        _sut = new ScheduleImportService(_db, _bells);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void ParseScheduleMatrix_ParsesBasicSchedule()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(2, 5).Value = "РАСПИСАНИЕ УЧЕБНЫХ ЗАНЯТИЙ";

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(5, 4).Value = "ПО 263";

        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";
        ws.Cell(6, 4).Value = "408 Математика (1-16) Глебова Л.Н.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(2);
        entries[0].GroupName.Should().Be("ПО 262");
        entries[0].Day.Should().Be("Monday");
        entries[0].Pair.Should().Be(1);
        entries[0].Subject.Should().Be("История");
        entries[0].Room.Should().Be("232");
        entries[0].TeacherName.Should().Be("Петренко В.Б.");
        entries[0].Weeks.Should().BeEquivalentTo(Enumerable.Range(1, 16));

        entries[1].GroupName.Should().Be("ПО 263");
        entries[1].Subject.Should().Be("Математика");
        entries[1].Room.Should().Be("408");
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesChitZal()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "РЭУ 263";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "ч.з. Рус.язык (1-8,10-16) Бекетова В.М.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Room.Should().Be("ч.з.");
        entries[0].Subject.Should().Be("Рус.язык");
        entries[0].TeacherName.Should().Be("Бекетова В.М.");
        entries[0]
            .Weeks.Should()
            .BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16]);
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesLabSuffix()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ИП 252";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "307л ИТ (1-16) Николаенко И.Д.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Room.Should().Be("307л");
        entries[0].Subject.Should().Be("ИТ");
        entries[0].TeacherName.Should().Be("Николаенко И.Д.");
    }

    [Fact]
    public void ParseScheduleMatrix_SkipsEmptyCells()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
    }

    [Fact]
    public void ParseWeeks_ParsesRangeAndSingle()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(5, 4).Value = "ПО 263";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;

        ws.Cell(6, 3).Value = "232 История (1-5,8,10-12) Петренко В.Б.";
        ws.Cell(6, 4).Value = "408 Математика (3) Глебова Л.Н.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries[0].Weeks.Should().BeEquivalentTo([1, 2, 3, 4, 5, 8, 10, 11, 12]);
        entries[1].Weeks.Should().BeEquivalentTo([3]);
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesMultipleSubjectsPerPair()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "414 Математика (1) Марченко В.Ф.";
        ws.Cell(7, 3).Value = "316л ПД (2,4,6,8,10,12,14,16) Строганова Е.М.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(2);
        entries[0].Subject.Should().Be("Математика");
        entries[0].Weeks.Should().BeEquivalentTo([1]);
        entries[1].Subject.Should().Be("ПД");
        entries[1].Weeks.Should().BeEquivalentTo([2, 4, 6, 8, 10, 12, 14, 16]);
    }

    [Fact]
    public void ParseScheduleMatrix_HandlesSportZal()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ВТОРНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "с.з. Физкультура (14,16) Волков В.В.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Room.Should().Be("с.з.");
        entries[0].Subject.Should().Be("Физкультура");
        entries[0].TeacherName.Should().Be("Волков В.В.");
        entries[0].Weeks.Should().BeEquivalentTo([14, 16]);
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
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.PreviewAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.Preview!.TotalEntries.Should().Be(1);
        result.Preview.Entries.Should().HaveCount(1);
        result.Preview.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmAsync_ClearsPreviousScheduleHistory()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО 262",
            Course = 2,
        };
        _db.Groups.Add(group);
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Replace,
                AppliedAt = DateTime.UtcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = group.Id,
                Subject = "Новая дисциплина",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                Week = 1,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ConfirmAsync(
            new ConfirmImportRequest
            {
                Entries =
                [
                    new SchedulePreviewEntry
                    {
                        GroupName = group.Name,
                        Day = "Monday",
                        Pair = 1,
                        Subject = "История",
                        Room = "232",
                        TeacherName = "Петров П.П.",
                        Weeks = [1],
                    },
                ],
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        _db.ScheduleHistory.Should().BeEmpty();
        _db.ScheduleEntries.Should().ContainSingle(e => e.Subject == "История");
    }

    [Fact]
    public void ParseScheduleMatrix_MondayPair1_ReturnsCorrectTime()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].StartTime.Should().Be(new TimeSpan(9, 10, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(10, 40, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_MondayPair6_ReturnsCorrectTime()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 6;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].StartTime.Should().Be(new TimeSpan(17, 50, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(19, 20, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_ThursdayPair3_Returns13Start()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ЧЕТВЕРГ";
        ws.Cell(6, 2).Value = 3;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].StartTime.Should().Be(new TimeSpan(13, 0, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(14, 30, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_ThursdayPair6_Returns18Start()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ЧЕТВЕРГ";
        ws.Cell(6, 2).Value = 6;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].StartTime.Should().Be(new TimeSpan(18, 0, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(19, 30, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_TuesdayPair7_ReturnsCorrectTime()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ВТОРНИК";
        ws.Cell(6, 2).Value = 7;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].StartTime.Should().Be(new TimeSpan(18, 50, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(20, 20, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_PairNumberExceedMax_ClampsToLast()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 7;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Pair.Should().Be(7);
        entries[0].StartTime.Should().Be(new TimeSpan(17, 50, 0));
        entries[0].EndTime.Should().Be(new TimeSpan(19, 20, 0));
    }

    [Fact]
    public void ParseScheduleMatrix_NormalizesIstR()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 Ист.Р. (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Subject.Should().Be("ИсторияРоссии");
    }

    [Fact]
    public void ParseScheduleMatrix_NormalizesMatem()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 Матем. (1-16) Глебова Л.Н.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Subject.Should().Be("Математика");
    }

    [Fact]
    public void ParseScheduleMatrix_NormalizesFizKul()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ВТОРНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "с.з. Физ.кул. (1-16) Волков В.В.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Subject.Should().Be("Физкультура");
    }

    [Fact]
    public void ParseScheduleMatrix_NormalizesElectroTekh()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "СРЕДА";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 Электротех. (1-16) Иванов И.И.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Subject.Should().Be("ЭлектрТех.");
    }

    [Fact]
    public void ParseScheduleMatrix_NormalizesOhrTruda()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПЯТНИЦА";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 Охр.тр. (1-16) Петров П.П.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(1);
        entries[0].Subject.Should().Be("ОхранаТруда");
    }

    [Fact]
    public void ParseScheduleMatrix_NoGroups_ReturnsStructureError()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(7, 2).Value = 9;

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        errors
            .Should()
            .Contain(e => e.Level == "structure" && e.Message.Contains("названия групп"));
        errors.Should().Contain(e => e.Level == "data" && e.Message.Contains("вне диапазона 1–8"));
    }

    [Fact]
    public void ParseScheduleMatrix_ReturnsError_WhenNoDays()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        errors.Should().ContainSingle();
        errors[0].Level.Should().Be("structure");
        errors[0].Message.Should().Contain("дни недели");
    }

    [Fact]
    public void ParseScheduleMatrix_ReturnsError_WhenWeeksEmpty()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        errors.Should().Contain(e => e.Message.Contains("недели"));
    }

    [Fact]
    public void ParseScheduleMatrix_ReturnsError_WhenWeeksExceedSemester()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-55) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        errors.Should().Contain(e => e.Message.Contains("неделя 17 вне семестра (1–16)"));
    }

    [Fact]
    public void ParseScheduleMatrix_WeekAboveSemester_ReturnsErrorWithNewFormat()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-20) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        var error = errors.Should().ContainSingle().Subject;
        error.Level.Should().Be("data");
        error.Sheet.Should().Be("Расписание");
        error.Message.Should().StartWith("Лист Расписание, строка 6, столбец 3:");
        error.Message.Should().Contain("столбец");
        error.Message.Should().Contain("вне семестра (1–16)");
    }

    [Fact]
    public void ParseScheduleMatrix_Pair8_Parsed()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";
        ws.Cell(7, 2).Value = 8;
        ws.Cell(7, 3).Value = "316л ПД (1-16) Строганова Е.М.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().HaveCount(2);
        entries[1].Pair.Should().Be(8);
        entries[1].Subject.Should().Be("ПД");
    }

    [Fact]
    public void ParseScheduleMatrix_Pair9_ReturnsDataError()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 9;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        errors
            .Should()
            .Contain(e =>
                e.Level == "data" && e.Message.Contains("номер пары 9 вне диапазона 1–8")
            );
    }

    [Fact]
    public void ParseScheduleMatrix_UnknownDay_ReturnsStructureError()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().BeEmpty();
        var error = errors
            .Should()
            .ContainSingle(e =>
                e.Level == "structure" && e.Message.Contains("неизвестный день недели")
            )
            .Subject;
        error.Sheet.Should().Be("Расписание");
        error.Message.Should().Contain("\"ПНЕДЕЛЬНИК\"");
    }

    [Fact]
    public void ParseScheduleMatrix_SameCell_ReturnsEntriesAndErrors()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(5, 4).Value = "ПО 263";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";
        ws.Cell(6, 4).Value = "408 Математика (1-20) Глебова Л.Н.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        entries.Should().ContainSingle();
        entries[0].GroupName.Should().Be("ПО 262");
        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("вне семестра (1–16)");
    }

    [Fact]
    public void ParseScheduleMatrix_PairTimeFromBells()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";

        _bells.TimeMap = new Dictionary<int, (TimeSpan Start, TimeSpan End)>
        {
            [1] = (new TimeSpan(10, 0, 0), new TimeSpan(11, 20, 0)),
        };

        var (bellEntries, bellErrors) = _sut.ParseScheduleMatrix(workbook, _bells.TimeMap);

        bellErrors.Should().BeEmpty();
        bellEntries[0].StartTime.Should().Be(new TimeSpan(10, 0, 0));
        bellEntries[0].EndTime.Should().Be(new TimeSpan(11, 20, 0));

        var (fallbackEntries, fallbackErrors) = _sut.ParseScheduleMatrix(
            workbook,
            new Dictionary<int, (TimeSpan Start, TimeSpan End)>()
        );

        fallbackErrors.Should().BeEmpty();
        fallbackEntries[0].StartTime.Should().Be(new TimeSpan(9, 10, 0));
        fallbackEntries[0].EndTime.Should().Be(new TimeSpan(10, 40, 0));
    }

    [Fact]
    public void NormalizeTeacherName_NormalizesInitialsSpacing()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Иванов И. И.";

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();
        entries.Should().ContainSingle();
        entries[0].TeacherName.Should().Be("Иванов И.И.");
        entries[0].Subject.Should().Be("История");
    }

    [Fact]
    public async Task PreviewAsync_WithErrors_ReturnsSuccessWithEntriesAndErrors()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Расписание");

        ws.Cell(5, 3).Value = "ПО 262";
        ws.Cell(6, 1).Value = "ПОНЕДЕЛЬНИК";
        ws.Cell(6, 2).Value = 1;
        ws.Cell(6, 3).Value = "232 История (1-16) Петренко В.Б.";
        ws.Cell(7, 3).Value = "408 Математика (1-20) Глебова Л.Н.";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var result = await _sut.PreviewAsync(ms, default);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Preview!.TotalEntries.Should().Be(1);
        result.Preview.Entries.Should().ContainSingle();
        result.Preview.Errors.Should().ContainSingle();
    }
}
