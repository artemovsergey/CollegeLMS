using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class CourseDocumentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<ICourseAccessService> _accessMock;
    private readonly CourseDocumentService _sut;

    public CourseDocumentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _fileServiceMock = new Mock<IFileService>();
        _fileServiceMock
            .Setup(x =>
                x.SaveFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<IFormFile>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("documents/test/1.pdf");
        _fileServiceMock
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _accessMock = new Mock<ICourseAccessService>();
        _accessMock
            .Setup(x =>
                x.CanManageCourseAsync(It.IsAny<Course>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _sut = new CourseDocumentService(_db, _fileServiceMock.Object, _accessMock.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsDocuments_WhenExist()
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
        _db.CourseDocuments.AddRange(
            new CourseDocument
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                FileName = "a.pdf",
                FilePath = "documents/1/a.pdf",
                ContentType = "application/pdf",
                SizeBytes = 100,
            },
            new CourseDocument
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                FileName = "b.pdf",
                FilePath = "documents/1/b.pdf",
                ContentType = "application/pdf",
                SizeBytes = 200,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(course.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.GetAllAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Upload_SavesFile_WhenTeacherCanManage()
    {
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CyclicalCommission = "ЦК",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Курс",
            Description = "",
            TeacherId = teacher.Id,
            Status = CourseStatus.Draft,
        };
        _db.Teachers.Add(teacher);
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("док.pdf");
        file.Setup(f => f.Length).Returns(42);
        file.Setup(f => f.ContentType).Returns("application/pdf");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

        var result = await _sut.UploadAsync(
            course.Id,
            file.Object,
            teacher.UserId,
            "Teacher",
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.FileName.Should().Be("док.pdf");
        result.Data!.SizeBytes.Should().Be(42);
    }

    [Fact]
    public async Task Delete_ReturnsForbidden_WhenNotOwner()
    {
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CyclicalCommission = "ЦК",
        };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Чужой курс",
            Description = "",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Draft,
        };
        _db.Teachers.Add(teacher);
        _db.Courses.Add(course);
        var doc = new CourseDocument
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            FileName = "a.pdf",
            FilePath = "documents/1/a.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1,
        };
        _db.CourseDocuments.Add(doc);
        await _db.SaveChangesAsync();

        _accessMock
            .Setup(x =>
                x.CanManageCourseAsync(It.IsAny<Course>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(false);

        var result = await _sut.DeleteAsync(doc.Id, teacher.UserId, "Teacher", default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }
}