using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class GroupServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new GroupService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoGroups()
    {
        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsGroups_WhenGroupsExist()
    {
        var groups = GroupFixture.CreateFaker().Generate(3);
        _db.Groups.AddRange(groups);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsGroup_WhenFound()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(group.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(group.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesGroup()
    {
        var request = new CreateGroupRequest { Name = "Новая-ГР", Course = 1 };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Новая-ГР");
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDuplicateName()
    {
        var existing = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(existing);
        await _db.SaveChangesAsync();

        var request = new CreateGroupRequest { Name = existing.Name, Course = 1 };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Update_UpdatesGroup()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var request = new UpdateGroupRequest { Name = "Обновлённая-ГР", Course = 2 };

        var result = await _sut.UpdateAsync(group.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Обновлённая-ГР");
    }

    [Fact]
    public async Task Delete_RemovesGroup()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(group.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Groups.AnyAsync(g => g.Id == group.Id);
        exists.Should().BeFalse();
    }
}
