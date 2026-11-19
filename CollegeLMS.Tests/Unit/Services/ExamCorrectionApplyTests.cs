using System.Net.Http;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Экзамен как отдельная операция корректировки: применение создаёт запись
/// расписания с LessonType.Exam, откат убирает только её и её журнал.
/// </summary>
public class ExamCorrectionApplyTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _sut;
    private readonly ScheduleCorrectionService _correctionService;
    private readonly CorrectionApplyEngine _engine;

    private static readonly DateTime TestDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    public ExamCorrectionApplyTests()
    {
        _db = TestDbContextFactory.Create();
        _engine = new CorrectionApplyEngine(_db, new BellScheduleServiceStub());
        var maxBot = new MaxBotHttpClient(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );
        _correctionService = new ScheduleCorrectionService(_db, maxBot, _engine);
        _sut = new CorrectionBatchService(
            _db,
            _correctionService,
            _engine,
            new CorrectionImageService(),
            maxBot,
            NullLogger<CorrectionBatchService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    // ───────────────────────────── посев данных ─────────────────────────────

    private async Task<Group> SeedGroupAsync()
    {
        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО-262",
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
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

    private async Task<int> WeekAsync(Guid batchId) =>
        (await _db.CorrectionBatches.FindAsync(batchId))!.Week;

    private Task<CreateCorrectionPositionRequest> ExamRequestAsync(Group group) =>
        Task.FromResult(
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Exam,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 0,
                Subject = "ЭКЗАМЕН",
            }
        );

    private async Task AddExamAsync(Guid batchId, Group group)
    {
        var result = await _sut.AddPositionAsync(
            batchId,
            await ExamRequestAsync(group),
            CancellationToken.None
        );
        result.IsSuccess.Should().BeTrue();
    }

    private async Task SeedNeighborAsync(Guid groupId, int pair, int week)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Subject = "Физика",
                Room = "301",
                DayOfWeek = DayOfWeek.Thursday,
                NumberPair = pair,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Weeks = [week],
                LessonType = LessonType.None,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedNonWorkingDayAsync(DateTime date, string title = "День города")
    {
        var utcNow = DateTime.UtcNow;
        _db.NonWorkingDays.Add(
            new NonWorkingDay
            {
                Id = Guid.NewGuid(),
                DateFrom = date.Date,
                DateTo = date.Date,
                Title = title,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private Task<Result<CorrectionApplyResult>> ApplyAsync(Guid batchId) =>
        _sut.ApplyAsync(batchId, Guid.NewGuid().ToString(), Guid.NewGuid(), CancellationToken.None);

    /// <summary>
    /// Валидацию движка проверяем на позиции, вставленной напрямую: сервис
    /// AddPositionAsync не даёт создать заведомо неверный экзамен, а движок
    /// обязан проверять и такие строки (в том числе при импорте файла).
    /// </summary>
    private async Task<List<ScheduleValidationError>> ValidateExamAsync(
        Group group,
        Action<CorrectionPosition>? configure = null
    )
    {
        var batchId = await CreateBatchAsync();
        var utcNow = DateTime.UtcNow;
        var position = new CorrectionPosition
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            Row = 1,
            ChangeType = ScheduleChangeType.Exam,
            GroupId = group.Id,
            GroupName = group.Name,
            DayOfWeek = (int)TestDate.DayOfWeek,
            Week = await WeekAsync(batchId),
            NumberPair = 0,
            Subject = "ЭКЗАМЕН",
            Status = CorrectionPositionStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        configure?.Invoke(position);
        _db.CorrectionPositions.Add(position);
        await _db.SaveChangesAsync();

        var batch = await _db
            .CorrectionBatches.Include(b => b.Positions)
            .SingleAsync(b => b.Id == batchId);
        return await _engine.ValidateBatchAsync(batch, CancellationToken.None);
    }

    // ─────────────────────────── применение и откат ───────────────────────────

    [Fact]
    public async Task ApplyExamCreatesEntryWithExamLessonType()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();
        var week = await WeekAsync(batchId);
        await AddExamAsync(batchId, group);

        var applied = await ApplyAsync(batchId);

        applied.IsSuccess.Should().BeTrue();
        applied.Data!.Applied.Should().Be(1);
        var exam = await _db.ScheduleEntries.SingleAsync(e => e.GroupId == group.Id);
        exam.LessonType.Should().Be(LessonType.Exam);
        exam.NumberPair.Should().Be(0);
        exam.Subject.Should().Be("ЭКЗАМЕН");
        exam.Weeks.Should().BeEquivalentTo([week]);
        exam.TeacherId.Should().BeNull();
        exam.StartTime.Should().Be(TimeSpan.Zero);
        exam.EndTime.Should().Be(TimeSpan.Zero);

        var history = await _db.ScheduleHistory.SingleAsync();
        history.ChangeType.Should().Be(ScheduleChangeType.Exam);
        applied.Data.History.Should().ContainSingle(h => h.ChangeType == ScheduleChangeType.Exam);
        // Бейдж «Добавлено» строится ниже по потоку из ChangeType=Exam (в вебе
        // changeTagKind("Exam") === "Add"), поэтому здесь проверяем запись-источник.
    }

    [Fact]
    public async Task RevertExamRemovesOnlyExamEntry()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();
        await SeedNeighborAsync(group.Id, 1, 3);
        await AddExamAsync(batchId, group);
        var applied = await ApplyAsync(batchId);
        applied.IsSuccess.Should().BeTrue();
        var historyId = applied.Data!.History.Single().Id;

        var reverted = await _correctionService.RevertHistoryAsync(
            historyId,
            CancellationToken.None
        );

        reverted.IsSuccess.Should().BeTrue();
        reverted.Data!.Message.Should().Contain("ЭКЗАМЕН").And.Contain("убрано");
        (await _db.ScheduleEntries.SingleOrDefaultAsync(e => e.Subject == "ЭКЗАМЕН"))
            .Should()
            .BeNull();
        (await _db.ScheduleHistory.FindAsync(historyId)).Should().BeNull();
        var neighbor = await _db.ScheduleEntries.SingleAsync(e => e.Subject == "Физика");
        neighbor.Weeks.Should().BeEquivalentTo([3], "соседняя пара другой недели не тронута");
    }

    [Fact]
    public async Task ReapplyExamBatchNoDuplicate()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();
        await AddExamAsync(batchId, group);
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        var second = await ApplyAsync(batchId);

        second.IsSuccess.Should().BeFalse();
        (await _db.ScheduleEntries.CountAsync(e => e.Subject == "ЭКЗАМЕН")).Should().Be(1);
        (await _db.ScheduleHistory.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApplyExamOnNonWorkingDateFails()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();
        await AddExamAsync(batchId, group);
        await SeedNonWorkingDayAsync(TestDate);

        var result = await ApplyAsync(batchId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Дата нерабочая:");
        (await _db.CorrectionPositions.SingleAsync())
            .Status.Should()
            .Be(CorrectionPositionStatus.Draft);
        (await _db.ScheduleEntries.CountAsync()).Should().Be(0);
    }

    // ───────────────────────── валидация движка ─────────────────────────

    [Fact]
    public async Task ValidateBatchAsync_ValidExamPosition_NoErrors()
    {
        var group = await SeedGroupAsync();

        var errors = await ValidateExamAsync(group);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateBatchAsync_ExamWithWrongPairOrSubject_Errors()
    {
        var group = await SeedGroupAsync();

        var wrongPair = await ValidateExamAsync(group, p => p.NumberPair = 3);
        wrongPair
            .Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("Пара для экзамена должна быть 0.");

        var wrongSubject = await ValidateExamAsync(group, p => p.Subject = "Физика");
        wrongSubject
            .Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("Предмет экзамена должен быть «ЭКЗАМЕН».");
    }

    [Fact]
    public async Task ValidateBatchAsync_ExamWithForbiddenFields_Errors()
    {
        var group = await SeedGroupAsync();
        var teacherId = Guid.NewGuid();

        var errors = await ValidateExamAsync(
            group,
            p =>
            {
                p.TeacherId = teacherId;
                p.TeacherName = "Иванов И.И.";
                p.Note = "сам.р.";
                p.RemovedSubject = "Физика";
                p.RemovedTeacherId = teacherId;
                p.RemovedTeacherName = "Петров П.П.";
                p.RemovedNumberPair = 2;
            }
        );

        errors
            .Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("Для экзамена не указываются преподаватель, примечание и снимаемое занятие.");
    }
}
