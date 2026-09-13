using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class DispatcherAuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ITokenService> _tokens = new();
    private readonly DispatcherAuthService _sut;

    public DispatcherAuthServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _db.DispatcherCredentials.Add(
            new DispatcherCredential
            {
                Id = Guid.NewGuid(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();

        _tokens
            .Setup(t => t.GenerateCustomToken(It.IsAny<IReadOnlyCollection<string>>(), 30, It.IsAny<string>()))
            .Returns("dispatcher-token");
        _sut = new DispatcherAuthService(_db, _tokens.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var result = await _sut.LoginAsync("secret", "127.0.0.1", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("dispatcher-token");
        _tokens.Verify(
            t =>
                t.GenerateCustomToken(
                    It.Is<IReadOnlyCollection<string>>(r => r.Contains("Dispatcher")),
                    30,
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var result = await _sut.LoginAsync("wrong", "127.0.0.2", default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_AfterFiveFailures_LocksIp()
    {
        var ip = "127.0.0.3";
        for (var i = 0; i < 5; i++)
        {
            var failed = await _sut.LoginAsync("wrong", ip, default);
            failed.IsSuccess.Should().BeFalse();
            failed.StatusCode.Should().Be(401);
        }

        var locked = await _sut.LoginAsync("secret", ip, default);

        locked.IsSuccess.Should().BeFalse();
        locked.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task Login_FailedThenCorrect_ResetsCounter()
    {
        var ip = "127.0.0.4";
        await _sut.LoginAsync("wrong", ip, default);

        var ok = await _sut.LoginAsync("secret", ip, default);

        ok.IsSuccess.Should().BeTrue();
    }
}