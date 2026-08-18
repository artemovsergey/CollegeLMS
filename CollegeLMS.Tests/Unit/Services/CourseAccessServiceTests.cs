using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLMS.Tests.Unit.Services;

public class CourseAccessServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Access_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsTrue_ForOwner()
    {
        await using var db = CreateDb();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(
            course.Id,
            course.TeacherId,
            CancellationToken.None
        );

        Assert.True(result);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsTrue_ForCoAuthor()
    {
        await using var db = CreateDb();
        var courseId = Guid.NewGuid();
        var coAuthor = Guid.NewGuid();
        db.Courses.Add(
            new Course
            {
                Id = courseId,
                TeacherId = Guid.NewGuid(),
                Title = "Курс",
                Description = "",
                Status = CourseStatus.Active,
                IsActive = true,
            }
        );
        db.CourseAuthors.Add(
            new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                TeacherId = coAuthor,
            }
        );
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(courseId, coAuthor, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanManageCourseAsync_ReturnsFalse_ForForeignTeacher()
    {
        await using var db = CreateDb();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.CanManageCourseAsync(
            course.Id,
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task GetManagedCourseIdsAsync_ReturnsOwnerAndCoAuthorCourses()
    {
        await using var db = CreateDb();
        var teacherId = Guid.NewGuid();
        var ownedId = Guid.NewGuid();
        var coOwnedId = Guid.NewGuid();
        db.Courses.Add(
            new Course
            {
                Id = ownedId,
                TeacherId = teacherId,
                Title = "Мой",
                Description = "",
                Status = CourseStatus.Active,
                IsActive = true,
            }
        );
        db.Courses.Add(
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = Guid.NewGuid(),
                Title = "Владение",
                Description = "",
                Status = CourseStatus.Active,
                IsActive = true,
            }
        );
        var coCourse = new Course
        {
            Id = coOwnedId,
            TeacherId = Guid.NewGuid(),
            Title = "Соавтор",
            Description = "",
            Status = CourseStatus.Active,
            IsActive = true,
        };
        db.Courses.Add(coCourse);
        db.CourseAuthors.Add(
            new CourseAuthor
            {
                Id = Guid.NewGuid(),
                CourseId = coOwnedId,
                TeacherId = teacherId,
            }
        );
        await db.SaveChangesAsync();

        var service = new CourseAccessService(db);
        var result = await service.GetManagedCourseIdsAsync(teacherId, CancellationToken.None);

        Assert.Contains(ownedId, result);
        Assert.Contains(coOwnedId, result);
        Assert.Equal(2, result.Count);
    }
}
