using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class UserServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new UserService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoUsers()
    {
        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUsers_WhenUsersExist()
    {
        var faker = UserFixture.CreateFaker();
        var users = faker.Generate(5);
        _db.Users.AddRange(users);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(5);
        result.Data.Should().BeInAscendingOrder(u => u.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenFound()
    {
        var user = UserFixture.CreateFaker().Generate();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFail_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesUser()
    {
        var request = new CreateUserRequest
        {
            Login = "newuser",
            Email = "new@test.ru",
            Password = "password123",
            FullName = "New User",
            Role = UserRole.Student,
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Login.Should().Be("newuser");
        result.Data.Email.Should().Be("new@test.ru");
        result.Data.FullName.Should().Be("New User");
        result.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenEmailExists()
    {
        var existing = UserFixture.CreateFaker().Generate();
        _db.Users.Add(existing);
        await _db.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Login = existing.Login,
            Email = existing.Email,
            Password = "password123",
            FullName = "Another",
            Role = UserRole.Student,
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser()
    {
        var user = UserFixture.CreateFaker().Generate();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Login = "updateduser",
            Email = "updated@test.ru",
            FullName = "Updated Name",
            Role = UserRole.Teacher,
        };

        var result = await _sut.UpdateAsync(user.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Login.Should().Be("updateduser");
        result.Data.FullName.Should().Be("Updated Name");
        result.Data.Role.Should().Be("Teacher");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenNotFound()
    {
        var request = new UpdateUserRequest
        {
            Login = "anyuser",
            Email = "any@test.ru",
            FullName = "Any",
            Role = UserRole.Student,
        };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_DeactivatesUser()
    {
        var user = UserFixture.CreateFaker().Generate();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var deleted = await _db.Users.FindAsync([user.Id]);
        deleted!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenNotFound()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangeRoleAsync_ChangesRole()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.Role = UserRole.Student;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var request = new ChangeRoleRequest { Role = UserRole.Dispatcher };

        var result = await _sut.ChangeRoleAsync(user.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Role.Should().Be("Dispatcher");
    }

    [Fact]
    public async Task ChangeRoleAsync_ReturnsFail_WhenNotFound()
    {
        var request = new ChangeRoleRequest { Role = UserRole.Admin };

        var result = await _sut.ChangeRoleAsync(Guid.NewGuid(), request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ToggleActiveAsync_TogglesActive_WhenUserFound()
    {
        var user = UserFixture.CreateFaker().Generate();
        user.IsActive = true;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.ToggleActiveAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsActive.Should().BeFalse();

        var toggledBack = await _sut.ToggleActiveAsync(user.Id, CancellationToken.None);
        toggledBack.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_ReturnsFail_WhenNotFound()
    {
        var result = await _sut.ToggleActiveAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
