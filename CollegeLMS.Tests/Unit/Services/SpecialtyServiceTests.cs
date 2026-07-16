using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class SpecialtyServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly SpecialtyService _sut;

    public SpecialtyServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new SpecialtyService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoSpecialties()
    {
        var result = await _sut.GetAllAsync(null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSpecialties()
    {
        var specialties = SpecialtyFixture.CreateFaker().Generate(3);
        _db.Specialties.AddRange(specialties);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_FiltersBySearch()
    {
        var specialties = SpecialtyFixture.CreateFaker().Generate(3);
        specialties[0].Code = "09.02.01";
        specialties[1].Code = "09.02.02";
        _db.Specialties.AddRange(specialties);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync("09.02.01", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSpecialty_WhenFound()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(specialty.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(specialty.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenDeleted()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        specialty.IsDeleted = true;
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(specialty.Id, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesSpecialty()
    {
        var result = await _sut.CreateAsync(
            new CreateSpecialtyRequest
            {
                Code = "09.02.07",
                Name = "Информационные системы",
                Description = "Подготовка специалистов",
                Department = "ИТ",
            },
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Code.Should().Be("09.02.07");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenDuplicateCode()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateSpecialtyRequest { Code = specialty.Code, Name = "Другая" },
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSpecialty()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            specialty.Id,
            new UpdateSpecialtyRequest
            {
                Code = "09.02.99",
                Name = "Обновлённая",
                Description = "Новое описание",
                Department = "ИТ",
            },
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Code.Should().Be("09.02.99");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesSpecialty()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(specialty.Id, default);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _db.Specialties.FindAsync(specialty.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenAlreadyDeleted()
    {
        var specialty = SpecialtyFixture.CreateFaker().Generate();
        specialty.IsDeleted = true;
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(specialty.Id, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
