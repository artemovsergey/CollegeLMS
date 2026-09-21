using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class BellScheduleServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BellScheduleService _sut;

    public BellScheduleServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new BellScheduleService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static UpdateBellScheduleRequest Request(
        int count,
        BigBreakRequest? bigBreak = null,
        int startHour = 8
    )
    {
        var slots = new List<BellSlotRequest>();
        for (var i = 1; i <= count; i++)
        {
            var start = new TimeSpan(startHour + (i - 1), 0, 0);
            slots.Add(
                new BellSlotRequest
                {
                    NumberPair = i,
                    StartTime = start,
                    EndTime = start.Add(TimeSpan.FromMinutes(45)),
                }
            );
        }

        return new UpdateBellScheduleRequest { Slots = slots, BigBreak = bigBreak };
    }

    [Fact]
    public async Task GetAsync_Empty_ReturnsEmptySlots()
    {
        var result = await _sut.GetAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Slots.Should().BeEmpty();
        result.Data.BigBreak.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_EightPairs_ReturnsSortedSlots()
    {
        var result = await _sut.UpdateAsync(Request(8), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Slots.Should().HaveCount(8);
        result.Data.Slots.Select(s => s.NumberPair).Should().BeInAscendingOrder();
        (await _db.BellSlots.CountAsync()).Should().Be(8);
    }

    [Fact]
    public async Task UpdateAsync_WithBigBreak_PersistsBigBreak()
    {
        var request = Request(
            4,
            new BigBreakRequest
            {
                AfterPair = 2,
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(10, 50, 0),
            }
        );

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BigBreak.Should().NotBeNull();
        result.Data.BigBreak!.AfterPair.Should().Be(2);
        (await _db.BigBreaks.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_EmptySlots_Returns400()
    {
        var result = await _sut.UpdateAsync(
            new UpdateBellScheduleRequest { Slots = [] },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("хотя бы одну пару");
    }

    [Fact]
    public async Task UpdateAsync_MoreThanEightPairs_Returns400()
    {
        var request = Request(9);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("больше 8");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNumbers_Returns400()
    {
        var request = Request(2);
        request.Slots[1].NumberPair = 1;

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("несколько раз");
    }

    [Fact]
    public async Task UpdateAsync_BeforeAllowedRange_Returns400()
    {
        var request = Request(1);
        request.Slots[0].StartTime = new TimeSpan(6, 30, 0);
        request.Slots[0].EndTime = new TimeSpan(7, 15, 0);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("07:00–21:00");
    }

    [Fact]
    public async Task UpdateAsync_AfterAllowedRange_Returns400()
    {
        var request = Request(1, startHour: 20);
        request.Slots[0].StartTime = new TimeSpan(20, 30, 0);
        request.Slots[0].EndTime = new TimeSpan(21, 30, 0);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("07:00–21:00");
    }

    [Fact]
    public async Task UpdateAsync_StartNotBeforeEnd_Returns400()
    {
        var request = Request(1);
        request.Slots[0].StartTime = new TimeSpan(10, 0, 0);
        request.Slots[0].EndTime = new TimeSpan(10, 0, 0);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("раньше времени окончания");
    }

    [Fact]
    public async Task UpdateAsync_OverlappingPairs_Returns400()
    {
        var request = Request(2);
        request.Slots[0].StartTime = new TimeSpan(9, 0, 0);
        request.Slots[0].EndTime = new TimeSpan(10, 30, 0);
        request.Slots[1].StartTime = new TimeSpan(10, 0, 0);
        request.Slots[1].EndTime = new TimeSpan(11, 30, 0);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("пересекаются");
    }

    [Fact]
    public async Task UpdateAsync_DescendingStartTime_Returns400()
    {
        var request = Request(2);
        request.Slots[0].StartTime = new TimeSpan(10, 0, 0);
        request.Slots[0].EndTime = new TimeSpan(11, 0, 0);
        request.Slots[1].StartTime = new TimeSpan(9, 0, 0);
        request.Slots[1].EndTime = new TimeSpan(10, 0, 0);

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("по возрастанию");
    }

    [Fact]
    public async Task UpdateAsync_InvalidBigBreak_Returns400()
    {
        var request = Request(
            2,
            new BigBreakRequest
            {
                AfterPair = 9,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 20, 0),
            }
        );

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("большой перемены");
    }

    [Fact]
    public async Task UpdateAsync_BigBreakStartNotBeforeEnd_Returns400()
    {
        var request = Request(
            2,
            new BigBreakRequest
            {
                AfterPair = 1,
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(10, 10, 0),
            }
        );

        var result = await _sut.UpdateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("раньше времени окончания");
    }

    [Fact]
    public async Task UpdateAsync_Twice_ReplacesSlots()
    {
        await _sut.UpdateAsync(Request(8), CancellationToken.None);

        var result = await _sut.UpdateAsync(Request(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.BellSlots.CountAsync()).Should().Be(2);
        var get = await _sut.GetAsync(CancellationToken.None);
        get.Data!.Slots.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTimeMapAsync_ReturnsPairTimes()
    {
        await _sut.UpdateAsync(Request(2), CancellationToken.None);

        var map = await _sut.GetTimeMapAsync(DayOfWeek.Monday, CancellationToken.None);

        map.Should().HaveCount(2);
        map[1].Start.Should().Be(new TimeSpan(8, 0, 0));
        map[1].End.Should().Be(new TimeSpan(8, 45, 0));
    }

    // ─────────────────────────── Профили ───────────────────────────

    private static BellProfileRequest ProfileRequest(
        string name,
        int[]? days = null,
        int count = 2,
        int startHour = 8,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    )
    {
        var slots = new List<BellSlotRequest>();
        for (var i = 1; i <= count; i++)
        {
            var start = new TimeSpan(startHour + (i - 1), 0, 0);
            slots.Add(
                new BellSlotRequest
                {
                    NumberPair = i,
                    StartTime = start,
                    EndTime = start.Add(TimeSpan.FromMinutes(45)),
                }
            );
        }

        var request = new BellProfileRequest
        {
            Name = name,
            DaysOfWeek = days?.ToList() ?? [],
            Slots = slots,
        };
        if (dateFrom.HasValue && dateTo.HasValue)
            request.Dates.Add(
                new BellProfileDateRequest { DateFrom = dateFrom.Value, DateTo = dateTo.Value }
            );
        return request;
    }

    [Fact]
    public async Task CreateProfile_ReturnsProfileWithSlots()
    {
        var result = await _sut.CreateProfileAsync(
            ProfileRequest("Понедельник", [1]),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Понедельник");
        result.Data.IsDefault.Should().BeFalse();
        result.Data.DaysOfWeek.Should().Equal(1);
        result.Data.Slots.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateProfile_EmptyName_Returns400()
    {
        var result = await _sut.CreateProfileAsync(ProfileRequest("  "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("название");
    }

    [Fact]
    public async Task CreateProfile_DuplicateName_Returns400()
    {
        await _sut.CreateProfileAsync(ProfileRequest("Смена"), CancellationToken.None);

        var result = await _sut.CreateProfileAsync(ProfileRequest("Смена"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("уже существует");
    }

    [Fact]
    public async Task CreateProfile_InvalidDays_Returns400()
    {
        var result = await _sut.CreateProfileAsync(
            ProfileRequest("Кривой", [0, 8]),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("1 (Пн)");
    }

    [Fact]
    public async Task GetResolvedAsync_ByDateRange_Wins()
    {
        var day = new DateTime(2026, 9, 7); // понедельник
        await _sut.CreateProfileAsync(
            ProfileRequest("Сессия", dateFrom: day, dateTo: day, startHour: 14),
            CancellationToken.None
        );
        await _sut.CreateProfileAsync(ProfileRequest("Понедельник", [1]), CancellationToken.None);

        var resolved = await _sut.GetResolvedAsync(day, CancellationToken.None);

        resolved.Data!.Name.Should().Be("Сессия");
    }

    [Fact]
    public async Task GetResolvedAsync_SubstituteDay_WinsOverDayOfWeek()
    {
        await _sut.CreateProfileAsync(
            ProfileRequest("Понедельник", [1], startHour: 9),
            CancellationToken.None
        );
        await _sut.CreateProfileAsync(
            ProfileRequest("Четверг", [4], startHour: 10),
            CancellationToken.None
        );

        var sunday = new DateTime(2026, 9, 6);
        var resolved = await _sut.GetResolvedAsync(
            sunday,
            substituteDayOfWeek: 4,
            CancellationToken.None
        );

        resolved.Data!.Name.Should().Be("Четверг");
    }

    [Fact]
    public async Task GetResolvedAsync_ByDayOfWeek()
    {
        await _sut.CreateProfileAsync(ProfileRequest("Понедельник", [1]), CancellationToken.None);

        var monday = new DateTime(2026, 9, 7);
        var resolved = await _sut.GetResolvedAsync(monday, CancellationToken.None);

        resolved.Data!.Name.Should().Be("Понедельник");
    }

    [Fact]
    public async Task GetResolvedAsync_NoMatch_ReturnsDefault()
    {
        await _sut.UpdateAsync(Request(2), CancellationToken.None);
        await _sut.CreateProfileAsync(ProfileRequest("Понедельник", [1]), CancellationToken.None);

        var wednesday = new DateTime(2026, 9, 9);
        var resolved = await _sut.GetResolvedAsync(wednesday, CancellationToken.None);

        resolved.Data!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetTimeMapAsync_Date_UsesSubstituteDayProfile()
    {
        await _sut.CreateProfileAsync(
            ProfileRequest("Понедельник", [1], startHour: 9),
            CancellationToken.None
        );
        await _sut.CreateProfileAsync(
            ProfileRequest("Четверг", [4], startHour: 10),
            CancellationToken.None
        );

        var sunday = new DateTime(2026, 9, 6);
        var map = await _sut.GetTimeMapAsync(sunday, 4, CancellationToken.None);

        map[1].Start.Should().Be(new TimeSpan(10, 0, 0));
    }

    [Fact]
    public async Task DeleteProfile_Default_Returns400()
    {
        await _sut.UpdateAsync(Request(2), CancellationToken.None);
        var defaultId = (await _sut.GetProfilesAsync(CancellationToken.None))
            .Data!.Single(p => p.IsDefault)
            .Id;

        var result = await _sut.DeleteProfileAsync(defaultId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Be("Базовый профиль удалить нельзя.");
    }

    [Fact]
    public async Task DeleteProfile_Custom_Removes()
    {
        var created = (
            await _sut.CreateProfileAsync(ProfileRequest("Смена"), CancellationToken.None)
        ).Data!;

        var result = await _sut.DeleteProfileAsync(created.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var profiles = (await _sut.GetProfilesAsync(CancellationToken.None)).Data!;
        profiles.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndSlots()
    {
        var created = (
            await _sut.CreateProfileAsync(ProfileRequest("Смена"), CancellationToken.None)
        ).Data!;

        var result = await _sut.UpdateProfileAsync(
            created.Id,
            ProfileRequest("Новая смена", [2, 3], count: 3),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Новая смена");
        result.Data.DaysOfWeek.Should().Equal(2, 3);
        result.Data.Slots.Should().HaveCount(3);
    }
}
