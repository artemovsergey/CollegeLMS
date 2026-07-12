using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class FeedbackServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly FeedbackService _sut;

    public FeedbackServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new FeedbackService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_ReturnsOk_WithValidRequest()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван Иванов",
            Email = "ivan@test.ru",
            Message = "Отличный сайт!",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Message.Should().Be("Сообщение отправлено");

        var saved = await _db.Set<API.Entities.Feedback>().FirstAsync();
        saved.Name.Should().Be("Иван Иванов");
        saved.Email.Should().Be("ivan@test.ru");
        saved.Message.Should().Be("Отличный сайт!");
    }

    [Fact]
    public async Task CreateAsync_Returns429_WhenDuplicateWithin5Minutes()
    {
        var firstRequest = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Первое сообщение",
        };

        var firstResult = await _sut.CreateAsync(firstRequest, default);
        firstResult.IsSuccess.Should().BeTrue();

        var secondRequest = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Второе сообщение",
        };

        var secondResult = await _sut.CreateAsync(secondRequest, default);

        secondResult.IsSuccess.Should().BeFalse();
        secondResult.StatusCode.Should().Be(429);
        secondResult.ErrorMessage.Should().Be("Вы уже отправляли сообщение. Попробуйте позже.");
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_SameEmailAfter5Minutes()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Сообщение",
        };

        var firstResult = await _sut.CreateAsync(request, default);
        firstResult.IsSuccess.Should().BeTrue();

        // Manually set CreatedAt to 6 minutes ago to bypass rate limit
        var saved = await _db.Set<API.Entities.Feedback>().FirstAsync();
        saved.CreatedAt = DateTime.UtcNow.AddMinutes(-6);
        await _db.SaveChangesAsync();

        var secondResult = await _sut.CreateAsync(request, default);

        secondResult.IsSuccess.Should().BeTrue();
    }
}
