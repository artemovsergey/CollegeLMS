using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class AssignmentServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly AssignmentService _sut;

    public AssignmentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new AssignmentService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoAssignments()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsAssignments_WhenExist()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var assignments = AssignmentFixture.CreateFaker().Generate(3);
        foreach (var a in assignments)
            a.CourseId = courseId;
        _db.Assignments.AddRange(assignments);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsAssignment_WhenFound()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var assignment = AssignmentFixture.CreateFaker().Generate();
        assignment.CourseId = courseId;
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(courseId, assignment.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(assignment.Id);
    }

    [Fact]
    public async Task Create_CreatesAssignment_WhenAdmin()
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
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            courseId,
            new CreateAssignmentRequest
            {
                Title = "Новое задание",
                Description = "Описание",
                MaxScore = 100,
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Новое задание");
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
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        _db.Assignments.Add(
            new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Existing",
                Description = "Desc",
                Order = 3,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            courseId,
            new CreateAssignmentRequest
            {
                Title = "Новое",
                Description = "Описание",
                MaxScore = 50,
            },
            adminId,
            "Admin",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.Order.Should().Be(4);
    }

    [Fact]
    public async Task Delete_RemovesAssignment_WhenAdmin()
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
                GroupId = Guid.NewGuid(),
                Status = CourseStatus.Draft,
            }
        );
        var assignment = AssignmentFixture.CreateFaker().Generate();
        assignment.CourseId = courseId;
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(courseId, assignment.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Assignments.AnyAsync(a => a.Id == assignment.Id);
        exists.Should().BeFalse();
    }
}
