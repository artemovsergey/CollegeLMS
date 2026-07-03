namespace CollegeLMS.API.SwaggerExamples;

public static class SubmissionResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        assignmentId = Guid.NewGuid(),
        studentId = Guid.NewGuid(),
        studentName = "Иванов Иван Иванович",
        filePath = "submissions/assignment-id/student-id_12345678.bin",
        comment = "Выполнено в срок",
        score = (int?)85,
        submittedAt = DateTime.UtcNow,
    };

    public static object CreateList() => new
    {
        isSuccess = true,
        data = new[] { Create() },
        statusCode = 200,
    };
}
