using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly ScheduleService _sut;

    public ScheduleServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var exportService = new ScheduleExportService(_db);
        var importService = new ScheduleImportService(_db);
        _sut = new ScheduleService(_db, exportService, importService);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoEntries()
    {
        var result = await _sut.GetAllAsync(null, null, null, null, null, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntries()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, null, null, null, null, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByGroupId()
    {
        var groupId = Guid.NewGuid();
        var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
        entries[0].GroupId = groupId;
        entries[0].Group!.Id = groupId;
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(groupId, null, null, null, null, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntry_WhenFound()
    {
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entry.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(entry.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesEntry_WhenValid()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Department = "CS",
            Position = "Professor",
        };
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var request = new CreateScheduleRequest
        {
            GroupId = group.Id,
            TeacherId = teacher.Id,
            Subject = "Математика",
            Room = "301",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture.ToString(),
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subject.Should().Be("Математика");
        result.Data.Room.Should().Be("301");
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenGroupNotFound()
    {
        var request = new CreateScheduleRequest
        {
            GroupId = Guid.NewGuid(),
            Subject = "Математика",
            Room = "301",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture.ToString(),
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenTeacherNotFound()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var request = new CreateScheduleRequest
        {
            GroupId = group.Id,
            TeacherId = Guid.NewGuid(),
            Subject = "Математика",
            Room = "301",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture.ToString(),
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenTimeOverlap()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var existing = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            Subject = "Физика",
            Room = "301",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture,
        };
        _db.ScheduleEntries.Add(existing);
        await _db.SaveChangesAsync();

        var request = new CreateScheduleRequest
        {
            GroupId = group.Id,
            Subject = "Математика",
            Room = "302",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            LessonType = LessonType.Practice.ToString(),
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntry_WhenValid()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            Subject = "Старая тема",
            Room = "101",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture,
        };
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();

        var request = new UpdateScheduleRequest
        {
            GroupId = group.Id,
            Subject = "Обновлённая тема",
            Room = "402",
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            LessonType = LessonType.Lab.ToString(),
        };

        var result = await _sut.UpdateAsync(entry.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subject.Should().Be("Обновлённая тема");
        result.Data.Room.Should().Be("402");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateScheduleRequest
        {
            GroupId = Guid.NewGuid(),
            Subject = "Тема",
            Room = "301",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            LessonType = LessonType.Lecture.ToString(),
        };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntry()
    {
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(entry.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.ScheduleEntries.AnyAsync(s => s.Id == entry.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ExportScheduleAsync_ReturnsPdfBytes_WhenEntriesExist()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Pdf, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeEmpty();
        result.Data.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ExportScheduleAsync_ReturnsXlsxBytes_WhenEntriesExist()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Xlsx, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeEmpty();
        result.Data.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportScheduleAsync_ReturnsFail_WhenNoData()
    {
        var result = await _sut.ExportScheduleAsync(null, null, null, null, ExportFormat.Pdf, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
