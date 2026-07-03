namespace CollegeLMS.API.SwaggerExamples;

public static class MaterialResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        courseId = Guid.NewGuid(),
        lectureId = (Guid?)null,
        assignmentId = (Guid?)null,
        fileName = "Лекция_1.pdf",
        fileSize = 102400,
        mimeType = "application/pdf",
        createdAt = DateTime.UtcNow,
    };
}
