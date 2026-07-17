using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
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

public class MaterialServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly MaterialService _sut;

    public MaterialServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _fileServiceMock = new Mock<IFileService>();
        _sut = new MaterialService(_db, _fileServiceMock.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByCourse_ReturnsMaterials_WhenExist()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Active,
            }
        );
        var materials = MaterialFixture.CreateFaker().Generate(3);
        foreach (var m in materials)
            m.CourseId = courseId;
        _db.CourseMaterials.AddRange(materials);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByCourseAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByCourse_ReturnsNotFound_WhenCourseMissing()
    {
        var result = await _sut.GetByCourseAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByCourse_ReturnsEmptyList_WhenNoMaterials()
    {
        var courseId = Guid.NewGuid();
        _db.Courses.Add(
            new Course
            {
                Id = courseId,
                Title = "Test",
                TeacherId = Guid.NewGuid(),
                Status = CourseStatus.Active,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetByCourseAsync(courseId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_CreatesRecord_WhenValid()
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
                Status = CourseStatus.Active,
            }
        );
        await _db.SaveChangesAsync();

        _fileServiceMock
            .Setup(f => f.SaveFileAsync("materials", courseId, It.IsAny<IFormFile>(), default))
            .ReturnsAsync("materials/test/file.pdf");

        var file = new FormFile(new MemoryStream("test"u8.ToArray()), 0, 4, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
        };

        var result = await _sut.UploadAsync(courseId, file, null, null, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FileName.Should().Be("test.pdf");
        result.Data.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Delete_RemovesRecord_WhenAdmin()
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

        var material = MaterialFixture.CreateFaker().Generate();

        var course = new Course
        {
            Id = material.CourseId,
            Title = "Test",
            TeacherId = Guid.NewGuid(),
            Status = CourseStatus.Active,
        };
        _db.Courses.Add(course);

        _db.CourseMaterials.Add(material);
        await _db.SaveChangesAsync();

        _fileServiceMock
            .Setup(f => f.DeleteFileAsync(material.FilePath, default))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(material.Id, adminId, "Admin", default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.CourseMaterials.AnyAsync(m => m.Id == material.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin", default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.DownloadAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
