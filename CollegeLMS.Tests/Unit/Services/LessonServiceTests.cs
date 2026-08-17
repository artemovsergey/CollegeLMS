using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class LessonServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly Mock<ICourseAccessService> _accessMock;
    private readonly LessonService _sut;

    public LessonServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _accessMock = new Mock<ICourseAccessService>();
        _accessMock
            .Setup(x => x.CanManageCourseAsync(It.IsAny<Course>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _accessMock
            .Setup(x => x.CanManageCourseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sut = new LessonService(_db, _accessMock.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoLessons()
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
    public async Task GetAll_ReturnsLessons_WhenExist()
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
        var lessons = LessonFixture.CreateFaker().Generate(3);
        foreach (var l in lessons)
            l.CourseId = courseId;
        _db.Lessons.AddRange(lessons);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsLesson_WhenFound()
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
        var lesson = LessonFixture.CreateFaker().Generate();
        lesson.CourseId = courseId;
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(courseId, lesson.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(lesson.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesLesson_WhenAdmin()
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
            new CreateLessonRequest
            {
                Title = "Новое занятие",
                Content = "Содержание занятия",
                Kind = "Practice",
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новое занятие");
        result.Data!.Kind.Should().Be("Practice");
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
        _db.Lessons.Add(
            new Lesson
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
            new CreateLessonRequest { Title = "Новая", Content = "Контент" },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Order.Should().Be(6);
    }

    [Fact]
    public async Task Update_UpdatesLesson_WhenAdmin()
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
        var lesson = LessonFixture.CreateFaker().Generate();
        lesson.CourseId = courseId;
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            courseId,
            lesson.Id,
            new UpdateLessonRequest
            {
                Title = "Обновлённое занятие",
                Content = "Контент",
                Kind = "SelfStudy",
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Обновлённое занятие");
        result.Data!.Kind.Should().Be("SelfStudy");
    }

    [Fact]
    public async Task Delete_RemovesLesson_WhenAdmin()
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
        var lesson = LessonFixture.CreateFaker().Generate();
        lesson.CourseId = courseId;
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(courseId, lesson.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Lessons.AnyAsync(l => l.Id == lesson.Id);
        exists.Should().BeFalse();
    }
}