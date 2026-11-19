using System.Net.Http;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Импорт строки «ЭКЗАМЕН» из файла диспетчера: пустая или посторонняя пара в
/// колонке «№» не должна отвергать строку — экзамен — отдельная операция с парой 0.
/// Экспорт такой позиции уже совпадает с образцом, поэтому проверяем только, что
/// предмет остаётся «ЭКЗАМЕН», а ячейка «№» выгружается пустой.
/// </summary>
public class ExamCorrectionXlsxTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleCorrectionService _corrections;
    private readonly CorrectionBatchService _batches;

    private static readonly DateTime TestDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    public ExamCorrectionXlsxTests()
    {
        _db = TestDbContextFactory.Create();
        var engine = new CorrectionApplyEngine(_db, new BellScheduleServiceStub());
        var maxBot = new MaxBotHttpClient(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );
        _corrections = new ScheduleCorrectionService(_db, maxBot, engine);
        _batches = new CorrectionBatchService(
            _db,
            _corrections,
            engine,
            new CorrectionImageService(),
            maxBot,
            NullLogger<CorrectionBatchService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync()
    {
        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "РЭУ242",
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    /// <summary>Строка диспетчера: A=«РЭУ242», D=«ЭКЗАМЕН», F — пустая/чужая пара.</summary>
    private static MemoryStream BuildExamWorkbook(int? pairNumber)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Корректировка");
        ws.Cell(3, 1).Value = "Корректировка на 10.09.2026 г.";
        ws.Cell(5, 1).Value = "Группа";
        ws.Cell(5, 2).Value = "Снимается по расписанию";
        ws.Cell(5, 3).Value = "Преподаватель";
        ws.Cell(5, 4).Value = "Вводится в расписание";
        ws.Cell(5, 5).Value = "Преподаватель";
        ws.Cell(5, 6).Value = "№ пары";
        ws.Cell(5, 7).Value = "Примечание";

        ws.Cell(7, 1).Value = "РЭУ242";
        ws.Cell(7, 4).Value = "ЭКЗАМЕН";
        if (pairNumber is { } pair)
            ws.Cell(7, 6).Value = pair;

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);
        workbook.Dispose();
        return stream;
    }

    [Fact]
    public async Task ImportExamRowCreatesExamPosition()
    {
        await SeedGroupAsync();
        using var stream = BuildExamWorkbook(pairNumber: null);

        var result = await _corrections.PreviewAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        result.Data!.Errors.Should().BeEmpty();
        result.Data.Entries.Should().ContainSingle();
        var entry = result.Data.Entries[0];
        entry.ChangeType.Should().Be(ScheduleChangeType.Exam);
        entry.NumberPair.Should().Be(0);
        entry.Subject.Should().Be("ЭКЗАМЕН");
        entry.GroupName.Should().Be("РЭУ242");
        entry.TeacherId.Should().BeNull();
        entry.TeacherName.Should().BeNull();
        entry.Note.Should().BeNull();
        entry.RemovedSubject.Should().BeNull();
        entry.RemovedTeacherId.Should().BeNull();
        entry.RemovedTeacherName.Should().BeNull();
        entry.RemovedNumberPair.Should().BeNull();
    }

    [Fact]
    public async Task ImportExamRowWithFilledPairDoesNotFail()
    {
        await SeedGroupAsync();
        using var stream = BuildExamWorkbook(pairNumber: 4);

        var result = await _corrections.PreviewAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        result.Data!.Errors.Should().BeEmpty();
        result.Data.Entries.Should().ContainSingle();
        var entry = result.Data.Entries[0];
        entry.ChangeType.Should().Be(ScheduleChangeType.Exam);
        entry.NumberPair.Should().Be(0);
        entry.Subject.Should().Be("ЭКЗАМЕН");
        entry.TeacherId.Should().BeNull();
        entry.Note.Should().BeNull();
    }

    [Fact]
    public async Task ExportExamRowHasEmptyPairCellAndExamSubject()
    {
        CorrectionTemplateFixture.Ensure();
        var group = await SeedGroupAsync();
        var batch = await _batches.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate },
            Guid.NewGuid(),
            CancellationToken.None
        );
        batch.IsSuccess.Should().BeTrue(batch.ErrorMessage);
        var added = await _batches.AddPositionAsync(
            batch.Data!.Id,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Exam,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 0,
                Subject = "ЭКЗАМЕН",
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue(added.ErrorMessage);

        var exported = await _batches.ExportAsync(batch.Data.Id, CancellationToken.None);

        exported.IsSuccess.Should().BeTrue(exported.ErrorMessage);
        using var book = new XLWorkbook(new MemoryStream(exported.Data!.Content));
        var sheet = book.Worksheet(1);
        sheet.Cell(7, 4).GetString().Trim().Should().Be("ЭКЗАМЕН");
        sheet.Cell(7, 6).GetString().Trim().Should().BeEmpty();
    }
}
