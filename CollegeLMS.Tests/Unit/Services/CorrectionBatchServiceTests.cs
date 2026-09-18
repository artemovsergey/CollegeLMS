using System.Net.Http;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

public class CorrectionBatchServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _sut;

    public CorrectionBatchServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var engine = new CorrectionApplyEngine(_db);
        var maxBot = new MaxBotHttpClient(new HttpClient(), NullLogger<MaxBotHttpClient>.Instance);
        var correctionService = new ScheduleCorrectionService(_db, maxBot, engine);
        _sut = new CorrectionBatchService(
            _db,
            correctionService,
            engine,
            new CorrectionImageService(),
            maxBot,
            NullLogger<CorrectionBatchService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync(string name = "ПО-262")
    {
        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
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
            Email = "teacher@collegelms.ru",
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
            CyclicalCommission = "Общих гуманитарных дисциплин",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = user,
        };
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return teacher;
    }

    private async Task<ScheduleEntry> SeedEntryAsync(
        Guid groupId,
        Guid teacherId,
        string subject,
        int numberPair,
        int[] weeks
    )
    {
        var utcNow = DateTime.UtcNow;
        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = teacherId,
            Subject = subject,
            Room = "301",
            DayOfWeek = DayOfWeek.Thursday,
            NumberPair = numberPair,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Weeks = weeks.ToList(),
            LessonType = LessonType.None,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    private static readonly DateTime TestDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateBatchAsync_ComputesWeekAndDayOfWeek()
    {
        var result = await _sut.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Week.Should().Be(2);
        result.Data.DayOfWeek.Should().Be((int)DayOfWeek.Thursday);
        result.Data.Status.Should().Be(CorrectionBatchStatus.Draft);
    }

    [Fact]
    public async Task CreateBatchAsync_RejectsWeekend()
    {
        var result = await _sut.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = new DateTime(2026, 9, 12) },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AddPositionAsync_RejectsSelfStudyOnAdd()
    {
        var group = await SeedGroupAsync();
        var batch = await CreateBatchAsync();

        var result = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Математика",
                Note = "сам.р.",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("сам.р.");
    }

    [Fact]
    public async Task ApplyAsync_RemoveSelfStudy_KeepsEntryAndMarksApplied()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var existing = await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [1, 2]);
        var batch = await CreateBatchAsync();

        var added = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Remove,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                Note = "сам.р.",
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue();

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Applied.Should().Be(1);

        var entry = _db.ScheduleEntries.Single();
        entry.Id.Should().Be(existing.Id);
        entry.Weeks.Should().BeEquivalentTo([1, 2]);

        var batchEntity = _db.CorrectionBatches.Single();
        batchEntity.Status.Should().Be(CorrectionBatchStatus.Applied);

        var position = _db.CorrectionPositions.Single();
        position.Status.Should().Be(CorrectionPositionStatus.Applied);
        position.HistoryId.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyAsync_Twice_ReturnsConflict()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [1, 2]);
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Remove,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
            },
            CancellationToken.None
        );

        var first = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );
        first.IsSuccess.Should().BeTrue();

        var second = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );
        second.IsSuccess.Should().BeFalse();
        second.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task DeleteBatchAsync_AppliedBatch_ReturnsConflict()
    {
        var batch = await CreateBatchAsync();

        var delete = await _sut.DeleteBatchAsync(batch, CancellationToken.None);
        delete.IsSuccess.Should().BeTrue();

        var missing = await _sut.DeleteBatchAsync(batch, CancellationToken.None);
        missing.IsSuccess.Should().BeFalse();
        missing.StatusCode.Should().Be(404);
    }

    private async Task<Guid> CreateBatchAsync()
    {
        var result = await _sut.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate },
            Guid.NewGuid(),
            CancellationToken.None
        );
        result.IsSuccess.Should().BeTrue();
        return result.Data!.Id;
    }

    private static MemoryStream BuildWorkbook(string dateText, Action<IXLWorksheet> fill)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Корректировка");
        ws.Cell(3, 1).Value = dateText;
        ws.Cell(5, 1).Value = "Группа";
        ws.Cell(5, 2).Value = "Снимается по расписанию";
        ws.Cell(5, 3).Value = "Преподаватель";
        ws.Cell(5, 4).Value = "Вводится в расписание";
        ws.Cell(5, 5).Value = "Преподаватель";
        ws.Cell(5, 6).Value = "№ пары";
        ws.Cell(5, 7).Value = "Примечание";
        fill(ws);

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        workbook.Dispose();
        return ms;
    }

    private async Task<CorrectionBatch> SeedBatchEntityAsync(
        DateTime date,
        CorrectionBatchStatus status
    )
    {
        var utcNow = DateTime.UtcNow;
        var batch = new CorrectionBatch
        {
            Id = Guid.NewGuid(),
            CorrectionDate = date,
            Week = CollegeLMS.API.Services.StudyWeek.ForDate(date),
            DayOfWeek = (int)date.DayOfWeek,
            Status = status,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.CorrectionBatches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    // --- UC-SCH-19: импорт с ошибками создаёт пакет ---

    [Fact]
    public async Task ImportAsync_RowWithDataError_StillCreatesBatch()
    {
        await SeedTeacherAsync();

        using var stream = BuildWorkbook(
            "Корректировка на 08.09.2026 г.",
            ws =>
            {
                ws.Cell(7, 1).Value = "ИНВ-999";
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
            }
        );

        var result = await _sut.ImportAsync(stream, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BatchId.Should().NotBeNull();
        result.Data.Errors.Should().NotBeEmpty();
        result.Data.Errors.Should().OnlyContain(e => e.Message.StartsWith("Строка 1:"));
        result.Data.Positions.Should().ContainSingle();
        _db.CorrectionBatches.Should().ContainSingle();
        _db.CorrectionPositions.Should().ContainSingle();
    }

    [Fact]
    public async Task ImportAsync_StructuralError_DoesNotCreateBatch()
    {
        using var stream = BuildWorkbook(
            "без даты",
            ws =>
            {
                ws.Cell(7, 1).Value = "ПО-262";
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
            }
        );

        var result = await _sut.ImportAsync(stream, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BatchId.Should().BeNull();
        result.Data.Errors.Should().OnlyContain(e => e.Level == "structure");
        _db.CorrectionBatches.Should().BeEmpty();
        _db.CorrectionPositions.Should().BeEmpty();
    }

    // --- UC-SCH-18: список пакетов — фильтры и пагинация ---

    [Fact]
    public async Task GetBatchesAsync_FiltersByStatus()
    {
        await SeedBatchEntityAsync(TestDate, CorrectionBatchStatus.Draft);
        await SeedBatchEntityAsync(TestDate, CorrectionBatchStatus.Applied);

        var result = await _sut.GetBatchesAsync(
            CorrectionBatchStatus.Applied,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].Status.Should().Be(CorrectionBatchStatus.Applied);
    }

    [Fact]
    public async Task GetBatchesAsync_FiltersByPeriod()
    {
        await SeedBatchEntityAsync(new DateTime(2026, 9, 1), CorrectionBatchStatus.Draft);
        await SeedBatchEntityAsync(new DateTime(2026, 9, 15), CorrectionBatchStatus.Draft);

        var result = await _sut.GetBatchesAsync(
            null,
            new DateTime(2026, 9, 15),
            new DateTime(2026, 9, 15),
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].CorrectionDate.Should().Be(new DateTime(2026, 9, 15));
    }

    [Fact]
    public async Task GetBatchesAsync_PaginationAndPageSizeClamp()
    {
        await SeedBatchEntityAsync(new DateTime(2026, 9, 1), CorrectionBatchStatus.Draft);
        await SeedBatchEntityAsync(new DateTime(2026, 9, 8), CorrectionBatchStatus.Draft);
        await SeedBatchEntityAsync(new DateTime(2026, 9, 15), CorrectionBatchStatus.Draft);

        var firstPage = await _sut.GetBatchesAsync(null, null, null, 1, 2, CancellationToken.None);

        firstPage.IsSuccess.Should().BeTrue();
        firstPage.Data!.Items.Should().HaveCount(2);
        firstPage.Data.TotalCount.Should().Be(3);
        firstPage.Data.PageSize.Should().Be(2);
        firstPage.Data.TotalPages.Should().Be(2);

        var clamp = await _sut.GetBatchesAsync(null, null, null, 1, 500, CancellationToken.None);

        clamp.IsSuccess.Should().BeTrue();
        clamp.Data!.PageSize.Should().Be(100);
    }

    // --- UC-SCH-24: применение — 400 со «Строка N» ---

    [Fact]
    public async Task ApplyAsync_InvalidPosition_ReturnsBadRequestWithRowMessage()
    {
        var group = await SeedGroupAsync();
        var batch = await CreateBatchAsync();
        var added = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 3,
                Subject = "Математика",
                TeacherName = "Неизвестный Х.Х.",
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue();

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().StartWith("Строка 1:");
        result.ErrorMessage.Should().Contain("не найден");
    }

    [Fact]
    public async Task ApplyAsync_ValidPosition_SetsStatusesAndHistoryId()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await CreateBatchAsync();
        var added = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 3,
                Subject = "Математика",
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue();

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Applied.Should().Be(1);

        var position = _db.CorrectionPositions.Single();
        position.Status.Should().Be(CorrectionPositionStatus.Applied);
        position.HistoryId.Should().NotBeNull();
        _db.CorrectionBatches.Single().Status.Should().Be(CorrectionBatchStatus.Applied);
        _db.ScheduleHistory.Should().ContainSingle();
    }
}
