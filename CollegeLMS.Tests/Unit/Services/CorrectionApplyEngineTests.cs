using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Симуляция и применение позиций корректировки (UC-SCH-22–26):
/// порядок строк, «сам.р.», перенос, замена и снятие.
/// </summary>
public class CorrectionApplyEngineTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionApplyEngine _sut;

    private static readonly DateTime TestDate = new(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
    private const int TestDay = (int)DayOfWeek.Tuesday;
    private const int TestWeek = 2;

    public CorrectionApplyEngineTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new CorrectionApplyEngine(_db, new BellScheduleServiceStub());
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

    private async Task<ScheduleEntry> SeedEntryAsync(
        Guid groupId,
        Guid? teacherId,
        string subject,
        int numberPair,
        params int[] weeks
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
            DayOfWeek = DayOfWeek.Tuesday,
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

    private async Task<CorrectionBatch> SeedBatchAsync(
        Group group,
        params CorrectionPosition[] positions
    )
    {
        var utcNow = DateTime.UtcNow;
        var batch = new CorrectionBatch
        {
            Id = Guid.NewGuid(),
            CorrectionDate = TestDate,
            Week = TestWeek,
            DayOfWeek = TestDay,
            Status = CorrectionBatchStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        foreach (var position in positions)
        {
            position.BatchId = batch.Id;
            position.GroupId = group.Id;
            position.GroupName = group.Name;
            position.DayOfWeek = TestDay;
            position.Week = TestWeek;
            position.Status = CorrectionPositionStatus.Draft;
            position.CreatedAt = utcNow;
            position.UpdatedAt = utcNow;
            batch.Positions.Add(position);
        }

        _db.CorrectionBatches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    private static CorrectionPosition Pos(
        int row,
        ScheduleChangeType changeType,
        int numberPair,
        string? subject = null,
        Guid? teacherId = null,
        string? teacherName = null,
        string? removedSubject = null,
        Guid? removedTeacherId = null,
        string? removedTeacherName = null,
        int? removedNumberPair = null,
        string? note = null
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Row = row,
            ChangeType = changeType,
            NumberPair = numberPair,
            Subject = subject,
            TeacherId = teacherId,
            TeacherName = teacherName,
            RemovedSubject = removedSubject,
            RemovedTeacherId = removedTeacherId,
            RemovedTeacherName = removedTeacherName,
            RemovedNumberPair = removedNumberPair,
            Note = note,
        };

    [Fact]
    public async Task ValidateBatchAsync_AddThenRemoveSamePair_IsValid()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Add,
                3,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName
            ),
            Pos(
                2,
                ScheduleChangeType.Remove,
                3,
                removedSubject: "Математика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteBatchAsync_AddThenRemoveSamePair_RemovesAddedEntry()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Add,
                3,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName
            ),
            Pos(
                2,
                ScheduleChangeType.Remove,
                3,
                removedSubject: "Математика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName
            )
        );

        var outcome = await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        outcome.History.Should().HaveCount(2);
        outcome
            .History.Select(h => h.ChangeType)
            .Should()
            .BeEquivalentTo([ScheduleChangeType.Add, ScheduleChangeType.Remove]);
        _db.ScheduleEntries.Should().BeEmpty();
        _db.CorrectionPositions.Should()
            .OnlyContain(p => p.Status == CorrectionPositionStatus.Applied);
    }

    [Fact]
    public async Task ExecuteBatchAsync_RemoveWithoutSelfStudy_RemovesWeek()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, 2, 3);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Remove,
                2,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName
            )
        );

        var result = await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        result.History.Should().ContainSingle();
        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.Weeks.Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ExecuteBatchAsync_RemoveLastWeek_DeletesEntry()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, 2);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Remove,
                2,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName
            )
        );

        await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        _db.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildEffectiveEntriesAsync_RemoveWithSelfStudy_MarksEntry()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, 2);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Remove,
                2,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName,
                note: "сам.р."
            )
        );

        var entries = await _sut.BuildEffectiveEntriesAsync(
            group.Id,
            DayOfWeek.Tuesday,
            TestWeek,
            batch.Id,
            CancellationToken.None
        );

        var entry = entries.Should().ContainSingle().Subject;
        entry.IsSelfStudy.Should().BeTrue();
        entry.Removed.Should().BeFalse();
        entry.Note.Should().Be("сам.р.");
        entry.PendingChangeType.Should().Be(nameof(ScheduleChangeType.Remove));

        var persisted = _db.ScheduleEntries.Should().ContainSingle().Subject;
        persisted.Weeks.Should().BeEquivalentTo([2]);
    }

    [Fact]
    public async Task ValidateBatchAsync_AddWithSelfStudyNote_IsAllowed()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Add,
                3,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName,
                note: "сам.р."
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteBatchAsync_Replace_ChangesPairAndWritesRemovedHistory()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, 2);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Replace,
                2,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName,
                removedNumberPair: 2
            )
        );

        await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(2);
        entry.Subject.Should().Be("Математика");

        var history = _db.ScheduleHistory.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Replace);
        history.NumberPair.Should().Be(2);
        history.RemovedNumberPair.Should().Be(2);
        history.RemovedSubject.Should().Be("Физика");
        history.RemovedTeacherId.Should().Be(teacher.Id);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Move_ChangesPairAndWritesRemovedHistory()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, 2);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Move,
                4,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName,
                removedNumberPair: 2
            )
        );

        await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(4);
        entry.Subject.Should().Be("Математика");

        var history = _db.ScheduleHistory.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Move);
        history.NumberPair.Should().Be(4);
        history.RemovedNumberPair.Should().Be(2);
        history.RemovedSubject.Should().Be("Физика");
    }

    [Fact]
    public async Task ExecuteBatchAsync_ReplaceWithMove_RemovesBothPairsAndWritesTwoHistories()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 3, TestWeek);
        await SeedEntryAsync(group.Id, teacher.Id, "Математика", 5, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Replace,
                3,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName,
                removedSubject: "Физика",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName,
                removedNumberPair: 5,
                note: "вм.5 п."
            )
        );

        await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(3);
        entry.Subject.Should().Be("Математика");

        _db.ScheduleHistory.Should().HaveCount(2);
        _db.ScheduleHistory.Should()
            .Contain(h =>
                h.ChangeType == ScheduleChangeType.Replace
                && h.NumberPair == 3
                && h.RemovedNumberPair == 3
                && h.RemovedSubject == "Физика"
            );
        _db.ScheduleHistory.Should()
            .Contain(h =>
                h.ChangeType == ScheduleChangeType.Remove
                && h.NumberPair == 5
                && h.Subject == "Математика"
            );
    }

    [Fact]
    public async Task BuildEffectiveEntriesAsync_MoveWithSelfStudyNote_MarksNewPairAsSelfStudy()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync("Федорова А.А.");
        await SeedEntryAsync(group.Id, teacher.Id, "МДК.04.02.", 1, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Move,
                5,
                subject: "МДК.04.02.",
                teacherName: teacher.User.FullName,
                removedSubject: "МДК.04.02.",
                removedTeacherName: teacher.User.FullName,
                removedNumberPair: 1,
                note: "сам.р.вм.1 п."
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);
        errors.Should().BeEmpty();

        var entries = await _sut.BuildEffectiveEntriesAsync(
            group.Id,
            DayOfWeek.Tuesday,
            TestWeek,
            batch.Id,
            CancellationToken.None
        );

        entries.Should().NotContain(e => e.NumberPair == 1);
        entries.Should().Contain(e => e.NumberPair == 5 && e.IsSelfStudy);
    }

    [Fact]
    public async Task ValidateBatchAsync_MoveWithoutRemovedNumberPair_ReturnsRowError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Move,
                4,
                subject: "Математика",
                teacherId: teacher.Id,
                teacherName: teacher.User.FullName
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        var error = errors.Should().ContainSingle().Subject;
        error.Message.Should().StartWith("Строка 1:");
        error.Message.Should().Contain("старая пара");
    }

    [Fact]
    public async Task ValidateBatchAsync_MissingPair_ReturnsNotFoundError()
    {
        var group = await SeedGroupAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(1, ScheduleChangeType.Remove, 5, removedSubject: "Физика")
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        var error = errors.Should().ContainSingle().Subject;
        error.Message.Should().StartWith("Строка 1:");
        error.Message.Should().Contain("пара 5 не найдена");
    }

    [Fact]
    public async Task ValidateBatchAsync_UnknownTeacher_ReturnsRowError()
    {
        var group = await SeedGroupAsync();
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Add,
                3,
                subject: "Математика",
                teacherName: "Неизвестный Х.Х."
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        var error = errors.Should().ContainSingle().Subject;
        error.Message.Should().StartWith("Строка 1:");
        error.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task ValidateBatchAsync_SubjectSpellingDiffers_MatchesByLookupKey()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync("Абатуров С.А.");
        await SeedEntryAsync(group.Id, teacher.Id, "ОБПиЗР", 5, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Remove,
                5,
                removedSubject: "ОБП и ЗР",
                removedTeacherId: teacher.Id,
                removedTeacherName: teacher.User.FullName
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateBatchAsync_AbbreviatedSubject_MatchesByUniqueTeacher()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync("Минина М.Х.");
        await SeedEntryAsync(group.Id, teacher.Id, "Обществоз.", 4, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Remove,
                4,
                removedSubject: "Обществ.",
                removedTeacherName: teacher.User.FullName
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateBatchAsync_AmbiguousPairWithoutTeacher_ReturnsNotFoundError()
    {
        var group = await SeedGroupAsync();
        var firstTeacher = await SeedTeacherAsync("Иванова И.И.");
        var secondTeacher = await SeedTeacherAsync("Петрова П.П.");
        await SeedEntryAsync(group.Id, firstTeacher.Id, "Биология", 2, TestWeek);
        await SeedEntryAsync(group.Id, secondTeacher.Id, "Химия", 2, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(1, ScheduleChangeType.Remove, 2, removedSubject: "Биологи")
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        var error = errors.Should().ContainSingle().Subject;
        error.Message.Should().StartWith("Строка 1:");
        error.Message.Should().Contain("пара 2 не найдена");
    }

    [Fact]
    public async Task ExecuteBatchAsync_SlashTeacher_ResolvesFirstKnownTeacher()
    {
        var group = await SeedGroupAsync();
        var removedTeacher = await SeedTeacherAsync("Кобзаренко Л.Н.");
        var firstVariantTeacher = await SeedTeacherAsync("Кривцова С.Н.");
        await SeedTeacherAsync("Степаненко А.В.");
        await SeedEntryAsync(group.Id, removedTeacher.Id, "ЭкономОтр.", 4, TestWeek);
        var batch = await SeedBatchAsync(
            group,
            Pos(
                1,
                ScheduleChangeType.Replace,
                4,
                subject: "Ин.язык(1и2)",
                teacherName: "Кривцова/Степаненко",
                removedSubject: "Эконом.отр.",
                removedTeacherName: removedTeacher.User.FullName,
                removedNumberPair: 4
            )
        );

        var errors = await _sut.ValidateBatchAsync(batch, CancellationToken.None);

        errors.Should().BeEmpty();

        await _sut.ExecuteBatchAsync(batch, Guid.NewGuid(), CancellationToken.None);
        await _db.SaveChangesAsync();

        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.TeacherId.Should().Be(firstVariantTeacher.Id);
    }
}
