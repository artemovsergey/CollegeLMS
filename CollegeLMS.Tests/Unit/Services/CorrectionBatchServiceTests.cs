using System.Net.Http;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

public class CorrectionBatchServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _sut;

    public CorrectionBatchServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var engine = new CorrectionApplyEngine(_db, new BellScheduleServiceStub());
        var maxBot = new MaxBotHttpClient(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );
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
    public async Task AddPositionAsync_PairOutsideDayRange_ReturnsBadRequest()
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
                NumberPair = 8,
                Subject = "Математика",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("1 до 7");
    }

    [Fact]
    public async Task AddPositionAsync_LegacyReplaceRequest_StoredAsAddWithRemovedLesson()
    {
        // Старые клиенты и файлы присылают замену отдельным типом — в базе она
        // хранится как добавление со снимаемым занятием.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await CreateBatchAsync();

        var result = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Replace,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Математика",
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
                RemovedSubject = "ОБП и ЗР",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                RemovedNumberPair = 2,
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.ChangeType.Should().Be(ScheduleChangeType.Add);
        result.Data!.RemovedSubject.Should().Be("ОБП и ЗР");
        result.Data!.RemovedNumberPair.Should().BeNull();
    }

    [Fact]
    public async Task AddPositionAsync_AllowsSelfStudyOnAdd()
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

        result.IsSuccess.Should().BeTrue();
        result.Data!.Note.Should().Be("сам.р.");
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
    public async Task ApplyAsync_AddWithFromPairNote_MovesLessonAndVacatesSource()
    {
        // «Добавить» + «вм.4 п.» при вводе в пару 2 — это перенос: занятие
        // переезжает из пары 4 в пару 2, в паре 4 остаётся пусто. Без этой
        // отметки оно считалось бы в обеих парах.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 4, [2]);
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Move,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Физика",
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                RemovedNumberPair = 4,
                Note = "вм.4 п.",
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(2);
        entry.Weeks.Should().BeEquivalentTo([2]);
    }

    [Fact]
    public async Task ApplyAsync_ReplaceWithFromPairNote_ReplacesInTargetAndFreesSource()
    {
        // «Заменить» + «вм.4 п.» при вводе в пару 2: в паре 2 вместо
        // прежнего встанет новое, а само новое освободит пару 4.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var newTeacher = await SeedTeacherAsync("Сидоров С.С.");
        await SeedEntryAsync(group.Id, teacher.Id, "ОБП и ЗР", 2, [2]);
        await SeedEntryAsync(group.Id, newTeacher.Id, "Обществ.", 4, [2]);
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Replace,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Обществ.",
                TeacherId = newTeacher.Id,
                TeacherName = newTeacher.User.FullName,
                RemovedSubject = "ОБП и ЗР",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                RemovedNumberPair = 4,
                Note = "вм.4 п.",
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        // Пара 4 освободилась, в паре 2 осталось новое занятие.
        var entries = _db.ScheduleEntries.OrderBy(e => e.NumberPair).ToList();
        entries.Should().ContainSingle();
        entries[0].NumberPair.Should().Be(2);
        entries[0].Subject.Should().Be("Обществ.");

        // В журнале две записи: добавленное занятие в паре 2 (вместо прежнего) и
        // снятое из пары 4.
        result.Data!.Applied.Should().Be(2);
        result
            .Data!.History.Select(h => h.ChangeType)
            .Should()
            .BeEquivalentTo([ScheduleChangeType.Add, ScheduleChangeType.Remove]);
    }

    [Fact]
    public async Task ApplyAsync_ReplaceInSamePair_SwapsLessonInOneTransaction()
    {
        // Обычная замена без «вм.X»: прежнее занятие исчезает, новое встаёт
        // на его место, пара не меняется.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var newTeacher = await SeedTeacherAsync("Сидоров С.С.");
        await SeedEntryAsync(group.Id, teacher.Id, "ОБП и ЗР", 2, [2]);
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Replace,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Обществ.",
                TeacherId = newTeacher.Id,
                TeacherName = newTeacher.User.FullName,
                RemovedSubject = "ОБП и ЗР",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                RemovedNumberPair = 2,
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(2);
        entry.Subject.Should().Be("Обществ.");
        // Одна запись журнала: замена — это добавление со снимаемым занятием,
        // поэтому в журнале тип «добавлено» и снятое занятие в полях Removed*.
        var history = result.Data!.History.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Add);
        history.RemovedSubject.Should().Be("ОБП и ЗР");
    }

    [Fact]
    public async Task ApplyAsync_AddWithSelfStudyNote_OnlyInformsAndKeepsHistory()
    {
        // «Добавить» с «сам.р.»: пара в расписание не встаёт — нужна только
        // пометка, поэтому в базе остаётся запись журнала.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
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
                Note = "сам.р.",
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        _db.ScheduleEntries.Should().BeEmpty("пара только для информирования");

        var history = result.Data!.History.Should().ContainSingle().Subject;
        history.ChangeType.Should().Be(ScheduleChangeType.Add);
        history.NumberPair.Should().Be(3);
        history.Note.Should().Be("сам.р.");
    }

    [Fact]
    public async Task ApplyAsync_AddWithSelfStudyPlusNote_AddsLessonAndKeepsBadge()
    {
        // «сам.р+»: та же самостоятельная работа, но пара физически встаёт в
        // расписание.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
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
                Note = "сам.р+",
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var entry = _db.ScheduleEntries.Should().ContainSingle().Subject;
        entry.NumberPair.Should().Be(3);
        entry.Subject.Should().Be("Математика");
        result.Data!.History.Should().ContainSingle().Which.Note.Should().Be("сам.р+");
    }

    [Fact]
    public async Task ApplyAsync_AddIntoOccupiedPair_KeepsBothLessons()
    {
        // Пара — это время: в неё можно положить второе занятие.
        // Добавление не должно молча превращаться в замену.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2]);
        var batch = await CreateBatchAsync();

        var added = await _sut.AddPositionAsync(
            batch,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 2,
                Subject = "Математика",
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue();
        added.Data!.ChangeType.Should().Be(ScheduleChangeType.Add);
        added.Data.RemovedSubject.Should().BeNull();
        added.Data.RemovedNumberPair.Should().BeNull();

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        // Обе пары на месте: исходная не тронута, новая добавлена рядом.
        _db.ScheduleEntries.Should().HaveCount(2);
        _db.ScheduleEntries.Select(e => e.Subject)
            .Should()
            .BeEquivalentTo(["Физика", "Математика"]);
        var change = result.Data!.History.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ScheduleChangeType.Add);
        change.Subject.Should().Be("Математика");
    }

    [Fact]
    public async Task ApplyAsync_RemoveSelfStudy_NotifiesTeacherWithNote()
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
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
                Note = "сам.р.",
            },
            CancellationToken.None
        );

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        // Уведомление уходит так же, как при обычном снятии: процессы одинаковые,
        // отличаются только примечание «сам.р.» и отсутствие удаления из базы.
        var change = result.Data!.History.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ScheduleChangeType.Remove);
        change.Note.Should().Be("сам.р.");
        change.TeacherId.Should().Be(teacher.Id);
        change.Subject.Should().Be("Физика");
        change.GroupName.Should().Be(group.Name);
    }

    [Fact]
    public async Task DeleteBatchAsync_AppliedBatch_RevertsScheduleAndHistory()
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
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
            },
            CancellationToken.None
        );

        var applied = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );
        applied.IsSuccess.Should().BeTrue();
        _db.ScheduleEntries.Single().Weeks.Should().BeEquivalentTo([1]);

        var deleted = await _sut.DeleteBatchAsync(batch, CancellationToken.None);

        deleted.IsSuccess.Should().BeTrue();
        deleted.Data!.Batches.Should().Be(1);
        deleted.Data.Reverted.Should().Be(1);
        _db.CorrectionBatches.Should().BeEmpty();
        _db.ScheduleHistory.Should().BeEmpty();
        // Пара вернулась в исходное состояние — обе недели на месте.
        _db.ScheduleEntries.Single().Weeks.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task ClearAppliedBatchesAsync_RevertsEveryAppliedBatch()
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
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
            },
            CancellationToken.None
        );
        await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        // Черновой пакет должен уцелеть.
        var draftBatch = await CreateBatchAsync();

        var cleared = await _sut.ClearAppliedBatchesAsync(CancellationToken.None);

        cleared.IsSuccess.Should().BeTrue();
        cleared.Data!.Batches.Should().Be(1);
        cleared.Data.Reverted.Should().Be(1);
        _db.CorrectionBatches.Select(b => b.Id)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(draftBatch);
        _db.ScheduleHistory.Should().BeEmpty();
        _db.ScheduleEntries.Single().Weeks.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task DeleteBatchAsync_LegacyHistoryWithoutBatch_RevertsAndRemovesIt()
    {
        // Записи, сделанные до появления связи с пакетом, лежат в журнале без
        // BatchId: по пакету они не находятся. Раньше из-за них пакет удалялся,
        // а изменение навсегда оставалось в меню «Изменения», и расписание не
        // возвращалось в исходное состояние.
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
                RemovedSubject = "Физика",
                RemovedTeacherId = teacher.Id,
                RemovedTeacherName = teacher.User.FullName,
            },
            CancellationToken.None
        );
        await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );
        _db.ScheduleEntries.Single().Weeks.Should().BeEquivalentTo([1]);

        // Обрываем связь с пакетом — как у записей, созданных старой версией.
        var applied = await _db.ScheduleHistory.ToListAsync();
        applied.Should().ContainSingle();
        applied[0].BatchId = null;
        await _db.SaveChangesAsync();

        var deleted = await _sut.DeleteBatchAsync(batch, CancellationToken.None);

        deleted.IsSuccess.Should().BeTrue();
        deleted.Data!.Reverted.Should().Be(1);
        _db.ScheduleHistory.Should().BeEmpty();
        _db.ScheduleEntries.Single().Weeks.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task DeleteBatchAsync_UnrelatedHistoryWithoutBatch_IsKept()
    {
        // Чужая запись журнала описывает другое изменение — её откатывать нельзя,
        // иначе удаление пакета испортило бы чужое расписание.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [1, 2]);
        await SeedEntryAsync(group.Id, teacher.Id, "Математика", 5, [2]);
        var batch = await CreateBatchAsync();

        await _sut.AddPositionAsync(
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
            },
            CancellationToken.None
        );
        await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        var applied = await _db.ScheduleHistory.ToListAsync();
        applied.Should().ContainSingle();
        applied[0].BatchId = null;
        applied[0].NumberPair = 5;
        applied[0].Subject = "Математика";
        await _db.SaveChangesAsync();

        await _sut.DeleteBatchAsync(batch, CancellationToken.None);

        _db.ScheduleHistory.Should().ContainSingle().Which.Subject.Should().Be("Математика");
    }

    [Fact]
    public async Task UpdateBatchAsync_ChangesDateAndPositionsWeek()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [3]);
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
            },
            CancellationToken.None
        );

        var newDate = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        var updated = await _sut.UpdateBatchAsync(
            batch,
            new CreateCorrectionBatchRequest { CorrectionDate = newDate },
            CancellationToken.None
        );

        updated.IsSuccess.Should().BeTrue();
        updated.Data!.Week.Should().Be(3);
        updated.Data.DayOfWeek.Should().Be((int)DayOfWeek.Thursday);
        // Позиция унаследовала новую неделю — иначе применилась бы не туда.
        _db.CorrectionPositions.Single(p => p.Id == added.Data!.Id).Week.Should().Be(3);
    }

    [Fact]
    public async Task UpdateBatchAsync_AppliedBatch_ReturnsConflict()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 2, [2]);
        var batch = await CreateBatchAsync();
        await _sut.AddPositionAsync(
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
            },
            CancellationToken.None
        );
        await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        var updated = await _sut.UpdateBatchAsync(
            batch,
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate.AddDays(7) },
            CancellationToken.None
        );

        updated.IsSuccess.Should().BeFalse();
        updated.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task DeleteBatchAsync_MissingBatch_ReturnsNotFound()
    {
        var missing = await _sut.DeleteBatchAsync(Guid.NewGuid(), CancellationToken.None);

        missing.IsSuccess.Should().BeFalse();
        missing.StatusCode.Should().Be(404);
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
    public async Task ImportAsync_FileRows_BecomeAddAndRemovePositions()
    {
        // В файле три вида строк: замена (обе колонки), перенос («вм.X») и
        // снятие. В пакете остаются только два типа — добавление и снятие.
        var group = await SeedGroupAsync();

        using var stream = BuildWorkbook(
            "Корректировка на 10.09.2026 г.",
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 2).Value = "ОБП и ЗР";
                ws.Cell(7, 3).Value = "Абатуров С.А.";
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 2;
                ws.Cell(8, 1).Value = group.Name;
                ws.Cell(8, 4).Value = "Математика";
                ws.Cell(8, 5).Value = "Марченко И.А.";
                ws.Cell(8, 6).Value = 5;
                ws.Cell(8, 7).Value = "вм.4 п.";
                ws.Cell(9, 1).Value = group.Name;
                ws.Cell(9, 2).Value = "Физика";
                ws.Cell(9, 3).Value = "Марченко И.А.";
                ws.Cell(9, 6).Value = 4;
                ws.Cell(9, 7).Value = "снять";
            }
        );

        var imported = await _sut.ImportAsync(stream, Guid.NewGuid(), CancellationToken.None);

        imported.IsSuccess.Should().BeTrue();
        imported.Data!.BatchId.Should().NotBeNull();
        imported
            .Data.Positions.Select(p => p.ChangeType)
            .Should()
            .Equal(ScheduleChangeType.Add, ScheduleChangeType.Add, ScheduleChangeType.Remove);

        // Замена: снимаемое занятие — из колонки «снимается».
        var replace = imported.Data.Positions[0];
        replace.RemovedSubject.Should().Be("ОБП и ЗР");
        replace.RemovedTeacherName.Should().Be("Абатуров С.А.");
        replace.RemovedNumberPair.Should().BeNull();

        // Перенос: снимается само вводимое занятие из пары «вм.X».
        var move = imported.Data.Positions[1];
        move.RemovedSubject.Should().Be("Математика");
        move.RemovedTeacherName.Should().Be("Марченко И.А.");
        move.RemovedNumberPair.Should().Be(4);

        var remove = imported.Data.Positions[2];
        remove.RemovedSubject.Should().Be("Физика");
    }

    [Fact]
    public async Task ExportAsync_Move_FillsRemovedColumnsWithFreedLesson()
    {
        // Перенос в файле диспетчера несёт только примечание «вм.X», и колонка
        // «снимается» выглядит незаполненной. В выгрузке занятие, которое
        // освобождает пару «откуда», должно быть видно — иначе по файлу не
        // понять, что именно переносится.
        var group = await SeedGroupAsync();

        using var stream = BuildWorkbook(
            "Корректировка на 11.09.2026 г.",
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 2;
                ws.Cell(7, 7).Value = "вм.4 п.";
            }
        );

        var imported = await _sut.ImportAsync(stream, Guid.NewGuid(), CancellationToken.None);
        imported.IsSuccess.Should().BeTrue();

        var exported = await _sut.ExportAsync(
            imported.Data!.BatchId!.Value,
            CancellationToken.None
        );
        exported.IsSuccess.Should().BeTrue();

        using var book = new XLWorkbook(new MemoryStream(exported.Data!.Content));
        var sheet = book.Worksheet(1);

        sheet.Cell(7, 2).GetString().Trim().Should().Be("Математика");
        sheet.Cell(7, 3).GetString().Trim().Should().Be("Марченко И.А.");
        sheet.Cell(7, 4).GetString().Trim().Should().Be("Математика");
        sheet.Cell(7, 7).GetString().Trim().Should().Be("вм.4 п.");

        // Выгруженный файл должен импортироваться обратно тем же переносом:
        // заполненная «снимается» — это то же вводимое занятие, а не замена
        // занятия в паре ввода.
        var reimported = await _sut.ImportAsync(
            new MemoryStream(exported.Data!.Content),
            Guid.NewGuid(),
            CancellationToken.None
        );

        reimported.IsSuccess.Should().BeTrue();
        reimported.Data!.Positions.Should().ContainSingle();
        var position = reimported.Data.Positions[0];
        position.ChangeType.Should().Be(ScheduleChangeType.Add);
        position.Subject.Should().Be("Математика");
        position.RemovedSubject.Should().Be("Математика");
        position.RemovedNumberPair.Should().Be(4);
    }

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

    // --- Нерабочие дни: создание и применение корректировки запрещены ---

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

    [Fact]
    public async Task CreateBatchAsync_NonWorkingDate_Returns400()
    {
        await SeedNonWorkingDayAsync(TestDate);

        var result = await _sut.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate },
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Дата нерабочая: День города.");
    }

    [Fact]
    public async Task ApplyAsync_NonWorkingDate_Returns400()
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
            },
            CancellationToken.None
        );
        added.IsSuccess.Should().BeTrue();

        await SeedNonWorkingDayAsync(TestDate);

        var result = await _sut.ApplyAsync(
            batch,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Дата нерабочая: День города.");
    }

    [Fact]
    public async Task ImportAsync_LaterRowUsesLessonAddedByEarlierRow_AndBatchApplies()
    {
        // Сквозной сценарий: файл вводит занятие и следом снимает его же. До правки
        // вторая строка не находилась и пакет оставался с ошибкой валидации.
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        using var stream = BuildWorkbook(
            "Корректировка на 08.09.2026 г.",
            ws =>
            {
                ws.Cell(7, 1).Value = group.Name;
                ws.Cell(7, 4).Value = "Математика";
                ws.Cell(7, 5).Value = "Марченко И.А.";
                ws.Cell(7, 6).Value = 4;
                ws.Cell(8, 1).Value = group.Name;
                ws.Cell(8, 2).Value = "Математика";
                ws.Cell(8, 3).Value = "Марченко И.А.";
                ws.Cell(8, 6).Value = 4;
            }
        );

        var imported = await _sut.ImportAsync(stream, Guid.NewGuid(), CancellationToken.None);

        imported.IsSuccess.Should().BeTrue();
        imported.Data!.BatchId.Should().NotBeNull();
        imported
            .Data.Errors.Should()
            .BeEmpty("позиция пакета ссылается на занятие из предыдущей строки того же файла");
        imported.Data.Positions.Should().HaveCount(2);
        imported
            .Data.Positions.Select(p => p.ChangeType)
            .Should()
            .Equal(ScheduleChangeType.Add, ScheduleChangeType.Remove);
        imported.Data.Positions.Should().OnlyContain(p => p.Errors.Count == 0);

        // Пакет применяется: добавленное и снятое взаимно уничтожаются, в базе пусто.
        var applied = await _sut.ApplyAsync(
            imported.Data.BatchId!.Value,
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        applied.IsSuccess.Should().BeTrue();
        (await _db.ScheduleEntries.CountAsync()).Should().Be(0);
        _db.ScheduleHistory.Should().HaveCount(2);
    }
}
