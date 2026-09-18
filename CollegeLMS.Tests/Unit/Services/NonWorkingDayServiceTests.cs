using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class NonWorkingDayServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly NonWorkingDayService _sut;

    public NonWorkingDayServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new NonWorkingDayService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static NonWorkingDayRequest Request(
        string title = "Праздник",
        DateTime? from = null,
        DateTime? to = null
    ) =>
        new()
        {
            Title = title,
            DateFrom = from ?? new DateTime(2026, 9, 7),
            DateTo = to ?? new DateTime(2026, 9, 7),
        };

    private async Task<NonWorkingDay> SeedAsync(
        DateTime from,
        DateTime to,
        string title = "Праздник"
    )
    {
        var utcNow = DateTime.UtcNow;
        var entity = new NonWorkingDay
        {
            Id = Guid.NewGuid(),
            DateFrom = from,
            DateTo = to,
            Title = title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.NonWorkingDays.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccess()
    {
        var result = await _sut.CreateAsync(
            Request("  Новый год  ", new DateTime(2026, 12, 31), new DateTime(2027, 1, 8)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новый год");
        result.Data.DateFrom.Should().Be(new DateTime(2026, 12, 31));
        result.Data.DateTo.Should().Be(new DateTime(2027, 1, 8));
        (await _db.NonWorkingDays.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Returns400()
    {
        var result = await _sut.CreateAsync(Request("   "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("название");
    }

    [Fact]
    public async Task CreateAsync_TitleTooLong_Returns400()
    {
        var result = await _sut.CreateAsync(Request(new string('Я', 201)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("200");
    }

    [Fact]
    public async Task CreateAsync_DateFromAfterDateTo_Returns400()
    {
        var result = await _sut.CreateAsync(
            Request(from: new DateTime(2026, 9, 10), to: new DateTime(2026, 9, 5)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("позже даты окончания");
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 7), new DateTime(2026, 9, 7));

        var result = await _sut.UpdateAsync(
            entity.Id,
            Request("Каникулы", new DateTime(2026, 10, 1), new DateTime(2026, 10, 10)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Каникулы");
        result.Data.DateTo.Should().Be(new DateTime(2026, 10, 10));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Returns404()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), Request(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAsync_Invalid_Returns400()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 7), new DateTime(2026, 9, 7));

        var result = await _sut.UpdateAsync(entity.Id, Request(""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesEntity()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 7), new DateTime(2026, 9, 7));

        var result = await _sut.DeleteAsync(entity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.NonWorkingDays.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Returns404()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllAsync_FilterFrom_ReturnsOverlapping()
    {
        await SeedAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 3), "Ранний");
        await SeedAsync(new DateTime(2026, 9, 10), new DateTime(2026, 9, 12), "Поздний");

        var result = await _sut.GetAllAsync(
            new DateTime(2026, 9, 9),
            null,
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].Title.Should().Be("Поздний");
    }

    [Fact]
    public async Task GetAllAsync_FilterTo_ReturnsOverlapping()
    {
        await SeedAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 3), "Ранний");
        await SeedAsync(new DateTime(2026, 9, 10), new DateTime(2026, 9, 12), "Поздний");

        var result = await _sut.GetAllAsync(
            null,
            new DateTime(2026, 9, 5),
            null,
            null,
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].Title.Should().Be("Ранний");
    }

    [Fact]
    public async Task GetAllAsync_Paginates()
    {
        for (var i = 0; i < 5; i++)
            await SeedAsync(
                new DateTime(2026, 9, 1).AddDays(i),
                new DateTime(2026, 9, 1).AddDays(i),
                $"Д{i}"
            );

        var result = await _sut.GetAllAsync(null, null, 2, 2, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(5);
        result.Data.Page.Should().Be(2);
        result.Data.PageSize.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_PageSizeClampedTo100()
    {
        var result = await _sut.GetAllAsync(null, null, 1, 500, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PageSize.Should().Be(100);
    }
}
