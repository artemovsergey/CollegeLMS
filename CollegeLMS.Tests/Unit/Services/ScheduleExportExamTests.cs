using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Регрессия после Задачи 5: экзамены вынесены из Entries в массив Exams.
/// Экспорт дня/недели должен видеть экзамен и печатать его без номера пары,
/// времени и кабинета.
/// </summary>
public class ScheduleExportExamTests : IDisposable
{
    // 2026-09-01 — вторник первой недели семестра, обычный рабочий день.
    private static readonly DateTime ExamDate = new(2026, 9, 1);

    private readonly AppDbContext _db;
    private readonly ScheduleExportService _sut;

    public ScheduleExportExamTests()
    {
        _db = TestDbContextFactory.Create();
        var bells = new BellScheduleServiceStub();
        _sut = new ScheduleExportService(
            _db,
            bells,
            new ScheduleViewService(
                _db,
                bells,
                new PracticeService(_db),
                new ScheduleInsertService(_db)
            )
        );
    }

    public void Dispose() => _db.Dispose();

    /// <summary>День, единственное содержимое которого — экзамен (ровно как его создаёт движок корректировок).</summary>
    private async Task<Guid> SeedExamOnlyDayAsync()
    {
        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО-261",
            Course = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.Groups.Add(group);
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                Subject = "ЭКЗАМЕН",
                DayOfWeek = ExamDate.DayOfWeek,
                NumberPair = 0,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromMinutes(1),
                Weeks = [1],
                LessonType = LessonType.Exam,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
        return group.Id;
    }

    private Task<Result<ExportResult>> ExportAsync(
        string scope,
        Guid? groupId,
        ExportLayout layout
    ) =>
        _sut.ExportAsync(
            groupId,
            null,
            null,
            scope,
            ExamDate,
            null,
            ExportFormat.Xlsx,
            layout,
            CancellationToken.None
        );

    [Fact]
    public async Task ExportDayWithOnlyExamDoesNotReturn404()
    {
        var groupId = await SeedExamOnlyDayAsync();

        var result = await ExportAsync("day", groupId, ExportLayout.DayCards);

        result
            .IsSuccess.Should()
            .BeTrue(
                $"день с экзаменом не пуст, ожидается файл, а не 404. Ошибка: {result.ErrorMessage}"
            );
        result.Data!.FileContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExamDayIsNotTreatedAsEmptyForWeekExport()
    {
        var groupId = await SeedExamOnlyDayAsync();

        var result = await ExportAsync("week", groupId, ExportLayout.DayCards);

        result
            .IsSuccess.Should()
            .BeTrue(
                $"неделя с экзаменом не пуста, ожидается файл, а не 404. Ошибка: {result.ErrorMessage}"
            );
        result.Data!.FileContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExamLineHasNoPairNumberAndNoTime()
    {
        var groupId = await SeedExamOnlyDayAsync();

        // DayCards использует DayLines, Grid (раскладка API по умолчанию) — SpecialLines.
        foreach (var layout in new[] { ExportLayout.DayCards, ExportLayout.Grid })
        {
            var result = await ExportAsync("day", groupId, layout);
            result.IsSuccess.Should().BeTrue();

            using var stream = new MemoryStream(result.Data!.FileContent);
            using var workbook = new XLWorkbook(stream);
            var examLines = workbook
                .Worksheets.SelectMany(ws => ws.CellsUsed())
                .Select(cell => cell.GetString())
                .Where(text => text.Contains("ЭКЗАМЕН"))
                .ToList();

            examLines.Should().NotBeEmpty($"экзамен должен попадать в строки экспорта ({layout})");
            examLines
                .Should()
                .OnlyContain(text =>
                    !text.Contains("0.") && !text.Contains("00:00") && !text.Contains("00:01")
                );
        }
    }

    /// <summary>
    /// Семестровый экспорт читает записи напрямую из БД (включая экзамены с NumberPair=0
    /// и техническим временем 00:00–00:01). Экзамен обязан печататься отдельной строкой
    /// «🎓 ЭКЗАМЕН» без номера пары, времени и кабинета.
    /// </summary>
    [Fact]
    public async Task SemesterExportRendersExamWithoutPairNumberOrTechnicalTime()
    {
        var groupId = await SeedExamOnlyDayAsync();

        var result = await ExportAsync("semester", groupId, ExportLayout.Grid);

        result
            .IsSuccess.Should()
            .BeTrue(
                $"семестровый экспорт с экзаменом должен возвращать файл. Ошибка: {result.ErrorMessage}"
            );

        using var stream = new MemoryStream(result.Data!.FileContent);
        using var workbook = new XLWorkbook(stream);
        var allText = string.Join(
            "\n",
            workbook.Worksheets.SelectMany(ws => ws.CellsUsed()).Select(cell => cell.GetString())
        );

        allText.Should().Contain("🎓 ЭКЗАМЕН");
        allText.Should().NotContain("0. ЭКЗАМЕН");
        allText.Should().NotContain("00:00");
        allText.Should().NotContain("00:01");
    }
}
