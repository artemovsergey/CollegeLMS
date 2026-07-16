using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class AuthServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _tokenServiceMock = new Mock<ITokenService>();
        _sut = new AuthService(_db, _tokenServiceMock.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsValid()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        user.IsActive = true;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<API.Entities.User>()))
            .Returns("test-token");

        var result = await _sut.LoginAsync(
            new LoginRequest { Login = user.Login, Password = "correct-password" },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("test-token");
        result.Data.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenPasswordInvalid()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        user.IsActive = true;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync(
            new LoginRequest { Login = user.Login, Password = "wrong-password" },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenUserNotFound()
    {
        var result = await _sut.LoginAsync(
            new LoginRequest { Login = "nonexistent", Password = "pass" },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenUserDeactivated()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password");
        user.IsActive = false;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync(
            new LoginRequest { Login = user.Login, Password = "password" },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsUser_WhenFound()
    {
        var user = UserFixture.CreateFaker().Generate();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.GetProfileAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(user.Id);
        result.Data.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsFail_WhenNotFound()
    {
        var result = await _sut.GetProfileAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangePasswordAsync_ChangesPassword_WhenOldPasswordCorrect()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest
            {
                OldPassword = "old-password",
                NewPassword = "new-password",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();

        var updated = await _db.Users.FindAsync([user.Id]);
        BCrypt.Net.BCrypt.Verify("new-password", updated!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFail_WhenUserNotFound()
    {
        var result = await _sut.ChangePasswordAsync(
            Guid.NewGuid(),
            new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFail_WhenOldPasswordInvalid()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest
            {
                OldPassword = "wrong-password",
                NewPassword = "new-password",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }
}
