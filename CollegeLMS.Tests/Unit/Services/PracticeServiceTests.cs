using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class PracticeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PracticeService _sut;

    public PracticeServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new PracticeService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync(string name = "ПО-262", int course = 2)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Course = course,
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

    private static List<PracticeDayRequest> UpDays(DateTime from, DateTime to)
    {
        var days = new List<PracticeDayRequest>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            days.Add(new PracticeDayRequest { Date = date, PairNumbers = [1, 2, 3, 4, 5, 6] });
        }
        return days;
    }

    private static PracticeRequest Request(
        Guid groupId,
        Guid teacherId,
        PracticeKind kind = PracticeKind.Pp,
        DateTime? from = null,
        DateTime? to = null,
        string name = "УП 01",
        List<PracticeDayRequest>? days = null,
        string? room = null,
        int? subgroup = null
    )
    {
        var dateFrom = from ?? new DateTime(2026, 9, 7);
        var dateTo = to ?? new DateTime(2026, 9, 11);
        return new()
        {
            Kind = kind,
            Name = name,
            GroupId = groupId,
            TeacherIds = [teacherId],
            DateFrom = dateFrom,
            DateTo = dateTo,
            Note = "примечание",
            Room = room,
            Subgroup = subgroup,
            Days = kind == PracticeKind.Up ? days ?? UpDays(dateFrom, dateTo) : null,
        };
    }

    // ---------- CRUD ----------

    [Fact]
    public async Task CreateAsync_ValidUp_ReturnsNameTeachersAndDays()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up, name: "  УП 01  "),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("УП 01");
        result.Data.GroupName.Should().Be(group.Name);
        result.Data.TeacherIds.Should().ContainSingle().Which.Should().Be(teacher.Id);
        result.Data.Teachers[0].Name.Should().Be(teacher.User.FullName);
        result.Data.Days.Should().NotBeEmpty();
        result.Data.Days.Should().OnlyContain(d => d.PairNumbers.Count == 6);
        result
            .Data.Days.Should()
            .OnlyContain(d => d.PairNumbers.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }));
        (await _db.Practices.CountAsync()).Should().Be(1);
        (await _db.PracticeTeachers.CountAsync()).Should().Be(1);
        (await _db.PracticeDays.CountAsync()).Should().Be(result.Data.Days.Count);
    }

    [Fact]
    public async Task CreateAsync_RoomAndSubgroup_PersistedTrimmed()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up, room: "  202л  ", subgroup: 2),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Room.Should().Be("202л");
        result.Data.Subgroup.Should().Be(2);
        var stored = await _db.Practices.SingleAsync();
        stored.Room.Should().Be("202л");
        stored.Subgroup.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_NonContiguousPairNumbers_PersistedSorted()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [3, 1, 5] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Days[0].PairNumbers.Should().Equal(1, 3, 5);
        var stored = await _db.PracticeDays.SingleAsync();
        stored.PairNumbers.Should().Equal(1, 3, 5);
        stored.PairCount.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_MultipleTeachers_ReturnsAll()
    {
        var group = await SeedGroupAsync();
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");

        var request = Request(group.Id, teacherA.Id, PracticeKind.Pp);
        request.TeacherIds = [teacherA.Id, teacherB.Id];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TeacherIds.Should().HaveCount(2).And.Contain([teacherA.Id, teacherB.Id]);
        result.Data.Teachers.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_NoTeachers_Returns400()
    {
        var group = await SeedGroupAsync();
        var request = Request(group.Id, Guid.NewGuid(), PracticeKind.Pp);
        request.TeacherIds = [];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("хотя бы одного преподавателя");
    }

    [Fact]
    public async Task CreateAsync_EmptyName_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Pp, name: "   "),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("название");
    }

    [Fact]
    public async Task CreateAsync_UpWithoutDays_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days = [];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("дни с номерами пар");
    }

    [Fact]
    public async Task CreateAsync_DayOutsidePeriod_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 20), PairNumbers = [1, 2, 3] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("вне периода");
    }

    [Fact]
    public async Task CreateAsync_DayWithoutPairs_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("хотя бы одну пару");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task CreateAsync_PairNumberOutsideRange_Returns400(int pairNumber)
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [pairNumber] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("от 1 до 8");
    }

    [Fact]
    public async Task CreateAsync_DuplicatePairNumbersInDay_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [1, 2, 2] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("не должны повторяться");
    }

    [Fact]
    public async Task CreateAsync_DuplicateDays_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [1, 2] },
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [3, 4] },
        ];

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("не должны повторяться");
    }

    [Fact]
    public async Task CreateAsync_RoomTooLong_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Pp, room: new string('к', 21)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("20 символов");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateAsync_SubgroupNotPositive_Returns400(int subgroup)
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Pp, subgroup: subgroup),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("подгруппы");
    }

    [Fact]
    public async Task CreateAsync_GroupNotFound_Returns400()
    {
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(Guid.NewGuid(), teacher.Id),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Группа не найдена");
    }

    [Fact]
    public async Task CreateAsync_TeacherNotFound_Returns400()
    {
        var group = await SeedGroupAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Преподаватель не найден");
    }

    [Fact]
    public async Task CreateAsync_PeriodOutsideSemester_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 8, 1),
                to: new DateTime(2026, 8, 5)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("семестра");
    }

    [Fact]
    public async Task CreateAsync_DateFromAfterDateTo_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 9, 10),
                to: new DateTime(2026, 9, 5)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("позже даты окончания");
    }

    [Fact]
    public async Task CreateAsync_InvalidKind_Returns400()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, kind: (PracticeKind)99),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("УП или ПП");
    }

    // ---------- Пересечение практик ----------

    [Fact]
    public async Task CreateAsync_OverlappingPractice_Returns409()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                from: new DateTime(2026, 9, 9),
                to: new DateTime(2026, 9, 12)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("уже есть практика");
    }

    [Fact]
    public async Task CreateAsync_AnotherGroupSamePeriod_IsAllowed()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(Request(groupA.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.CreateAsync(Request(groupB.Id, teacher.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_IdenticalPeriodUpSubgroups_IsAllowed()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var first = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up, subgroup: 1),
            CancellationToken.None
        );

        var second = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up, subgroup: 2),
            CancellationToken.None
        );

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Data!.Subgroup.Should().Be(2);
        (await _db.Practices.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_IdenticalPeriodUpAndPp_Returns409()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );

        var result = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Pp),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("уже есть практика");
    }

    [Fact]
    public async Task CreateAsync_IdenticalPeriodTwoPp_Returns409()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CreateAsync_PartialOverlapTwoUp_Returns409()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );

        var result = await _sut.CreateAsync(
            Request(
                group.Id,
                teacher.Id,
                PracticeKind.Up,
                from: new DateTime(2026, 9, 9),
                to: new DateTime(2026, 9, 12)
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    // ---------- Обновление и удаление ----------

    [Fact]
    public async Task UpdateAsync_SamePeriod_DoesNotConflictWithItselfAndReplacesDays()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var created = await _sut.CreateAsync(
            Request(group.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );

        var update = Request(group.Id, teacher.Id, PracticeKind.Up, name: "УП 02");
        update.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 7), PairNumbers = [2, 4] },
        ];

        var result = await _sut.UpdateAsync(created.Data!.Id, update, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("УП 02");
        result.Data.Days.Should().ContainSingle();
        result.Data.Days[0].PairNumbers.Should().Equal(2, 4);
        (await _db.PracticeDays.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Returns404()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();

        var result = await _sut.UpdateAsync(
            Guid.NewGuid(),
            Request(group.Id, teacher.Id),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesEntity()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var created = await _sut.CreateAsync(Request(group.Id, teacher.Id), CancellationToken.None);

        var result = await _sut.DeleteAsync(created.Data!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Practices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Returns404()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ---------- Список ----------

    [Fact]
    public async Task GetAllAsync_FiltersByGroupAndKind()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacher = await SeedTeacherAsync();
        await _sut.CreateAsync(
            Request(groupA.Id, teacher.Id, PracticeKind.Up),
            CancellationToken.None
        );
        await _sut.CreateAsync(
            Request(groupB.Id, teacher.Id, PracticeKind.Pp),
            CancellationToken.None
        );

        var result = await _sut.GetAllAsync(
            groupB.Id,
            null,
            PracticeKind.Pp,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].GroupName.Should().Be("ПО-263");
        result.Data.Items[0].Kind.Should().Be(PracticeKind.Pp);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByTeacherViaJoin()
    {
        var groupA = await SeedGroupAsync("ПО-262");
        var groupB = await SeedGroupAsync("ПО-263");
        var teacherA = await SeedTeacherAsync("Марченко И.А.");
        var teacherB = await SeedTeacherAsync("Петрова М.С.");
        await _sut.CreateAsync(Request(groupA.Id, teacherA.Id), CancellationToken.None);
        await _sut.CreateAsync(Request(groupB.Id, teacherB.Id), CancellationToken.None);

        var result = await _sut.GetAllAsync(
            null,
            teacherA.Id,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].GroupName.Should().Be("ПО-262");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRoomSubgroupAndPairNumbers()
    {
        var group = await SeedGroupAsync();
        var teacher = await SeedTeacherAsync();
        var request = Request(group.Id, teacher.Id, PracticeKind.Up, room: "202л", subgroup: 3);
        request.Days =
        [
            new PracticeDayRequest { Date = new DateTime(2026, 9, 8), PairNumbers = [1, 3] },
        ];
        await _sut.CreateAsync(request, CancellationToken.None);

        var result = await _sut.GetAllAsync(
            group.Id,
            null,
            PracticeKind.Up,
            null,
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var item = result.Data!.Items.Should().ContainSingle().Subject;
        item.Room.Should().Be("202л");
        item.Subgroup.Should().Be(3);
        item.Days.Should().ContainSingle();
        item.Days[0].PairNumbers.Should().Equal(1, 3);
    }
}
