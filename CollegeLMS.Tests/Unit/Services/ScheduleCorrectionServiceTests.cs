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

public class ScheduleCorrectionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleCorrectionService _sut;

    public ScheduleCorrectionServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = CreateSut(new HttpClient());
    }

    public void Dispose() => _db.Dispose();

    private ScheduleCorrectionService CreateSut(HttpClient http) =>
        new(_db, new MaxBotHttpClient(http, NullLogger<MaxBotHttpClient>.Instance));

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
        int pair,
        List<int> weeks
    )
    {
        var utcNow = DateTime.UtcNow;
        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = teacherId,
            Subject = subject,
            Room = "303",
            DayOfWeek = DayOfWeek.Tuesday,
            NumberPair = pair,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Weeks = weeks,
            LessonType = LessonType.Lecture,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    private static (Stream Stream, string FileName) BuildWorkbook(Action<IXLWorksheet> fill)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Корректировка");
        ws.Cell(3, 1).Value = "Корректировка на 08.09.2026 г.";
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
        return (ms, "correction.xlsx");
    }

    // --- Задача 8.1: превью ---

    [Fact]
    public async Task PreviewAsync_InvalidFile_ReturnsStructureError()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var result = await _sut.PreviewAsync(stream, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task PreviewAsync_ValidAdd_ReturnsEntry()
    {
        var group = await SeedGroupAsync();
        await SeedTeacherAsync();

        var (stream, _) = BuildWorkbook(
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
                ws.Cell(7, 7).Value = "вм. 4 п";
            }
        );

        using (stream)
        {
            var result = await _sut.PreviewAsync(stream, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Errors.Should().BeEmpty();
            result.Data!.TotalEntries.Should().Be(1);
            result.Data!.CorrectionDate.Should().Be(new DateTime(2026, 9, 8));
            result.Data!.Week.Should().Be(2);
            var entry = result.Data!.Entries[0];
            entry.ChangeType.Should().Be(ScheduleChangeType.Add);
            entry.NumberPair.Should().Be(4);
            entry.GroupId.Should().Be(group.Id);
            entry.Subject.Should().Be("Математика");
        }
    }

    [Fact]
    public async Task PreviewAsync_UnknownGroup_ReturnsDataError()
    {
        await SeedTeacherAsync("Марченко И.А.");

        var (stream, _) = BuildWorkbook(
            ws =>
            {
                ws.Cell(7, 1).Value = "ИНВ-999";
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
            }
        );

        using (stream)
        {
            var result = await _sut.PreviewAsync(stream, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var error = result.Data!.Errors.Should().ContainSingle().Subject;
            error.Level.Should().Be("data");
            error.Message.Should().Contain("группа");
        }
    }

    [Fact]
    public async Task PreviewAsync_AddOnBusyPair_ReturnsLogicError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 4, [2]);

        var (stream, _) = BuildWorkbook(
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
            }
        );

        using (stream)
        {
            var result = await _sut.PreviewAsync(stream, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Entries.Should().BeEmpty();
            var error = result.Data!.Errors.Should().ContainSingle().Subject;
            error.Level.Should().Be("logic");
            error.Message.Should().Contain("уже занята");
        }
    }

    [Fact]
    public async Task PreviewAsync_ReplaceSamePair_ReturnsLogicError()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 4, [2]);

        var (stream, _) = BuildWorkbook(
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 2).Value = "Физика";
                ws.Cell(7, 3).Value = "Марченко И.А.";
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
            }
        );

        using (stream)
        {
            var result = await _sut.PreviewAsync(stream, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Entries.Should().BeEmpty();
            var error = result.Data!.Errors.Should().ContainSingle().Subject;
            error.Level.Should().Be("logic");
            error.Message.Should().Contain("одинакова");
        }
    }

    [Fact]
    public async Task PreviewAsync_EmptyAddAndRemove_ReturnsDataError()
    {
        var group = await SeedGroupAsync();

        var (stream, _) = BuildWorkbook(
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 6).Value = 2;
            }
        );

        using (stream)
        {
            var result = await _sut.PreviewAsync(stream, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var error = result.Data!.Errors.Should().ContainSingle().Subject;
            error.Level.Should().Be("data");
            error.Message.Should().Contain("не заполнены");
        }
    }

    // --- Задача 8.2: подтверждение ---

    [Fact]
    public async Task ConfirmAsync_Add_CreatesEntryAndHistory()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var appliedBy = Guid.NewGuid();

        var result = await _sut.ConfirmAsync(
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Add,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 4,
                        Subject = "Математика",
                        TeacherId = teacher.Id,
                        TeacherName = "Марченко И.А.",
                        Note = "вм. 4 п",
                    },
                ],
            },
            appliedBy,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Applied.Should().Be(1);

        var saved = _db.ScheduleEntries.Should().ContainSingle().Subject;
        saved.Weeks.Should().BeEquivalentTo([2]);
        saved.LessonType.Should().Be(LessonType.Practice);
        saved.NumberPair.Should().Be(4);
        saved.EndTime.Should().BeGreaterThan(saved.StartTime);

        var history = _db.ScheduleHistory.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Add);
        history.Week.Should().Be(2);
        history.AppliedByUserId.Should().Be(appliedBy);
    }

    [Fact]
    public async Task ConfirmAsync_Remove_RemovesWeekFromEntry()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var entry = await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [1, 2]);

        var result = await _sut.ConfirmAsync(
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Remove,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 2,
                    },
                ],
            },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var saved = _db.ScheduleEntries.Single();
        saved.Id.Should().Be(entry.Id);
        saved.Weeks.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public async Task ConfirmAsync_RemoveLastWeek_DeletesEntry()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2]);

        var result = await _sut.ConfirmAsync(
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Remove,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 2,
                    },
                ],
            },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        _db.ScheduleEntries.Should().BeEmpty();
        _db.ScheduleHistory.Should().ContainSingle(h => h.ChangeType == ScheduleChangeType.Remove);
    }

    [Fact]
    public async Task ConfirmAsync_Replace_SwapsEntryAndWritesHistory()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var entry = await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2]);

        var result = await _sut.ConfirmAsync(
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Replace,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 4,
                        Subject = "Математика",
                        TeacherId = teacher.Id,
                        TeacherName = "Марченко И.А.",
                        RemovedSubject = "Физика",
                        RemovedTeacherId = teacher.Id,
                        RemovedTeacherName = "Марченко И.А.",
                        RemovedNumberPair = 2,
                        Note = "вм. 4 п",
                    },
                ],
            },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();

        var saved = _db.ScheduleEntries.Should().ContainSingle().Subject;
        saved.Id.Should().NotBe(entry.Id);
        saved.NumberPair.Should().Be(4);
        saved.Subject.Should().Be("Математика");

        var history = _db.ScheduleHistory.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Replace);
        history.RemovedSubject.Should().Be("Физика");
        history.RemovedNumberPair.Should().Be(2);
        history.NumberPair.Should().Be(4);
    }

    [Fact]
    public async Task ConfirmAsync_FailedOperation_RollsBackAllEntries()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        // Первая операция валидна, вторая (Replace) падает — для неё нет занятия в БД.
        var request = new CorrectionConfirmRequest
        {
            Entries =
            [
                new CorrectionPreviewEntry
                {
                    GroupId = group.Id,
                    ChangeType = ScheduleChangeType.Add,
                    DayOfWeek = 2,
                    Week = 2,
                    NumberPair = 4,
                    Subject = "Математика",
                    TeacherId = teacher.Id,
                },
                new CorrectionPreviewEntry
                {
                    GroupId = group.Id,
                    ChangeType = ScheduleChangeType.Replace,
                    DayOfWeek = 2,
                    Week = 2,
                    NumberPair = 4,
                    Subject = "История",
                    TeacherId = teacher.Id,
                    RemovedSubject = "Физика",
                    RemovedTeacherId = teacher.Id,
                },
            ],
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmAsync(request, Guid.NewGuid(), CancellationToken.None)
        );

        using var fresh = TestDbContextFactory.Create();
        fresh.ScheduleEntries.Should().BeEmpty();
        fresh.ScheduleHistory.Should().BeEmpty();
    }

    // --- Задача 8.3: fail-safe MaxBot ---

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => throw new HttpRequestException("MaxBot недоступен");
    }

    [Fact]
    public async Task ConfirmAsync_MaxBotDown_StillReturnsOk()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var sut = CreateSut(new HttpClient(new ThrowingHandler()));

        var result = await sut.ConfirmAsync(
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Add,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 4,
                        Subject = "Математика",
                        TeacherId = teacher.Id,
                    },
                ],
            },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Applied.Should().Be(1);
        _db.ScheduleHistory.Should().ContainSingle();
    }
}