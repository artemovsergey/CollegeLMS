using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class LectureServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly LectureService _sut;

    public LectureServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new LectureService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoLectures()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsLectures_WhenExist()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var lectures = LectureFixture.CreateFaker().Generate(3);
        foreach (var l in lectures)
            l.CourseId = courseId;
        _db.Lectures.AddRange(lectures);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsLecture_WhenFound()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var lecture = LectureFixture.CreateFaker().Generate();
        lecture.CourseId = courseId;
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(courseId, lecture.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(lecture.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesLecture_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = adminId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            courseId,
            new CreateLectureRequest { Title = "Новая лекция", Content = "Содержание лекции" },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новая лекция");
        result.Data.Order.Should().Be(1);
    }

    [Fact]
    public async Task Create_AutoAssignsOrder()
    {
        var adminId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = adminId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        _db.Lectures.Add(
            new Lecture
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Existing",
                Content = "Content",
                Order = 5,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            courseId,
            new CreateLectureRequest { Title = "Новая", Content = "Контент" },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Order.Should().Be(6);
    }

    [Fact]
    public async Task Delete_RemovesLecture_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = adminId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var lecture = LectureFixture.CreateFaker().Generate();
        lecture.CourseId = courseId;
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(courseId, lecture.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Lectures.AnyAsync(l => l.Id == lecture.Id);
        exists.Should().BeFalse();
    }
}
