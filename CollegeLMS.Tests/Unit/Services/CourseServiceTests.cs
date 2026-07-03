using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class CourseServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly CourseService _sut;

    public CourseServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new CourseService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoCourses()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);

        var result = await _sut.GetAllAsync(null, null, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsCourses_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var courses = CourseFixture.CreateFaker().Generate(3);
        _db.Courses.AddRange(courses);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, null, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_ReturnsOwnCourses_WhenTeacher()
    {
        var teacherUserId = Guid.NewGuid();
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUserId,
            Department = "ИТ",
            Position = "Преподаватель",
        };
        var otherTeacherId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = teacherUserId,
                Email = "teacher@test.ru",
                FullName = "Учитель",
                PasswordHash = "hash",
                Role = UserRole.Teacher,
                IsActive = true,
            }
        );
        _db.Teachers.Add(teacher);
        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "ГР-21",
                Course = 2,
            }
        );
        var ownCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Мой курс",
            TeacherId = teacher.Id,
            GroupId = groupId,
            Status = CourseStatus.Draft,
        };
        var otherCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            TeacherId = otherTeacherId,
            GroupId = groupId,
            Status = CourseStatus.Draft,
        };
        _db.Courses.AddRange(ownCourse, otherCourse);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, null, teacherUserId, "Teacher", default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetById_ReturnsCourse_WhenFound()
    {
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(course.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(course.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesCourse_WhenTeacher()
    {
        var teacherUserId = Guid.NewGuid();
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUserId,
            Department = "ИТ",
            Position = "Преподаватель",
        };
        _db.Users.Add(
            new User
            {
                Id = teacherUserId,
                Email = "teacher@test.ru",
                FullName = "Учитель",
                PasswordHash = "hash",
                Role = UserRole.Teacher,
                IsActive = true,
            }
        );
        _db.Teachers.Add(teacher);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-21",
            Course = 2,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateCourseRequest
            {
                Title = "Новый курс",
                Description = "Описание",
                GroupId = group.Id,
            },
            teacherUserId,
            "Teacher",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новый курс");
        result.Data.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Update_UpdatesCourse_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            course.Id,
            new UpdateCourseRequest
            {
                Title = "Обновлённый курс",
                Description = "Новое описание",
                GroupId = course.GroupId,
                Status = "Active",
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Обновлённый курс");
        result.Data.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Delete_RemovesCourse_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(course.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Courses.AnyAsync(c => c.Id == course.Id);
        exists.Should().BeFalse();
    }

    private void AddAdminUser(Guid userId)
    {
        _db.Users.Add(
            new User
            {
                Id = userId,
                Email = "admin@test.ru",
                FullName = "Admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );
        _db.SaveChanges();
    }
}
