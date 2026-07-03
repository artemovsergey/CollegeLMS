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
        course.GroupId = group.Id;
        var assignment = DashboardFixture.CreateAssignmentFaker().Generate();
        assignment.CourseId = course.Id;
        var student = DashboardFixture.CreateStudentFaker().Generate();
        student.GroupId = group.Id;
        var submission = DashboardFixture.CreateSubmissionFaker().Generate();
        submission.AssignmentId = assignment.Id;
        submission.StudentId = student.Id;

        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        _db.Assignments.Add(assignment);
        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var result = await _sut.GetTeacherDashboardAsync(teacher.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.CoursesCount.Should().Be(1);
        result.Data.Courses.Should().Contain(course.Title);
        result.Data.RecentSubmissions.Should().HaveCount(1);
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
        result.Data!.CoursesCount.Should().Be(0);
        result.Data.StudentsCount.Should().Be(0);
        result.Data.RecentSubmissions.Should().BeEmpty();
        result.Data.Courses.Should().BeEmpty();
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
        course.GroupId = group.Id;
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        course.TeacherId = teacher.Id;
        var assignment = DashboardFixture.CreateAssignmentFaker().Generate();
        assignment.CourseId = course.Id;
        var submission = DashboardFixture.CreateSubmissionFaker().Generate();
        submission.AssignmentId = assignment.Id;
        submission.StudentId = student.Id;
        submission.Score = 85;

        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        _db.Assignments.Add(assignment);
        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var result = await _sut.GetStudentDashboardAsync(student.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.CoursesCount.Should().Be(1);
        result.Data.RecentGrades.Should().HaveCount(1);
        result.Data.RecentGrades[0].Grade.Should().Be(85);
    }

    [Fact]
    public async Task GetStudentDashboard_ReturnsUpcomingDeadlines()
    {
        var student = DashboardFixture.CreateStudentFaker().Generate();
        var group = DashboardFixture.CreateGroupFaker().Generate();
        student.GroupId = group.Id;
        var course = DashboardFixture.CreateCourseFaker().Generate();
        course.GroupId = group.Id;
        var teacher = DashboardFixture.CreateTeacherFaker().Generate();
        course.TeacherId = teacher.Id;
        var assignment = DashboardFixture.CreateAssignmentFaker().Generate();
        assignment.CourseId = course.Id;
        assignment.DueDate = DateTime.UtcNow.AddDays(7);

        _db.Users.Add(student.User);
        _db.Students.Add(student);
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        _db.Groups.Add(group);
        _db.Courses.Add(course);
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var result = await _sut.GetStudentDashboardAsync(student.UserId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UpcomingDeadlines.Should().HaveCount(1);
        result.Data.UpcomingDeadlines[0].AssignmentTitle.Should().Be(assignment.Title);
    }
}
