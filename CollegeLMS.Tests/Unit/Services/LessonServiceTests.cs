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
    public async Task Delete_CompactsOrders_WhenLessonRemoved()
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
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        var c = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "C", Order = 3 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(a, b, c);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(course.Id, b.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        (await _db.Lessons.SingleAsync(l => l.Id == a.Id)).Order.Should().Be(1);
        (await _db.Lessons.SingleAsync(l => l.Id == c.Id)).Order.Should().Be(2);
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

    [Fact]
    public async Task Create_InsertsAfterGivenLesson_WhenAfterLessonIdSet()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var first = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Первое",
            Order = 1,
        };
        var second = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Второе",
            Order = 2,
        };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(first, second);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            course.Id,
            new CreateLessonRequest
            {
                Title = "Между",
                Content = "",
                Kind = "Lecture",
                AfterLessonId = first.Id,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        var lessons = await _db
            .Lessons.Where(l => l.CourseId == course.Id)
            .OrderBy(l => l.Order)
            .ToListAsync();
        lessons.Should().HaveCount(3);
        lessons[0].Id.Should().Be(first.Id);
        lessons[1].Id.Should().Be(result.Data!.Id);
        lessons[0].Order.Should().Be(1);
        lessons[1].Order.Should().Be(2);
        lessons[2].Order.Should().Be(3);
    }

    [Fact]
    public async Task Create_InsertsAtStart_WhenAfterLessonIdNull()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var first = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Первое",
            Order = 1,
        };
        _db.Courses.Add(course);
        _db.Lessons.Add(first);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            course.Id,
            new CreateLessonRequest
            {
                Title = "В начало",
                Content = "",
                Kind = "Lecture",
                AfterLessonId = null,
            },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        var lessons = await _db
            .Lessons.Where(l => l.CourseId == course.Id)
            .OrderBy(l => l.Order)
            .ToListAsync();
        lessons[0].Id.Should().Be(result.Data!.Id);
        lessons[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task Reorder_ReassignsOrders_WhenValid()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "A", Order = 1 };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        var c = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "C", Order = 3 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(a, b, c);
        await _db.SaveChangesAsync();

        var result = await _sut.ReorderAsync(
            course.Id,
            new ReorderLessonsRequest { LessonIds = [c.Id, a.Id, b.Id] },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        (await _db.Lessons.SingleAsync(l => l.Id == c.Id)).Order.Should().Be(1);
        (await _db.Lessons.SingleAsync(l => l.Id == a.Id)).Order.Should().Be(2);
        (await _db.Lessons.SingleAsync(l => l.Id == b.Id)).Order.Should().Be(3);
    }

    [Fact]
    public async Task Reorder_ReturnsBadRequest_WhenLessonFromAnotherCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var other = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Title = "Чужое",
            Order = 1,
        };
        _db.Courses.Add(course);
        _db.Lessons.Add(other);
        await _db.SaveChangesAsync();

        var result = await _sut.ReorderAsync(
            course.Id,
            new ReorderLessonsRequest { LessonIds = [other.Id] },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SetCurrent_True_SetsLessonAndResetsOthers()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "A",
            Order = 1,
            IsCurrent = true,
        };
        var b = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "B", Order = 2 };
        _db.Courses.Add(course);
        _db.Lessons.AddRange(a, b);
        await _db.SaveChangesAsync();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            b.Id,
            new UpdateLessonCurrentRequest { IsCurrent = true },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        (await _db.Lessons.SingleAsync(l => l.Id == a.Id)).IsCurrent.Should().BeFalse();
        (await _db.Lessons.SingleAsync(l => l.Id == b.Id)).IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task SetCurrent_False_UnsetsLesson()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        var a = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "A",
            Order = 1,
            IsCurrent = true,
        };
        _db.Courses.Add(course);
        _db.Lessons.Add(a);
        await _db.SaveChangesAsync();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            a.Id,
            new UpdateLessonCurrentRequest { IsCurrent = false },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        (await _db.Lessons.SingleAsync(l => l.Id == a.Id)).IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task SetCurrent_ReturnsNotFound_WhenLessonMissing()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.SetCurrentAsync(
            course.Id,
            Guid.NewGuid(),
            new UpdateLessonCurrentRequest { IsCurrent = true },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}