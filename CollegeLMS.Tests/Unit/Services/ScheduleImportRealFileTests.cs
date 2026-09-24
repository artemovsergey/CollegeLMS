using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleImportRealFileTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleImportService _sut;

    public ScheduleImportRealFileTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleImportService(_db, new BellScheduleServiceStub());
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void ParseScheduleMatrix_RealSchedule_ParsesWeeksWithTrailingDot()
    {
        var path = FindRepositoryFile(Path.Combine("import", "schedule", "Расписание.xlsx"));
        using var workbook = new XLWorkbook(path);

        var (entries, errors) = _sut.ParseScheduleMatrix(workbook);

        errors.Should().BeEmpty();

        var abaturov = entries.FirstOrDefault(e =>
            e.Subject?.Contains("ОБПиЗР") == true
            && e.TeacherName?.Contains("Абатуров") == true
            && e.Weeks.SequenceEqual(new[] { 3, 4 })
        );
        abaturov
            .Should()
            .NotBeNull("ячейка «224 ОБПиЗР (3,4.) Абатуров С.А.» должна сохранить неделю 4");

        var lab = entries.FirstOrDefault(e =>
            e.Subject?.Contains("ОПБД") == true
            && e.TeacherName?.Contains("Буценко") == true
            && e.Weeks.SequenceEqual(new[] { 12, 13 })
        );
        lab.Should()
            .NotBeNull("ячейка «203л ОПБД (12,13.) Буценко Е.В.» должна сохранить неделю 13");
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Файл «{relativePath}» не найден в репозитории.");
    }
}
