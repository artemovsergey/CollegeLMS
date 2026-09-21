using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class WorkingDayServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WorkingDayService _sut;

    public WorkingDayServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new WorkingDayService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static WorkingDayRequest Request(
        string title = "Рабочая суббота",
        DateTime? from = null,
        DateTime? to = null,
        int? substituteDayOfWeek = 1
    ) =>
        new()
        {
            Title = title,
            DateFrom = from ?? new DateTime(2026, 9, 5),
            DateTo = to ?? new DateTime(2026, 9, 5),
            SubstituteDayOfWeek = substituteDayOfWeek,
        };

    private async Task<WorkingDayOverride> SeedAsync(
        DateTime from,
        DateTime to,
        string title = "Рабочий день"
    )
    {
        var utcNow = DateTime.UtcNow;
        var entity = new WorkingDayOverride
        {
            Id = Guid.NewGuid(),
            DateFrom = from,
            DateTo = to,
            SubstituteDayOfWeek = 1,
            Title = title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.WorkingDayOverrides.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    private async Task SeedNonWorkingAsync(DateTime from, DateTime to, string title = "Праздник")
    {
        var utcNow = DateTime.UtcNow;
        _db.NonWorkingDays.Add(
            new NonWorkingDay
            {
                Id = Guid.NewGuid(),
                DateFrom = from,
                DateTo = to,
                Title = title,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccess()
    {
        var result = await _sut.CreateAsync(
            Request("  Рабочая суббота  ", new DateTime(2026, 9, 5), new DateTime(2026, 9, 5)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Рабочая суббота");
        result.Data.SubstituteDayOfWeek.Should().Be(1);
        (await _db.WorkingDayOverrides.CountAsync()).Should().Be(1);
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
    public async Task CreateAsync_InvalidSubstituteDay_Returns400()
    {
        var result = await _sut.CreateAsync(
            Request(substituteDayOfWeek: 7),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("1 (Пн) до 5 (Пт)");
    }

    [Fact]
    public async Task CreateAsync_OverlapsNonWorkingDay_Returns409()
    {
        await SeedNonWorkingAsync(new DateTime(2026, 9, 5), new DateTime(2026, 9, 5));

        var result = await _sut.CreateAsync(Request(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Be("Дата уже отмечена как нерабочая.");
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 5), new DateTime(2026, 9, 5));

        var result = await _sut.UpdateAsync(
            entity.Id,
            Request("Перенос", new DateTime(2026, 9, 12), new DateTime(2026, 9, 12), 3),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Перенос");
        result.Data.SubstituteDayOfWeek.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_OverlapsNonWorkingDay_Returns409()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 5), new DateTime(2026, 9, 5));
        await SeedNonWorkingAsync(new DateTime(2026, 9, 12), new DateTime(2026, 9, 12));

        var result = await _sut.UpdateAsync(
            entity.Id,
            Request(from: new DateTime(2026, 9, 12), to: new DateTime(2026, 9, 12)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Returns404()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), Request(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesEntity()
    {
        var entity = await SeedAsync(new DateTime(2026, 9, 5), new DateTime(2026, 9, 5));

        var result = await _sut.DeleteAsync(entity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.WorkingDayOverrides.CountAsync()).Should().Be(0);
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
        await SeedAsync(new DateTime(2026, 9, 5), new DateTime(2026, 9, 5), "Ранний");
        await SeedAsync(new DateTime(2026, 9, 12), new DateTime(2026, 9, 12), "Поздний");

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
    public async Task GetAllAsync_PageSizeClampedTo100()
    {
        var result = await _sut.GetAllAsync(null, null, 1, 500, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PageSize.Should().Be(100);
    }
}
