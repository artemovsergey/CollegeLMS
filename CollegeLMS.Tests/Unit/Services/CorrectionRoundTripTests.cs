using System.Net.Http;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

public class CorrectionRoundTripTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _batches;
    private readonly ScheduleCorrectionService _corrections;

    public CorrectionRoundTripTests()
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

    public static IEnumerable<object[]> CorrectionFiles() =>
        Directory
            .GetFiles(
                FindRepositoryPath(Path.Combine("import", "schedule", "Корректировки")),
                "*.xlsx"
            )
            .OrderBy(f => f)
            .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(CorrectionFiles))]
    public async Task CorrectionFile_RoundTrip_ExportedFileMatchesSource(string fileName)
    {
        EnsureCorrectionTemplate();
        var path = FindRepositoryPath(
            Path.Combine("import", "schedule", "Корректировки", fileName)
        );
        var bytes = await File.ReadAllBytesAsync(path);

        var import = await _batches.ImportAsync(
            new MemoryStream(bytes),
            Guid.NewGuid(),
            CancellationToken.None
        );

        import.IsSuccess.Should().BeTrue();
        import.Data!.BatchId.Should().NotBeNull();

        var exported = await _batches.ExportAsync(
            import.Data.BatchId!.Value,
            CancellationToken.None
        );

        exported.IsSuccess.Should().BeTrue();

        using var sourceBook = new XLWorkbook(path);
        using var resultBook = new XLWorkbook(new MemoryStream(exported.Data!.Content));

        var (sourceDate, sourceRows) = ReadRows(sourceBook.Worksheet(1));
        var (resultDate, resultRows) = ReadRows(resultBook.Worksheet(1));

        resultDate.Should().Be(sourceDate, "заголовок A3 должен совпадать с исходным файлом");
        resultRows.Should().HaveCount(sourceRows.Count, "число строк должно совпадать");

        for (var index = 0; index < sourceRows.Count; index++)
        {
            resultRows[index]
                .Should()
                .BeEquivalentTo(sourceRows[index], "строка {0} файла {1}", 7 + index, fileName);
        }

        // Отдельно проверяем, что расхождение у переносов ровно одно и ожидаемое:
        // в файле диспетчера колонка «снимается» пустая, в нашей выгрузке там
        // стоит освобождаемое занятие.
        resultRows
            .Where(row => row[1].Length > 0 && row[1] == row[3])
            .Should()
            .OnlyContain(
                row => IsMoveRow(row),
                "заполненная «снимается» допустима только у переноса"
            );
    }

    /// <summary>Перенос: примечание «вм.X» и пара «откуда» отличается от пары ввода.</summary>
    private static bool IsMoveRow(string[] row)
    {
        var match = Regex.Match(row[6] ?? string.Empty, @"вм\.?\s*(\d{1,2})");
        return match.Success && match.Groups[1].Value != NormalizePair(row[5]);
    }

    private static (string Date, List<string[]> Rows) ReadRows(IXLWorksheet sheet)
    {
        var date = sheet.Cell(3, 1).GetString().Trim();
        var rows = new List<string[]>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var row = 7; row <= lastRow; row++)
        {
            var values = Enumerable
                .Range(1, 7)
                .Select(column => sheet.Cell(row, column).GetString().Trim())
                .ToArray();

            if (values.All(string.IsNullOrEmpty))
                continue;

            values[5] = NormalizePair(values[5]);
            // Перенос в файле диспетчера несёт примечание «вм.X» и пустую
            // колонку «снимается», в нашей выгрузке там освобождаемое занятие —
            // для сравнения приводим оба варианта к виду файла диспетчера.
            if (values[1].Length > 0 && values[1] == values[3])
            {
                values[1] = string.Empty;
                values[2] = string.Empty;
            }

            rows.Add(values);
        }

        return (date, rows);
    }

    private static string NormalizePair(string text)
    {
        var match = Regex.Match(text, @"^(\d{1,2})\s*п?\.?$");
        return match.Success ? int.Parse(match.Groups[1].Value).ToString() : text;
    }

    private static void EnsureCorrectionTemplate()
    {
        var relativeDir = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "import", "schedule")
        );
        var target = Path.Combine(relativeDir, "Корректировка.xlsx");
        if (File.Exists(target))
            return;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "import",
                "schedule",
                "Корректировка.xlsx"
            );
            if (File.Exists(candidate))
            {
                Directory.CreateDirectory(relativeDir);
                File.Copy(candidate, target, true);
                return;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Шаблон «Корректировка.xlsx» не найден.");
    }

    private static string FindRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Путь «{relativePath}» не найден в репозитории.");
    }
}
