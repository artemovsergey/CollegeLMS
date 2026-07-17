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
            CyclicalCommission = "ИТ",
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
        var ownCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Мой курс",
            TeacherId = teacher.Id,
            Status = CourseStatus.Draft,
        };
        var otherCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            TeacherId = otherTeacherId,
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
            CyclicalCommission = "ИТ",
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
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateCourseRequest
            {
                Title = "Новый курс",
                Description = "Описание",
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

    [Fact]
    public async Task AssignGroupsAsync_AssignsGroups_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        var group1 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        var group2 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-12",
            Course = 1,
        };
        _db.Groups.AddRange(group1, group2);
        await _db.SaveChangesAsync();

        var result = await _sut.AssignGroupsAsync(
            course.Id,
            new AssignGroupsRequest
            {
                GroupIds = new List<Guid> { group1.Id, group2.Id },
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        var assignments = await _db
            .CourseGroups.Where(cg => cg.CourseId == course.Id)
            .ToListAsync();
        assignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task AssignGroupsAsync_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.AssignGroupsAsync(
            Guid.NewGuid(),
            new AssignGroupsRequest { GroupIds = new List<Guid>() },
            Guid.NewGuid(),
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCourseGroupsAsync_ReturnsGroups()
    {
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        _db.CourseGroups.Add(
            new CourseGroup
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                GroupId = group.Id,
                Group = group,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetCourseGroupsAsync(course.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].GroupName.Should().Be("ГР-11");
    }

    [Fact]
    public async Task GetCourseGroupsAsync_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.GetCourseGroupsAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveGroupAsync_RemovesGroup_WhenAdmin()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        _db.CourseGroups.Add(
            new CourseGroup
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                GroupId = group.Id,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.RemoveGroupAsync(course.Id, group.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.CourseGroups.AnyAsync(cg =>
            cg.CourseId == course.Id && cg.GroupId == group.Id
        );
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveGroupAsync_ReturnsNotFound_WhenNotAssigned()
    {
        var adminId = Guid.NewGuid();
        AddAdminUser(adminId);
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.RemoveGroupAsync(
            course.Id,
            Guid.NewGuid(),
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsProgress_WhenStudentInCourse()
    {
        var course = CourseFixture.CreateFaker().Generate();
        course.Assignments = new List<Assignment>();
        _db.Courses.Add(course);

        var studentUserId = Guid.NewGuid();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = group.Id,
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);
        _db.CourseGroups.Add(
            new CourseGroup
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                GroupId = group.Id,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetProgressAsync(course.Id, studentUserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.CourseId.Should().Be(course.Id);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.GetProgressAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsNotFound_WhenStudentNotFound()
    {
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.GetProgressAsync(course.Id, Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetProgressAsync_ReturnsForbidden_WhenNotInCourse()
    {
        var course = CourseFixture.CreateFaker().Generate();
        _db.Courses.Add(course);

        var studentUserId = Guid.NewGuid();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            GroupId = Guid.NewGuid(),
            RecordBookNumber = "ЗК-001",
        };
        _db.Users.Add(
            new User
            {
                Id = studentUserId,
                FullName = "Студент",
                Email = "s@t.ru",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true,
            }
        );
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var result = await _sut.GetProgressAsync(course.Id, studentUserId, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
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
