using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class SemesterServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly SemesterService _sut;

    public SemesterServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new SemesterService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoSemesters()
    {
        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSemesters()
    {
        var semesters = SemesterFixture.CreateFaker().Generate(3);
        _db.Semesters.AddRange(semesters);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSemester_WhenFound()
    {
        var semester = SemesterFixture.CreateFaker().Generate();
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(semester.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(semester.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesSemester()
    {
        var result = await _sut.CreateAsync(
            new CreateSemesterRequest
            {
                Name = "Осенний семестр 2025",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 12, 31),
                Type = "Autumn",
                AcademicYear = "2025-2026",
            },
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Осенний семестр 2025");
        result.Data.Type.Should().Be("Autumn");
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenInvalidType()
    {
        var result = await _sut.CreateAsync(
            new CreateSemesterRequest { Name = "Семестр", Type = "Invalid" },
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSemester()
    {
        var semester = SemesterFixture.CreateFaker().Generate();
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            semester.Id,
            new UpdateSemesterRequest
            {
                Name = "Обновлённый семестр",
                StartDate = new DateTime(2025, 2, 1),
                EndDate = new DateTime(2025, 6, 30),
                Type = "Spring",
                AcademicYear = "2025-2026",
            },
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Обновлённый семестр");
        result.Data.Type.Should().Be("Spring");
    }

    [Fact]
    public async Task DeleteAsync_RemovesSemester()
    {
        var semester = SemesterFixture.CreateFaker().Generate();
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(semester.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Semesters.AnyAsync(s => s.Id == semester.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
