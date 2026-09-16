using System.Net.Http;
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
        var maxBot = new MaxBotHttpClient(new HttpClient(), NullLogger<MaxBotHttpClient>.Instance);
        var correctionService = new ScheduleCorrectionService(_db, maxBot);
        _sut = new CorrectionBatchService(_db, correctionService, maxBot);
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
}
