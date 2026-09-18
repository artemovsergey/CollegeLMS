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

        var map = await _sut.GetTimeMapAsync(CancellationToken.None);

        map.Should().HaveCount(2);
        map[1].Start.Should().Be(new TimeSpan(8, 0, 0));
        map[1].End.Should().Be(new TimeSpan(8, 45, 0));
    }
}
