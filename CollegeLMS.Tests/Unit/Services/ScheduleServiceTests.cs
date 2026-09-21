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
        var bells = new BellScheduleServiceStub();
        var exportService = new ScheduleExportService(_db, bells);
        _sut = new ScheduleService(_db, exportService, bells);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoEntries()
    {
        var result = await _sut.GetAllAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            default
        );

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

        var result = await _sut.GetAllAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            default
        );

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

        var result = await _sut.GetAllAsync(
            groupId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_DayQueryByDate_ReturnsAllPairsForThatDay()
    {
        // Группа с постоянными парами 1–8 на понедельник (все учебные недели)
        // + пара 6 добавлена корректировкой на 3-ю неделю (Weeks = [3]).
        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "ГР-11",
                Course = 1,
            }
        );
        var basePairs = Enumerable
            .Range(1, 8)
            .Select(n => new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Subject = $"Предмет {n}",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = n,
                StartTime = new TimeSpan(9 + n, 0, 0),
                EndTime = new TimeSpan(10 + n, 20, 0),
                Weeks = Enumerable.Range(1, 16).ToList(),
                LessonType = LessonType.Lecture,
            })
            .ToList();

        _db.ScheduleEntries.AddRange(basePairs);
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Add,
                GroupId = groupId,
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 6,
                Week = 3,
                Subject = "Физкультура",
                AppliedAt = DateTime.UtcNow,
                AppliedByUserId = Guid.NewGuid(),
            }
        );
        await _db.SaveChangesAsync();

        // Понедельник 3-й учебной недели: 2026-09-14.
        var dayResult = await _sut.GetAllAsync(
            groupId,
            null,
            null,
            null,
            null,
            null,
            new DateTime(2026, 9, 14),
            null,
            null,
            null,
            default
        );

        dayResult.IsSuccess.Should().BeTrue();
        dayResult.Data!.Items.Should().HaveCount(8);
        dayResult
            .Data.Items.Select(i => i.NumberPair)
            .Should()
            .BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8]);
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
            CyclicalCommission = "CS",
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Weeks = new() { 1 },
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
            NumberPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Weeks = new() { 1 },
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

        var result = await _sut.ExportScheduleAsync(
            null,
            null,
            null,
            null,
            null,
            ExportFormat.Pdf,
            ExportLayout.Grid,
            default
        );

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

        var result = await _sut.ExportScheduleAsync(
            null,
            null,
            null,
            null,
            null,
            ExportFormat.Xlsx,
            ExportLayout.Grid,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileContent.Should().NotBeEmpty();
        result
            .Data.ContentType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportScheduleAsync_ReturnsFail_WhenNoData()
    {
        var result = await _sut.ExportScheduleAsync(
            null,
            null,
            null,
            null,
            null,
            ExportFormat.Pdf,
            ExportLayout.Grid,
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetSubjectsAsync_ReturnsDistinctSubjects_FromEntriesAndHistory()
    {
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 1 },
                LessonType = LessonType.Lecture,
            }
        );
        _db.ScheduleHistory.AddRange(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Add,
                Subject = "История",
                AppliedAt = DateTime.UtcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                Week = 1,
            },
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Remove,
                Subject = "История",
                RemovedSubject = "Литература",
                AppliedAt = DateTime.UtcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 2,
                Week = 1,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetSubjectsAsync(null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Should().BeEquivalentTo(["Математика", "История", "Литература"]);
    }

    [Fact]
    public async Task GetSubjectsAsync_FiltersByQuery_IgnoreCase()
    {
        _db.ScheduleEntries.AddRange(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 1 },
                LessonType = LessonType.Lecture,
            },
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Subject = "Литература",
                Room = "401",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 1 },
                LessonType = LessonType.Lecture,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetSubjectsAsync("мат", null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Should().ContainSingle().Which.Should().Be("Математика");
    }

    [Fact]
    public async Task GetSubjectsAsync_WithTeacherId_ReturnsOnlyTeacherSubjects()
    {
        var utcNow = DateTime.UtcNow;
        var teacherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher-subjects@collegelms.ru",
            FullName = "Иванов И.И.",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            CyclicalCommission = "ЦК",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = teacherUser,
        };
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-subjects@collegelms.ru",
            FullName = "Петров П.П.",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var otherTeacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            CyclicalCommission = "ЦК",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = otherUser,
        };
        _db.Teachers.AddRange(teacher, otherTeacher);

        _db.ScheduleEntries.AddRange(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = teacher.Id,
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 1 },
                LessonType = LessonType.Lecture,
            },
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = otherTeacher.Id,
                Subject = "История",
                Room = "302",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 2,
                StartTime = new TimeSpan(10, 40, 0),
                EndTime = new TimeSpan(12, 10, 0),
                Weeks = new List<int> { 1 },
                LessonType = LessonType.Lecture,
            }
        );
        _db.ScheduleHistory.AddRange(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Add,
                Subject = "Физика",
                TeacherId = teacher.Id,
                AppliedAt = utcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 3,
                Week = 1,
            },
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = ScheduleChangeType.Remove,
                Subject = "Литература",
                RemovedSubject = "Химия",
                RemovedTeacherId = teacher.Id,
                AppliedAt = utcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 4,
                Week = 1,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetSubjectsAsync(null, teacher.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Should().BeEquivalentTo(["Математика", "Физика", "Химия"]);
    }

    [Fact]
    public async Task GetJournalAsync_ReturnsConductedOnly_WithDayOfWeek()
    {
        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "journal@collegelms.ru",
            FullName = "Марченко И.А.",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "CS",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = user,
        };
        _db.Teachers.Add(teacher);

        // Проведённое занятие: Вторник 2-й недели => 08.09.2026 (не позже сегодняшнего дня).
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = teacher.Id,
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 2 },
                LessonType = LessonType.Lecture,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        // Будущее занятие: Вторник 6-й недели => 05.10.2026 (должно быть пропущено).
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = teacher.Id,
                Subject = "Литература",
                Room = "302",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new List<int> { 6 },
                LessonType = LessonType.Lecture,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TeacherName.Should().Be("Марченко И.А.");
        result.Data.Subjects.Should().ContainSingle(s => s.Subject == "Математика");
        result.Data.Subjects.Should().NotContain(s => s.Subject == "Литература");
        var item = result.Data.Subjects.Single().Items.Should().ContainSingle().Subject;
        item.DayOfWeek.Should().Be((int)DayOfWeek.Tuesday);
        item.Date.Should().Be(new DateTime(2026, 9, 8));
        item.NumberPairs.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public async Task GetJournalAsync_ReturnsNotFound_WhenTeacherMissing()
    {
        var result = await _sut.GetJournalAsync(Guid.NewGuid(), null, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllAsync_DateOnNonWorkingDay_ReturnsEmpty()
    {
        var utcNow = DateTime.UtcNow;
        _db.NonWorkingDays.Add(
            new NonWorkingDay
            {
                Id = Guid.NewGuid(),
                DateFrom = new DateTime(2026, 9, 8),
                DateTo = new DateTime(2026, 9, 8),
                Title = "Праздник",
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            new DateTime(2026, 9, 8),
            null,
            null,
            null,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_DateOffNonWorkingDay_ReturnsEntriesForThatDay()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
        foreach (var entry in entries)
        {
            entry.DayOfWeek = DayOfWeek.Tuesday;
            entry.Weeks = [2];
        }
        _db.ScheduleEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            new DateTime(2026, 9, 8),
            null,
            null,
            null,
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
    }
}
