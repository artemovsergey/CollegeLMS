using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new DashboardService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetTeacherDashboard_ReturnsNotFound_WhenTeacherDoesNotExist()
    {
        var result = await _sut.GetTeacherDashboardAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetTeacherDashboard_ReturnsDashboard_WhenTeacherExists()
    {
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        var group = DashboardFixture.CreateGroupFaker().Generate();
        var course = DashboardFixture.CreateCourseFaker().Generate();
        course.TeacherId = teacher.Id;
        var student = DashboardFixture.CreateStudentFaker().Generate();
        student.GroupId = group.Id;

        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        _db.CourseGroups.Add(
            new CourseGroup
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                GroupId = group.Id,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetTeacherDashboardAsync(teacher.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Courses.Should().HaveCount(1);
        result.Data.Courses[0].Title.Should().Be(course.Title);
    }

    [Fact]
    public async Task GetTeacherDashboard_ReturnsZeroCounts_WhenNoCourses()
    {
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var result = await _sut.GetTeacherDashboardAsync(teacher.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Courses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        var result = await _sut.GetStudentDashboardAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsDashboard_WhenStudentExists()
    {
        var student = DashboardFixture.CreateStudentFaker().Generate();
        var group = DashboardFixture.CreateGroupFaker().Generate();
        student.GroupId = group.Id;
        var course = DashboardFixture.CreateCourseFaker().Generate();
        course.IsActive = true;
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        course.TeacherId = teacher.Id;
        var test = TestFixture.CreateFaker().Generate();
        test.CourseId = course.Id;
        test.Course = course;
        var attempt = new TestAttempt
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            StudentId = student.Id,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            Status = AttemptStatus.Completed,
            Score = 80,
            MaxScore = 100,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        _db.Tests.Add(test);
        _db.TestAttempts.Add(attempt);
        _db.CourseGroups.Add(
            new CourseGroup
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                GroupId = group.Id,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetStudentDashboardAsync(student.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Courses.Should().HaveCount(1);
        result.Data.Courses[0].CompletedItems.Should().Be(1);
        result.Data.Courses[0].TotalItems.Should().Be(1);
        result.Data.Courses[0].CompletionPercent.Should().Be(100.0);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsEmptyCourses_WhenNoCourseGroup()
    {
        var student = DashboardFixture.CreateStudentFaker().Generate();
        var group = DashboardFixture.CreateGroupFaker().Generate();
        student.GroupId = group.Id;
        var course = DashboardFixture.CreateCourseFaker().Generate();
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        course.TeacherId = teacher.Id;

        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var result = await _sut.GetStudentDashboardAsync(student.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Courses.Should().BeEmpty();
    }
}
