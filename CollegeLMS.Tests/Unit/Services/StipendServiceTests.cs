using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class StipendServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly StipendService _sut;

    public StipendServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new StipendService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GenerateAsync_ReturnsEmptyList_WhenNoStudents()
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр 1",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow,
            Type = SemesterType.Autumn,
            AcademicYear = "2025-2026",
        };
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync();

        var result = await _sut.GenerateAsync(semester.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ReturnsFail_WhenSemesterNotFound()
    {
        var result = await _sut.GenerateAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoLists()
    {
        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsLists()
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };
        _db.Semesters.Add(semester);
        _db.StipendLists.Add(
            new StipendList
            {
                Id = Guid.NewGuid(),
                SemesterId = semester.Id,
                Name = "Список 1",
                Semester = semester,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsList_WhenFound()
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };
        var list = new StipendList
        {
            Id = Guid.NewGuid(),
            SemesterId = semester.Id,
            Name = "Список 1",
            Semester = semester,
        };
        _db.Semesters.Add(semester);
        _db.StipendLists.Add(list);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(list.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Список 1");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_RemovesList()
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };
        var list = new StipendList
        {
            Id = Guid.NewGuid(),
            SemesterId = semester.Id,
            Name = "Список",
        };
        _db.Semesters.Add(semester);
        _db.StipendLists.Add(list);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(list.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.StipendLists.AnyAsync(l => l.Id == list.Id);
        exists.Should().BeFalse();
    }
}
