using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleInsertServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleInsertService _sut;

    public ScheduleInsertServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleInsertService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static ScheduleInsertRequest Request(
        string title = "Кураторский час",
        DayOfWeek day = DayOfWeek.Monday,
        int? course = null,
        bool isActive = true
    ) =>
        new()
        {
            Title = title,
            DayOfWeek = day,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(8, 30, 0),
            Course = course,
            IsActive = isActive,
        };

    private async Task<ScheduleInsert> SeedAsync(
        string title,
        DayOfWeek day,
        int? course = null,
        bool isActive = true
    )
    {
        var utcNow = DateTime.UtcNow;
        var entity = new ScheduleInsert
        {
            Id = Guid.NewGuid(),
            Title = title,
            DayOfWeek = day,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(8, 30, 0),
            Course = course,
            IsActive = isActive,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.ScheduleInserts.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccess()
    {
        var result = await _sut.CreateAsync(
            Request("  Линейка  ", DayOfWeek.Tuesday, 2),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Линейка");
        result.Data.DayOfWeek.Should().Be((int)DayOfWeek.Tuesday);
        result.Data.Course.Should().Be(2);
        (await _db.ScheduleInserts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Returns400()
    {
        var result = await _sut.CreateAsync(Request("  "), CancellationToken.None);

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
    public async Task CreateAsync_Sunday_Returns400()
    {
        var result = await _sut.CreateAsync(Request(day: DayOfWeek.Sunday), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("воскресенье");
    }

    [Fact]
    public async Task CreateAsync_StartNotBeforeEnd_Returns400()
    {
        var request = Request();
        request.StartTime = new TimeSpan(9, 0, 0);
        request.EndTime = new TimeSpan(9, 0, 0);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("раньше времени окончания");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task CreateAsync_CourseOutOfRange_Returns400(int course)
    {
        var result = await _sut.CreateAsync(Request(course: course), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Курс должен быть от 1 до 4");
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var entity = await SeedAsync("Старая", DayOfWeek.Monday);

        var result = await _sut.UpdateAsync(
            entity.Id,
            Request("Новая", DayOfWeek.Friday, 3),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новая");
        result.Data.DayOfWeek.Should().Be((int)DayOfWeek.Friday);
        result.Data.Course.Should().Be(3);
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
        var entity = await SeedAsync("Вставка", DayOfWeek.Wednesday);

        var result = await _sut.DeleteAsync(entity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.ScheduleInserts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Returns404()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllAsync_FilterByDayOfWeek()
    {
        await SeedAsync("Пн", DayOfWeek.Monday);
        await SeedAsync("Вт", DayOfWeek.Tuesday);

        var result = await _sut.GetAllAsync(DayOfWeek.Monday, null, false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        result.Data[0].Title.Should().Be("Пн");
    }

    [Fact]
    public async Task GetAllAsync_FilterByCourse_IncludesNullCourse()
    {
        await SeedAsync("Все курсы", DayOfWeek.Monday, course: null);
        await SeedAsync("2 курс", DayOfWeek.Monday, course: 2);
        await SeedAsync("3 курс", DayOfWeek.Monday, course: 3);

        var result = await _sut.GetAllAsync(null, 2, false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Select(i => i.Title).Should().BeEquivalentTo(["Все курсы", "2 курс"]);
    }

    [Fact]
    public async Task GetAllAsync_ActiveOnly_ExcludesInactive()
    {
        await SeedAsync("Активная", DayOfWeek.Monday, isActive: true);
        await SeedAsync("Неактивная", DayOfWeek.Monday, isActive: false);

        var result = await _sut.GetAllAsync(null, null, true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        result.Data[0].Title.Should().Be("Активная");
    }
}
