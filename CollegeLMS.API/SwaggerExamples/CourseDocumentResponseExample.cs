namespace CollegeLMS.API.SwaggerExamples;

public static class CourseDocumentResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            courseId = Guid.NewGuid(),
            fileName = "учебный-план.pdf",
            contentType = "application/pdf",
            sizeBytes = 1048576,
            createdAt = DateTime.UtcNow,
        };
}
