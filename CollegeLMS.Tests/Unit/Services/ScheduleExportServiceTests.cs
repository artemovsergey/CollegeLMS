using System.Text;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleExportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleExportService _sut;

    public ScheduleExportServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleExportService(_db, new BellScheduleServiceStub());
    }

    public void Dispose() => _db.Dispose();

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

    private async Task SeedEntryAsync(
        Group group,
        Teacher teacher,
        DayOfWeek day,
        params int[] weeks
    )
    {
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                TeacherId = teacher.Id,
                Group = group,
                Teacher = teacher,
                Subject = "Математика",
                Room = "301",
                DayOfWeek = day,
                NumberPair = 2,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Weeks = weeks.ToList(),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private static string Cell(byte[] content, int row, int col)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        return workbook.Worksheet(1).Cell(row, col).GetString();
    }

    [Fact]
    public async Task ExportSemester_Xlsx_ReturnsPkSignatureAndFileName()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group, teacher, DayOfWeek.Tuesday, 2);

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        Encoding.ASCII.GetString(result.Data!.FileContent, 0, 2).Should().Be("PK");
        result
            .Data.FileName.Should()
            .MatchRegex(@"^Расписание_семестр_\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}\.xlsx$");
    }

    [Fact]
    public async Task ExportSemester_Pdf_ReturnsPdfSignatureAndFileName()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group, teacher, DayOfWeek.Tuesday, 2);

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Pdf,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        Encoding.ASCII.GetString(result.Data!.FileContent, 0, 4).Should().Be("%PDF");
        result
            .Data.FileName.Should()
            .MatchRegex(@"^Расписание_семестр_\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}\.pdf$");
    }

    [Fact]
    public async Task ExportSemester_BothGroupAndTeacher_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            teacher.Id,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExportSemester_NeitherGroupNorTeacher_Returns400()
    {
        var result = await _sut.ExportAsync(
            null,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExportSemester_NonWorkingDay_MarksCell()
    {
        var group = await SeedGroupAsync();
        _db.NonWorkingDays.Add(
            new NonWorkingDay
            {
                Id = Guid.NewGuid(),
                DateFrom = new DateTime(2026, 9, 7),
                DateTo = new DateTime(2026, 9, 7),
                Title = "Праздник",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        // Неделя 2, понедельник (09.07) → строка 3, колонка 2.
        Cell(result.Data!.FileContent, 3, 2).Should().Contain("Не работает: Праздник");
    }

    [Fact]
    public async Task ExportSemester_Practice_ReplacesCell()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        _db.Practices.Add(
            new Practice
            {
                Id = Guid.NewGuid(),
                Kind = PracticeKind.Up,
                GroupId = group.Id,
                Group = group,
                TeacherId = teacher.Id,
                Teacher = teacher,
                DateFrom = new DateTime(2026, 9, 8),
                DateTo = new DateTime(2026, 9, 9),
                Organization = "ООО Ромашка",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        // Неделя 2, вторник (08.09) → строка 3, колонка 3.
        var text = Cell(result.Data!.FileContent, 3, 3);
        text.Should().Contain("УП:");
        text.Should().Contain(group.Name);
        text.Should().Contain("ООО Ромашка");
    }

    [Fact]
    public async Task ExportSemester_ActiveInsert_AppearsInCell()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group, teacher, DayOfWeek.Tuesday, 2);
        _db.ScheduleInserts.Add(
            new ScheduleInsert
            {
                Id = Guid.NewGuid(),
                Title = "Линейка",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(8, 30, 0),
                Course = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        Cell(result.Data!.FileContent, 3, 3).Should().Contain("Линейка");
    }

    [Fact]
    public async Task ExportSemester_Entry_RendersPairAndMarks()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group, teacher, DayOfWeek.Tuesday, 2);
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Replace,
                AppliedAt = DateTime.UtcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = group.Id,
                TeacherId = teacher.Id,
                Subject = "Математика",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 2,
                Week = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "semester",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var text = Cell(result.Data!.FileContent, 3, 3);
        text.Should().Contain("2. Математика");
        text.Should().Contain("замена");
    }

    [Fact]
    public async Task ExportSemester_ScopeCaseInsensitive()
    {
        var group = await SeedGroupAsync();

        var result = await _sut.ExportAsync(
            group.Id,
            null,
            null,
            null,
            "SEMESTER",
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.FileName.Should().Contain("семестр");
    }
}
