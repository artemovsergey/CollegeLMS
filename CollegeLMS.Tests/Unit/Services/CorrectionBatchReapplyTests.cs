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
/// Правка уже применённого пакета. Позиция возвращается в черновики, а
/// повторное применение выполняет только черновики: уже применённые позиции
/// остаются в расписании как есть, и уведомление уходит по новым изменениям.
/// </summary>
public class CorrectionBatchReapplyTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _sut;

    private static readonly DateTime TestDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    public CorrectionBatchReapplyTests()
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

    // ───────────────────────────── посев данных ─────────────────────────────

    private async Task<(Group Group, Teacher Teacher)> SeedAsync()
    {
        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@collegelms.ru",
            FullName = "Марченко И.А.",
            Login = "teacher",
            PasswordHash = "x",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Category = TeacherCategory.Higher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО-262",
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.Users.Add(user);
        _db.Teachers.Add(teacher);
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return (group, teacher);
    }

    private async Task SeedEntryAsync(Guid groupId, Guid teacherId, string subject, int pair)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                TeacherId = teacherId,
                Subject = subject,
                Room = "301",
                DayOfWeek = DayOfWeek.Thursday,
                NumberPair = pair,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Weeks = [2],
                LessonType = LessonType.None,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
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

    private Task<CreateCorrectionPositionRequest> AddRequestAsync(
        Group group,
        Teacher teacher,
        int pair,
        string subject
    ) =>
        Task.FromResult(
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = pair,
                Subject = subject,
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
            }
        );

    private Task<Result<CorrectionApplyResult>> ApplyAsync(Guid batchId) =>
        _sut.ApplyAsync(batchId, Guid.NewGuid().ToString(), Guid.NewGuid(), CancellationToken.None);

    private async Task<List<CorrectionPosition>> PositionsAsync(Guid batchId) =>
        await _db
            .CorrectionPositions.Where(p => p.BatchId == batchId)
            .OrderBy(p => p.Row)
            .ToListAsync();

    private async Task<List<string>> ScheduleAsync(Guid groupId) =>
        await _db
            .ScheduleEntries.Where(e => e.GroupId == groupId)
            .Select(e => $"{e.NumberPair}:{e.Subject}")
            .ToListAsync();

    // ─────────────────────────────── проверки ───────────────────────────────

    [Fact]
    public async Task Позицию_можно_добавить_в_применённый_пакет()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var first = await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 2, "Математика"),
            CancellationToken.None
        );
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        var second = await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 3, "История"),
            CancellationToken.None
        );

        second.IsSuccess.Should().BeTrue("применённый пакет можно дополнить");
        second.Data!.Status.Should().Be(CorrectionPositionStatus.Draft);
    }

    [Fact]
    public async Task Повторное_применение_берёт_только_черновики()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 2, "Математика"),
            CancellationToken.None
        );
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();
        (await ScheduleAsync(group.Id)).Should().BeEquivalentTo(["1:Физика", "2:Математика"]);

        await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 3, "История"),
            CancellationToken.None
        );
        var applied = await ApplyAsync(batchId);

        applied.IsSuccess.Should().BeTrue();
        applied.Data!.Applied.Should().Be(1, "применяется только новая позиция");
        (await ScheduleAsync(group.Id))
            .Should()
            .BeEquivalentTo(["1:Физика", "2:Математика", "3:История"], "прежнее не задваивается");
        _db.ScheduleHistory.Should().HaveCount(2);
    }

    [Fact]
    public async Task Правка_применённой_позиции_отменяет_прежнее_и_возвращает_в_черновики()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var position = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 2, "Математика"),
                CancellationToken.None
            )
        ).Data!;
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();
        (await ScheduleAsync(group.Id)).Should().Contain("2:Математика");

        var request = await AddRequestAsync(group, teacher, 2, "История");
        var updated = await _sut.UpdatePositionAsync(
            batchId,
            position.Id,
            request,
            CancellationToken.None
        );

        updated.IsSuccess.Should().BeTrue();
        updated.Data!.Status.Should().Be(CorrectionPositionStatus.Draft);
        updated.Data.HistoryId.Should().BeNull("прежнее изменение отменено");
        (await ScheduleAsync(group.Id))
            .Should()
            .NotContain("2:Математика", "прежняя версия уже отменена");

        var applied = await ApplyAsync(batchId);
        applied.IsSuccess.Should().BeTrue();
        (await ScheduleAsync(group.Id))
            .Should()
            .Contain("2:История")
            .And.NotContain("2:Математика");
    }

    [Fact]
    public async Task Ошибки_применённого_пакета_относятся_только_к_черновикам()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 2, "Математика"),
            CancellationToken.None
        );
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        // Снятие пары, в которой ничего нет: при добавлении это проходит, а
        // ошибка всплывает только при применении — и только у новой позиции.
        var broken = await _sut.AddPositionAsync(
            batchId,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Remove,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 5,
                RemovedSubject = "Физика",
            },
            CancellationToken.None
        );
        broken.IsSuccess.Should().BeTrue();

        var batch = (await _sut.GetBatchAsync(batchId, CancellationToken.None)).Data!;
        batch.PendingCount.Should().Be(1);
        batch.Errors.Should().HaveCount(1);
        batch.Errors[0].Message.Should().Contain("пара 5 не найдена");
        batch.Errors[0].Row.Should().Be(2, "ошибка относится к черновиковой позиции");
    }

    [Fact]
    public async Task Повторное_применение_полностью_применённого_пакета_отклоняется()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        await _sut.AddPositionAsync(
            batchId,
            await AddRequestAsync(group, teacher, 2, "Математика"),
            CancellationToken.None
        );
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        var again = await ApplyAsync(batchId);

        again.IsSuccess.Should().BeFalse();
        again.StatusCode.Should().Be(400);
        again.ErrorMessage.Should().Contain("уже применены");
    }

    [Fact]
    public async Task PendingCount_растёт_при_правке_и_обнуляется_после_применения()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var position = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 2, "Математика"),
                CancellationToken.None
            )
        ).Data!;

        (await _sut.GetBatchAsync(batchId, CancellationToken.None))
            .Data!.PendingCount.Should()
            .Be(1);
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();
        (await _sut.GetBatchAsync(batchId, CancellationToken.None))
            .Data!.PendingCount.Should()
            .Be(0);

        await _sut.UpdatePositionAsync(
            batchId,
            position.Id,
            await AddRequestAsync(group, teacher, 2, "История"),
            CancellationToken.None
        );
        (await _sut.GetBatchAsync(batchId, CancellationToken.None))
            .Data!.PendingCount.Should()
            .Be(1);
    }

    [Fact]
    public async Task Неудачная_правка_не_отменяет_прежнее_изменение()
    {
        // Откат выполняется только после проверки новой версии, иначе при
        // неверных данных прежняя пара снялась бы из расписания, а позиция
        // осталась бы применённой.
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var position = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 2, "Математика"),
                CancellationToken.None
            )
        ).Data!;
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        var invalid = await _sut.UpdatePositionAsync(
            batchId,
            position.Id,
            await AddRequestAsync(group, teacher, 9, "История"),
            CancellationToken.None
        );

        invalid.IsSuccess.Should().BeFalse();
        invalid.ErrorMessage.Should().Contain("Номер пары");
        (await ScheduleAsync(group.Id)).Should().Contain("2:Математика", "прежнее не отменено");
        _db.ScheduleHistory.Should().HaveCount(1);
        var stored = await PositionsAsync(batchId);
        stored.Should().HaveCount(1);
        stored[0].Status.Should().Be(CorrectionPositionStatus.Applied);
        stored[0].HistoryId.Should().NotBeNull();
        (await _sut.GetBatchAsync(batchId, CancellationToken.None))
            .Data!.PendingCount.Should()
            .Be(0);
    }

    [Fact]
    public async Task Применённую_позицию_нельзя_удалить_а_черновиковую_можно()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var appliedPosition = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 2, "Математика"),
                CancellationToken.None
            )
        ).Data!;
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        // Черновик в применённом пакете появляется только после применения.
        var draftPosition = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 3, "История"),
                CancellationToken.None
            )
        ).Data!;

        var deleted = await _sut.DeletePositionAsync(
            batchId,
            appliedPosition.Id,
            CancellationToken.None
        );
        deleted.IsSuccess.Should().BeFalse();
        deleted.StatusCode.Should().Be(409);
        deleted.ErrorMessage.Should().Contain("журнале");

        var deletedDraft = await _sut.DeletePositionAsync(
            batchId,
            draftPosition.Id,
            CancellationToken.None
        );
        deletedDraft.IsSuccess.Should().BeTrue("черновик удаляется и в применённом пакете");
        (await PositionsAsync(batchId)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Правка_не_ломает_уже_применённые_позиции_пакета()
    {
        var (group, teacher) = await SeedAsync();
        await SeedEntryAsync(group.Id, teacher.Id, "Физика", 1);
        var batchId = await CreateBatchAsync();

        var first = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 2, "Математика"),
                CancellationToken.None
            )
        ).Data!;
        var second = (
            await _sut.AddPositionAsync(
                batchId,
                await AddRequestAsync(group, teacher, 3, "История"),
                CancellationToken.None
            )
        ).Data!;
        (await ApplyAsync(batchId)).IsSuccess.Should().BeTrue();

        // Правят только одну позицию — вторая остаётся применённой.
        await _sut.UpdatePositionAsync(
            batchId,
            first.Id,
            await AddRequestAsync(group, teacher, 2, "Биология"),
            CancellationToken.None
        );
        await ApplyAsync(batchId);

        var positions = await PositionsAsync(batchId);
        positions
            .Single(p => p.Row == first.Row)
            .Status.Should()
            .Be(CorrectionPositionStatus.Applied);
        positions
            .Single(p => p.Row == second.Row)
            .Status.Should()
            .Be(CorrectionPositionStatus.Applied);
        (await ScheduleAsync(group.Id))
            .Should()
            .BeEquivalentTo(["1:Физика", "2:Биология", "3:История"]);
        _db.ScheduleHistory.Should().HaveCount(2);
        // Две записи, а не три: правка первой позиции отменила её прежнее
        // изменение, и запись журнала была удалена. Второе изменение не трогали.
        _db.ScheduleHistory.Select(h => h.Subject)
            .Should()
            .BeEquivalentTo(["Биология", "История"]);
    }
}
