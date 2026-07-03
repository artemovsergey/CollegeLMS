namespace CollegeLMS.API.SwaggerExamples;

public static class CourseResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        title = "Основы программирования",
        description = "Курс по основам программирования на C#",
        teacherId = Guid.NewGuid(),
        teacherName = "Иванов Иван Иванович",
        groupId = Guid.NewGuid(),
        groupName = "ГР-21",
        status = "Draft",
        lectureCount = 0,
        assignmentCount = 0,
    };
}
