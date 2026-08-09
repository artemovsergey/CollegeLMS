using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CollegeLMS.Tests.Unit.Services;

public class FileServiceTests : IDisposable
{
    private readonly FileService _sut = new();
    private readonly string _uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public void Dispose()
    {
        if (Directory.Exists(_uploadsRoot))
            Directory.Delete(_uploadsRoot, recursive: true);
    }

    private static IFormFile CreateFormFile(string fileName, string content = "file content")
    {
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(ms, 0, ms.Length, "file", fileName);
    }

    [Fact]
    public async Task SaveFileAsync_StripsPathTraversal_FromFileName()
    {
        var entityType = "materials";
        var entityId = Guid.NewGuid();

        var result = await _sut.SaveFileAsync(
            entityType,
            entityId,
            CreateFormFile(@"..\..\evil.txt"),
            default
        );

        result.Should().NotContain("..");
        result.Should().NotContain("\\");
        result.Should().StartWith("materials/");

        var fullPath = Path.Combine(_uploadsRoot, result);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_ReplacesInvalidPathChars()
    {
        var entityType = "materials";
        var entityId = Guid.NewGuid();

        var result = await _sut.SaveFileAsync(
            entityType,
            entityId,
            CreateFormFile("name:with*chars?.txt"),
            default
        );

        var fileName = result.Split('/').Last();
        fileName.Should().NotContainAny(Path.GetInvalidFileNameChars().Select(c => c.ToString()));
    }
}